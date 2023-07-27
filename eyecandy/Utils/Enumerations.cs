
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
        Decibels = 2,

        /// <summary>
        /// Calcualte all frequency representations.
        /// </summary>
        All = 999
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
        RMS = 1,

        /// <summary>
        /// Calculate all volume representations.
        /// </summary>
        All = 999
    }

    /// <summary>
    /// Controls error logging for the library overall and OpenGL/OpenAL error-reporting.
    /// Errors are always written to an ILogger if one is provided via ErrorLogging.Logger.
    /// </summary>
    public enum LoggingStrategy
    {
        /// <summary>
        /// If an ILogger is present, that is the only log output. Otherwise, outputs to
        /// console.
        /// </summary>
        Automatic = 0,

        /// <summary>
        /// Output to console even if an ILogger is available. The ILogger is still used.
        /// </summary>
        AlwaysOutputToConsole = 1,

        /// <summary>
        /// Store even if an ILogger is available. The ILogger is still used.
        /// </summary>
        AlwaysStore = 2,
    }
}
