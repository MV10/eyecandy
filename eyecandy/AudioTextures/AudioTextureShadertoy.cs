
namespace eyecandy
{
    /// <summary>
    /// Shadertoy uses a two-row texture. The first row (y=0) is frequency data,
    /// the second row (y=1) is raw PCM wave data. In the frag shader sampler,
    /// use 0.25 and 0.75 to hit the middle of these rows. The resolution and
    /// representation of the audio data in eyecandy is slightly different from
    /// the WebAudio data used by the Shadertoy website. Also, Shadertoy data is
    /// in the x channel, but these textures are RGBA and the data is in the green
    /// channel (the eyecandy default). Any SampleMultiplier is only applied to the
    /// frequency data, the raw PCM data is not altered. Like the real Shadertoy
    /// data, only half of the frequency data range is available (0-11025Hz).
    /// </summary>
    public class AudioTextureShadertoy : AudioTexture
    {
        /// <inheritdoc/>
        public AudioTextureShadertoy()
        {
            PixelWidth = AudioCaptureProcessor.Configuration.SampleSize;
            Rows = 2;

            FrequencyCalc = FrequencyAlgorithm.WebAudioDecibels;
        }

        /// <inheritdoc/>
        public override void UpdateChannelBuffer(AudioData audioBuffers)
        {
            lock (ChannelBufferLock)
            {
                for (int x = 0; x < PixelWidth; x++)
                {

                    // Shadertoy actually discards the upper half of the frequency spectrum and uses
                    // a half-size texture (512 texels), so we replicate this by interpolating so that
                    // we still output a full-width texture (1024 texels).
                    //
                    // array index:  00 01 02 03 04 05 06 07 08 09
                    // buffer data:   A  B  C  D  E  F  G  H  I  J
                    //
                    // array index:  00 01 02 03 04 05 06 07 08 09
                    // output data:   A AB  B BC  C CD  D DE  E EF
                    int halfX = x / 2;
                    float sample = (float)audioBuffers.FrequencyWebAudioDecibels[halfX];
                    if (x % 2 == 1) sample = (sample + (float)audioBuffers.FrequencyWebAudioDecibels[halfX]) / 2f;

                    int y0green = (x * AudioTextureEngine.RGBAPixelSize) + 1;
                    ChannelBuffer[y0green] = sample / (float)AudioCaptureProcessor.Configuration.NormalizeFrequencyDecibelsPeak * SampleMultiplier;

                    int y1green = y0green + BufferWidth;
                    ChannelBuffer[y1green] = (float)audioBuffers.Wave[x] / (float)short.MaxValue;

                }
            }
        }
    }
}
