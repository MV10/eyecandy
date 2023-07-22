
namespace eyecandy
{
    /// <summary>
    /// Convenience storage identifying which post-processing calculations
    /// should be performed by AudioCaptureProcessor after PCM samples are received.
    /// The property names are self-explanatory. These are initialize-only; any
    /// changes should generate and apply a new instance.
    /// </summary>
    public struct AudioProcessingRequirements
    {
        public bool CalculateVolumeRMS { get; init; } = false;
        public bool CalculateFrequency { get; init; } = false;
        public bool CalculateFFTMagnitude { get; init; } = false;
        public bool CalculateFFTDecibels { get; init; } = false;

        public AudioProcessingRequirements()
        { }
    }
}
