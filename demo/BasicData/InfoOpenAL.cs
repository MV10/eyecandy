
using eyecandy; // for error logging only

using OpenTK.Audio.OpenAL;
using System.Diagnostics;

namespace demo;

internal class InfoOpenAL
{
    public static async Task Demo()
    {
        Console.WriteLine("OpenAL Device Information and record/playback test");
        Console.WriteLine("---------------------------------------------------------------");

        var devices = ALC.GetStringList(GetEnumerationStringList.DeviceSpecifier);
        Console.WriteLine($"\n\nDrivers:\n  {string.Join("\n  ", devices)}");

        var deviceName = ALC.GetString(ALDevice.Null, AlcGetString.DefaultDeviceSpecifier);
        Console.WriteLine($"\n\nDefault driver:\n  {deviceName}");
        foreach (var d in devices)
        {
            if (d.Contains("OpenAL Soft"))
            {
                deviceName = d;
                Console.WriteLine($"  Using: \"{deviceName}\"");
            }
        }

        var allDevices = ALC.EnumerateAll.GetStringList(GetEnumerateAllContextStringList.AllDevicesSpecifier);
        Console.WriteLine($"\n\nPlayback devices:\n  {string.Join("\n  ", allDevices)}");

        var list = ALC.GetStringList(GetEnumerationStringList.CaptureDeviceSpecifier);
        Console.WriteLine($"\n\nCapture devices:\n  {string.Join("\n  ", list)}");

        var defaultPlayback = ALC.GetString(ALDevice.Null, AlcGetString.DefaultAllDevicesSpecifier);
        var defaultCapture = ALC.GetString(ALDevice.Null, AlcGetString.CaptureDefaultDeviceSpecifier);
        Console.WriteLine($"\n\nDefault  devices:\n  Playback: {defaultPlayback}\n  Capture: {defaultCapture}");

        var device = ALC.OpenDevice(deviceName);
        var context = ALC.CreateContext(device, (int[])null);
        ALC.MakeContextCurrent(context);

        ErrorLogging.OpenALErrorCheck("Startup");

        var attrs = ALC.GetContextAttributes(device);
        Console.WriteLine($"\n\nOpenAL Context attributes:\n  {attrs}");

        ALC.GetInteger(device, AlcGetInteger.MajorVersion, 1, out int alcMajorVersion);
        ALC.GetInteger(device, AlcGetInteger.MinorVersion, 1, out int alcMinorVersion);
        string alcExts = ALC.GetString(device, AlcGetString.Extensions);
        string exts = AL.Get(ALGetString.Extensions);
        string rend = AL.Get(ALGetString.Renderer);
        string vend = AL.Get(ALGetString.Vendor);
        string vers = AL.Get(ALGetString.Version);
        Console.WriteLine($"\n\nOpenAL Context extensions:\n  Vendor: {vend}\n  Version: {vers}\n  Renderer: {rend}\n  Extensions:\n    {string.Join("\n    ", exts.Split(" "))}\n  ALC Version: {alcMajorVersion}.{alcMinorVersion}\n  ALC Extensions:\n    {string.Join("\n    ", alcExts.Split(" "))}");

        Console.WriteLine("\n\nESC      - Exit utility\nSpacebar - Test recording and playback");
        while (true)
        {
            var k = Console.ReadKey(true);
            if (k.Key == ConsoleKey.Escape) goto ExitProgram; // ha! goto! suck it, haters!
            if (k.Key == ConsoleKey.Spacebar) break;
        }

        Console.WriteLine("\n\nRecording up to five seconds of audio (within a maximum wait-time of 30 seconds)...");

        ErrorLogging.OpenALErrorCheck("Before recording");

        Stopwatch ticktock = new();
        long lastElapsed = 0;
        ticktock.Start();

        short[] audio = new short[44100 * 5]; // 5 is the duration in seconds
        int current = 0;

        ALCaptureDevice captureDevice = ALC.CaptureOpenDevice(null, 44100, ALFormat.Mono16, 1024);
        {
            ALC.CaptureStart(captureDevice);

            while (current < audio.Length && lastElapsed < 30)
            {
                long secs = ticktock.ElapsedMilliseconds / 1000;
                if (secs > lastElapsed)
                {
                    Console.Write(".");
                    lastElapsed = secs;
                }

                int samplesAvailable = ALC.GetInteger(captureDevice, AlcGetInteger.CaptureSamples);
                if (samplesAvailable > 512)
                {
                    int samplesToRead = Math.Min(samplesAvailable, audio.Length - current);
                    ALC.CaptureSamples(captureDevice, ref audio[current], samplesToRead);
                    current += samplesToRead;
                }

                Thread.Yield();
            }

            Console.WriteLine();
            ALC.CaptureStop(captureDevice);
            ALC.CaptureCloseDevice(captureDevice);
        }

        ErrorLogging.OpenALErrorCheck("After recording");

        if (current == 0)
        {
            Console.WriteLine("\n\nNo audio captured. Press any key to exit.");
            Console.ReadKey(true);
            goto ExitProgram;
        }

        Console.WriteLine("\n\nPause your source audio, then press any key to play back the recording...");
        Console.ReadKey(true);

        ErrorLogging.OpenALErrorCheck("Before buffer setup");

        AL.GenBuffer(out int alBuffer);
        AL.BufferData(alBuffer, ALFormat.Mono16, ref audio[0], audio.Length * 2, 44100);

        ErrorLogging.OpenALErrorCheck("After buffer setup");

        AL.Listener(ALListenerf.Gain, 1.0f);

        AL.GenSource(out int alSource);
        AL.Source(alSource, ALSourcef.Gain, 1f);
        AL.Source(alSource, ALSourcei.Buffer, alBuffer);

        ErrorLogging.OpenALErrorCheck("Before playing");

        Console.WriteLine("\n\nPlayback starting...");
        AL.SourcePlay(alSource);

        ErrorLogging.OpenALErrorCheck("Playback started");

        lastElapsed = 0;
        ticktock.Restart();

        while ((ALSourceState)AL.GetSource(alSource, ALGetSourcei.SourceState) == ALSourceState.Playing)
        {
            // These stats are extremely noisy...

            //if (DeviceClock.IsExtensionPresent(device))
            //{
            //    long[] clockLatency = new long[2];
            //    DeviceClock.GetInteger(device, GetInteger64.DeviceClock, clockLatency);
            //    Console.WriteLine("  Clock: " + clockLatency[0] + ", Latency: " + clockLatency[1]);
            //    CheckALError("Clock latency check");
            //}

            //if (SourceLatency.IsExtensionPresent())
            //{
            //    SourceLatency.GetSource(alSource, SourceLatencyVector2d.SecOffsetLatency, out var values);
            //    SourceLatency.GetSource(alSource, SourceLatencyVector2i.SampleOffsetLatency, out var values1, out var values2, out var values3);
            //    Console.WriteLine("  Source latency: " + values);
            //    Console.WriteLine($"  Source latency 2: {Convert.ToString(values1, 2)}, {values2}; {values3}");
            //    CheckALError("Source latency check");
            //}

            long secs = ticktock.ElapsedMilliseconds / 1000;
            if (secs > lastElapsed)
            {
                Console.Write(".");
                lastElapsed = secs;
            }

            Thread.Sleep(10);
        }

        Console.WriteLine("\n\n\nPlayback stopping.");
        AL.SourceStop(alSource);

        ErrorLogging.OpenALErrorCheck("Playback stopped");

    ExitProgram:
        Console.WriteLine("\n\nExiting...");
        ALC.MakeContextCurrent(ALContext.Null);
        ALC.DestroyContext(context);
        ALC.CloseDevice(device);
    }
}
