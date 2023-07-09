
namespace eyecandy
{
    public class AudioTextureVolumeHistory : AudioTexture
    {
        private static readonly int GreenChannel = 1;

        public AudioTextureVolumeHistory()
        {
            PixelWidth = 1;
            Rows = AudioCaptureProcessor.Configuration.HistorySize;

            VolumeCalc = VolumeAlgorithm.RMS;
        }

        public override void UpdateChannelBuffer(AudioData audioBuffers)
        {
            lock(ChannelBufferLock)
            {
                ScrollHistoryBuffer();
                var volume = (float)audioBuffers.RealtimeRMSVolume / (float)AudioCaptureProcessor.Configuration.NormalizeRMSVolumePeak;
                ChannelBuffer[GreenChannel] = volume;
            }
        }
    }
}
