
namespace eyecandy
{
    /// <summary>
    /// Indicates which audio FFT frequency calculation is needed, if any.
    /// </summary>
    public enum FrequencyAlgorithm
    {
        /// <summary>
        /// Do not calculate audio frequency data.
        /// </summary>
        NotApplicable = 0,
        
        /// <summary>
        /// Calculate frequency magnitude.
        /// </summary>
        Magnitude = 1,
        
        /// <summary>
        /// Calculate frequency decibels.
        /// </summary>
        Decibels = 2
    }

    /// <summary>
    /// Indicates which audio volume calculation is needed, if any.
    /// </summary>
    public enum VolumeAlgorithm
    {
        /// <summary>
        /// Do not calculate audio volume data.
        /// </summary>
        NotApplicable = 0,

        /// <summary>
        /// Calculate audio using the Root Mean Squared algorithm. The calculation
        /// window is controlled by the RMSVolumeMilliseconds property in the capture
        /// configuration object. The default is 300ms.
        /// </summary>
        RMS = 1
    }
}
