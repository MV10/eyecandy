using eyecandy;

/*

Reports the current status of the capture engine's silence-detection code and
calculates a few statistics.

Spacebar  Reset statistics
Up/Down   Adjust RMS volume silence threshold by 0.25
ESC       Exit
 
*/

namespace demo;

internal class Silence
{
    static EyeCandyCaptureConfig config;
    static AudioCaptureBase audio;

    static double nowRMSVolume = double.MinValue;
    static double minRMSVolume = double.MaxValue;
    static double maxRMSVolume = double.MinValue;
    static bool IsSilent = false;
    static DateTime SilenceStarted = DateTime.MaxValue;
    static DateTime SilenceEnded = DateTime.MinValue;

    public static async Task Demo()
    {
        Console.WriteLine("\n\nsilence: Utility to test the capture engine's silence-detection code.\n");
        Console.WriteLine("  Spacebar  Reset statistics");
        Console.WriteLine("  Up/Down   Adjust RMS volume silence threshold by 0.25");
        Console.WriteLine("  ESC       Exit");
        Console.WriteLine("\n\nStart playback and press any key to begin capturing audio....");
        Console.ReadKey(true);

        config = new EyeCandyCaptureConfig()
        {
            DetectSilence = true,
        };

        audio = AudioCaptureBase.Factory(config);
        audio.Requirements = new()
        {
            CalculateVolumeRMS = true,
        };

        var defaultThreshold = config.MaximumSilenceRMS;

        Console.Clear();
        Console.WriteLine("Capture starting...");

        var ctsAbortCapture = new CancellationTokenSource();
        var captureTask = Task.Run(() => audio.Capture(CheckSamples, ctsAbortCapture.Token));

        var exit = false;
        while(!exit)
        {
            while (!Console.KeyAvailable)
            {
                await Task.Delay(100);
                Console.Clear();
                Report();
            }
            var key = Console.ReadKey(true);
            switch(key.Key)
            {
                case ConsoleKey.Escape:
                    exit = true;
                    break;

                case ConsoleKey.UpArrow:
                    AudioCaptureBase.Configuration.MaximumSilenceRMS += 0.25d;
                    break;

                case ConsoleKey.DownArrow:
                    AudioCaptureBase.Configuration.MaximumSilenceRMS = Math.Min(0, AudioCaptureBase.Configuration.MaximumSilenceRMS - 0.25);
                    break;

                case ConsoleKey.Spacebar:
                    AudioCaptureBase.Configuration.MaximumSilenceRMS = defaultThreshold;
                    IsSilent = false;
                    SilenceStarted = DateTime.MaxValue;
                    SilenceEnded = DateTime.MinValue;
                    break;
            }
        }

        ctsAbortCapture.Cancel();
        await captureTask;
        audio.Dispose();

        Console.Clear();
        Console.WriteLine("\n\nCapture ended. Final values:\n");
        Report();
        Console.WriteLine();
    }

    static void CheckSamples()
    {
        nowRMSVolume = audio.Buffers.RealtimeRMSVolume;
        minRMSVolume = Math.Min(minRMSVolume, nowRMSVolume);
        maxRMSVolume = Math.Max(maxRMSVolume, nowRMSVolume);

        if(IsSilent)
        {
            if(audio.Buffers.SilenceStarted == DateTime.MaxValue)
            {
                IsSilent = false;
                SilenceEnded = DateTime.Now;
            }
        }
        else
        {
            if (audio.Buffers.SilenceStarted != DateTime.MaxValue)
            {
                SilenceStarted = audio.Buffers.SilenceStarted;
                SilenceEnded = DateTime.MinValue;
                IsSilent = true;
            }
        }

    }

    static void Report()
    {
        Console.WriteLine("Most recent silence period:");
        Console.WriteLine($"Silence started:\t{(SilenceStarted == DateTime.MaxValue ? "(never)" : SilenceStarted)}");
        Console.WriteLine($"Silence ended:\t\t{(SilenceEnded == DateTime.MinValue ? (IsSilent ? "(ongoing)" : "(never)") : SilenceEnded)}");
        Console.WriteLine($"Silence duration:\t{(SilenceStarted == DateTime.MaxValue ? "(n/a)" : (SilenceEnded == DateTime.MinValue ? DateTime.Now.Subtract(SilenceStarted).TotalSeconds : SilenceEnded.Subtract(SilenceStarted).TotalSeconds))} sec.");
        Console.WriteLine($"Silence RMS max (double):\t{AudioCaptureBase.Configuration.MaximumSilenceRMS,11:0.0000}");
        Console.WriteLine($"RMS volume now (double):\t{nowRMSVolume,11:0.0000}");
        Console.WriteLine($"Min RMS volume (double):\t{minRMSVolume,11:0.0000}");
        Console.WriteLine($"Max RMS volume (double):\t{maxRMSVolume,11:0.0000}");
    }
}
