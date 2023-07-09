﻿namespace demo
{
    internal class Program
    {
        public static bool StartFullScreen = false;

        static async Task Main(string[] args)
        {
            //if(args.Length == 0) args = new[]{ "wave" };

            if(args.Length == 0 || args.Length > 2)
            {
                Help();
                Environment.Exit(0);
            }

            if(args.Length == 2)
            {
                StartFullScreen = args[1].Contains("F", StringComparison.InvariantCultureIgnoreCase);
            }

            switch(args[0].ToLower())
            {
                case "peaks":
                    await Peaks.Demo();
                    break;

                case "text":
                    await Text.Demo();
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
                    Console.WriteLine("TODO");
                    //await Frag.Demo();
                    break;

                case "info":
                    await Info.Demo();
                    break;

                default:
                    Help();
                    break;
            }
        }

        static void Help()
        {
            Console.WriteLine("\n\neyecandy demos:\n");
            Console.WriteLine("demo [type] [options]");

            Console.WriteLine("\n[type]");
            Console.WriteLine("peaks\t\tPeak audio capture values (use for configuration)");
            Console.WriteLine("text\t\tText-based audio visualizations");
            Console.WriteLine("history\t\tRaw history-texture dumps");
            Console.WriteLine("wave\t\tRaw PCM wave audio visualization");
            Console.WriteLine("freq\t\tFrequency magnitude and volume history (multiple shaders)");
            Console.WriteLine("vert\t\tVertexShaderArt-style integer-array vertex shader (no audio)");
            Console.WriteLine("frag\t\tShadertoy-style pixel fragment shader");
            Console.WriteLine("info\t\tOpenAL information (devices, defaults, extensions, etc.)");

            Console.WriteLine("\n[options]");
            Console.WriteLine("F\t\tFull-screen mode");
        }
    }
}