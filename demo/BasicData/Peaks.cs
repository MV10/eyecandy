using eyecandy;

/*

Reports the peak values from the audio capture and post-processing calcs. You should
run this against maximum-volume playback, and try many different tracks to get a good
sample. These can be used as the normalization inputs in the capture config class.
 
*/

namespace demo
{
    internal static class Peaks
    {
        static EyeCandyCaptureConfig config;
        static AudioCaptureProcessor audio;

        static short maxWave = short.MinValue;
        static double minRMSVolume = double.MaxValue;
        static double maxRMSVolume = double.MinValue;
        static double maxFreqMagnitude = double.MinValue;
        static double maxFreqDecibels = double.MinValue;

        public static async Task Demo()
        {
            Console.WriteLine("\n\npeaks: Utility to report peak RMS volume and peak FFT frequency values.");
            Console.WriteLine("\nStart playback, set volume to maximum, and press any key to begin capturing audio....");
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

            Console.Clear();
            Console.WriteLine("Capture starting...");

            var ctsAbortCapture = new CancellationTokenSource();
            var captureTask = Task.Run(() => audio.Capture(CheckSamples, ctsAbortCapture.Token));

            while (!Console.KeyAvailable)
            {
                await Task.Delay(1000);
                Console.SetCursorPosition(0, 0);
                Report();
            }
            Console.ReadKey(true);

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
            minRMSVolume = Math.Min(minRMSVolume, audio.Buffers.RealtimeRMSVolume);
            maxRMSVolume = Math.Max(maxRMSVolume, audio.Buffers.RealtimeRMSVolume);

            for (int i = 0; i < config.SampleSize; i++)
            {
                maxWave = Math.Max(maxWave, Math.Abs(audio.Buffers.Wave[i]));
                maxFreqMagnitude = Math.Max(maxFreqMagnitude, Math.Abs(audio.Buffers.FrequencyMagnitude[i]));
                maxFreqDecibels = Math.Max(maxFreqDecibels, Math.Abs(audio.Buffers.FrequencyDecibels[i]));
            }
        }

        static void Report()
        {
            Console.WriteLine($"Raw PCM wave (short):\t\t{maxWave,11:0.0000}");
            Console.WriteLine($"Min RMS volume (double):\t{minRMSVolume,11:0.0000}");
            Console.WriteLine($"Max RMS volume (double):\t{maxRMSVolume,11:0.0000}");
            Console.WriteLine($"Freq magnitude (double):\t{maxFreqMagnitude,11:0.0000}");
            Console.WriteLine($"Freq decibels (double):\t\t{maxFreqDecibels,11:0.0000}");
        }
    }
}
