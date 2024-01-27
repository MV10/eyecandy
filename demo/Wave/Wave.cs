using eyecandy;

/*

A simple audio-reactive shader that shows the raw PCM wave data, colorized
according to volume (although the wave "strength" is also volume-driven).

Hit ESC to exit.
 
Stick with 16:9 aspect-ratio resolutions for consistency when going full-screen
https://www.studio1productions.com/Articles/16x9-Resolution.htm

*/

namespace demo
{
    internal class Wave
    {
        public static async Task Demo()
        {
            Console.WriteLine("\n\nwave: Simple audio-reactive shader driven by raw PCM wave audio data");

            var audioConfig = new EyeCandyCaptureConfig();
            if (Program.WindowsUseOpenALSoft) audioConfig.LoopbackApi = LoopbackApi.OpenALSoft;

            var windowConfig = new EyeCandyWindowConfig();
            windowConfig.OpenTKNativeWindowSettings.Title = "Eyecandy Demo: Raw PCM Wave Audio";
            windowConfig.OpenTKNativeWindowSettings.ClientSize = (960, 540);
            windowConfig.StartFullScreen = Program.StartFullScreen;

            // remember Linux is case-sensitive...
            windowConfig.VertexShaderPathname = "Wave/wave.vert";
            windowConfig.FragmentShaderPathname = "Wave/wave.frag";

            var win = new WaveWindow(windowConfig, audioConfig);
            win.Focus();
            win.Run();
            win.Dispose();
        }
    }
}
