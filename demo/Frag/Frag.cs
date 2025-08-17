using eyecandy;

/*

A frag (aka pixel) shader like Shadertoy.com. The vertex data is just a big quad
covering the entire display area. All of the work goes into the frag shader. This
reproduces my simple mic/loopback example here:

https://www.shadertoy.com/view/mdScDh

Hit ESC to exit.
 
*/

namespace demo;

internal class Frag
{
    public static async Task Demo()
    {
        Console.WriteLine("\n\nfrag: A Shadertoy-like fragment / pixel shader");

        var config = new EyeCandyCaptureConfig();

        if (Program.UseSyntheticDataOnly) config.LoopbackApi = LoopbackApi.SyntheticData;

        if (Program.WindowsUseOpenALSoft) config.LoopbackApi = LoopbackApi.OpenALSoft;

        var windowConfig = new EyeCandyWindowConfig();
        windowConfig.OpenTKNativeWindowSettings.Title = "Eyecandy Demo: Fragment / Pixel Shader";
        //windowConfig.OpenTKNativeWindowSettings.ClientSize = (960, 540);
        windowConfig.OpenTKNativeWindowSettings.ClientSize = (640, 360); // match Shadertoy preview size for easy comparison
        windowConfig.StartFullScreen = Program.StartFullScreen;

        // remember Linux is case-sensitive...
        windowConfig.VertexShaderPathname = "Frag/frag.vert";
        windowConfig.FragmentShaderPathname = "Frag/frag.frag";

        var win = new FragWindow(windowConfig, config);
        win.Focus();
        win.Run();
        win.Dispose();

    }
}
