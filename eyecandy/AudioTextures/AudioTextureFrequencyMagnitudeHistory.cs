
namespace eyecandy
{
    public class AudioTextureFrequencyMagnitudeHistory : AudioTexture
    {
        public AudioTextureFrequencyMagnitudeHistory()
        {
            PixelWidth = AudioCaptureProcessor.Configuration.SampleSize;
            Rows = AudioCaptureProcessor.Configuration.HistorySize;

            FrequencyCalc = FrequencyAlgorithm.Magnitude;
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
                    ChannelBuffer[green] = (float)audioBuffers.FrequencyMagnitude[x] / (float)AudioCaptureProcessor.Configuration.NormalizeFrequencyMagnitudePeak;
                }
            }
        }
    }
}
