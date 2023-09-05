
namespace eyecandy
{
    /// <summary>
    /// Represents an audio texture containing FFT frequency magnitude
    /// data in row 0, and history data in higher rows.
    /// </summary>
    public class AudioTextureFrequencyMagnitudeHistory : AudioTexture
    {
        /// <inheritdoc/>
        public AudioTextureFrequencyMagnitudeHistory()
        {
            PixelWidth = AudioCaptureProcessor.Configuration.SampleSize;
            Rows = AudioCaptureProcessor.Configuration.HistorySize;

            FrequencyCalc = FrequencyAlgorithm.Magnitude;
        }

        /// <inheritdoc/>
        public override void UpdateChannelBuffer(AudioData audioBuffers)
        {
            lock (ChannelBufferLock)
            {
                ScrollHistoryBuffer();

                for (int x = 0; x < PixelWidth; x++)
                {
                    int green = (x * AudioTextureEngine.RGBAPixelSize) + 1;
                    ChannelBuffer[green] = (float)audioBuffers.FrequencyMagnitude[x] / (float)AudioCaptureProcessor.Configuration.NormalizeFrequencyMagnitudePeak;
                }
            }
        }
    }
}
