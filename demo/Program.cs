namespace demo
{
    internal class Program
    {
        public static bool StartFullScreen = false;

        static async Task Main(string[] args)
        {
            args = new[]{ "history" };

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

                //case "wave":
                //    await Wave.Demo();
                //    break;

                //case "vert":
                //    await Vert.Demo();
                //    break;

                //case "frag":
                //    await Frag.Demo();
                //    break;

                //case "test":
                //    await Test.Demo();
                //    break;

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
            Console.WriteLine("wave\t\tPCM wave audio visualization");
            Console.WriteLine("vert\t\tVertexShaderArt-style integer-array vertex shader (no audio)");
            Console.WriteLine("frag\t\tShadertoy-style pixel fragment shader");
            Console.WriteLine("test\t\t??? (whatever I happen to be testing at the moment)");

            Console.WriteLine("\n[options]");
            Console.WriteLine("F\t\tFull-screen mode");
        }
    }
}