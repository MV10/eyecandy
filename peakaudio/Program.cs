using eyecandy;

namespace peakaudio
{
    internal class Program
    {
        static EyeCandyCaptureConfig config;
        static AudioCaptureProcessor audio;

        static short maxWave = short.MinValue;
        static double maxRMSVolume = double.MinValue;
        static double maxFreqMagnitude = double.MinValue;
        static double maxFreqDecibels = double.MinValue;

        static async Task Main(string[] args)
        {
            Console.WriteLine("\n\npeakaudio\nEyecandy utility to report peak RMS volume and peak FFT frequency values.");
            Console.WriteLine("\nStart playback, set volume to maximum, and press any key to begin capturing audio....");
            Console.ReadKey(true);

            config = new EyeCandyCaptureConfig();
            audio = new AudioCaptureProcessor(config);
            audio.Requirements = new()
            {
                CalculateVolumeRMS = true,
                CalculateFrequency = true,
                CalculateFFTMagnitude = true,
                CalculateFFTDecibels = true,
            };

            Console.Clear();

            var ctsAbortCapture = new CancellationTokenSource();
            var captureTask = Task.Run(() => audio.Capture(CheckSamples, ctsAbortCapture.Token));

            while(!Console.KeyAvailable)
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
            Console.WriteLine("\n\nCapture ended. Maximum values:\n");
            Report();
            Console.WriteLine();
        }

        static void CheckSamples()
        {
            maxRMSVolume = Math.Max(maxRMSVolume, audio.Buffers.RealtimeRMSVolume);

            for(int i = 0; i < config.SampleSize; i++)
            {
                maxWave = Math.Max(maxWave, Math.Abs(audio.Buffers.Wave[i]));
                maxFreqMagnitude = Math.Max(maxFreqMagnitude, Math.Abs(audio.Buffers.FrequencyMagnitude[i]));
                maxFreqDecibels = Math.Max(maxFreqDecibels, Math.Abs(audio.Buffers.FrequencyDecibels[i]));
            }
        }

        static void Report()
        {
            Console.WriteLine($"Raw PCM wave (short):\t{maxWave,11:0.0000}");
            Console.WriteLine($"RMS volume (double):\t{maxRMSVolume,11:0.0000}");
            Console.WriteLine($"Freq magnitude (double)\t{maxFreqMagnitude,11:0.0000}");
            Console.WriteLine($"Freq decibels (double)\t{maxFreqDecibels,11:0.0000}");
        }
    }
}