using NAudio.CoreAudioApi;

namespace demo;
internal class InfoWASAPI
{
    public static async Task Demo()
    {
        Console.WriteLine("WASAPI Device Information (excluding \"Not Present\" devices)");
        Console.WriteLine("---------------------------------------------------------------");

        var enumerator = new MMDeviceEnumerator();

        var states = DeviceState.Active | DeviceState.Disabled | DeviceState.Unplugged;

        Console.Write("\n\nPlayback devices:\n  ");
        var playbackDevices = enumerator.EnumerateAudioEndPoints(DataFlow.Render, states);
        if (playbackDevices.Count > 0) Console.WriteLine(string.Join("\n  ", playbackDevices.Select(d => $"{d.FriendlyName} ({d.State})")));
        if (playbackDevices.Count == 0) Console.WriteLine("  <none>");

        Console.Write("\n\nCapture devices:\n  ");
        var captureDevices = enumerator.EnumerateAudioEndPoints(DataFlow.Capture, states);
        if (captureDevices.Count > 0) Console.WriteLine(string.Join("\n  ", captureDevices.Select(d => $"{d.FriendlyName} ({d.State})")));
        if (captureDevices.Count == 0) Console.WriteLine("  <none>");

        Console.WriteLine("\n\nDefault devices:");
        try
        {
            var defaultPlayback = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            Console.WriteLine($"  Playback: {defaultPlayback.FriendlyName}");
        }
        catch
        {
            Console.WriteLine("  Playback: <none>");
        }
        try
        {
            var defaultCapture = enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Multimedia);
            Console.WriteLine($"  Capture:  {defaultCapture.FriendlyName}");
        }
        catch
        {
            Console.WriteLine("  Capture:  <none>");
        }
    }
}
