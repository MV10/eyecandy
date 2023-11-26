
using eyecandy.Utils;
using Microsoft.Extensions.Logging;
using OpenTK.Graphics.OpenGL;

namespace eyecandy
{
    /// <summary>
    /// The base class for all OpenGL audio texture handling. Typically, derived classes only
    /// need to set PixelWidth and Rows, as well as feature flags like VolumeCalc or FrequencyCalc
    /// in their constructors, and provide an UpdateChannelBuffer implementation to copy audio 
    /// buffer data to the ChannelBuffer used to generate textures.
    /// </summary>
    public abstract class AudioTexture : IDisposable
    {
        /// <summary>
        /// The Handle is set to this until GenerateTexture has been called.
        /// </summary>
        public static readonly int UninitializedTexture = int.MinValue;

        /// <summary>
        /// The name of this texture in the shader uniform declaration.
        /// </summary>
        public string UniformName { get; private set; } = string.Empty;

        /// <summary>
        /// The unit where the texture definition is stored.
        /// </summary>
        public int AssignedTextureUnit { get; private set; } = (int)TextureUnit.Texture0;

        /// <summary>
        /// Do not set this directly. Call Enable/Disable in AudioTextureEngine, which
        /// allows the audio processor to re-evaluate which post-processing activities
        /// must be invoked. When false, all processing on this texture is suspended.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// The algorithm used to calculate volume, if any.
        /// </summary>
        public VolumeAlgorithm VolumeCalc { get; protected internal set; } = VolumeAlgorithm.NotApplicable;

        /// <summary>
        /// The FFT algorithm used to calculate frequency, if any.
        /// </summary>
        public FrequencyAlgorithm FrequencyCalc { get; protected internal set; } = FrequencyAlgorithm.NotApplicable;

        /// <summary>
        /// OpenGL handle to the texture object.
        /// </summary>
        public int Handle = UninitializedTexture;

        /// <summary>
        /// Audio data converted to RGBA channel data.
        /// </summary>
        public float[] ChannelBuffer;

        /// <summary>
        /// Number of pixels in each row of the texture.
        /// Remember the "power of 2" rule for less-modern GPUs.
        /// </summary>
        public int PixelWidth = -1;

        /// <summary>
        /// Number of rows in the audio Buffer data and the texture.
        /// Remember the "power of 2" rule for less-modern GPUs.
        /// </summary>
        public int Rows = -1;

        /// <summary>
        /// Number of raw columns in the ChannelBuffer (ie. pixel width multiplied by 4 for RGBA).
        /// </summary>
        public int BufferWidth;

        /// <summary>
        /// Lock-section object for protecting access during UpdateChannelBuffer and GenerateTexture calls.
        /// </summary>
        protected internal object ChannelBufferLock = new();

        /// <summary>
        /// AudioTexture objects are not directly creatable. This factory method ensures they are correctly initialized.
        /// The factory method, in turn, is called from the AudioTextureEngine.Create method.
        /// </summary>
        internal static AudioTexture Factory<AudioTextureType>(string uniformName, int assignedTextureUnit, bool enabled = true)
        {
            var texture = Activator.CreateInstance<AudioTextureType>() as AudioTexture;

            if (texture.PixelWidth == -1 || texture.Rows == -1) throw new InvalidOperationException($"The {texture.GetType()} constructor must set PixelWidth and Rows.");

            texture.UniformName = uniformName;
            texture.AssignedTextureUnit = assignedTextureUnit;
            texture.Enabled = enabled;

            texture.BufferWidth = texture.PixelWidth * AudioTextureEngine.RGBAPixelSize;
            texture.ChannelBuffer = new float[texture.BufferWidth * texture.Rows];

            return texture;
        }

        /// <summary>
        /// The derived constructor must set PixelWidth and Rows (at a minimum). The factory method will store the
        /// UniformName, AssignedTextureUnit, Enabled flag, calculate BufferWidth, and allocate ChannelBuffer.
        /// </summary>
        protected AudioTexture()
        { }

        /// <summary>
        /// Invoked whenever new audio data is available. Call lock(ChannelBufferLock) before
        /// modifying the ChannelBuffer contents. This is normally invoked on a separate thread by the
        /// AudioTextureEngine callback when AudioCaptureProcessor has new audio sample data available.
        /// </summary>
        public abstract void UpdateChannelBuffer(AudioData audioBuffers);

        /// <summary>
        /// Copies audio Buffer data into a 2D texture object associated with the AssignedTextureUnit.
        /// </summary>
        public virtual void GenerateTexture()
        {
            if (IsDisposed) return;

            if (Handle == UninitializedTexture)
            {
                Handle = GL.GenTexture();
            }

            if (!Enabled) return;

            lock (AudioTextureEngine.GLTextureLock)
            {
                GL.ActiveTexture(AssignedTextureUnit.ToTextureUnitEnum());
                GL.BindTexture(TextureTarget.Texture2D, Handle);

                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

                lock (ChannelBufferLock)
                {
                    GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba32f, PixelWidth, Rows, 0, PixelFormat.Rgba, PixelType.Float, ChannelBuffer);
                }

                ErrorLogging.OpenGLErrorCheck($"{GetType()}.{nameof(GenerateTexture)}");
            }
        }

        /// <summary>
        /// This must be called within a lock(ChannelBufferLock) region.
        /// Presuming row 0 is the "bottom" row containing the newest data, this scrolls buffer rows "up"
        /// through the array (row 0 becomes row 1 and so on).
        /// </summary>
        protected internal void ScrollHistoryBuffer()
            => Array.Copy(ChannelBuffer, 0, ChannelBuffer, BufferWidth, ChannelBuffer.Length - BufferWidth);

        /// <summary/>
        public virtual void Dispose()
        {
            if (IsDisposed) return;
            ErrorLogging.Logger?.LogTrace($"{GetType()}.Dispose() ----------------------------");

            if (Handle != UninitializedTexture)
            {
                ErrorLogging.Logger?.LogTrace($"  {GetType()}.Dispose() DeleteTexture {UniformName}");
                GL.DeleteTexture(Handle);
                Handle = UninitializedTexture;
            }

            IsDisposed = true;
            GC.SuppressFinalize(this);
        }
        internal bool IsDisposed = false;
    }
}
