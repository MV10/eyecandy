using NAudio.CoreAudioApi;

// This is a variant on the loopback class in NAudio, except
// that it accepts a buffer size (in milliseconds), which is
// an option with the underlying WASAPI class. The built-in
// capture class uses 100ms but eyecandy expects approx 22ms.
// https://github.com/naudio/NAudio/blob/master/NAudio.Wasapi/WasapiLoopbackCapture.cs
// https://github.com/naudio/NAudio/blob/master/NAudio.Wasapi/WasapiCapture.cs

namespace eyecandy;

/// <summary>
/// A copy of the WASAPI Loopback Capture class which allows buffer-size control.
/// </summary>
internal class WindowsLoopbackWrapper : WasapiCapture
{
    /// <summary>
    /// Initialises a new instance of the WASAPI capture class
    /// </summary>
    public WindowsLoopbackWrapper()
    : this(GetDefaultLoopbackCaptureDevice())
    { }

    /// <summary>
    /// Initialises a new instance of the WASAPI capture class
    /// </summary>
    /// <param name="captureDevice">Capture device to use</param>
    public WindowsLoopbackWrapper(MMDevice captureDevice)
    : base(captureDevice)
    { }

    /// <summary>
    /// Initialises a new instance of the WASAPI capture class
    /// </summary>
    /// <param name="audioBufferMillisecondsLength">Length of the audio buffer in milliseconds. A lower value means lower latency but increased CPU usage.</param>
    public WindowsLoopbackWrapper(int audioBufferMillisecondsLength)
    : base(GetDefaultLoopbackCaptureDevice(), false, audioBufferMillisecondsLength)
    { }

    /// <summary>
    /// Gets the default audio loopback capture device
    /// </summary>
    public static MMDevice GetDefaultLoopbackCaptureDevice()
    {
        MMDeviceEnumerator devices = new MMDeviceEnumerator();
        return devices.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
    }

    /// <summary>
    /// Specify loopback
    /// </summary>
    protected override AudioClientStreamFlags GetAudioClientStreamFlags()
    {
        return AudioClientStreamFlags.Loopback | base.GetAudioClientStreamFlags();
    }
}
