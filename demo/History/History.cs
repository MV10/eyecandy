using eyecandy;

/*

2023-09-30: This is also an example of log-file output (./demo.log)

A trivial shader which only shows the raw history textures mapped to a rectangle.

P - Raw PCM wave data
V - RMS volume (realtime)
F - Frequency magnitude
D - Frequency decibels
4 - 4-channel data
W - WebAudio API (time-smoothed decibel freq)

Hit ESC to exit.
 
Stick with 16:9 aspect-ratio resolutions for consistency when going full-screen
https://www.studio1productions.com/Articles/16x9-Resolution.htm

*/

namespace demo
{
    internal class History
    {
        public static async Task Demo()
        {
            Console.WriteLine("\n\nhistory: Basic audio capture history texture visualization");

            Console.WriteLine("\nDuring playback:\n");
            Console.WriteLine("ESC\tEnd program");
            Console.WriteLine(" P\tRaw PCM wave data");
            Console.WriteLine(" V\tRMS volume (realtime)");
            Console.WriteLine(" F\tFrequency magnitude");
            Console.WriteLine(" D\tFrequency decibels");
            Console.WriteLine(" 4\tCombined 4ch buffer");
            Console.WriteLine(" W\tWebAudio API (time-smooted decibel freq)");

            var config = new EyeCandyCaptureConfig();

            if (Program.UseSyntheticData) config.LoopbackApi = LoopbackApi.SyntheticData;

            if (Program.WindowsUseOpenALSoft) config.LoopbackApi = LoopbackApi.OpenALSoft;

            var windowConfig = new EyeCandyWindowConfig();
            windowConfig.OpenTKNativeWindowSettings.Title = "Eyecandy Demo: History Textures";
            windowConfig.OpenTKNativeWindowSettings.ClientSize = (960, 540);
            windowConfig.StartFullScreen = Program.StartFullScreen;

            // remember Linux is case-sensitive...
            windowConfig.VertexShaderPathname = "History/history.vert";
            windowConfig.FragmentShaderPathname = "History/history.frag";

            var win = new HistoryWindow(windowConfig, config);
            win.Focus();
            win.Run();
            win.Dispose();
        }
    }
}
