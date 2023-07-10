
namespace eyecandy
{
    /// <summary>
    /// Shadertoy uses a two-row texture. The first row (y=0) is frequency data,
    /// the second row (y=1) is raw PCM wave data. In the frag shader sampler,
    /// use 0.25 and 0.75 to hit the middle of these rows. The resolution and
    /// representation of the audio data in eyecandy is slightly different from
    /// the WebAudio data used by the Shadertoy website. Also, Shadertoy data is
    /// in the x channel, but these textures are RGBA and the data is in the green
    /// channel (the eyecandy default).
    /// </summary>
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
                    int y1green = y0green + BufferWidth;

                    ChannelBuffer[y0green] = (float)audioBuffers.FrequencyMagnitude[x] / (float)AudioCaptureProcessor.Configuration.NormalizeFrequencyMagnitudePeak;
                    ChannelBuffer[y1green] = (float)audioBuffers.Wave[x] / (float)short.MaxValue;
                }
            }
        }
    }
}
