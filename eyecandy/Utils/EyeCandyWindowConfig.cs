using OpenTK.Mathematics;
using OpenTK.Windowing.Desktop;

namespace eyecandy
{
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
        /// When true, any shader compile error writes an error to the console and aborts.
        /// Default is true. When false, check the Shader object's IsValid property and
        /// the ErrorLogging.ShaderError list before use.
        /// </summary>
        public bool ExitOnInvalidShader { get; set; } = true;
    }
}
