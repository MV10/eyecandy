﻿
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
        /// Calculate frequency decibels the WebAudio API way (smoothed, with a
        /// 70dB window between -30dB and -100dB, and scaled 0 to 255)
        /// </summary>
        WebAudioDecibels = 3,

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
    /// Determines how loopback audio is processed.
    /// </summary>
    public enum LoopbackApi
    {
        /// <summary>
        /// WindowsInternal is WASAPI loopback provided by NAudio.
        /// </summary>
        WindowsInternal = 0,

        /// <summary>
        /// OpenAL-Soft works on Windows or Linux but requires external loopback support
        /// (a driver on Windows, or manual configuration on Linux such as PulseAudio).
        /// </summary>
        OpenALSoft = 1
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
