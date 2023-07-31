
namespace eyecandy
{
    /// <summary>
    /// Represents an audio texture containing WebAudio-style pseudo-decibel
    /// data in row 0, and history data in higher rows.
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
                    ChannelBuffer[green] = (float)audioBuffers.FrequencyWebAudio[x] / (float)AudioCaptureProcessor.Configuration.NormalizeWebAudioPeak * SampleMultiplier;
                }
            }
        }
    }
}
