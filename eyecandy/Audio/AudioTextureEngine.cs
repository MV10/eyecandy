using eyecandy.Audio;
using OpenTK.Graphics.OpenGL;

namespace eyecandy
{
    /// <summary>
    /// Produces OpenGL textures based on OpenAL audio data provided by an AudioCaptureProcessor
    /// running on a background thread.
    /// </summary>
    public class AudioTextureEngine : IDisposable
    {
        /// <summary>
        /// Every RGBA pixel has 4 channels of data.
        /// </summary>
        public static readonly int RGBAPixelSize = 4;

        /// <summary>
        /// When true, the engine is actively capturing and processing audio.
        /// </summary>
        public bool IsCapturing { get; private set; } = false;

        /// <summary>
        /// DateTime.Now value when the texture data was updated from the underlying audio buffers.
        /// </summary>
        public DateTime TexturesUpdatedTimestamp = DateTime.MaxValue;

        private Dictionary<Type, AudioTexture> Textures = new();
        private bool TextureHandlesAllocated = false;

        private AudioCaptureProcessor AudioProcessor;
        private Task AudioTask;
        private CancellationTokenSource ctsAudioProcessing;

        /// <summary>
        /// The constructor requries a configuration object. This object is stored and is accessible
        /// but should not be altered during program execution. Some settings are cached elsewhere
        /// for performance and/or thread-safety considerations and would not be updated.
        /// </summary>
        public AudioTextureEngine(EyeCandyCaptureConfig configuration)
        {
            AudioProcessor = new(configuration);
        }

        /// <summary>
        /// Invokes the AudioCaptureProcessor on a background thread and begins updating any
        /// requested, enabled AudioTextures.
        /// </summary>
        public void BeginAudioProcessing()
        {
            if (IsCapturing) return;
            ctsAudioProcessing = new();
            AudioTask = Task.Run(() => AudioProcessor.Capture(ProcessAudioDataCallback, ctsAudioProcessing.Token));
            IsCapturing = true;
        }

        /// <summary>
        /// Cancels the AudioCaptureProcessor background thread. Callers should await this to
        /// ensure clean shutdown of the wrapping task before calling Dispose.
        /// </summary>
        public async Task EndAudioProcessing()
        {
            if (!IsCapturing) return;
            ctsAudioProcessing.Cancel();
            IsCapturing = false;
            await AudioTask;
        }

        /// <summary>
        /// Not recommended, safer to correctly await the EndAudioProcessing task.
        /// Added because the OpenTK window events like OnUpdateFrame are synchronous.
        /// Pitfalls described here: 
        /// https://learn.microsoft.com/en-us/archive/msdn-magazine/2015/july/async-programming-brownfield-async-development#the-blocking-hack
        /// </summary>
        public void EndAudioProcessing_SynchronousHack()
            => EndAudioProcessing().GetAwaiter().GetResult();

        /// <summary>
        /// AudioTexture objects can't be created directly. This factory method ensures they are properly
        /// initialized and ready for use. Normally it isn't necessary to manipulate the AudioTexture objects
        /// directly.
        /// </summary>
        public void Create<AudioTextureType>(string uniformName, TextureUnit assignedTextureUnit, float sampleMultiplier = 1.0f, bool enabled = true)
        where AudioTextureType : AudioTexture
        {
            var type = typeof(AudioTextureType);
            if (Textures.ContainsKey(type)) throw new InvalidOperationException($"Texture of type {type} already exists.");
            if (Textures.Any(t => t.Value.UniformName.Equals(uniformName))) throw new ArgumentException($"Texture uniform name {uniformName} already exists.");
            if (Textures.Any(t => t.Value.AssignedTextureUnit.Equals(assignedTextureUnit))) throw new ArgumentException($"Texture unit {assignedTextureUnit} already assigned.");
            var texture = AudioTexture.Factory<AudioTextureType>(uniformName, assignedTextureUnit, sampleMultiplier, enabled);
            Textures.Add(type, texture);
            EvaluateRequirements();
        }

        /// <summary>
        /// If an AudioTexture object of the requested type has been created, this returns a reference.
        /// Otherwise null is returned.
        /// </summary>
        public AudioTextureType Get<AudioTextureType>()
        where AudioTextureType : AudioTexture
        {
            var type = typeof(AudioTextureType);
            if (!Textures.ContainsKey(type)) return null;
            return Textures[type] as AudioTextureType;
        }

        /// <summary>
        /// Removes a given type of AudioTexture object from the application.
        /// </summary>
        public void Destroy<AudioTextureType>()
        where AudioTextureType : AudioTexture
        {
            var type = typeof(AudioTextureType);
            if (!Textures.ContainsKey(type)) return;
            Textures.Remove(type);
            EvaluateRequirements();
        }

        /// <summary>
        /// When enabled, the AudioTexture is updated whenever new audio data is available.
        /// </summary>
        public void Enable<AudioTextureType>()
        where AudioTextureType : AudioTexture
        {
            var type = typeof(AudioTextureType);
            if (!Textures.ContainsKey(type)) return;
            Textures[type].Enabled = true;
            EvaluateRequirements();
        }

        /// <summary>
        /// Changes the AudioTexture's SampleMultiplier value.
        /// </summary>
        public void SetMultiplier<AudioTextureType>(float multiplier)
        where AudioTextureType : AudioTexture
        {
            var type = typeof(AudioTextureType);
            if (!Textures.ContainsKey(type)) return;
            Textures[type].SampleMultiplier = multiplier;
        }

        /// <summary>
        /// When disabled, an AudioTexture is not updated by audio data sampling.
        /// </summary>
        public void Disable<AudioTextureType>()
        where AudioTextureType : AudioTexture
        {
            var type = typeof(AudioTextureType);
            if (!Textures.ContainsKey(type)) return;
            Textures[type].Enabled = false;
            EvaluateRequirements();
        }

        /// <summary>
        /// The render loop should call this before any textures are used. It will short-circuit if
        /// new audio data isn't available yet since the render loop is much faster than the audio
        /// sampling rate, so it is safe to call on every render pass.
        /// </summary>
        public void UpdateTextures()
        {
            // Short-circuit if the audio buffers haven't changed yet AND we've already generated textures at least once.
            if (AudioProcessor.Buffers.Timestamp < TexturesUpdatedTimestamp && TextureHandlesAllocated) return;

            foreach(var tex in Textures)
            {
                tex.Value.GenerateTexture();
            }

            TextureHandlesAllocated = true;
            TexturesUpdatedTimestamp = DateTime.Now;
        }

        /// <summary>
        /// This is invoked by the AudioCaptureProcessor thread whenever new audio sample data is available.
        /// </summary>
        public void ProcessAudioDataCallback()
        {
            foreach (var t in Textures)
            {
                if (t.Value.Enabled) t.Value.UpdateChannelBuffer(AudioProcessor.Buffers);
            }
        }

        /// <summary>
        /// Calls Shader.SetTexture for each of the currently-enabled AudioTexture objects.
        /// </summary>
        public void SetTextureUniforms(Shader shader)
        {
            foreach (var t in Textures)
            {
                if (t.Value.Enabled) shader.SetTexture(t.Value);
            }
        }

        public void Dispose()
        {
            if (IsCapturing) throw new InvalidOperationException("Dispose invoked before audio processing was terminated.");
            AudioProcessor?.Dispose();
        }

        /// <summary>
        /// Generates an updated AudioProcessingRequirements structure and applies it to the AudioCaptureProcessor.
        /// This is primarily used internally when an AudioTexture is created, deleted, or the enabled state changes.
        /// </summary>
        public void EvaluateRequirements()
        {
            AudioProcessor.Requirements = new()
            {
                CalculateVolumeRMS = Textures.Any(t => t.Value.VolumeCalc == VolumeAlgorithm.RMS || t.Value.VolumeCalc == VolumeAlgorithm.All),
                CalculateFrequency = !Textures.All(t => t.Value.FrequencyCalc == FrequencyAlgorithm.NotApplicable),
                CalculateFFTMagnitude = Textures.Any(t => t.Value.FrequencyCalc == FrequencyAlgorithm.Magnitude || t.Value.FrequencyCalc == FrequencyAlgorithm.All),
                CalculateFFTDecibels = Textures.Any(t => t.Value.FrequencyCalc == FrequencyAlgorithm.Decibels || t.Value.FrequencyCalc == FrequencyAlgorithm.All),
            };
        }
    }
}
