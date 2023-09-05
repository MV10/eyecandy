using eyecandy;
using OpenTK.Graphics.OpenGL;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System.ComponentModel;

namespace demo
{
    public class HistoryWindow : BaseWindow
    {
        enum AudioTextureType { Wave, Volume, FreqMag, FreqDb, Combined, WebAudio }

        private AudioTextureType DemoMode = AudioTextureType.Wave;

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

        public HistoryWindow(EyeCandyWindowConfig windowConfig, EyeCandyCaptureConfig audioConfig)
            : base(windowConfig)
        {
            Engine = new(audioConfig);

            // the multiplier makes the green easier to see
            Engine.Create<AudioTextureWaveHistory>("wave");
            Engine.Create<AudioTextureVolumeHistory>("volume");
            Engine.Create<AudioTextureFrequencyMagnitudeHistory>("fmag");
            Engine.Create<AudioTextureFrequencyDecibelHistory>("fdb");
            Engine.Create<AudioTexture4ChannelHistory>("combined");
            Engine.Create<AudioTextureWebAudioHistory>("webaudio");
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

            int textureHandle = DemoMode switch
            {
                AudioTextureType.Wave => Engine.Get<AudioTextureWaveHistory>().Handle,
                AudioTextureType.Volume => Engine.Get<AudioTextureVolumeHistory>().Handle,
                AudioTextureType.FreqMag => Engine.Get<AudioTextureFrequencyMagnitudeHistory>().Handle,
                AudioTextureType.FreqDb => Engine.Get<AudioTextureFrequencyDecibelHistory>().Handle,
                AudioTextureType.Combined => Engine.Get<AudioTexture4ChannelHistory>().Handle,
                AudioTextureType.WebAudio => Engine.Get<AudioTextureWebAudioHistory>().Handle,
            };

            int textureUnit = DemoMode switch
            {
                AudioTextureType.Wave => Engine.Get<AudioTextureWaveHistory>().AssignedTextureUnit,
                AudioTextureType.Volume => Engine.Get<AudioTextureVolumeHistory>().AssignedTextureUnit,
                AudioTextureType.FreqMag => Engine.Get<AudioTextureFrequencyMagnitudeHistory>().AssignedTextureUnit,
                AudioTextureType.FreqDb => Engine.Get<AudioTextureFrequencyDecibelHistory>().AssignedTextureUnit,
                AudioTextureType.Combined => Engine.Get<AudioTexture4ChannelHistory>().AssignedTextureUnit,
                AudioTextureType.WebAudio => Engine.Get<AudioTextureWebAudioHistory>().AssignedTextureUnit,
            };

            // The demo frag shader declares uniform "audioTexture"; we're disregarding the uniform
            // names passed from the constructor and assigned to each AudioTexture data object.
            Shader.SetTexture("audioTexture", textureHandle, textureUnit);

            // Prior to v2 each audio texture had a multiplier associated with it to "strengthen" the
            // signal in the data. This is better handled at the individual shader level. For example,
            // multi-pass shaders may re-use the same audio texture data, but have different requirements.
            float multiplier = DemoMode switch
            {
                AudioTextureType.Wave => 5f,
                AudioTextureType.FreqMag => 5f,
                _ => 1f
            };
            Shader.SetUniform("multiplier", multiplier);

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
                Console.WriteLine("update engine end");
                Engine.EndAudioProcessing_SynchronousHack();
                Console.WriteLine("update Close()");
                Close();
                Console.WriteLine($"\n\n{FramesPerSecond} FPS\n{AverageFramesPerSecond} average FPS, last {AverageFPSTimeframeSeconds} seconds");
                Console.WriteLine("update return");
                return;
            }

            if (input.IsKeyReleased(Keys.P))
            {
                Console.WriteLine("--> PCM / WAVE");
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
                Console.WriteLine("--> FREQ MAGNITUDE");
                DemoMode = AudioTextureType.FreqMag;
                return;
            }

            if (input.IsKeyReleased(Keys.D))
            {
                Console.WriteLine("--> FREQ DB");
                DemoMode = AudioTextureType.FreqDb;
                return;
            }

            if (input.IsKeyReleased(Keys.D4))
            {
                Console.WriteLine("--> COMBINED 4CH");
                DemoMode = AudioTextureType.Combined;
                return;
            }

            if (input.IsKeyReleased(Keys.W))
            {
                Console.WriteLine("--> WEBAUDIO (SMOOTHED DB FREQ)");
                DemoMode = AudioTextureType.WebAudio;
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