
using Microsoft.Extensions.Logging;

namespace eyecandy;

/// <summary>
/// This capture processor generates alternate data which can be requested as output
/// when silence is detected. It could also be used as primary output for testing
/// or development purposes (the easiest way is to simply not play audio, but it
/// should also work to wire it up directly as the primary capture source).
/// </summary>
public class AudioCaptureSyntheticData : AudioCaptureBase, IDisposable
{
    private readonly int BufferSize;
    private readonly double BufferDurationMs;
    private readonly double BeatDurationSeconds;
    private readonly long SamplesPerBeat;
    private readonly long SamplesPerSpike;
    private readonly double AngularFrequency;
    private readonly double MaxAmplitude;
    private readonly double MinAmplitude;

    private long SampleIndex;

    /// <inheritdoc/>
    internal AudioCaptureSyntheticData(EyeCandyCaptureConfig configuration)
    : base(configuration)
    {
        var sampleRate = (double)SampleRate;
        BufferSize = Configuration.SampleSize;
        BufferDurationMs = (BufferSize / sampleRate) * 1000.0;
        BeatDurationSeconds = 60.0 / Configuration.SyntheticDataBPM;
        SamplesPerBeat = (long)(BeatDurationSeconds * sampleRate);
        SamplesPerSpike = (long)(Configuration.SyntheticDataBeatDuration * sampleRate);
        AngularFrequency = 2.0 * Math.PI * Configuration.SyntheticDataBeatFrequency / sampleRate;
        MaxAmplitude = short.MaxValue * Configuration.SyntheticDataAmplitude;
        MinAmplitude = MaxAmplitude * Configuration.SyntheticDataMinimumLevel;

        ErrorLogging.Logger?.LogTrace($"{nameof(AudioCaptureSyntheticData)}: constructor completed");
    }

    /// <inheritdoc/>
    public override void Capture(Action newAudioDataCallback, CancellationToken cancellationToken)
    {
        ErrorLogging.Logger?.LogDebug($"{nameof(AudioCaptureSyntheticData)}: Capture starting");
        if (IsDisposed)
        {
            ErrorLogging.EyecandyError($"{nameof(AudioCaptureSyntheticData)}.{nameof(Capture)}", "Aborting, object has been disposed", LogLevel.Error);
            return;
        }

        base.Capture(newAudioDataCallback, cancellationToken);

        Interlocked.Exchange(ref IsCapturing, 1);

        SampleIndex = 0;

        while (!cancellationToken.IsCancellationRequested)
        {
            var startTime = DateTime.UtcNow;

            switch (Configuration.SyntheticAlgorithm)
            {
                case SyntheticDataAlgorithm.MetronomeBeat:
                default:
                    GenerateMetronomeData();
                    break;
            }

            SampleIndex += BufferSize;
            base.ProcessSamples();
            InternalBuffers = Interlocked.Exchange(ref Buffers, InternalBuffers);
            DetectSilence();
            NewAudioDataCallback.Invoke();

            while (!cancellationToken.IsCancellationRequested)
            {
                var ms = (DateTime.UtcNow - startTime).TotalMilliseconds;
                if (ms > BufferDurationMs) break;
                Thread.Sleep(0); // see Capture in OpenALSoft version for the reason to use this
            }
        }

        ErrorLogging.Logger?.LogDebug($"{nameof(AudioCaptureSyntheticData)}: Capture ending");
        CaptureEnding();

        Interlocked.Exchange(ref IsCapturing, 0);
        Buffers.Timestamp = DateTime.MaxValue;
        InternalBuffers.Timestamp = DateTime.MaxValue;

        NewAudioDataCallback = null;
    }

    private void GenerateMetronomeData()
    {
        for (int i = 0; i < BufferSize; i++)
        {
            long currentSample = SampleIndex + i;
            double sampleValue = Math.Sin(AngularFrequency * currentSample);

            // determine if the current sample is within the spike duration of a beat
            bool isSpike = (currentSample % SamplesPerBeat) < SamplesPerSpike;
            double currentAmplitude = isSpike ? MaxAmplitude : MinAmplitude;

            // scale the sample and convert to short
            InternalBuffers.Wave[i] = (short)(sampleValue * currentAmplitude);
        }
    }

    /// <inheritdoc/>
    public override void Dispose()
    {
        base.Dispose();

        if (IsDisposed) return;
        ErrorLogging.Logger?.LogTrace($"{GetType()}.Dispose() ----------------------------");

        // nothing to dispose but it will prevent calling Capture
        // again given the base class may dispose resources

        IsDisposed = true;
        GC.SuppressFinalize(this);
    }
    private bool IsDisposed = false;
}
