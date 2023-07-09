using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

namespace eyecandy
{
    public abstract class BaseWindow : GameWindow, IDisposable
    {
        /// <summary>
        /// The compiled and linked vertex and fragment shaders. Check the IsValid property
        /// to confirm they are available, and check the Error properties if IsValid is false.
        /// This must be disposed.
        /// </summary>
        public Shader Shader = null;

        /// <summary>
        /// Automatically-calculated FPS (based on render-loop calls, updated once per second).
        /// </summary>
        public int FramesPerSecond = 0;

        public EyeCandyWindowConfig Configuration;

        // FPS calcs
        private int CurrentSecond = DateTime.Now.Second;
        private int FramesThisSecond = 0;

        private bool InitialFullScreenApplied;

        private static NativeWindowSettings ForceOpenGLES(NativeWindowSettings nativeWindowSettings)
        {
            nativeWindowSettings.API = ContextAPI.OpenGLES;
            nativeWindowSettings.APIVersion = new Version(3, 2);
            nativeWindowSettings.Profile = ContextProfile.Core;
            return nativeWindowSettings;
        }

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
            CalculateFPS();
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

        private void CalculateFPS()
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
