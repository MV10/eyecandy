﻿
namespace eyecandy
{
    /// <summary>
    /// Represents an audio texture containing the raw PCM wave
    /// data in row 0, and history data in higher rows. Values are both
    /// negative and positive.
    /// </summary>
    public class AudioTextureWaveHistory : AudioTexture
    {
        /// <inheritdoc/>
        public AudioTextureWaveHistory()
        {
            PixelWidth = AudioCaptureBase.Configuration.SampleSize;
            Rows = AudioCaptureBase.Configuration.HistorySize;
        }

        /// <inheritdoc/>
        public override void UpdateChannelBuffer(AudioData audioBuffers)
        {
            lock(ChannelBufferLock)
            {
                ScrollHistoryBuffer();

                for(int x = 0; x < PixelWidth; x++)
                {
                    int green = (x * AudioTextureEngine.RGBAPixelSize) + 1;
                    ChannelBuffer[green] = (float)audioBuffers.Wave[x] / (float)short.MaxValue;
                }
            }
        }
    }
}
