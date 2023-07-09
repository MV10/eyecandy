using eyecandy;

/*

A simple text-based "visualizer" for the audio capture data and post-processing calcs.
Frequency decibels is a little sloppy but it does work. The output method is actually
invoked on a separate thread by the capture code, so occasionally switching to another
view will happen during rendering, which means the clear-screen doesn't work. Just hit
the key again.

W - Raw PCM wave data
V - RMS volume (realtime)
F - Frequency magnitude with the bass range shown in red
D - Frequency decibels with the bass range shown in red

Hit ESC to exit.

*/

namespace demo
{
    internal class Text
    {
        static EyeCandyCaptureConfig config;
        static AudioCaptureProcessor audio;

        static ConsoleKey key = ConsoleKey.W;

        public static async Task Demo()
        {
            Console.WriteLine("\n\ntext: Text-based audio capture data visualization");

            Console.WriteLine("\nDuring playback:\n");
            Console.WriteLine("ESC\tEnd program");
            Console.WriteLine(" W\tRaw PCM wave data");
            Console.WriteLine(" V\tRMS volume (realtime)");
            Console.WriteLine(" F\tFrequency magnitude (bass in red)");
            Console.WriteLine(" D\tFrequency decibels (bass in red)");

            Console.WriteLine("\n\nStart playback, press any key to begin capturing audio...");
            Console.ReadKey(true);

            config = new EyeCandyCaptureConfig();
            audio = new AudioCaptureProcessor(config)
            {
                Requirements = new()
                {
                    CalculateVolumeRMS = true,
                    CalculateFrequency = true,
                    CalculateFFTMagnitude = true,
                    CalculateFFTDecibels = true,
                }
            };

            var ctsAbortCapture = new CancellationTokenSource();
            var captureTask = Task.Run(() => audio.Capture(RenderToConsole, ctsAbortCapture.Token));

            while (key != ConsoleKey.Escape)
            {
                await Task.Delay(100);
                if(Console.KeyAvailable)
                {
                    var k = Console.ReadKey(true);
                    switch(k.Key)
                    {
                        case ConsoleKey.Escape:
                        case ConsoleKey.W:
                        case ConsoleKey.V:
                        case ConsoleKey.F:
                        case ConsoleKey.D:
                            Console.Clear();
                            key = k.Key;
                            break;

                        default:
                            break;
                    }
                }
            }

            ctsAbortCapture.Cancel();
            await captureTask;
            audio.Dispose();

            Console.Clear();
        }

        static void RenderToConsole()
        {
            double width = Console.WindowWidth;
            double centerline = Console.WindowWidth / 2;
            int spacing;

            switch (key)
            {
                case ConsoleKey.F:
                case ConsoleKey.D:
                    // for 1024 samples from a 44.1kHz signal, 100Hz bass is only the first 9-10 samples; draw them in red
                    for (int i = 0; i < Console.WindowHeight - 1; i++)
                    {
                        if(key == ConsoleKey.F)
                        {
                            spacing = (int)Math.Clamp((audio.Buffers.FrequencyMagnitude[i] / config.NormalizeFrequencyMagnitudePeak) * width * 1.5, 0, width - 1);
                        }
                        else
                        {
                            spacing = (int)Math.Clamp((audio.Buffers.FrequencyDecibels[i] / config.NormalizeFrequencyDecibelsPeak) * width * 1.5, 0, width - 1);
                        }
                        Console.SetCursorPosition(0, i);
                        Console.ForegroundColor = (i < 11) ? ConsoleColor.Red : ConsoleColor.White;
                        Console.WriteLine(">".PadLeft(spacing, '#').PadRight(Console.WindowWidth - spacing - 1));
                    }
                    break;

                case ConsoleKey.V:
                    spacing = (int)Math.Clamp((audio.Buffers.RealtimeRMSVolume / config.NormalizeRMSVolumePeak) * width, 0, width - 1);
                    Console.SetCursorPosition(0, 25);
                    Console.WriteLine(">".PadLeft(spacing, '#').PadRight(Console.WindowWidth - spacing - 1));
                    break;

                case ConsoleKey.W:
                    for (int i = 0; i < config.SampleSize; i++)
                    {
                        spacing = (int)(centerline + Math.Clamp(((double)audio.Buffers.Wave[i] / short.MaxValue) * centerline, -centerline, centerline));
                        Console.WriteLine("|".PadLeft(spacing));
                    }
                    break;

                default:
                    break;
            }
        }
    }
}
