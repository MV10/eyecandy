﻿
using Microsoft.Extensions.Logging;
using OpenTK.Graphics.OpenGL;
using System.Runtime.InteropServices;

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
        /// Lock-section object for protecting access to GL texture-binding commands.
        /// </summary>
        public static object GLTextureLock = new();

        /// <summary>
        /// When true, the engine is actively capturing and processing audio.
        /// </summary>
        public bool IsCapturing { get => (AudioProcessor?.IsCapturing ?? 0) == 1; }

        /// <summary>
        /// DateTime.Now value when the texture data was updated from the underlying audio buffers.
        /// </summary>
        public DateTime TexturesUpdatedTimestamp = DateTime.MaxValue;

        /// <summary>
        /// True if the capture engine is currently detecting silence.
        /// </summary>
        public bool IsSilent = false;

        /// <summary>
        /// Start time of the most recently-detected slient period. If no silence has been
        /// detected yet, this will be DateTime.MaxValue.
        /// </summary>
        public DateTime SilenceStarted = DateTime.MaxValue;

        /// <summary>
        /// When IsSilent is false, the end time of the most recently-detected silent period.
        /// During active silent periods, or if no slience has been detected yet, this will
        /// be DateTime.MinValue.
        /// </summary>
        public DateTime SilenceEnded = DateTime.MinValue;

        private Dictionary<Type, AudioTexture> Textures = new();
        private Dictionary<Type, int> TextureUnitAssignments = new();
        private int MaximumTextureUnitNumber;
        private bool TextureHandlesInitialized = false;

        private AudioCaptureBase AudioProcessor;
        private Task AudioTask;
        private CancellationTokenSource ctsAudioProcessing;

        /// <summary>
        /// The constructor requries a configuration object. This object is stored and is accessible
        /// but should not be altered during program execution. Some settings are cached elsewhere
        /// for performance and/or thread-safety considerations and would not be updated.
        /// </summary>
        public AudioTextureEngine(EyeCandyCaptureConfig configuration)
        {
            if (configuration.LoopbackApi == LoopbackApi.WindowsInternal && !RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) 
                throw new ArgumentException($"{nameof(EyeCandyCaptureConfig.LoopbackApi)} WASAPI can only be used with Windows");

            // Instead of requiring the library consumer to manage TextureUnit assignments, query the
            // driver for the highest possible TU count, and hard-assign AudioTexture types to TUs in
            // descending order. The TU assignment is cached by type, so if a texture is created,
            // destroyed, and later created again, it will get the same TU assignment.
            GL.GetInteger(GetPName.MaxCombinedTextureImageUnits, out MaximumTextureUnitNumber);
            MaximumTextureUnitNumber -= 1; // TextureUnit is 0-based

            AudioProcessor = AudioCaptureBase.Factory(configuration);

            ErrorLogging.Logger?.LogTrace($"AudioTextureEngine: constructor completed");
        }

        /// <summary>
        /// Invokes the AudioCaptureProcessor on a background thread and begins updating any
        /// requested, enabled AudioTextures.
        /// </summary>
        public void BeginAudioProcessing()
        {
            ErrorLogging.Logger?.LogTrace($"AudioTextureEngine: BeginAudioProcessing");
            if(IsDisposed)
            {
                ErrorLogging.LibraryError($"{nameof(AudioTextureEngine)}.{nameof(BeginAudioProcessing)}", "Aborting, object has been disposed", LogLevel.Error);
                return;
            }

            if (IsCapturing)
            {
                ErrorLogging.LibraryError($"{nameof(AudioTextureEngine)}.{nameof(BeginAudioProcessing)}", "Invoked but already capturing audio.", LogLevel.Warning);
                return;
            }
            ctsAudioProcessing = new();
            AudioTask = Task.Run(() => AudioProcessor.Capture(ProcessAudioDataCallback, ctsAudioProcessing.Token));
        }

        /// <summary>
        /// Cancels the AudioCaptureProcessor background thread. The return value indicates
        /// whether this has completed within the specified timeout (default is 1000ms). Typically
        /// cancellation is nearly immediate (a few milliseconds). If it doesn't happen, callers
        /// can continue checking IsCapturing, which is a pass-through to the underlying state of
        /// AudioCaptureProcessor. The async Task based method was removed (instead of being marked
        /// obsolete) to avoid ambiguity.
        /// </summary>
        public bool EndAudioProcessing(int timeoutMS = 1000)
        {
            ErrorLogging.Logger?.LogTrace($"AudioTextureEngine: EndAudioProcessing");

            if (!IsCapturing) return true;
            ctsAudioProcessing.Cancel();
            using var ctsTimer = new CancellationTokenSource();
            ctsTimer.CancelAfter(timeoutMS);
            while (IsCapturing && !ctsTimer.IsCancellationRequested) Thread.Sleep(0);
            return !ctsTimer.IsCancellationRequested;
        }

        /// <summary>
        /// AudioTexture objects can't be created directly. This factory method ensures they are properly
        /// initialized and ready for use. Normally it isn't necessary to manipulate the AudioTexture objects
        /// directly.
        /// </summary>
        public void Create<AudioTextureType>(string uniformName, bool enabled = true)
        where AudioTextureType : AudioTexture
        {
            var type = typeof(AudioTextureType);

            if (Textures.ContainsKey(type))
            {
                var existingUniform = Textures[type].UniformName;
                if (uniformName.Equals(existingUniform, StringComparison.InvariantCultureIgnoreCase))
                {
                    ErrorLogging.Logger?.LogWarning($"{nameof(AudioTextureEngine.Create)}: AudioTexture of type {type} already exists.");
                    return;
                }

                throw new InvalidOperationException($"AudioTexture of type {type} already exists with a different uniform name ({existingUniform}), can't create as {uniformName}.");
            }

            if (Textures.Any(t => t.Value.UniformName.Equals(uniformName))) throw new ArgumentException($"AudioTexture uniform name {uniformName} already exists.");

            int assignedTextureUnit = 0;
            if(TextureUnitAssignments.ContainsKey(type))
            {
                assignedTextureUnit = TextureUnitAssignments[type];
            }
            else
            {
                assignedTextureUnit = MaximumTextureUnitNumber - TextureUnitAssignments.Count;
                TextureUnitAssignments.Add(type, assignedTextureUnit);
                ErrorLogging.Logger?.LogDebug($"Assigned {type} to TextureUnit {assignedTextureUnit}");
            }

            var texture = AudioTexture.Factory<AudioTextureType>(uniformName, assignedTextureUnit, enabled);
            Textures.Add(type, texture);
            EvaluateRequirements();
            if(IsCapturing) TextureHandlesInitialized = false;
            ErrorLogging.Logger?.LogDebug($"AudioTextureEngine: Created {type}");
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
            (Textures[type] as IDisposable).Dispose();
            Textures.Remove(type);
            EvaluateRequirements();
            ErrorLogging.Logger?.LogDebug($"AudioTextureEngine: Destroyed {type}");
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
            if (IsDisposed) return;

            // Short-circuit if the audio buffers haven't changed yet AND we've already generated textures at least once.
            if (AudioProcessor.Buffers.Timestamp < TexturesUpdatedTimestamp && TextureHandlesInitialized) return;

            foreach(var tex in Textures)
            {
                tex.Value.GenerateTexture();
            }

            TextureHandlesInitialized = true;
            TexturesUpdatedTimestamp = DateTime.Now;
        }

        /// <summary>
        /// This is invoked by the AudioCaptureProcessor thread whenever new audio sample data is available.
        /// </summary>
        public void ProcessAudioDataCallback()
        {
            if (IsDisposed) return;

            foreach (var t in Textures)
            {
                if (t.Value.Enabled) t.Value.UpdateChannelBuffer(AudioProcessor.Buffers);
            }

            if (IsSilent)
            {
                if(AudioProcessor.Buffers.SilenceStarted == DateTime.MaxValue)
                {
                    SilenceEnded = DateTime.Now;
                    IsSilent = false;
                }
            }
            else
            {
                if(AudioProcessor.Buffers.SilenceStarted != DateTime.MaxValue)
                {
                    SilenceStarted = AudioProcessor.Buffers.SilenceStarted;
                    SilenceEnded = DateTime.MinValue;
                    IsSilent = true;
                }
            }
        }

        /// <summary>
        /// Calls Shader.SetTexture for each of the currently-enabled AudioTexture objects.
        /// </summary>
        public void SetTextureUniforms(Shader shader)
        {
            if (IsDisposed) return;

            foreach (var t in Textures)
            {
                if (t.Value.Enabled) shader.SetTexture(t.Value);
            }
        }

        /// <summary>
        /// Generates an updated AudioProcessingRequirements structure and applies it to the AudioCaptureProcessor.
        /// This is primarily used internally when an AudioTexture is created, deleted, or the enabled state changes.
        /// </summary>
        public void EvaluateRequirements()
        {
            AudioProcessor.Requirements = new()
            {
                CalculateVolumeRMS = AudioCaptureBase.Configuration.DetectSilence || Textures.Any(t => t.Value.VolumeCalc == VolumeAlgorithm.RMS || t.Value.VolumeCalc == VolumeAlgorithm.All),
                CalculateFFTMagnitude = !Textures.All(t => t.Value.FrequencyCalc == FrequencyAlgorithm.NotApplicable),
                CalculateFFTDecibels = Textures.Any(t => t.Value.FrequencyCalc == FrequencyAlgorithm.Decibels || t.Value.FrequencyCalc == FrequencyAlgorithm.All),
                CalculateFFTWebAudioDecibels = Textures.Any(t => t.Value.FrequencyCalc == FrequencyAlgorithm.WebAudioDecibels || t.Value.FrequencyCalc == FrequencyAlgorithm.All),
            };
        }

        /// <summary/>
        public void Dispose()
        {
            if (IsDisposed) return;
            ErrorLogging.Logger?.LogTrace($"{GetType()}.Dispose() ----------------------------");

            if (IsCapturing)
            {
                ErrorLogging.LibraryError($"{nameof(AudioTextureEngine)}.Dispose", "Dispose invoked before audio processing was terminated. Attempting to force termination.");
                try
                {
                    if(!EndAudioProcessing()) ErrorLogging.LibraryError($"{nameof(AudioTextureEngine)}.Dispose", "Audio processing did not terminate within the default 1000ms timeout.");
                }
                catch (Exception ex)
                {
                    ErrorLogging.LibraryError($"{nameof(AudioTextureEngine)}.Dispose", $"{ex.GetType()}: {ex.Message}");
                }
            }

            foreach (var kvp in Textures)
            {
                ErrorLogging.Logger?.LogTrace($"  {GetType()}.Dispose() AudioTexture");
                (kvp.Value as IDisposable).Dispose();
            }
            Textures.Clear();

            ErrorLogging.Logger?.LogTrace($"  {GetType()}.Dispose() AudioProcessor");
            AudioProcessor?.Dispose();

            IsDisposed = true;
            GC.SuppressFinalize(this);
        }
        private bool IsDisposed = false;

        /// <summary/>
        [Obsolete("Use the synchronous EndAudioProcessing method.")]
        public void EndAudioProcessing_SynchronousHack()
            => EndAudioProcessing();
    }
}
