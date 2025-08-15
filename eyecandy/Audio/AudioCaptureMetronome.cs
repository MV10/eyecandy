
using Microsoft.Extensions.Logging;
using NAudio.Wave;

namespace eyecandy;

/// <summary>
/// This capture processor generates alternate data which can be requested as output
/// when silence is detected. It could also be used as primary output for testing
/// or development purposes (the easiest way is to simply not play audio, but it
/// should also work to wire it up directly as the primary capture source).
/// </summary>
public class AudioCaptureMetronome : AudioCaptureBase, ISampleProvider, IDisposable
{
    /// <inheritdoc/>
    public WaveFormat WaveFormat { get => NAudioWaveFormat; }

    private readonly int BufferSize;
    private readonly int SamplesPerBeat;
    private readonly int BeatSampleLength;
    private readonly WaveFormat NAudioWaveFormat;
    private readonly double Amplitude;
    private readonly double Frequency;
    private int SampleIndex;

    private WaveOutEvent WaveOut;
    private Action NewAudioDataCallback;

    /// <inheritdoc/>
    public AudioCaptureMetronome(EyeCandyCaptureConfig configuration)
    : base(configuration)
    {
        NAudioWaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(SampleRate, 1); // 1 = Mono

        BufferSize = configuration.SampleSize;
        SamplesPerBeat = (int)(60.0 / configuration.MetronomeBPM * SampleRate);
        BeatSampleLength = (int)(configuration.MetronomeBeatDuration * SampleRate);
        Amplitude = configuration.MetronomeAmplitude;
        Frequency = configuration.MetronomeBeatFrequency;

        SampleIndex = 0;

        WaveOut = new();
        WaveOut.Volume = 0; // we want the data, but we don't want to hear it...
        WaveOut.Init(this);
    }

    /// <inheritdoc/>
    public override void Capture(Action newAudioDataCallback, CancellationToken cancellationToken)
    {
        ErrorLogging.Logger?.LogDebug($"{nameof(AudioCaptureMetronome)}: Capture starting");
        if (IsDisposed)
        {
            ErrorLogging.EyecandyError($"{nameof(AudioCaptureMetronome)}.{nameof(Capture)}", "Aborting, object has been disposed", LogLevel.Error);
            return;
        }

        NewAudioDataCallback = newAudioDataCallback;

        Interlocked.Exchange(ref IsCapturing, 1);
        WaveOut.Play();

        while (!cancellationToken.IsCancellationRequested)
        {
            // do nothing, sample-handling is event-driven
            Thread.Sleep(0); // see Capture in OpenALSoft version for the reason to use this
        }

        ErrorLogging.Logger?.LogDebug($"{nameof(AudioCaptureMetronome)}: Capture ending");

        Interlocked.Exchange(ref IsCapturing, 0);
        WaveOut.Stop();
        Buffers.Timestamp = DateTime.MaxValue;
        InternalBuffers.Timestamp = DateTime.MaxValue;

        NewAudioDataCallback = null;
    }

    /// <inheritdoc/>
    public int Read(float[] buffer, int offset, int count)
    {
        for (int i = 0; i < count; i++)
        {
            int currentBeatSample = SampleIndex % SamplesPerBeat;
            bool isBeatActive = currentBeatSample < BeatSampleLength;

            float sample = isBeatActive
                ? (float)(Amplitude * Math.Sin(2 * Math.PI * Frequency * SampleIndex / SampleRate))
                : 0.0f;

            buffer[offset + i] = sample;

            if (i % BufferSize == 0 && i > 0)
            {
                for (int j = 0; j < BufferSize; j++)
                {
                    InternalBuffers.Wave[j] = (short)(buffer[offset + i - BufferSize + j] * short.MaxValue);
                }
                base.ProcessSamples();
                InternalBuffers = Interlocked.Exchange(ref Buffers, InternalBuffers);
                NewAudioDataCallback.Invoke();
            }

            SampleIndex++;
            if (SampleIndex >= SamplesPerBeat)
                SampleIndex = 0;
        }

        // handle final partial buffer
        if (count >= BufferSize)
        {
            for (int j = 0; j < BufferSize; j++)
            {
                InternalBuffers.Wave[j] = (short)(buffer[offset + count - BufferSize + j] * short.MaxValue);
            }
            base.ProcessSamples();
            InternalBuffers = Interlocked.Exchange(ref Buffers, InternalBuffers);
            NewAudioDataCallback.Invoke();
        }

        return count;
    }

    /// <inheritdoc/>
    public override void Dispose()
    {
        if (IsDisposed) return;
        ErrorLogging.Logger?.LogTrace($"{GetType()}.Dispose() ----------------------------");

        WaveOut?.Stop();
        WaveOut?.Dispose();

        IsDisposed = true;
        GC.SuppressFinalize(this);
    }
    private bool IsDisposed = false;
}
