﻿
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
        /// Frequency data in WebAudio-style pseudo-decibels (time-domain smoothing, etc.)
        /// </summary>
        public double[] FrequencyWebAudio;

        /// <summary>
        /// A simplistic approximation of current volume based on a Root Mean Square 
        /// (RMS) calculation over time.
        /// </summary>
        public double RealtimeRMSVolume = 0;

        /// <summary>
        /// The amount of time silence has been detected. If MaxValue, not currently silent.
        /// </summary>
        public DateTime SilenceStarted = DateTime.MaxValue;

        /// <summary>
        /// The constructor allocates the buffer arrays according to the configured sample size.
        /// </summary>
        public AudioData()
        {
            Wave = new short[AudioCaptureBase.Configuration.SampleSize];
            FrequencyMagnitude = new double[AudioCaptureBase.Configuration.SampleSize];
            FrequencyDecibels = new double[AudioCaptureBase.Configuration.SampleSize];
            FrequencyWebAudio = new double[AudioCaptureBase.Configuration.SampleSize];
        }

        /// <summary>
        /// How long silence has been detected (only accurate since latest Timestamp update).
        /// </summary>
        public TimeSpan SilenceDuration()
            => SilenceStarted == DateTime.MaxValue ? TimeSpan.Zero : DateTime.Now.Subtract(SilenceStarted);
    }
}
