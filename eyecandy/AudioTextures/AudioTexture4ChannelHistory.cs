
namespace eyecandy
{
    /// <summary>
    /// Represents a single audio texture that uses all four RGBA channels.
    /// Red is RMS volume, Green is raw PCM wave data, Blue is frequency magnitude,
    /// and Alpha is frequency decibels. History data is tracked.
    /// </summary>
    public class AudioTexture4ChannelHistory : AudioTexture
    {
        /// <inheritdoc/>
        public AudioTexture4ChannelHistory()
        {
            PixelWidth = AudioCaptureProcessor.Configuration.SampleSize;
            Rows = AudioCaptureProcessor.Configuration.HistorySize;

            VolumeCalc = VolumeAlgorithm.All;
            FrequencyCalc = FrequencyAlgorithm.All;
        }

        /// <inheritdoc/>
        public override void UpdateChannelBuffer(AudioData audioBuffers)
        {
            lock (ChannelBufferLock)
            {
                ScrollHistoryBuffer();

                for (int x = 0; x < PixelWidth; x++)
                {
                    int chan = (x * AudioTextureEngine.RGBAPixelSize);

                    // Red - RMS volume
                    ChannelBuffer[chan + 0] = (float)audioBuffers.RealtimeRMSVolume / (float)AudioCaptureProcessor.Configuration.NormalizeRMSVolumePeak * SampleMultiplier;

                    // Green - raw PCM wave
                    ChannelBuffer[chan + 1] = (float)audioBuffers.Wave[x] / (float)short.MaxValue * SampleMultiplier;

                    // Blue - frequency magnitude
                    ChannelBuffer[chan + 2] = (float)audioBuffers.FrequencyMagnitude[x] / (float)AudioCaptureProcessor.Configuration.NormalizeFrequencyMagnitudePeak * SampleMultiplier;

                    // Alpha - frequency decibels
                    ChannelBuffer[chan + 3] = (float)audioBuffers.FrequencyDecibels[x] / (float)AudioCaptureProcessor.Configuration.NormalizeFrequencyDecibelsPeak * SampleMultiplier;
                }
            }
        }
    }
}
