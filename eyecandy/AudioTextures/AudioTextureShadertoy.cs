
namespace eyecandy
{
    public class AudioTextureShadertoy : AudioTexture
    {
        public AudioTextureShadertoy()
        {
            PixelWidth = AudioCaptureProcessor.Configuration.SampleSize;
            Rows = 2;

            FrequencyCalc = FrequencyAlgorithm.Magnitude;
        }

        public override void UpdateChannelBuffer(AudioData audioBuffers)
        {
            lock (ChannelBufferLock)
            {
                // TODO optimize the loop to eliminate the green calc
                for (int x = 0; x < PixelWidth; x++)
                {
                    int y0green = (x * AudioTextureEngine.RGBAPixelSize) + 1;
                    ChannelBuffer[y0green] = (float)audioBuffers.Wave[x] / (float)short.MaxValue;

                    int y1green = y0green + BufferWidth;
                    ChannelBuffer[y1green] = (float)audioBuffers.FrequencyMagnitude[x] / (float)AudioCaptureProcessor.Configuration.NormalizeFrequencyMagnitudePeak;
                }
            }
        }
    }
}
