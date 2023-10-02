using eyecandy;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System.Diagnostics;

namespace demo
{
    public class VertWindow : BaseWindow
    {
        private int VertexCount = 2000;
        private float[] VertexIds;

        private int VertexBufferObject;
        private int VertexArrayObject;

        private Stopwatch Clock = new();

        public VertWindow(EyeCandyWindowConfig windowConfig)
            : base(windowConfig, createShaderFromConfig: false)
        {
            // remember Linux is case-sensitive...
            var libraryShaderPathname = "Vert/vertdemo_library.vert";
            var vertexShaderPathname = "Vert/vertdemo.vert";
            var fragmentShaderPathname = "Vert/vertdemo.frag";

            var library = new ShaderLibrary(libraryShaderPathname, type: ShaderType.VertexShader);
            if (!library.IsValid) Environment.Exit(-1);

            Shader = new(vertexShaderPathname, fragmentShaderPathname, library);
            if (!Shader.IsValid) Environment.Exit(-1);

            // in a real program, we could save this compiled library for use with multiple shaders
            library.Dispose();

            VertexIds = new float[VertexCount];
            for (var i = 0; i < VertexCount; i++)
            {
                VertexIds[i] = i;
            }

            Clock.Start();
        }

        protected override void OnLoad()
        {
            base.OnLoad();

            VertexBufferObject = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, VertexBufferObject);
            GL.BufferData(BufferTarget.ArrayBuffer, VertexIds.Length * sizeof(float), VertexIds, BufferUsageHint.StaticDraw);

            VertexArrayObject = GL.GenVertexArray();
            GL.BindVertexArray(VertexArrayObject);
            GL.VertexAttribPointer(0, 1, VertexAttribPointerType.Float, false, sizeof(float), 0);
            GL.EnableVertexAttribArray(0); // 0 = location of vertexId attribute
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);

            Shader.SetUniform("vertexCount", (float)VertexCount);
            Shader.SetUniform("resolution", new Vector2(Size.X, Size.Y));
            Shader.SetUniform("time", (float)Clock.Elapsed.TotalSeconds);

            GL.BindVertexArray(VertexArrayObject);
            GL.DrawArrays(PrimitiveType.Points, 0, VertexCount);

            SwapBuffers();
            CalculateFPS();
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            base.OnUpdateFrame(e);

            var input = KeyboardState;

            if (input.IsKeyDown(Keys.Escape))
            {
                Close();
                Console.WriteLine($"\n\n{FramesPerSecond} FPS\n{AverageFramesPerSecond} average FPS, last {AverageFPSTimeframeSeconds} seconds");
            }

            if (input.IsKeyReleased(Keys.Space))
            {
                WindowState = (WindowState == WindowState.Fullscreen) ? WindowState.Normal : WindowState.Fullscreen;
            }
        }
    }
}