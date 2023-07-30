
namespace eyecandy
{
    /// <summary>
    /// Represents an audio texture containing smoothed decibel
    /// data in row 0, and history data in higher rows. This is not true
    /// WebAudio API data, which apparently also involves clamping to a
    /// 70dB range (-30dB to -100dB) and scaling to a 0-255 range, but
    /// applying those looks NOTHING like the frequency audio data on
    /// Shadertoy, whereas this is "usefully" similar (Shadertoy still
    /// exhibits more significant amplitude variances).
    /// </summary>
    public class AudioTextureWebAudioHistory : AudioTexture
    {
        /// <inheritdoc/>
        public AudioTextureWebAudioHistory()
        {
            PixelWidth = AudioCaptureProcessor.Configuration.SampleSize;
            Rows = AudioCaptureProcessor.Configuration.HistorySize;

            FrequencyCalc = FrequencyAlgorithm.WebAudioDecibels;
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
                    ChannelBuffer[green] = (float)audioBuffers.FrequencyWebAudioDecibels[x] / (float)AudioCaptureProcessor.Configuration.NormalizeFrequencyDecibelsPeak * SampleMultiplier;
                }
            }
        }
    }
}
