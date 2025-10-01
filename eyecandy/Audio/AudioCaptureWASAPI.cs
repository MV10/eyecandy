
using Microsoft.Extensions.Logging;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace eyecandy;

/// <summary>
/// Concrete implementation of AudioCaptureBase using Windows WASAPI.
/// </summary>
public class AudioCaptureWASAPI : AudioCaptureBase, IDisposable
{
    private static readonly int CaptureBufferMillisec = 23;

    private WasapiCapture CaptureDevice;

    /// <inheritdoc/>
    internal AudioCaptureWASAPI(EyeCandyCaptureConfig configuration)
    : base(configuration)
    {
        if(string.IsNullOrWhiteSpace(configuration.CaptureDeviceName))
        {
            CaptureDevice = new WindowsLoopbackWrapper(CaptureBufferMillisec);
        }
        else
        {
            using var devices = new MMDeviceEnumerator();
            var device =
                devices.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active)
                .FirstOrDefault(d => d.FriendlyName.Equals(configuration.CaptureDeviceName, StringComparison.OrdinalIgnoreCase)) 
                ?? throw new ArgumentException($"WASAPI capture device \"{configuration.CaptureDeviceName}\" not found");
            CaptureDevice = new WasapiCapture(device, false, CaptureBufferMillisec);
        }
        CaptureDevice.WaveFormat = new WaveFormat(SampleRate, 1);
        CaptureDevice.DataAvailable += ProcessSamples;

        Logger = ErrorLogging.LoggerFactory?.CreateLogger("Eyecandy." + nameof(AudioCaptureWASAPI));
        Logger?.LogTrace("Constructor completed");
    }

    private readonly ILogger Logger;

    /// <inheritdoc/>
    public override void Capture(Action newAudioDataCallback, CancellationToken cancellationToken)
    {
        Logger?.LogDebug("Capture starting");
        if (IsDisposed)
        {
            Logger?.LogError("Capture aborting, object has been disposed");
            return;
        }

        base.Capture(newAudioDataCallback, cancellationToken);

        Interlocked.Exchange(ref IsCapturing, 1);
        CaptureDevice.StartRecording();
        while (!cancellationToken.IsCancellationRequested)
        {
            // do nothing, sample-handling is event-driven
            Thread.Sleep(0); // see Capture in OpenALSoft version for the reason to use this

            DetectSilence();
        }

        Logger?.LogDebug("Capture ending");
        CaptureEnding();
        CaptureDevice.StopRecording();

        Interlocked.Exchange(ref IsCapturing, 0);
        Buffers.Timestamp = DateTime.MaxValue;
        InternalBuffers.Timestamp = DateTime.MaxValue;

        NewAudioDataCallback = null;
    }

    private void ProcessSamples(object eventSource, WaveInEventArgs bufferInfo)
    {
        // NAudio bug?
        if(bufferInfo.BytesRecorded < 2) return;

        var samples = new short[bufferInfo.BytesRecorded / 2];
        Buffer.BlockCopy(bufferInfo.Buffer, 0, samples, 0, bufferInfo.BytesRecorded);
        PopulateWaveBuffer(samples);
        base.ProcessSamples();

        InternalBuffers = Interlocked.Exchange(ref Buffers, InternalBuffers);
        NewAudioDataCallback.Invoke();
    }

    private void PopulateWaveBuffer(short[] source)
    {
        // Eyecandy defaults to a fixed buffer size of 1024 samples. This capture
        // implementation is based on NAudio's WASAPI capture, which automatically
        // calculates a buffer size based on an integer millisecond duration.
        // The eyecandy default of 1024 samples is 23.25ms of data at 44.1kHZ, but
        // a 23ms buffer is 1014 samples. Requesting 24ms of data (1058 samples) is
        // a larger error-margin, so we take the smaller dataset and apply a simple
        // interpolation loop. Note also that NAudio doesn't *always* populate the
        // largest possible buffer size, so the scaling is calculated dynamically.

        // First and last output elements are identical to source
        InternalBuffers.Wave[0] = source[0];
        InternalBuffers.Wave[InternalBuffers.Wave.Length - 1] = source[source.Length - 1];

        // The step rate through the source array to produce destination array values
        double interval = (double)source.Length / (double)InternalBuffers.Wave.Length;

        // Calculate 2nd through N-1 destination values
        for (int i = 1; i < InternalBuffers.Wave.Length - 1; i++)
        {
            // A pseudo-index value in the source array; for example, 2.375
            // represents source array element [2] added to 0.375 times the
            // difference between source array element [2] and element [3].
            double pseudoindex = interval * (double)i;

            // The starting array element for the interpolation (actual index)
            int baseindex = (int)double.Floor(pseudoindex);
            
            // Multiplier for the difference between the source array values
            double frac = pseudoindex % 1;

            if (frac == 0 || baseindex + 1 == source.Length)
            {
                // Exactly aligned with a source array element
                InternalBuffers.Wave[i] = source[baseindex];
            }
            else
            {
                // Calculate how much the two source elments differ
                double diff = source[baseindex + 1] - source[baseindex];
                double interpolated = diff * frac;

                // Add that variance to the starting element value
                InternalBuffers.Wave[i] = (short)((double)source[baseindex] + interpolated);
            }
        }
    }

    /// <inheritdoc/>
    public override void Dispose()
    {
        base.Dispose();

        if (IsDisposed) return;
        Logger?.LogTrace("Dispose() ----------------------------");

        if (IsCapturing == 1)
        {
            Logger?.LogError("Dispose invoked before audio processing was terminated");
        }

        if(CaptureDevice is not null)
        {
            CaptureDevice.StopRecording();
            CaptureDevice.Dispose();
            CaptureDevice = null;
        }

        NewAudioDataCallback = null;

        base.Dispose();

        IsDisposed = true;
        GC.SuppressFinalize(this);
    }
    private bool IsDisposed = false;
}
