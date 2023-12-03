using eyecandy;

/*

A simple audio-reactive shader that scrolls Frequency and Volume history. This is also
a VertexShaderArt.com-style integer-stream vertex shader. Sort of a cross between
the History and Vert demos. It only outputs a narrow bass-focused frequency area.

Hit Enter to swap in a different shader which shows the entire frequency range, colored
according to volume. Enter swaps back.

Hit ESC to exit.
 
Stick with 16:9 aspect-ratio resolutions for consistency when going full-screen
https://www.studio1productions.com/Articles/16x9-Resolution.htm

*/

namespace demo
{
    internal class Freq
    {
        public static async Task Demo()
        {
            Console.WriteLine("\n\nfreq: Simple audio-reactive shader driven by Frequency and Volume history");
            Console.WriteLine("\n\nPress Enter to toggle between history-scroll and frequency-wave shaders...");

            var audioConfig = new EyeCandyCaptureConfig();

            var windowConfig = new EyeCandyWindowConfig();
            windowConfig.OpenTKNativeWindowSettings.Title = "Eyecandy Demo: Bass Frequency and Volume";
            windowConfig.OpenTKNativeWindowSettings.ClientSize = (960, 540);
            windowConfig.StartFullScreen = Program.StartFullScreen;

            // remember Linux is case-sensitive...
            windowConfig.VertexShaderPathname = "Freq/freq_scroll.vert";
            windowConfig.FragmentShaderPathname = "Freq/freq.frag";

            var win = new FreqWindow(windowConfig, audioConfig);
            win.Focus();
            win.Run();
            win.Dispose();
        }
    }
}
