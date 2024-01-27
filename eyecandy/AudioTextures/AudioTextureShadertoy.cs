
namespace eyecandy
{
    /// <summary>
    /// Shadertoy uses a two-row texture. The first row (y=0) is WebAudio-style
    /// pseudo-decibel data, and the second row (y=1) is raw PCM wave data. In
    /// the frag shader sampler, use 0.25 and 0.75 to hit the middle of these rows.
    /// Shadertoy data is in the red channel (some code reads this via "x" instead),
    /// but these textures are RGBA and the data is in the green channel, as with all
    /// other single-element eyecandy textures. Like the real Shadertoy data, only
    /// half of the frequency data range is available (0-11025Hz). PCM wave data is
    /// represented as a normalized (0.0 to 1.0) range, with 0.5 representing silence.
    /// </summary>
    public class AudioTextureShadertoy : AudioTexture
    {
        /// <inheritdoc/>
        public AudioTextureShadertoy()
        {
            PixelWidth = AudioCaptureBase.Configuration.SampleSize;
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
                    float sample = (float)audioBuffers.FrequencyWebAudio[halfX];
                    if (x % 2 == 1) sample = (sample + (float)audioBuffers.FrequencyWebAudio[halfX + 1]) / 2f;

                    int y0green = (x * AudioTextureEngine.RGBAPixelSize) + 1;
                    ChannelBuffer[y0green] = sample / (float)AudioCaptureBase.Configuration.NormalizeWebAudioPeak;

                    int y1green = y0green + BufferWidth;
                    ChannelBuffer[y1green] = ((float)audioBuffers.Wave[x] / (float)short.MaxValue) / 2f + 0.5f;
                }
            }
        }
    }
}
