using eyecandy;

/*

A simple vertex shader that cycles through the various drawing modes. The
triangle representation isn't great.

points
line
line-strip
line-loop
triangle
triangle-strip
triangle-fan

Hit the spacebar to cycle.

Hit ESC to exit.
 
Stick with 16:9 aspect-ratio resolutions for consistency when going full-screen
https://www.studio1productions.com/Articles/16x9-Resolution.htm

*/

namespace demo
{
    internal class Modes
    {
        public static async Task Demo()
        {
            Console.WriteLine("\n\nmodes: Simple demonstration of OpenGL drawing modes");
            Console.WriteLine("\nPress spacebar to cycle through the modes.");

            var audioConfig = new EyeCandyCaptureConfig();

            var windowConfig = new EyeCandyWindowConfig();
            windowConfig.OpenTKNativeWindowSettings.Title = "Eyecandy Demo: OpenGL Drawing Modes";
            windowConfig.OpenTKNativeWindowSettings.Size = (960, 540);
            windowConfig.StartFullScreen = Program.StartFullScreen;

            // remember Linux is case-sensitive...
            windowConfig.VertexShaderPathname = "Modes/modes.vert";
            windowConfig.FragmentShaderPathname = "Modes/modes.frag";

            var win = new ModesWindow(windowConfig, audioConfig);
            win.Focus();
            win.Run();
            win.Dispose();
        }
    }
}
