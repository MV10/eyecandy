﻿
using System.Runtime.InteropServices;

namespace eyecandy
{
    /// <summary>
    /// AudioCaptureProcessor settings.
    /// </summary>
    public class EyeCandyCaptureConfig
    {
        /// <summary>
        /// Controls how loopback audio is captured. WindowsInternal relies on the WASAPI multimedia layer.
        /// OpenALSoft works on Windows or Linux, but requires external loopback support (a driver for Windows,
        /// and manual OS configuration changes on Linux such as PulseAudio settings). Windows systems will
        /// default to WindowsInternal, otherwise the default is OpenALSoft.
        /// </summary>
        public LoopbackApi LoopbackApi { get; set; } = 
            (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            ? LoopbackApi.WindowsInternal
            : LoopbackApi.OpenALSoft;

        /// <summary>
        /// Currently only supported for OpenAL-Soft. WindowsInternal will use the default.
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
        /// for the Frequency Magnitude data to produce the normalized range. Use the demo "peaks"
        /// option to analyze audio that is typical for your usage.
        /// </summary>
        public double NormalizeFrequencyMagnitudePeak { get; set; } = 6500;

        /// <summary>
        /// Texture data is normalized (values range from 0.0 to 1.0). This defines the divisor
        /// for the Frequency Decibels data to produce the normalized range. Use the demo "peaks"
        /// option to analyze audio that is typical for your usage.
        /// </summary>
        public double NormalizeFrequencyDecibelsPeak { get; set; } = 90;

        /// <summary>
        /// Texture data is normalized (values range from 0.0 to 1.0). This defines the divisor
        /// for the WebAudio data to produce the normalized range. Unlike the other normalization
        /// divisors, this was produced by visually comparing eyecandy data to Shadertoy output.
        /// </summary>
        public double NormalizeWebAudioPeak { get; set; } = 60;

        /// <summary>
        /// WebAudio applies a strange time-domain smoothing of the Frequency Magnitude data
        /// used to derive the Decibels output. The API specification defaults to 0.8.
        /// </summary>
        public double WebAudioSmoothingFactor { get; set; } = 0.8;

        /// <summary>
        /// When true, RMS volume is calculated even if no volume texture is enabled.
        /// </summary>
        public bool DetectSilence { get; set; } = true;

        /// <summary>
        /// Maximum RMS volume for slience-detection.
        /// </summary>
        public double MaximumSilenceRMS { get; set; } = 1.5;
    }
}
