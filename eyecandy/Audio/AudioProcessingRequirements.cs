
namespace eyecandy
{
    /// <summary>
    /// Convenience storage identifying which post-processing calculations
    /// should be performed by AudioCaptureProcessor after PCM samples are received.
    /// The property names are self-explanatory. These are initialize-only; any
    /// changes should generate and apply a new instance.
    /// </summary>
    public readonly record struct AudioProcessingRequirements
    {
        /// <summary>
        /// When true, RMS volume is populated in the AudioData buffer.
        /// </summary>
        public bool CalculateVolumeRMS { get; init; } = false;

        /// <summary>
        /// When true, FFT Magnitude data is populated in the AudioData buffer. Since Magnitude
        /// is used to calculate Decibels and WebAudio's weird pseudo-decibel data, this is
        /// true if either of those are requested, too.
        /// </summary>
        public bool CalculateFFTMagnitude { get; init; } = false;

        /// <summary>
        /// When true, FFT Decibel data is populated in the AudioData buffer.
        /// </summary>
        public bool CalculateFFTDecibels { get; init; } = false;

        /// <summary>
        /// When true, FFT Decibel data is populated in the AudioData buffer using the
        /// WebAudio API alogorithm on top of the standard FFT Magnitude/Power calc.
        /// </summary>
        public bool CalculateFFTWebAudioDecibels { get; init; } = false;

        /// <summary>
        /// Constructor.
        /// </summary>
        public AudioProcessingRequirements()
        { }
    }
}
