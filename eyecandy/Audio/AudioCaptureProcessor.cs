using FftSharp.Windows;
using FftSharp;
using OpenTK.Audio.OpenAL;
using eyecandy.Audio;

namespace eyecandy
{
    public class AudioCaptureProcessor : IDisposable
    {
        // fixed configuration
        public static readonly int SampleRate = 44100;
        public static readonly ALFormat SampleFormat = ALFormat.Mono16;

        /// <summary>
        /// The active configuration. This should never be changed during execution.
        /// </summary>
        public static EyeCandyCaptureConfig Configuration { get; private set; }

        /// <summary>
        /// A thread-safe publicly accessible copy of the various audio buffers.
        /// </summary>
        public AudioData Buffers;

        /// <summary>
        /// Controls which post-processing calculations are performed (volume etc.). This can
        /// be replaced with an updated structure during execution. All values are false by
        /// default (only raw Wave data would be available in the Buffers object).
        /// </summary>
        public AudioProcessingRequirements Requirements = new();

        // used with Interlock.Exchange to expose a thread-safe copy in the public Buffers field
        private AudioData InternalBuffers;

        // internal
        private static readonly int RmsTimeMs = 300;
        private static readonly int RmsBufferLength = (int)((double)RmsTimeMs / 1000.0 * SampleRate);

        private ALDevice Device;
        private ALContext Context;
        private ALCaptureDevice CaptureDevice;
        private bool IsCapturing = false;

        // private copy because we frequently read it inside another thread in the Capture method
        private int SampleSize;

        private double[] BufferFFTSource;
        private int[] BufferRMSVolume;
        private int RmsPointer = 0;
        private int RmsSum = 0;

        public AudioCaptureProcessor(EyeCandyCaptureConfig configuration)
        {
            Configuration = configuration;

            Buffers = new();
            InternalBuffers = new();

            SampleSize = Configuration.SampleSize;
            BufferFFTSource = new double[SampleSize * 2];
            BufferRMSVolume = new int[RmsBufferLength];
        }

        public void Capture(Action newAudioDataCallback, CancellationToken cancellationToken)
        {
            Connect();

            IsCapturing = true;
            ALC.CaptureStart(CaptureDevice);
            ErrorLogging.OpenALErrorCheck($"{nameof(AudioCaptureProcessor)}.{nameof(Capture)}");

            while (!cancellationToken.IsCancellationRequested)
            {
                int samplesAvailable = ALC.GetAvailableSamples(CaptureDevice);
                while (samplesAvailable >= SampleSize)
                {
                    ProcessSamples();
                    InternalBuffers = Interlocked.Exchange(ref Buffers, InternalBuffers);
                    newAudioDataCallback.Invoke();
                    samplesAvailable -= SampleSize;
                }
                Thread.Yield();
            }

            ALC.CaptureStop(CaptureDevice);
            ErrorLogging.OpenALErrorCheck($"{nameof(AudioCaptureProcessor)}.{nameof(Capture)}");

            IsCapturing = false;
            Buffers.Timestamp = DateTime.MaxValue;
            InternalBuffers.Timestamp = DateTime.MaxValue;
        }

        public void Dispose()
        {
            if (IsCapturing) throw new InvalidOperationException("Dispose invoked before audio processing was terminated.");

            ALC.CaptureCloseDevice(CaptureDevice);
            ALC.MakeContextCurrent(ALContext.Null);
            ALC.DestroyContext(Context);
            ALC.CloseDevice(Device);
        }

        private void Connect()
        {
            var targetDriver = string.IsNullOrEmpty(AudioCaptureProcessor.Configuration.DriverName) 
                ? "OpenAL Soft" 
                : AudioCaptureProcessor.Configuration.DriverName;

            var captureDeviceName = string.IsNullOrEmpty(AudioCaptureProcessor.Configuration.CaptureDeviceName)
                ? ALC.GetString(Device, AlcGetString.CaptureDefaultDeviceSpecifier)
                : AudioCaptureProcessor.Configuration.CaptureDeviceName;

            var devices = ALC.GetStringList(GetEnumerationStringList.DeviceSpecifier);
            var driverDeviceName = ALC.GetString(ALDevice.Null, AlcGetString.DefaultDeviceSpecifier);
            foreach (var d in devices)
            {
                if (d.Contains(targetDriver))
                {
                    driverDeviceName = d;
                    break;
                }
            }

            Device = ALC.OpenDevice(driverDeviceName);
            Context = ALC.CreateContext(Device, (int[])null);
            ALC.MakeContextCurrent(Context);
            CaptureDevice = ALC.CaptureOpenDevice(captureDeviceName, SampleRate, SampleFormat, SampleSize);

            ErrorLogging.OpenALErrorCheck($"{nameof(AudioCaptureProcessor)} ctor");
        }

        private void ProcessSamples()
        {
            // PCM data is just read straight off the capture device
            ALC.CaptureSamples(CaptureDevice, ref InternalBuffers.Wave[0], SampleSize);
            ErrorLogging.OpenALErrorCheck($"{nameof(AudioCaptureProcessor)}.{nameof(ProcessSamples)}");

            // Frequency magnitude is a windowed FFT of 2X PCM sample sets
            if (Requirements.CalculateFrequency) ProcessFrequency();

            // Volume is RMS of previous 300ms of PCM data
            if (Requirements.CalculateVolumeRMS) ProcessVolume();

            // Tell the world about our hot fresh new data
            InternalBuffers.Timestamp = DateTime.Now;
        }

        private void ProcessFrequency()
        {
            // FFT buffer is 2X larger, "slide" two sets of PCM data through it.
            // Copy the second half of the FFT buffer "back" to the first half:
            Array.Copy(BufferFFTSource, SampleSize, BufferFFTSource, 0, SampleSize);
            // Next, copy new PCM data to the second half of the FFT buffer:
            Array.Copy(InternalBuffers.Wave, 0, BufferFFTSource, SampleSize, SampleSize);

            var window = new Hanning();
            double[] windowed = window.Apply(BufferFFTSource);
            double[] zeroPadded = Pad.ZeroPad(windowed);

            // FftSharp.Complex is deprecated
            System.Numerics.Complex[] spectrum = FFT.Forward(zeroPadded);

            if (Requirements.CalculateFFTMagnitude) 
                InternalBuffers.FrequencyMagnitude = FFT.Magnitude(spectrum);

            if(Requirements.CalculateFFTDecibels) 
                InternalBuffers.FrequencyDecibels = FFT.Power(spectrum);
        }

        private void ProcessVolume()
        {
            // Currently only RMS volume is supported.
            for (int i = 0; i < SampleSize; i++)
            {
                RmsSum -= BufferRMSVolume[RmsPointer];
                int sample = Math.Abs(InternalBuffers.Wave[i] ^ 2);
                RmsSum += sample;
                BufferRMSVolume[RmsPointer] = sample;
                RmsPointer++;
                if (RmsPointer == RmsBufferLength) RmsPointer = 0;
            }

            InternalBuffers.RealtimeRMSVolume = Math.Sqrt((double)RmsSum / (double)RmsBufferLength);
        }
    }
}
