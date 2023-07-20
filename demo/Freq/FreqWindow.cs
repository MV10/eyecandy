using eyecandy;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System.Diagnostics;

namespace demo
{
    internal class FreqWindow : BaseWindow
    {
        private int VertexCount = 65000;
        private float[] VertexIds;

        private int VertexBufferObject;
        private int VertexArrayObject;

        private Stopwatch Clock = new();

        private AudioTextureEngine Engine;

        private Shader ScrollShader;
        private Shader WaveShader;
        private bool ScrollShaderActive = true;

        public FreqWindow(EyeCandyWindowConfig windowConfig, EyeCandyCaptureConfig audioConfig)
            : base(windowConfig)
        {
            Engine = new(audioConfig);

            Engine.Create<AudioTextureFrequencyMagnitudeHistory>("sound", TextureUnit.Texture0);
            Engine.Create<AudioTextureVolumeHistory>("volume", TextureUnit.Texture1);

            VertexIds = new float[VertexCount];
            for (var i = 0; i < VertexCount; i++)
            {
                VertexIds[i] = i;
            }

            ScrollShader = Shader;
            WaveShader = new Shader("Freq/freq_wave.vert", windowConfig.FragmentShaderPathname);

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

            Engine.BeginAudioProcessing();
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);

            Engine.UpdateTextures();

            Engine.SetTextureUniforms(Shader);

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
                Engine.EndAudioProcessing_SynchronousHack();
                Close();
                Console.WriteLine($"\n\n{FramesPerSecond} FPS\n{AverageFramesPerSecond} average FPS, last {AverageFPSTimeframeSeconds} seconds");
                return;
            }

            if (input.IsKeyReleased(Keys.Enter))
            {
                ScrollShaderActive = !ScrollShaderActive;
                Shader = ScrollShaderActive ? ScrollShader : WaveShader;
            }

            if (input.IsKeyReleased(Keys.Space))
            {
                WindowState = (WindowState == WindowState.Fullscreen) ? WindowState.Normal : WindowState.Fullscreen;
            }
        }

        public new void Dispose()
        {
            base.Dispose();
            Engine.Dispose();
        }
    }
}
