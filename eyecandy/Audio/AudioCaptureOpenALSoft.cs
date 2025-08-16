
using Microsoft.Extensions.Logging;
using OpenTK.Audio.OpenAL;

namespace eyecandy;

/// <summary>
/// Concrete implementation of AudioCaptureProcesor using OpenAL-Soft.
/// </summary>
public class AudioCaptureOpenALSoft : AudioCaptureBase, IDisposable
{
    /// <summary>
    /// Fixed sampling format is 16 bit.
    /// </summary>
    public static readonly ALFormat SampleFormat = ALFormat.Mono16;

    private ALCaptureDevice CaptureDevice;

    /// <inheritdoc/>
    public AudioCaptureOpenALSoft(EyeCandyCaptureConfig configuration)
    : base(configuration)
    { }

    /// <inheritdoc/>
    public override void Capture(Action newAudioDataCallback, CancellationToken cancellationToken)
    {
        ErrorLogging.Logger?.LogDebug($"{nameof(AudioCaptureOpenALSoft)}: Capture starting");
        if (IsDisposed)
        {
            ErrorLogging.EyecandyError($"{nameof(AudioCaptureOpenALSoft)}.{nameof(Capture)}", "Aborting, object has been disposed", LogLevel.Error);
            return;
        }

        base.Capture(newAudioDataCallback, cancellationToken);

        Connect();

        Interlocked.Exchange(ref IsCapturing, 1);
        ALC.CaptureStart(CaptureDevice);
        ErrorLogging.OpenALErrorCheck($"{nameof(AudioCaptureOpenALSoft)}.{nameof(ALC.CaptureStart)}");

        while (!cancellationToken.IsCancellationRequested)
        {
            int samplesAvailable = ALC.GetInteger(CaptureDevice, AlcGetInteger.CaptureSamples);
            while (samplesAvailable >= SampleSize)
            {
                ProcessSamples();
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

        ErrorLogging.Logger?.LogDebug($"{nameof(AudioCaptureOpenALSoft)}: Capture ending");

        ALC.CaptureStop(CaptureDevice);
        ErrorLogging.OpenALErrorCheck($"{nameof(AudioCaptureOpenALSoft)}.{nameof(ALC.CaptureStop)}");

        Interlocked.Exchange(ref IsCapturing, 0);
        Buffers.Timestamp = DateTime.MaxValue;
        InternalBuffers.Timestamp = DateTime.MaxValue;
    }

    private void Connect()
    {
        ErrorLogging.Logger?.LogTrace($"{nameof(AudioCaptureOpenALSoft)}: Connect");

        var captureDeviceName = string.IsNullOrEmpty(Configuration.CaptureDeviceName)
            ? ALC.GetString(ALDevice.Null, AlcGetString.CaptureDefaultDeviceSpecifier)
            : Configuration.CaptureDeviceName;

        CaptureDevice = ALC.CaptureOpenDevice(captureDeviceName, SampleRate, SampleFormat, SampleSize);

        // NOTE: If we end up supporting surround capture and the driver can't handle it, the
        // OpenAL Soft error looks like this. (Creative's OpenAL does not return an error and the
        // CaptureSamples call will crash with an Access Violation exception.)
        // [ALSOFT] (EE) Failed to match format, wanted: 5.1 Surround Int16 44100hz, got: 0x00000003 mask 2 channels 32-bit 44100hz
        // https://github.com/kcat/openal-soft/issues/893

        ErrorLogging.OpenALErrorCheck($"{nameof(AudioCaptureOpenALSoft)}.{nameof(Connect)}");
    }

    private protected override void ProcessSamples()
    {
        // PCM data is just read straight off the capture device
        ALC.CaptureSamples(CaptureDevice, ref InternalBuffers.Wave[0], SampleSize);
        ErrorLogging.OpenALErrorCheck($"{nameof(AudioCaptureOpenALSoft)}.{nameof(ProcessSamples)}");

        base.ProcessSamples();

        InternalBuffers = Interlocked.Exchange(ref Buffers, InternalBuffers);
    }

    /// <inheritdoc/>
    public override void Dispose()
    {
        if (IsDisposed) return;
        ErrorLogging.Logger?.LogTrace($"{GetType()}.Dispose() ----------------------------");

        if (IsCapturing == 1)
        {
            ErrorLogging.EyecandyError($"{nameof(AudioCaptureOpenALSoft)}.Dispose", "Dispose invoked before audio processing was terminated.");
        }

        ErrorLogging.Logger?.LogTrace($"  {GetType()}.Dispose() ALC.CaptureCloseDevice");
        ALC.CaptureCloseDevice(CaptureDevice);

        // This is fine on Windows but crashes Linux Pulse Audio.
        // ErrorLogging.OpenALErrorCheck($"{nameof(AudioCaptureProcessor)}.Dispose");

        base.Dispose();

        IsDisposed = true;
        GC.SuppressFinalize(this);
    }
    private bool IsDisposed = false;
}
