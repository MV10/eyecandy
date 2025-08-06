using eyecandy;
using Serilog.Extensions.Logging;
using Serilog;

namespace demo
{
    internal class Program
    {
        public static bool StartFullScreen = false;

        public static bool WindowsUseOpenALSoft = false;

        private static readonly StringComparison SC = StringComparison.InvariantCultureIgnoreCase;

        internal static Microsoft.Extensions.Logging.ILogger Logger;

        static async Task Main(string[] args)
        {
            //if(args.Length == 0) args = new[]{ "modes" };

            if(args.Length == 0 || args.Length > 2)
            {
                Help();
                Environment.Exit(0);
            }

            // For demo purposes, force all error output to the console.
            ErrorLogging.Strategy = LoggingStrategy.AlwaysOutputToConsole;

            if(args.Length == 2)
            {
                StartFullScreen = args[1].Contains("F", SC);
                WindowsUseOpenALSoft = args[1].Contains("O", SC);
                if (args[1].Contains("P", SC)) Console.WriteLine($"\nPID {Environment.ProcessId}\n\n");
            }

            ConfigureLogging(Logger);

            switch (args[0].ToLower())
            {
                case "info":
                    await Info.Demo();
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

                case "logging":
                    Info.UseLogging = true;
                    await Info.Demo();
                    break;

                default:
                    Help();
                    break;
            }

            Log.CloseAndFlush();

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
            Console.WriteLine("logging\tWrites system details like \"info\" to demo.log");
            Console.WriteLine("\ninfo\t\tOpenAL information (devices, defaults, extensions, etc.)");
            Console.WriteLine("\t\t(Windows requires a loopback driver; no WASAPI equivalent)");

            Console.WriteLine("\n[options]");
            Console.WriteLine("F\t\tFull-screen mode");
            Console.WriteLine("P\t\tOutput Process ID");
            Console.WriteLine("O\t\tWindows: Capture audio with OpenAL-Soft instead of WASAPI");
        }

        public static void ConfigureLogging(Microsoft.Extensions.Logging.ILogger logger)
        {
            var logPath = Path.GetFullPath("./demo.log");
            if (File.Exists(logPath)) File.Delete(logPath);

            Console.WriteLine($"Logging to {logPath}");

            var cfg = new LoggerConfiguration()
                    .MinimumLevel.Verbose()
                    .WriteTo.Async(a => a.File(logPath, shared: true))
                    .WriteTo.Console();

            Log.Logger = cfg.CreateLogger();

            logger = new SerilogLoggerFactory().CreateLogger("eyecandy-demo");

            ErrorLogging.Logger = logger;
        }
    }
}