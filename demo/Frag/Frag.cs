using eyecandy;

/*

A frag (aka pixel) shader like Shadertoy.com. The vertex data is just a big quad
covering the entire display area. All of the work goes into the frag shader. This
reproduces my simple mic/loopback example here:

https://www.shadertoy.com/view/mdScDh

Hit ESC to exit.
 
Stick with 16:9 aspect-ratio resolutions for consistency when going full-screen
https://www.studio1productions.com/Articles/16x9-Resolution.htm

*/

namespace demo
{
    internal class Frag
    {
        public static async Task Demo()
        {
            Console.WriteLine("\n\nfrag: A Shadertoy-like fragment / pixel shader");

            var audioConfig = new EyeCandyCaptureConfig();

            var windowConfig = new EyeCandyWindowConfig();
            windowConfig.OpenTKNativeWindowSettings.Title = "Eyecandy Demo: Fragment / Pixel Shader";
            windowConfig.OpenTKNativeWindowSettings.Size = (960, 540);
            windowConfig.StartFullScreen = Program.StartFullScreen;

            // remember Linux is case-sensitive...
            windowConfig.VertexShaderPathname = "Frag/frag.vert";
            windowConfig.FragmentShaderPathname = "Frag/frag.frag";

            var win = new FragWindow(windowConfig, audioConfig);
            win.Focus();
            win.Run();
            win.Dispose();

        }
    }
}
