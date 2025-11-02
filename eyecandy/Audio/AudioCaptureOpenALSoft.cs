
using System.Diagnostics;
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

    private ALDevice ContextDevice;   
    private ALContext Context;    
    private ALCaptureDevice CaptureDevice;

    private readonly ILogger Logger;

    /// <inheritdoc/>
    internal AudioCaptureOpenALSoft(EyeCandyCaptureConfig configuration)
    : base(configuration)
    {
        Logger = ErrorLogging.LoggerFactory?.CreateLogger("Eyecandy." + nameof(AudioCaptureOpenALSoft));
        Logger?.LogTrace("Constructor completed");
    }

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

        Connect();

        Interlocked.Exchange(ref IsCapturing, 1);
        ALC.CaptureStart(CaptureDevice);
        if (ErrorLogging.OpenALErrorCheck($"{nameof(AudioCaptureOpenALSoft)}.{nameof(ALC.CaptureStart)}"))
        {
            Console.WriteLine("Aborting due to OpenAL error starting capture.");
            Thread.Sleep(250); // slow console
            Environment.Exit(1);
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            int samplesAvailable = ALC.GetInteger(CaptureDevice, AlcGetInteger.CaptureSamples);
            while (samplesAvailable >= SampleSize)
            {
                ProcessSamples();
                samplesAvailable -= SampleSize;
                newAudioDataCallback.Invoke();
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

            DetectSilence();
        }

        Logger?.LogDebug($"Capture ending");
        CaptureEnding();

        ALC.CaptureStop(CaptureDevice);
        if (ErrorLogging.OpenALErrorCheck($"{nameof(AudioCaptureOpenALSoft)}.{nameof(ALC.CaptureStop)}"))
        {
            Console.WriteLine("Aborting due to OpenAL error stopping capture.");
            Thread.Sleep(250); // slow console
            Environment.Exit(1);
        }

        Interlocked.Exchange(ref IsCapturing, 0);
        Buffers.Timestamp = DateTime.MaxValue;
        InternalBuffers.Timestamp = DateTime.MaxValue;
    }
    
    private void Connect()
    {
        Logger?.LogTrace($"Connect");

        var contextDeviceName = string.IsNullOrEmpty(Configuration.OpenALContextDeviceName)
            ? ALC.GetString(ALDevice.Null, AlcGetString.DefaultDeviceSpecifier)
            : Configuration.OpenALContextDeviceName;

        Logger?.LogTrace($"OpenAL context device name: {contextDeviceName}");

        ContextDevice = ALC.OpenDevice(contextDeviceName);
        Context = ALC.CreateContext(ContextDevice, (int[])null);
        ALC.MakeContextCurrent(Context);
        
        var captureDeviceName = string.IsNullOrEmpty(Configuration.CaptureDeviceName)
            ? ALC.GetString(ALDevice.Null, AlcGetString.CaptureDefaultDeviceSpecifier)
            : Configuration.CaptureDeviceName;

        Logger?.LogTrace($"Audio capture device name: {captureDeviceName}");

        CaptureDevice = ALC.CaptureOpenDevice(captureDeviceName, SampleRate, SampleFormat, SampleSize);

        // NOTE: If we end up supporting surround capture and the driver can't handle it, the
        // OpenAL Soft error looks like this. (Creative's OpenAL does not return an error, but
        // the CaptureSamples call will crash with an Access Violation exception.)
        // [ALSOFT] (EE) Failed to match format, wanted: 5.1 Surround Int16 44100hz, got: 0x00000003 mask 2 channels 32-bit 44100hz
        // https://github.com/kcat/openal-soft/issues/893

        if (ErrorLogging.OpenALErrorCheck($"{nameof(AudioCaptureOpenALSoft)}.{nameof(ALC.CaptureOpenDevice)}"))
        {
            Console.WriteLine("Aborting due to OpenAL error connecting to capture device.");
            Thread.Sleep(250); // slow console
            Environment.Exit(1);
        }
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
        base.Dispose();

        if (IsDisposed) return;
        Logger?.LogTrace("Dispose() ----------------------------");

        if (IsCapturing == 1) Logger?.LogWarning("Dispose invoked before audio processing was terminated");

        ALC.CaptureCloseDevice(CaptureDevice);
        ALC.MakeContextCurrent(ALContext.Null);
        ALC.DestroyContext(Context);
        ALC.CloseDevice(ContextDevice);
        
        // Warning: This is fine on Windows but crashes Linux Pulse Audio.
        // ErrorLogging.OpenALErrorCheck($"{nameof(AudioCaptureOpenALSoft)}.Dispose");

        base.Dispose();

        IsDisposed = true;
        GC.SuppressFinalize(this);
    }
    private bool IsDisposed = false;
}
