﻿using eyecandy;
using OpenTK.Graphics.OpenGL;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace demo
{
    public class HistoryWindow : BaseWindow
    {
        enum AudioTextureType { Wave, Volume, Frequency }

        private AudioTextureType DemoMode = AudioTextureType.Wave;

        private AudioTextureEngine Engine;

        float[] vertices =
        {
          // position             texture coords
             0.5f,  0.5f, 0.0f,   1.0f, 1.0f,     // top right
             0.5f, -0.5f, 0.0f,   1.0f, 0.0f,     // bottom right
            -0.5f, -0.5f, 0.0f,   0.0f, 0.0f,     // bottom left
            -0.5f,  0.5f, 0.0f,   0.0f, 1.0f      // top left
        };

        private readonly uint[] indices =
        {
            0, 1, 3,
            1, 2, 3
        };

        private int ElementBufferObject;
        private int VertexBufferObject;
        private int VertexArrayObject;

        public HistoryWindow(EyeCandyWindowConfig windowConfig, EyeCandyCaptureConfig audioConfig)
            : base(windowConfig)
        {
            Engine = new(audioConfig);

            Engine.Create<AudioTextureFrequencyMagnitudeHistory>("sound", TextureUnit.Texture0);
            Engine.Create<AudioTextureWaveHistory>("wave", TextureUnit.Texture1);
            Engine.Create<AudioTextureVolumeHistory>("volume", TextureUnit.Texture2);
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

            ErrorLogging.OpenGLErrorCheck($"{nameof(HistoryWindow)}.{OnLoad}");

            Engine.BeginAudioProcessing();
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);

            Engine.UpdateTextures();

            Shader.Use();

            GL.BindVertexArray(VertexArrayObject);

            int handle = DemoMode switch
            {
                AudioTextureType.Wave => Engine.Get<AudioTextureWaveHistory>().Handle,
                AudioTextureType.Volume => Engine.Get<AudioTextureVolumeHistory>().Handle,
                AudioTextureType.Frequency => Engine.Get<AudioTextureFrequencyMagnitudeHistory>().Handle,
            };

            TextureUnit unit = DemoMode switch
            {
                AudioTextureType.Wave => Engine.Get<AudioTextureWaveHistory>().AssignedTextureUnit,
                AudioTextureType.Volume => Engine.Get<AudioTextureVolumeHistory>().AssignedTextureUnit,
                AudioTextureType.Frequency => Engine.Get<AudioTextureFrequencyMagnitudeHistory>().AssignedTextureUnit,
            };

            // The demo frag shader declares audioTexture; we're disregarding the uniform names
            // defined in the constructor and assigned to each AudioTexture data object.
            Shader.SetTexture("audioTexture", handle, unit);

            GL.DrawElements(PrimitiveType.Triangles, indices.Length, DrawElementsType.UnsignedInt, 0);

            SwapBuffers();

            ErrorLogging.OpenGLErrorCheck($"{nameof(HistoryWindow)}.{OnRenderFrame}");

            CalculateFPS();
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            base.OnUpdateFrame(e);

            var input = KeyboardState;

            if (input.IsKeyReleased(Keys.Escape))
            {
                Engine.EndAudioProcessing_SynchronousHack();
                Close();
                Console.WriteLine($"\n\n{FramesPerSecond} FPS");
                return;
            }

            if (input.IsKeyReleased(Keys.W))
            {
                Console.WriteLine("--> WAVE");
                DemoMode = AudioTextureType.Wave;
                return;
            }

            if (input.IsKeyReleased(Keys.V))
            {
                Console.WriteLine("--> VOLUME");
                DemoMode = AudioTextureType.Volume;
                return;
            }

            if (input.IsKeyReleased(Keys.F))
            {
                Console.WriteLine("--> FREQUENCY");
                DemoMode = AudioTextureType.Frequency;
                return;
            }
        }

        public new void Dispose()
        {
            base.Dispose();
            Engine.Dispose();
        }
    }
}