
namespace eyecandy
{
    public class AudioTextureWaveHistory : AudioTexture
    {
        public AudioTextureWaveHistory()
        {
            PixelWidth = AudioCaptureProcessor.Configuration.SampleSize;
            Rows = AudioCaptureProcessor.Configuration.HistorySize;
        }

        public override void UpdateChannelBuffer(AudioData audioBuffers)
        {
            //lock(ChannelBufferLock)
            {
                ScrollHistoryBuffer();

                // TODO optimize the loop to eliminate the green calc
                for(int x = 0; x < PixelWidth; x++)
                {
                    int green = (x * AudioTextureEngine.RGBAPixelSize) + 1;
                    ChannelBuffer[green] = (float)audioBuffers.Wave[x] / (float)short.MaxValue;
                    ChannelBuffer[green] *= AudioCaptureProcessor.Configuration.DebugTextureIntensityMultiplier;
                }
            }
        }
    }
}
