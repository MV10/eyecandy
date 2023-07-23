
using FftSharp.Windows;
using FftSharp;
using OpenTK.Audio.OpenAL;

namespace eyecandy
{
    /// <summary>
    /// Uses OpenAL to capture audio and invoke callbacks for further processing of
    /// the capture data. Can perform optional post-processing of the raw PCM samples.
    /// </summary>
    public class AudioCaptureProcessor : IDisposable
    {
        /// <summary>
        /// Fixed stereo sample rate, 44.1kHz is equivalent to CD audio.
        /// </summary>
        public static readonly int SampleRate = 44100;

        /// <summary>
        /// Fixed sampling format is 16 bit.
        /// </summary>
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
        private int RmsBufferLength;

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

        private bool IsSilent = false;
        private DateTime SilenceStarted = DateTime.MaxValue;

        /// <summary>
        /// The constructor requries a configuration object. This object is stored and is accessible
        /// but should not be altered during program execution. Some settings are cached elsewhere
        /// for performance and/or thread-safety considerations and would not be updated.
        /// </summary>
        public AudioCaptureProcessor(EyeCandyCaptureConfig configuration)
        {
            Configuration = configuration;

            RmsBufferLength = (int)((double)Configuration.RMSVolumeMilliseconds / 1000.0 * SampleRate);

            Buffers = new();
            InternalBuffers = new();

            SampleSize = Configuration.SampleSize;
            BufferFFTSource = new double[SampleSize * 2];
            BufferRMSVolume = new int[RmsBufferLength];
        }

        /// <summary>
        /// Enters the audio capture / processing loop. Typically this will be invoked with Task.Run.
        /// When the CancellationToken is canceled to end processing, the caller should await the
        /// Task.Run to ensure shutdown is completed.
        /// </summary>
        public void Capture(Action newAudioDataCallback, CancellationToken cancellationToken)
        {
            Connect();

            IsCapturing = true;
            ALC.CaptureStart(CaptureDevice);
            ErrorLogging.OpenALErrorCheck($"{nameof(AudioCaptureProcessor)}.{nameof(ALC.CaptureStart)}");

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
                // Relative FPS results using different methods with "demo freq" (worst-performer)
                // tested on Win10x64 debug build in IDE (Ryzen 9 3900XT / GeForce RTX 2060); very
                // little difference on the Raspberry Pi4B, however: ~50 FPS improves to ~60 FPS.
                Thread.Sleep(0);        // 4750     cede control to any thread of equal priority
                // spinWait.SpinOnce(); // 4100     periodically yields (default is 10 iterations)
                // Thread.Sleep(1);     // 3900     cede control to any thread of OS choice
                // Thread.Yield();      // 3650     cede control to any thread on the same core
                // await Task.Delay(0); // 3600     creates and waits on a system timer
                // do nothing           // 3600     burn down a CPU core
                // Thread.SpinWait(1);  // 3600     duration-limited Yield
                // await Task.Yield();  // 3250     suspend task indefinitely (scheduler control)

            }

            ALC.CaptureStop(CaptureDevice);
            ErrorLogging.OpenALErrorCheck($"{nameof(AudioCaptureProcessor)}.{nameof(ALC.CaptureStop)}");

            IsCapturing = false;
            Buffers.Timestamp = DateTime.MaxValue;
            InternalBuffers.Timestamp = DateTime.MaxValue;
        }

        public void Dispose()
        {
            if (IsCapturing)
            {
                ErrorLogging.LibraryError($"{nameof(AudioCaptureProcessor)}.Dispose", "Dispose invoked before audio processing was terminated.");
            }

            ALC.CaptureCloseDevice(CaptureDevice);
            ALC.MakeContextCurrent(ALContext.Null);
            ALC.DestroyContext(Context);
            ALC.CloseDevice(Device);

            // This is fine on Windows but crashes the Raspberry Pi...
            // ErrorLogging.OpenALErrorCheck($"{nameof(AudioCaptureProcessor)}.Dispose");
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

            ErrorLogging.OpenALErrorCheck($"{nameof(AudioCaptureProcessor)}.{nameof(Connect)}");
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

            var volumeRMS = Math.Sqrt((double)RmsSum / (double)RmsBufferLength);
            InternalBuffers.RealtimeRMSVolume = volumeRMS;

            // Silence detection
            if(Configuration.DetectSilence)
            {
                if (IsSilent)
                {
                    if (volumeRMS > Configuration.MaximumSilenceRMS)
                    {
                        IsSilent = false;
                        SilenceStarted = DateTime.MaxValue;
                    }
                }
                else
                {
                    if (volumeRMS <= Configuration.MaximumSilenceRMS)
                    {
                        IsSilent = true;
                        SilenceStarted = DateTime.Now;
                    }
                }
                InternalBuffers.SilenceStarted = SilenceStarted;
            }
        }
    }
}
