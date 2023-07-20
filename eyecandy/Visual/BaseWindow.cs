
using OpenTK.Graphics.OpenGL;
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
        /// The Raspberry Pi4B requires OpenGL ES 3.2; when true, the NativeWindowSettings
        /// are forced to the OpenGLES API version 3.1, Core profile. When false, you can
        /// pass whatever settings you want.
        /// </summary>
        public static bool ForceOpenGLES3dot2 = true;

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
        /// A longer-period averaging of the calcualted FPS.
        /// </summary>
        public int AverageFramesPerSecond = 0;

        /// <summary>
        /// Number of seconds over which AverageFramesPerSecond is calculated. Default is 10.
        /// </summary>
        public int AverageFPSTimeframeSeconds = 10;

        /// <summary>
        /// The active configuration. This should never be changed during execution.
        /// </summary>
        public EyeCandyWindowConfig Configuration;

        // FPS calcs
        private int CurrentSecond = DateTime.Now.Second;
        private int FramesThisSecond = 0;
        private int AverageFPSTotal = 0;
        private int[] FPSBuffer;
        private int FPSBufferIndex = 0;

        private bool InitialFullScreenApplied;

        // used to modify the settings passed to the base constructor
        private static NativeWindowSettings ForceOpenGLES(NativeWindowSettings nativeWindowSettings)
        {
            if(ForceOpenGLES3dot2)
            {
                nativeWindowSettings.API = ContextAPI.OpenGLES;
                nativeWindowSettings.APIVersion = new Version(3, 2);
                nativeWindowSettings.Profile = ContextProfile.Core;
            }
            return nativeWindowSettings;
        }

        /// <summary>
        /// The constructor requries a configuration object. This object is stored and is accessible
        /// but should not be altered during program execution. Some settings are cached elsewhere
        /// for performance and/or thread-safety considerations and would not be updated.
        /// </summary>
        public BaseWindow(EyeCandyWindowConfig configuration, bool createShaderFromConfig = true)
            : base(configuration.OpenTKGameWindowSettings, ForceOpenGLES(configuration.OpenTKNativeWindowSettings))
        {
            Configuration = configuration;
            if(createShaderFromConfig) SetShader(configuration.VertexShaderPathname, configuration.FragmentShaderPathname);
            InitialFullScreenApplied = !Configuration.StartFullScreen;
            FPSBuffer = new int[AverageFPSTimeframeSeconds];
        }

        // GameWindow is also disposable, but it cannot be overridden.
        /// <inheritdoc/>
        protected new void Dispose()
        {
            base.Dispose();
            Shader?.Dispose();
        }

        /// <inheritdoc/>
        protected override void OnLoad()
        {
            base.OnLoad();
            GL.ClearColor(Configuration.BackgroundColor);
        }

        /// <inheritdoc/>
        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);
            GL.Viewport(0, 0, e.Width, e.Height);
        }

        /// <inheritdoc/>
        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);
            GL.Clear(ClearBufferMask.ColorBufferBit);
            Shader?.Use();
        }

        /// <inheritdoc/>
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
                ErrorLogging.LibraryError($"{nameof(eyecandy)} {nameof(BaseWindow)}.{nameof(SetShader)}", $"Terminating, {nameof(Configuration.ExitOnInvalidShader)} is true.");
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

                AverageFPSTotal = AverageFPSTotal - FPSBuffer[FPSBufferIndex] + FramesPerSecond;
                AverageFramesPerSecond = AverageFPSTotal / AverageFPSTimeframeSeconds;
                FPSBuffer[FPSBufferIndex] = FramesPerSecond;
                FPSBufferIndex++;
                if (FPSBufferIndex == AverageFPSTimeframeSeconds) FPSBufferIndex = 0;
            }
        }
    }
}
