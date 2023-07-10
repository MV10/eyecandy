
namespace eyecandy
{
    /// <summary>
    /// Represents an audio texture containing FFT frequency decibel
    /// data in row 0, and history data in higher rows.
    /// </summary>
    public class AudioTextureFrequencyDecibelHistory : AudioTexture
    {
        public AudioTextureFrequencyDecibelHistory()
        {
            PixelWidth = AudioCaptureProcessor.Configuration.SampleSize;
            Rows = AudioCaptureProcessor.Configuration.HistorySize;

            FrequencyCalc = FrequencyAlgorithm.Decibels;
        }

        public override void UpdateChannelBuffer(AudioData audioBuffers)
        {
            lock (ChannelBufferLock)
            {
                ScrollHistoryBuffer();

                // TODO optimize the loop to eliminate the green calc
                for (int x = 0; x < PixelWidth; x++)
                {
                    int green = (x * AudioTextureEngine.RGBAPixelSize) + 1;
                    ChannelBuffer[green] = (float)audioBuffers.FrequencyDecibels[x] / (float)AudioCaptureProcessor.Configuration.NormalizeFrequencyDecibelsPeak * SampleMultiplier;
                }
            }
        }
    }
}
