using eyecandy;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace demo;

public class FragWindow : BaseWindow
{
    private AudioTextureEngine Engine;

    // just a quad from two triangles that cover the whole display area
    float[] vertices =
    {
      // position             texture coords
         1.0f,  1.0f, 0.0f,   1.0f, 1.0f,     // top right
         1.0f, -1.0f, 0.0f,   1.0f, 0.0f,     // bottom right
        -1.0f, -1.0f, 0.0f,   0.0f, 0.0f,     // bottom left
        -1.0f,  1.0f, 0.0f,   0.0f, 1.0f      // top left
    };

    private readonly uint[] indices =
    {
        0, 1, 3,
        1, 2, 3
    };

    private int ElementBufferObject;
    private int VertexBufferObject;
    private int VertexArrayObject;

    public FragWindow(EyeCandyWindowConfig windowConfig, EyeCandyCaptureConfig audioConfig)
        : base(windowConfig)
    {
        Engine = new(audioConfig);

        Engine.Create<AudioTextureShadertoy>("iChannel0");
    }

    protected override void OnLoad()
    {
        base.OnLoad();

        VertexArrayObject = GL.GenVertexArray();
        GL.BindVertexArray(VertexArrayObject);

        VertexBufferObject = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, VertexBufferObject);
        GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);

        ElementBufferObject = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, ElementBufferObject);
        GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw);

        Shader.Use();

        var locationVertices = Shader.GetAttribLocation("vertices");
        GL.EnableVertexAttribArray(locationVertices);
        GL.VertexAttribPointer(locationVertices, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);
        //                                       ^ 3 vertex is 3 floats                   ^ 5 per row        ^ 0 offset per row

        var locationTexCoords = Shader.GetAttribLocation("vertexTexCoords");
        GL.EnableVertexAttribArray(locationTexCoords);
        GL.VertexAttribPointer(locationTexCoords, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 3 * sizeof(float));
        //                                        ^ tex coords is 2 floats                 ^ 5 per row        ^ 4th and 5th float in each row

        //ErrorLogging.OpenGLErrorCheck($"{nameof(FragWindow)}.{OnLoad}");

        Engine.BeginAudioProcessing();
    }

    protected override void OnRenderFrame(FrameEventArgs e)
    {
        base.OnRenderFrame(e);

        Engine.UpdateTextures();

        Engine.SetTextureUniforms(Shader);
        Shader.SetUniform("iResolution", new Vector2(Size.X, Size.Y));

        GL.BindVertexArray(VertexArrayObject);
        GL.DrawElements(PrimitiveType.Triangles, indices.Length, DrawElementsType.UnsignedInt, 0);

        SwapBuffers();

        //ErrorLogging.OpenGLErrorCheck($"{nameof(FragWindow)}.{OnRenderFrame}");

        CalculateFPS();
    }

    protected override void OnUpdateFrame(FrameEventArgs e)
    {
        base.OnUpdateFrame(e);

        var input = KeyboardState;

        if (input.IsKeyReleased(Keys.Escape))
        {
            Engine.EndAudioProcessing();
            Close();
            Console.WriteLine($"\n\n{FramesPerSecond} FPS\n{AverageFramesPerSecond} average FPS, last {AverageFPSTimeframeSeconds} seconds");
            return;
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