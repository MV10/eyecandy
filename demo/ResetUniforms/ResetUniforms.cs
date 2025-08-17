using eyecandy;

/*

A frag (aka pixel) shader like Shadertoy.com. The vertex data is just a big quad
covering the entire display area. All of the work goes into the frag shader. This
reproduces my simple mic/loopback example here:

https://www.shadertoy.com/view/mdScDh

Hit ESC to exit.
 
*/

namespace demo;

internal class ResetUniforms
{
    public static async Task Demo()
    {
        Console.WriteLine("\n\nuniforms: Testing the Shader.ResetUniforms method");
        Console.WriteLine("\nDraws a line at a randomly-determined Y-coordinate.");
        Console.WriteLine("SPACE\tRandomly assign a value to the uniform");
        Console.WriteLine("R\tReset uniform to the default value (0.5, center of screen)");

        var audioConfig = new EyeCandyCaptureConfig();
        if (Program.WindowsUseOpenALSoft) audioConfig.LoopbackApi = LoopbackApi.OpenALSoft;

        var windowConfig = new EyeCandyWindowConfig();
        windowConfig.OpenTKNativeWindowSettings.Title = "Eyecandy Demo: Shader.ResetUniforms";
        windowConfig.OpenTKNativeWindowSettings.ClientSize = (960, 540);
        windowConfig.OpenTKNativeWindowSettings.APIVersion = new Version(4, 6);
        windowConfig.StartFullScreen = Program.StartFullScreen;

        // remember Linux is case-sensitive...
        windowConfig.VertexShaderPathname = "ResetUniforms/uniforms.vert";
        windowConfig.FragmentShaderPathname = "ResetUniforms/uniforms.frag";

        var win = new UniformsWindow(windowConfig, audioConfig);
        win.Focus();
        win.Run();
        win.Dispose();

    }
}
