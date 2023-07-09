
namespace eyecandy
{
    public class EyeCandyCaptureConfig
    {
        /// <summary>
        /// The audio driver to use. Defaults to "OpenAL Soft" which is normally what
        /// you want, but "Generic Driver" is how the old Creative driver appears if
        /// OpenAL Soft is not installed for some reason.
        /// </summary>
        public string DriverName { get; set; } = "OpenAL Soft";

        /// <summary>
        /// Provide a device name or leave it empty to use the default device. On Linux the
        /// device may not be available unless playback is already running. It may be possible
        /// to configure a capture device as permanently available (ie. set as the default).
        /// </summary>
        public string CaptureDeviceName { get; set; } = string.Empty;

        /// <summary>
        /// Adjust with caution. Use a power of 2. Defines the number of PCM samples required
        /// before the buffer is processed, and also the width of the audio textures. Higher
        /// values are slower response with better accuracy, lower values are faster response
        /// with worse accuracy. Higher values are probably undesirable, and anything less than
        /// 512 is probably not useful.
        /// </summary>
        public int SampleSize { get; set; } = 1024;

        /// <summary>
        /// The total number of rows in any of the history-tracking TextureType textures. This
        /// includes row zero which is the realtime sample. Use a power of 2.
        /// </summary>
        public int HistorySize { get; set; } = 128;

        /// <summary>
        /// Typically this should not be changed. Volume is calculated as RMS (Root Mean Squared),
        /// this defines the period of time over which volume is averaged for a single output
        /// value. Normal human hearing perceives volume changes over a period of about 300ms.
        /// </summary>
        public int RMSVolumeMilliseconds { get; set; } = 300;

        /// <summary>
        /// Texture data is normalized (values range from 0.0 to 1.0). This defines the divisor
        /// for the raw RMS Volume data to produce the normalized range. Use the demo "peaks"
        /// option to analyze audio that is typical for your usage.
        /// </summary>
        public int NormalizeRMSVolumePeak { get; set; } = 100;

        /// <summary>
        /// Texture data is normalized (values range from 0.0 to 1.0). This defines the divisor
        /// for the raw FFT Frequency data to produce the normalized range. Use the demo "peaks"
        /// option to analyze audio that is typical for your usage.
        /// </summary>
        public double NormalizeFrequencyMagnitudePeak { get; set; } = 6500;

        /// <summary>
        /// Texture data is normalized (values range from 0.0 to 1.0). This defines the divisor
        /// for the raw FFT Frequency data to produce the normalized range. Use the demo "peaks"
        /// option to analyze audio that is typical for your usage.
        /// </summary>
        public double NormalizeFrequencyDecibelsPeak { get; set; } = 90;

        /// <summary>
        /// For audio textures which support it, this can make texture contents more visible
        /// for debug purposes. Defaults to 1.0 which is no magnification.
        /// </summary>
        public float DebugTextureIntensityMultiplier { get; set; } = 1.0f;
    }
}
