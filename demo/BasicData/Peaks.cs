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
        static AudioCaptureBase audio;

        static short minWave = short.MaxValue;
        static short maxWave = short.MinValue;

        static double minRMSVolume = double.MaxValue;
        static double minFreqMagnitude = double.MaxValue;
        static double minFreqDecibels = double.MaxValue;
        static double minFreqWebAudio = double.MaxValue;

        static double maxRMSVolume = double.MinValue;
        static double maxFreqMagnitude = double.MinValue;
        static double maxFreqDecibels = double.MinValue;
        static double maxFreqWebAudio = double.MinValue;

        public static async Task Demo()
        {
            Console.WriteLine("\n\npeaks: Utility to report peak RMS volume and peak FFT frequency values.");
            Console.WriteLine("\nStart playback, set volume to maximum, and press any key to begin capturing audio....");
            Console.ReadKey(true);

            config = new EyeCandyCaptureConfig();
            audio = AudioCaptureBase.Factory(config);
            audio.Requirements = new()
            {
                CalculateVolumeRMS = true,
                CalculateFFTMagnitude = true,
                CalculateFFTDecibels = true,
                CalculateFFTWebAudioDecibels = true,
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
                minWave = Math.Min(minWave, audio.Buffers.Wave[i]);
                minFreqMagnitude = Math.Min(minFreqMagnitude, audio.Buffers.FrequencyMagnitude[i]);
                minFreqDecibels = Math.Min(minFreqDecibels, audio.Buffers.FrequencyDecibels[i]);
                minFreqWebAudio = Math.Min(minFreqWebAudio, audio.Buffers.FrequencyWebAudio[i]);

                maxWave = Math.Max(maxWave, audio.Buffers.Wave[i]);
                maxFreqMagnitude = Math.Max(maxFreqMagnitude, audio.Buffers.FrequencyMagnitude[i]);
                maxFreqDecibels = Math.Max(maxFreqDecibels, audio.Buffers.FrequencyDecibels[i]);
                maxFreqWebAudio = Math.Max(maxFreqWebAudio, audio.Buffers.FrequencyWebAudio[i]);
            }
        }

        static void Report()
        {
            Console.WriteLine($"Min PCM wave (short):\t\t{minWave,11:0.0000}");
            Console.WriteLine($"Max PCM wave (short):\t\t{maxWave,11:0.0000}\n");

            Console.WriteLine($"Min RMS volume (double):\t{minRMSVolume,11:0.0000}");
            Console.WriteLine($"Max RMS volume (double):\t{maxRMSVolume,11:0.0000}\n");

            Console.WriteLine($"Min freq mag (double):\t\t{minFreqMagnitude,11:0.0000}");
            Console.WriteLine($"Max freq mag (double):\t\t{maxFreqMagnitude,11:0.0000}\n");

            Console.WriteLine($"Min freq dBs (double):\t\t{minFreqDecibels,11:0.0000}");
            Console.WriteLine($"Max freq dBs (double):\t\t{maxFreqDecibels,11:0.0000}\n");

            Console.WriteLine($"Min WebAudio (double):\t\t{minFreqWebAudio,11:0.0000}");
            Console.WriteLine($"Max WebAudio (double):\t\t{maxFreqWebAudio,11:0.0000}\n");
        }
    }
}
