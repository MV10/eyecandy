using eyecandy.Audio;
using OpenTK.Graphics.OpenGL;

namespace eyecandy
{
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
        //private bool HasSampleWidthTextures = false;
        //private bool HasOnePixelTextures = false;
        //private bool HasHistoryTextures = false;

        private AudioCaptureProcessor AudioProcessor;
        private Task AudioTask;
        private CancellationTokenSource ctsAudioProcessing;

        public AudioTextureEngine(EyeCandyCaptureConfig configuration)
        {
            AudioProcessor = new(configuration);
        }

        public void BeginAudioProcessing()
        {
            if (IsCapturing) return;
            ctsAudioProcessing = new();
            AudioTask = Task.Run(() => AudioProcessor.Capture(ProcessAudioDataCallback, ctsAudioProcessing.Token));
            IsCapturing = true;
        }

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

        public void Create<AudioTextureType>(string uniformName, TextureUnit assignedTextureUnit, bool enabled = true)
        where AudioTextureType : AudioTexture
        {
            var type = typeof(AudioTextureType);
            if (Textures.ContainsKey(type)) throw new InvalidOperationException($"Texture of type {type} already exists.");
            if (Textures.Any(t => t.Value.UniformName.Equals(uniformName))) throw new ArgumentException($"Texture uniform name {uniformName} already exists.");
            if (Textures.Any(t => t.Value.AssignedTextureUnit.Equals(assignedTextureUnit))) throw new ArgumentException($"Texture unit {assignedTextureUnit} already assigned.");
            var texture = AudioTexture.Factory<AudioTextureType>(uniformName, assignedTextureUnit, enabled);
            Textures.Add(type, texture);
            EvaluateRequirements();
        }

        public AudioTextureType Get<AudioTextureType>()
        where AudioTextureType : AudioTexture
        {
            var type = typeof(AudioTextureType);
            if (!Textures.ContainsKey(type)) return null;
            return Textures[type] as AudioTextureType;
        }

        public void Destroy<AudioTextureType>()
        where AudioTextureType : AudioTexture
        {
            var type = typeof(AudioTextureType);
            if (!Textures.ContainsKey(type)) return;
            Textures.Remove(type);
            EvaluateRequirements();
        }

        public void Enable<AudioTextureType>()
        where AudioTextureType : AudioTexture
        {
            var type = typeof(AudioTextureType);
            if (!Textures.ContainsKey(type)) return;
            Textures[type].Enabled = true;
            EvaluateRequirements();
        }

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
        /// new audio data isn't available yet (since the render loop is much faster than the audio
        /// sampling rate).
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
        /// This is invoked by the AudioCaptureProcessor thread.
        /// </summary>
        public void ProcessAudioDataCallback()
        {
            foreach (var t in Textures)
            {
                if (t.Value.Enabled) t.Value.UpdateChannelBuffer(AudioProcessor.Buffers);
            }
        }

        public void SetUniforms(Shader shader)
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

        public void EvaluateRequirements()
        {
            AudioProcessor.Requirements = new()
            {
                CalculateVolumeRMS = Textures.Any(t => t.Value.VolumeCalc == VolumeAlgorithm.RMS),
                CalculateFrequency = !Textures.All(t => t.Value.FrequencyCalc == FrequencyAlgorithm.NotApplicable),
                CalculateFFTMagnitude = Textures.Any(t => t.Value.FrequencyCalc == FrequencyAlgorithm.Magnitude),
                CalculateFFTDecibels = Textures.Any(t => t.Value.FrequencyCalc == FrequencyAlgorithm.Decibels),
            };
        }
    }
}
