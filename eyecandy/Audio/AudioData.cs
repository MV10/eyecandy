
namespace eyecandy
{
    /// <summary>
    /// A container for the various buffers used by AudioCaptureProcessor.
    /// </summary>
    public class AudioData
    {
        /// <summary>
        /// DateTime.Now timestamp when the buffers were updated.
        /// </summary>
        public DateTime Timestamp = DateTime.MaxValue;

        /// <summary>
        /// The raw PCM (wave) sample data in 16-bit mono.
        /// </summary>
        public short[] Wave;

        /// <summary>
        /// Frequency data magnitude.
        /// </summary>
        public double[] FrequencyMagnitude;

        /// <summary>
        /// Frequency data in decibels.
        /// </summary>
        public double[] FrequencyDecibels;

        /// <summary>
        /// A simplistic approximation of current volume based on a Root Mean Square 
        /// (RMS) calculation over time.
        /// </summary>
        public double RealtimeRMSVolume = 0;

        /// <summary>
        /// The constructor allocates the buffer arrays according to the configured sample size.
        /// </summary>
        public AudioData()
        {
            Wave = new short[AudioCaptureProcessor.Configuration.SampleSize];
            FrequencyMagnitude = new double[AudioCaptureProcessor.Configuration.SampleSize];
            FrequencyDecibels = new double[AudioCaptureProcessor.Configuration.SampleSize];
        }
    }
}
