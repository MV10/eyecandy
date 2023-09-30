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
        internal static Microsoft.Extensions.Logging.ILogger Logger;

        public static async Task Demo()
        {
            Console.WriteLine("\n\nhistory: Basic audio capture history texture visualization");

            Program.ConfigureLogging(Logger);

            Console.WriteLine("\nDuring playback:\n");
            Console.WriteLine("ESC\tEnd program");
            Console.WriteLine(" P\tRaw PCM wave data");
            Console.WriteLine(" V\tRMS volume (realtime)");
            Console.WriteLine(" F\tFrequency magnitude");
            Console.WriteLine(" D\tFrequency decibels");
            Console.WriteLine(" 4\tCombined 4ch buffer");
            Console.WriteLine(" W\tWebAudio API (time-smooted decibel freq)");

            var audioConfig = new EyeCandyCaptureConfig();

            var windowConfig = new EyeCandyWindowConfig();
            windowConfig.OpenTKNativeWindowSettings.Title = "Eyecandy Demo: History Textures";
            windowConfig.OpenTKNativeWindowSettings.Size = (960, 540);
            windowConfig.StartFullScreen = Program.StartFullScreen;

            // remember Linux is case-sensitive...
            windowConfig.VertexShaderPathname = "History/history.vert";
            windowConfig.FragmentShaderPathname = "History/history.frag";

            var win = new HistoryWindow(windowConfig, audioConfig);
            win.Focus();
            win.Run();
            win.Dispose();
        }
    }
}
