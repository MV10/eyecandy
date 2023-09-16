
using FftSharp.Windows;
using FftSharp;
using Microsoft.Extensions.Logging;
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

        private ALCaptureDevice CaptureDevice;
        private bool IsCapturing = false;

        // private copy because we frequently read it inside another thread in the Capture method
        private int SampleSize;

        private double[] BufferFFTSource;
        private double[] BufferWebAudioSmoothing;
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
            BufferWebAudioSmoothing = new double[SampleSize];
            BufferRMSVolume = new int[RmsBufferLength];

            ErrorLogging.Logger?.LogTrace($"AudioCaptureProcessor: constructor completed");
        }

        /// <summary>
        /// Enters the audio capture / processing loop. Typically this will be invoked with Task.Run.
        /// When the CancellationToken is canceled to end processing, the caller should await the
        /// Task.Run to ensure shutdown is completed.
        /// </summary>
        public void Capture(Action newAudioDataCallback, CancellationToken cancellationToken)
        {
            ErrorLogging.Logger?.LogDebug($"AudioCaptureProcessor: Capture starting");
            if (IsDisposed)
            {
                ErrorLogging.LibraryError($"{nameof(AudioCaptureProcessor)}.{nameof(Capture)}", "Aborting, object has been disposed", LogLevel.Error);
                return;
            }

            Connect();

            IsCapturing = true;
            ALC.CaptureStart(CaptureDevice);
            ErrorLogging.OpenALErrorCheck($"{nameof(AudioCaptureProcessor)}.{nameof(ALC.CaptureStart)}");

            while (!cancellationToken.IsCancellationRequested)
            {
                int samplesAvailable = ALC.GetInteger(CaptureDevice, AlcGetInteger.CaptureSamples);
                while (samplesAvailable >= SampleSize)
                {
                    ProcessSamples();
                    InternalBuffers = Interlocked.Exchange(ref Buffers, InternalBuffers);
                    newAudioDataCallback.Invoke();
                    samplesAvailable -= SampleSize;
                }
                // Relative FPS results using different methods with "demo freq" (worst-performer).
                // FPS for Win10x64 debug build in IDE with a Ryzen 9 3900XT and GeForce RTX 2060.
                Thread.Sleep(0);        // 4750     cede control to any thread of equal priority
                // spinWait.SpinOnce(); // 4100     periodically yields (default is 10 iterations)
                // Thread.Sleep(1);     // 3900     cede control to any thread of OS choice
                // Thread.Yield();      // 3650     cede control to any thread on the same core
                // await Task.Delay(0); // 3600     creates and waits on a system timer
                // do nothing           // 3600     burn down a CPU core
                // Thread.SpinWait(1);  // 3600     duration-limited Yield
                // await Task.Yield();  // 3250     suspend task indefinitely (scheduler control)

            }

            ErrorLogging.Logger?.LogDebug($"AudioCaptureProcessor: Capture ending");

            ALC.CaptureStop(CaptureDevice);
            ErrorLogging.OpenALErrorCheck($"{nameof(AudioCaptureProcessor)}.{nameof(ALC.CaptureStop)}");

            IsCapturing = false;
            Buffers.Timestamp = DateTime.MaxValue;
            InternalBuffers.Timestamp = DateTime.MaxValue;
        }

        private void Connect()
        {
            ErrorLogging.Logger?.LogTrace($"AudioCaptureProcessor: Connect");

            var captureDeviceName = string.IsNullOrEmpty(AudioCaptureProcessor.Configuration.CaptureDeviceName)
                ? ALC.GetString(ALDevice.Null, AlcGetString.CaptureDefaultDeviceSpecifier)
                : AudioCaptureProcessor.Configuration.CaptureDeviceName;

            CaptureDevice = ALC.CaptureOpenDevice(captureDeviceName, SampleRate, SampleFormat, SampleSize);

            // NOTE: If we end up supporting surround capture and the driver can't handle it, the
            // OpenAL Soft error looks like this. (Creative's OpenAL does not return an error and the
            // CaptureSamples call will crash with an Access Violation exception.)
            // [ALSOFT] (EE) Failed to match format, wanted: 5.1 Surround Int16 44100hz, got: 0x00000003 mask 2 channels 32-bit 44100hz
            // https://github.com/kcat/openal-soft/issues/893

            ErrorLogging.OpenALErrorCheck($"{nameof(AudioCaptureProcessor)}.{nameof(Connect)}");
        }

        private void ProcessSamples()
        {
            // PCM data is just read straight off the capture device
            ALC.CaptureSamples(CaptureDevice, ref InternalBuffers.Wave[0], SampleSize);
            ErrorLogging.OpenALErrorCheck($"{nameof(AudioCaptureProcessor)}.{nameof(ProcessSamples)}");

            // Frequency is a windowed FFT of 2X PCM sample sets; decibels and WebAudio are
            // derived from the FFT magnitude calculations
            if (Requirements.CalculateFFTMagnitude) ProcessFrequency();

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

            // Although the FftSharp site notes the Hanning window is probably the
            // most commonly-used, apparently the W3C WebAudio API specification
            // recommends the use of the Blackman window, so v1.0.7 makes this change.
            //var window = new Hanning();
            var window = new Blackman();

            double[] windowed = window.Apply(BufferFFTSource);
            double[] zeroPadded = Pad.ZeroPad(windowed);

            // FftSharp.Complex is deprecated
            System.Numerics.Complex[] spectrum = FFT.Forward(zeroPadded);

            if (Requirements.CalculateFFTMagnitude) 
                InternalBuffers.FrequencyMagnitude = FFT.Magnitude(spectrum);

            if (Requirements.CalculateFFTDecibels) CalculateDecibels();

            if (Requirements.CalculateFFTWebAudioDecibels) CalculateWebAudio();
        }

        // Although FftSharp has a decibels method (FFT.Power), it begins by calling Magnitude,
        // which we already calculate and store. It is more efficient to skip that step here.
        private void CalculateDecibels()
        {
            InternalBuffers.FrequencyDecibels = new double[SampleSize];
            for (int i = 0; i < SampleSize; i++)
                InternalBuffers.FrequencyDecibels[i] = 20 * Math.Log10(InternalBuffers.FrequencyMagnitude[i]);
        }

        // WebAudio is a bizarre pseudo-decibel calculation. It involves a smoothing function that
        // mixes 20% of the previous frequency pass with 80% of the current pass. There is also a
        // -30dB to -100dB clamping range applied, according to the interpretation at the link below,
        // which doesn't seem to accurately reflect what is seen at Shadertoy. The conversion to byte
        // data is irrelevant as our FFT is much more accurate and the end result is still normalized.
        //
        // Interpretation:
        // https://gist.github.com/soulthreads/2efe50da4be1fb5f7ab60ff14ca434b8
        //
        // Compare the library's "frag" demo (the same shader) to this one:
        // https://www.shadertoy.com/view/mdScDh
        //
        private void CalculateWebAudio()
        {
            double k = Configuration.WebAudioSmoothingFactor;

            for (int i = 0; i < SampleSize; i++)
            {
                // value from the previous WebAudio calcs
                double v_prev = BufferWebAudioSmoothing[i];

                // dB is derived from magnitude
                double sample = InternalBuffers.FrequencyMagnitude[i];

                // time-domain smoothing (why???)
                sample = k * v_prev + (1d - k) * sample;

                // store for the next batch of samples
                BufferWebAudioSmoothing[i] = sample;

                // apply the normal Decibels calculation
                sample = 20d * Math.Log10(sample);

                // clip to the -30dB to -100dB range - makes no sense, severe clipping
                //sample = Math.Clamp(sample - 30d, 0d, 70d);

                // map to a 0-255 range - unnecessary, it gets normalized regardless
                //sample = (sample / 70d) * 255d;

                // store for output
                InternalBuffers.FrequencyWebAudio[i] = sample;
            }
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

        /// <summary/>
        public void Dispose()
        {
            if (IsDisposed) return;
            ErrorLogging.Logger?.LogTrace($"{GetType()}.Dispose() ----------------------------");

            if (IsCapturing)
            {
                ErrorLogging.LibraryError($"{nameof(AudioCaptureProcessor)}.Dispose", "Dispose invoked before audio processing was terminated.");
            }

            ErrorLogging.Logger?.LogTrace($"  {GetType()}.Dispose() ALC.CaptureCloseDevice");
            ALC.CaptureCloseDevice(CaptureDevice);

            // This is fine on Windows but crashes Linux Pulse Audio.
            // ErrorLogging.OpenALErrorCheck($"{nameof(AudioCaptureProcessor)}.Dispose");

            IsDisposed = true;
            GC.SuppressFinalize(this);
        }
        private bool IsDisposed = false;
    }
}
