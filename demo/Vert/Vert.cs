using eyecandy;

/*

A simple vertex-only shader whose only inputs are a list of sequential
integer values. This reproduces the VertexShaderArt.com tutorials prior to
the point where audio support is used.

Hit ESC to exit.
 
Stick with 16:9 aspect-ratio resolutions for consistency when going full-screen
https://www.studio1productions.com/Articles/16x9-Resolution.htm

*/

namespace demo
{
    internal class Vert
    {
        public static async Task Demo()
        {
            Console.WriteLine("\n\nvert: Simple vertex-only integer-stream shader (no audio support)");

            var windowConfig = new EyeCandyWindowConfig();
            windowConfig.OpenTKNativeWindowSettings.Title = "Eyecandy Demo: Vert Int-Stream Shader";
            windowConfig.OpenTKNativeWindowSettings.Size = (960, 540);

            // remember Linux is case-sensitive...
            windowConfig.VertexShaderPathname = "Vert/vertdemo.vert";
            windowConfig.FragmentShaderPathname = "Vert/vertdemo.frag";

            var win = new VertWindow(windowConfig);
            win.Focus();
            win.Run();
            win.Dispose();
        }
    }
}
