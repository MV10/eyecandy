using eyecandy;
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;

namespace demo;

internal class Program
{
    public static bool StartFullScreen = false;

    public static bool WindowsUseOpenALSoft = false;

    public static bool SimulateOpenGLErrors = false;

    public static bool UseSyntheticDataOnly = false;

    private static readonly StringComparison IgnoreCase = StringComparison.InvariantCultureIgnoreCase;

    private static LogLevel MinimumLogLevel = LogLevel.Error;

    internal static ILogger Logger;

    static async Task Main(string[] args)
    {
        if(args.Length == 0 || args.Length > 2)
        {
            Help();
            Environment.Exit(0);
        }

        Console.WriteLine($"\nPID {Environment.ProcessId}\n\n");

        if (args.Length == 2)
        {
            StartFullScreen = args[1].Contains("F", IgnoreCase);
            SimulateOpenGLErrors = args[1].Contains("E", IgnoreCase);
            UseSyntheticDataOnly = args[1].Contains("S", IgnoreCase);
            WindowsUseOpenALSoft = args[1].Contains("O", IgnoreCase);

            if (args[1].Contains("D", IgnoreCase)) MinimumLogLevel = LogLevel.Debug;
            if (args[1].Contains("V", IgnoreCase)) MinimumLogLevel = LogLevel.Trace;
        }

        // For demo purposes, force all error output to the console.
        using var loggerFactory = LoggerFactory.Create(config =>
        {
            config
            .AddSimpleConsole(options => options.SingleLine = true)
            .SetMinimumLevel(MinimumLogLevel);
        });
        ErrorLogging.LoggerFactory = loggerFactory;

        switch (args[0].ToLower())
        {
            case "info":
                if(WindowsUseOpenALSoft || RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    await InfoOpenAL.Demo();
                }
                else
                {
                    await InfoWASAPI.Demo();
                }
                break;

            case "peaks":
                await Peaks.Demo();
                break;

            case "text":
                await Text.Demo();
                break;

            case "silence":
                await Silence.Demo();
                break;

            case "history":
                await History.Demo();
                break;

            case "wave":
                await Wave.Demo();
                break;

            case "freq":
                await Freq.Demo();
                break;

            case "vert":
                await Vert.Demo();
                break;

            case "frag":
                await Frag.Demo();
                break;

            case "webaudio":
                await Decibels.Demo();
                break;

            case "modes":
                await Modes.Demo();
                break;

            case "uniforms":
                await ResetUniforms.Demo();
                break;

            default:
                Help();
                break;
        }

        // give the console time to output everything :(
        await Task.Delay(250);
    }

    static void Help()
    {
        Console.WriteLine("\n\neyecandy demos:\n");
        Console.WriteLine("demo [type] [options]");

        Console.WriteLine("\n[type]");
        Console.WriteLine("peaks\t\tPeak audio capture values (use for configuration)");
        Console.WriteLine("text\t\tText-based audio visualizations");
        Console.WriteLine("silence\t\tSilence-detection testing");
        Console.WriteLine("history\t\tRaw history-texture dumps");
        Console.WriteLine("wave\t\tRaw PCM wave audio visualization");
        Console.WriteLine("freq\t\tFrequency magnitude and volume history (multiple shaders)");
        Console.WriteLine("vert\t\tVertexShaderArt-style integer-array vertex shader (no audio)");
        Console.WriteLine("frag\t\tShadertoy-style pixel fragment shader");
        Console.WriteLine("webaudio\tCompares WebAudio pseudo-Decibels to pure FFT Decibels");
        Console.WriteLine("modes\t\tDifferent OpenGL drawing modes (points, lines, tris, etc)");
        Console.WriteLine("uniforms\tTesting the Shader.ResetUniforms call");
        Console.WriteLine("\ninfo\t\tList known audio devices (Windows: WASAPI, Linux: OpenAL)");
        Console.WriteLine("info O\t\tWindows: Use OpenAL instead of WASAPI (requires loopback)");

        Console.WriteLine("\n[options]");
        Console.WriteLine("F\t\tFull-screen mode");
        Console.WriteLine("S\t\tSimulate audio with the SyntheticData wave sample source");
        Console.WriteLine("E\t\tSimulate OpenGL errors (currently only \"freq\" does this)");
        Console.WriteLine("D\t\tShow Debug log messages (default is Error/Critical)");
        Console.WriteLine("V\t\tShow Verbose log messages (default is Error/Critical)");
        Console.WriteLine("O\t\tCapture Windows audio with OpenAL (requires loopback driver)");
    }
}