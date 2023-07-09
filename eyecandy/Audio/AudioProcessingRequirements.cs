
namespace eyecandy.Audio
{
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
