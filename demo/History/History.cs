using eyecandy;

/*

A trivial shader which only shows the raw history textures mapped to a rectangle.

W - Raw PCM wave data
V - RMS volume (realtime)
F - Frequency magnitude
D - Frequency decibels
4 - 4-channel data

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
            Console.WriteLine(" W\tRaw PCM wave data");
            Console.WriteLine(" V\tRMS volume (realtime)");
            Console.WriteLine(" F\tFrequency magnitude");
            Console.WriteLine(" D\tFrequency decibels");
            Console.WriteLine(" 4\tCombined 4ch buffer");

            var audioConfig = new EyeCandyCaptureConfig();

            var windowConfig = new EyeCandyWindowConfig();
            windowConfig.OpenTKNativeWindowSettings.Title = "Eyecandy Demo: History Textures";
            windowConfig.OpenTKNativeWindowSettings.Size = (960, 540);
            windowConfig.StartFullScreen = Program.StartFullScreen;
            windowConfig.BackgroundColor = new(0.2f, 0.4f, 0.4f, 1.0f);

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
