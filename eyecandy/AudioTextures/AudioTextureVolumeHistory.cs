
namespace eyecandy;

/// <summary>
/// Represents an audio texture containing RMS volume data in row 0, 
/// and history data in higher rows. Because this texture is one pixel
/// wide, sample the data at x=0.5 in the shader.
/// </summary>
public class AudioTextureVolumeHistory : AudioTexture
{
    private static readonly int GreenChannel = 1;

    /// <inheritdoc/>
    public AudioTextureVolumeHistory()
    {
        PixelWidth = 1;
        Rows = AudioCaptureBase.Configuration.HistorySize;

        VolumeCalc = VolumeAlgorithm.RMS;
    }

    /// <inheritdoc/>
    public override void UpdateChannelBuffer(AudioData audioBuffers)
    {
        lock(ChannelBufferLock)
        {
            ScrollHistoryBuffer();

            ChannelBuffer[GreenChannel] = (float)audioBuffers.RealtimeRMSVolume / (float)AudioCaptureBase.Configuration.NormalizeRMSVolumePeak;
        }
    }
}
