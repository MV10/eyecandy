using OpenTK.Mathematics;
using OpenTK.Windowing.Desktop;

namespace eyecandy;

/// <summary>
/// OpenTK GameWindow and NativeWindow settings, and also eyecandy BsaeWindow settings.
/// </summary>
public class EyeCandyWindowConfig
{
    /// <summary>
    /// Passed through to the base OpenTK GameWindow constructor. OpenTK defaults are used.
    /// </summary>
    public GameWindowSettings OpenTKGameWindowSettings { get; set; } = GameWindowSettings.Default;

    /// <summary>
    /// Passed through to the base OpenTK GameWindow constructor. OpenTK defaults are used, except
    /// the BaseWindow class always forces OpenGL ES 3.2 for Raspberry Pi 4B compatibility.
    /// </summary>
    public NativeWindowSettings OpenTKNativeWindowSettings { get; set; } = NativeWindowSettings.Default;

    /// <summary>
    /// The buffer is cleared to this color on every pass. Default is black.
    /// </summary>
    public Color4 BackgroundColor { get; set; } = new(0, 0, 0, 1);

    /// <summary>
    /// Pathname to the vertex shader source file. Default is "default.vert".
    /// </summary>
    public string VertexShaderPathname { get; set; } = "default.vert";

    /// <summary>
    /// Pathname to the fragment (aka pixel) shader source file. Default is "default.vert".
    /// </summary>
    public string FragmentShaderPathname { get; set; } = "default.vert";

    /// <summary>
    /// When true, the window is switched to full-screen on the first OnUpdateFrame call.
    /// </summary>
    public bool StartFullScreen { get; set; } = false;
    // Work-around for an OpenTK bug that occurs if Fullscreen is applied at startup:
    // https://github.com/opentk/opentk/issues/1591

    /// <summary>
    /// Only applies to the application window (including full-screen).
    /// </summary>
    public bool HideMousePointer { get; set; } = true;

    /// <summary>
    /// When true, any shader compile error writes an error to the console and aborts.
    /// Default is true. When false, check the Shader object's IsValid property and
    /// the ErrorLogging.ShaderError list before use.
    /// </summary>
    public bool ExitOnInvalidShader { get; set; } = true;

    /// <summary>
    /// Controls how OpenGL error logging works. Refer to the flags for more information.
    /// </summary>
    public OpenGLErrorLogFlags OpenGLErrorLogging { get; set; } = OpenGLErrorLogFlags.Normal;

    /// <summary>
    /// Issues a Debug.Break command when true, if the OpenGL error callback is invoked
    /// a debugger is attached.
    /// </summary>
    public bool OpenGLErrorBreakpoint { get; set; } = false;

    /// <summary>
    /// When true, OpenGL errors (which can happen at very high frequency) are checked for
    /// duplication and only logged at the specified interval. If the containing program
    /// calls ErrorLogging.FlushGLErrors, a final tally is output at program termination.
    /// Set this to zero to disable interval logging. The default is 36000, which is about
    /// once per minute at 60FPS.
    /// </summary>
    public long OpenGLErrorInterval { get; set; } = 36000;
}
