
using FftSharp.Windows;
using FftSharp;
using Microsoft.Extensions.Logging;

namespace eyecandy;

/// <summary>
/// Provides basic callbacks and related structuring for further processing of
/// captured PCM data. Can perform optional post-processing of the raw PCM samples.
/// </summary>
public abstract class AudioCaptureBase : IDisposable
{
    /// <summary>
    /// Returns a concrete instance based on the configured LoopbackApi setting.
    /// </summary>
    public static AudioCaptureBase Factory(EyeCandyCaptureConfig configuration)
    {
        switch(configuration.LoopbackApi)
        {
            case LoopbackApi.OpenALSoft:
                return new AudioCaptureOpenALSoft(configuration);

            case LoopbackApi.Metronome:
                return new AudioCaptureMetronome(configuration);

            default:
                return new AudioCaptureWASAPI(configuration);
        }
    }

    /// <summary>
    /// Returns a concrete instance of the AudioCaptureBase implementation that generates
    /// fake data which is primarily used when silence is detected.
    /// </summary>
    public static AudioCaptureBase SyntheticDataFactory(EyeCandyCaptureConfig configuration)
        => new AudioCaptureMetronome(configuration);

    /// <summary>
    /// Fixed stereo sample rate, 44.1kHz is equivalent to CD audio.
    /// </summary>
    public static readonly int SampleRate = 44100;

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

    /// <summary>
    /// Capture is active when the value is 1. (Capture is normally on a background thread,
    /// and Booleans aren't supported by Interlocked.Exchange thread-safe updates.)
    /// </summary>
    public int IsCapturing = 0;

    // used with Interlock.Exchange to expose a thread-safe copy in the public Buffers field
    private protected AudioData InternalBuffers;

    // internal
    private int RmsBufferLength;

    // private copy because we frequently read it inside another thread in the Capture method
    private protected int SampleSize;

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
    protected AudioCaptureBase(EyeCandyCaptureConfig configuration)
    {
        Configuration = configuration;

        RmsBufferLength = (int)((double)Configuration.RMSVolumeMilliseconds / 1000.0 * SampleRate);

        Buffers = new();
        InternalBuffers = new();

        SampleSize = Configuration.SampleSize;
        BufferFFTSource = new double[SampleSize * 2];
        BufferWebAudioSmoothing = new double[SampleSize];
        BufferRMSVolume = new int[RmsBufferLength];

        if(Configuration.ReplaceSilence && this is not AudioCaptureMetronome)
        {
            // TODO create a metronome object but don't start it until silence is detected
        }

        ErrorLogging.Logger?.LogTrace($"AudioCaptureProcessor: constructor completed");
    }

    /// <summary>
    /// Enters the audio capture / processing loop. Typically this will be invoked with Task.Run.
    /// When the CancellationToken is canceled to end processing, the caller should await the
    /// Task.Run to ensure shutdown is completed.
    /// </summary>
    public abstract void Capture(Action newAudioDataCallback, CancellationToken cancellationToken);

    // Before calling, implementations should override and fill PCM into InternalBuffers.Wave[]
    private protected virtual void ProcessSamples()
    {
        // Tell the world about our hot fresh new data
        InternalBuffers.Timestamp = DateTime.Now;

        // Volume is RMS of previous 300ms of PCM data
        if (Requirements.CalculateVolumeRMS) ProcessVolume();

        // TODO if synthetic data should replace silence, shortcut to that Capture instead of processing FFT data
        if(IsSilent && Configuration.ReplaceSilence)
        {
            return;
        }

        // Frequency is a windowed FFT of 2X PCM sample sets; decibels and WebAudio are
        // derived from the FFT magnitude calculations
        if (Requirements.CalculateFFTMagnitude) ProcessFrequency();
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

    // Protected rather than private because AudioCaptureSyntheticData.ProcessSamples has to call it
    protected void ProcessVolume()
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

    /// <inheritdoc/>
    public abstract void Dispose();
}
