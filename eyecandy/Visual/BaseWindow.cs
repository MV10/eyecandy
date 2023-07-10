using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

namespace eyecandy
{
    /// <summary>
    /// A base class derived from the OpenTK GameWindow. This manages a Shader object (which can
    /// be swapped out for other Shader objects at runtime and handles certain chores like setting
    /// the background color on every render pass. It also forces OpenGL ES 3.2 for Raspberry Pi 4B
    /// compatibility.
    /// </summary>
    public abstract class BaseWindow : GameWindow, IDisposable
    {
        /// <summary>
        /// The compiled and linked vertex and fragment shaders. Check the IsValid property
        /// to confirm they are available, and check the Error properties if IsValid is false.
        /// This must be disposed.
        /// </summary>
        public Shader Shader = null;

        /// <summary>
        /// If derived classes require FPS, they must call CalculateFPS before exiting their
        /// OnRenderFrame callback. It is not calculated automatically because it is impossible
        /// for this base class to eliminate skew introduced by short-circuit code that skips
        /// render calls.
        /// </summary>
        public int FramesPerSecond = 0;

        /// <summary>
        /// The active configuration. This should never be changed during execution.
        /// </summary>
        public EyeCandyWindowConfig Configuration;

        // FPS calcs
        private int CurrentSecond = DateTime.Now.Second;
        private int FramesThisSecond = 0;

        private bool InitialFullScreenApplied;

        // used to modify the settings passed to the base constructor
        private static NativeWindowSettings ForceOpenGLES(NativeWindowSettings nativeWindowSettings)
        {
            nativeWindowSettings.API = ContextAPI.OpenGLES;
            nativeWindowSettings.APIVersion = new Version(3, 2);
            nativeWindowSettings.Profile = ContextProfile.Core;
            return nativeWindowSettings;
        }

        /// <summary>
        /// The constructor requries a configuration object. This object is stored and is accessible
        /// but should not be altered during program execution. Some settings are cached elsewhere
        /// for performance and/or thread-safety considerations and would not be updated.
        /// </summary>
        public BaseWindow(EyeCandyWindowConfig configuration)
            : base(configuration.OpenTKGameWindowSettings, ForceOpenGLES(configuration.OpenTKNativeWindowSettings))
        {
            Configuration = configuration;
            SetShader(configuration.VertexShaderPathname, configuration.FragmentShaderPathname);
            InitialFullScreenApplied = !Configuration.StartFullScreen;
        }

        // GameWindow is also disposable, but it cannot be overridden.
        protected new void Dispose()
        {
            base.Dispose();
            Shader.Dispose();
        }

        protected override void OnLoad()
        {
            base.OnLoad();
            GL.ClearColor(Configuration.BackgroundColor);
        }

        protected override void OnUnload()
        {
            base.OnUnload();
            Shader.Dispose();
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);
            GL.Viewport(0, 0, e.Width, e.Height);
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);
            GL.Clear(ClearBufferMask.ColorBufferBit);
            Shader.Use();
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            base.OnUpdateFrame(e);

            if (!InitialFullScreenApplied)
            {
                WindowState = WindowState.Fullscreen;
                InitialFullScreenApplied = true;
            }
        }

        /// <summary>
        /// Compiles a new set of vertex and fragment shaders and immediately uses them. It is
        /// also possible to create a Shader object elsewhere in your code and update the window's
        /// Shader property as needed.
        /// </summary>
        public virtual void SetShader(string vertexShaderPathname, string fragmentShaderPathname)
        {
            Shader = new Shader(vertexShaderPathname, fragmentShaderPathname);

            if (!Shader.IsValid && Configuration.ExitOnInvalidShader)
            {
                ErrorLogging.WriteToConsole();
                Shader.Dispose();
                Environment.Exit(-1);
            }
        }

        /// <summary>
        /// Derived classes should call this before exiting their OnRenderFrame callback.
        /// </summary>
        protected void CalculateFPS()
        {
            FramesThisSecond++;
            if (DateTime.Now.Second != CurrentSecond)
            {
                CurrentSecond = DateTime.Now.Second;
                FramesPerSecond = FramesThisSecond;
                FramesThisSecond = 0;
            }
        }
    }
}
