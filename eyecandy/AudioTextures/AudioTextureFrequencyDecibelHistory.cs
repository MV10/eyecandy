﻿
namespace eyecandy
{
    /// <summary>
    /// Represents an audio texture containing FFT frequency decibel
    /// data in row 0, and history data in higher rows.
    /// </summary>
    public class AudioTextureFrequencyDecibelHistory : AudioTexture
    {
        /// <inheritdoc/>
        public AudioTextureFrequencyDecibelHistory()
        {
            PixelWidth = AudioCaptureBase.Configuration.SampleSize;
            Rows = AudioCaptureBase.Configuration.HistorySize;

            FrequencyCalc = FrequencyAlgorithm.Decibels;
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
                    ChannelBuffer[green] = (float)audioBuffers.FrequencyDecibels[x] / (float)AudioCaptureBase.Configuration.NormalizeFrequencyDecibelsPeak;
                }
            }
        }
    }
}
