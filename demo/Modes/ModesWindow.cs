using eyecandy;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System.Diagnostics;

namespace demo
{
    internal class ModesWindow : BaseWindow
    {
        private int DrawingMode = 0;
        private PrimitiveType[] Modes =
        {
            PrimitiveType.Points,
            PrimitiveType.Lines,
            PrimitiveType.LineStrip,
            PrimitiveType.LineLoop,
            PrimitiveType.Triangles,
            PrimitiveType.TriangleStrip,
            PrimitiveType.TriangleFan,

            // Requires more complex data, or uninteresting
            //PrimitiveType.Quads,
            //PrimitiveType.QuadStrip,
            //PrimitiveType.Polygon,
            //PrimitiveType.LinesAdjacency,
            //PrimitiveType.LineStripAdjacency,
            //PrimitiveType.TrianglesAdjacency,
            //PrimitiveType.TriangleStripAdjacency,
            //PrimitiveType.Patches,
        };
        
        private int VertexCount = 64;
        private float[] VertexIds;

        private int VertexBufferObject;
        private int VertexArrayObject;

        private Stopwatch Clock = new();

        public ModesWindow(EyeCandyWindowConfig windowConfig, EyeCandyCaptureConfig audioConfig)
            : base(windowConfig)
        {
            VertexIds = new float[VertexCount];
            for (var i = 0; i < VertexCount; i++)
            {
                VertexIds[i] = i;
            }

            Console.WriteLine($"--> {Modes[DrawingMode].ToString().ToUpper()}");
            
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
            GL.DrawArrays(Modes[DrawingMode], 0, VertexCount);

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
                Console.WriteLine($"\n\n{FramesPerSecond} FPS");
                return;
            }

            if (input.IsKeyReleased(Keys.Space))
            {
                DrawingMode++;
                if (DrawingMode == Modes.Length) DrawingMode = 0;
                Console.WriteLine($"--> {Modes[DrawingMode].ToString().ToUpper()}");
            }
        }
    }
}
