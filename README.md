# eyecandy
.NET library for processing OpenAL audio captures as OpenGL textures

Work in progress.

While doing related experimental work in my [vertViwer](https://github.com/MV10/vertViewer) repository, it quickly became apparent the overhead of audio capture needs to be handled by a separate thread. A shader-only test (no audio) runs on my Raspberry Pi at a surprising and respectable 410 FPS, but a very basic test with audio capture only manages about 55 FPS. (By comparison, my Windows desktop runs the same test programs at a mind-bending 4083 FPS and 1150 FPS, respectively.)

I had planned to start from scratch on a library for my "piFX" audio visualization program anyway, so no time like the present...

#### Notes

Random notes from the old code, maybe it'll get stashed into the Wiki section at some point, or something like that:

PCM (wave) data is just the raw OpenAL capture buffer, default is 1024 samples. At 44.1kHz, 1024 samples are available to read a bit more than 43 times per second.

Volume is a single realtime value. It is the Root Mean Square (RMS) of the most recent 300ms of PCM data squared, which is written into a circular buffer.

* 300ms / 1000ms = 0.3 * 44100 = 13230 samples

Frequency data is complicated:

Per this explanation: https://github.com/swharden/FftSharp/issues/25#issuecomment-687653093

Input must be 2x the PCM samples; updates at the same PCM rate using a sliding buffer.

Each frequency data point is 44100 / 2 / 2048 = 22050 / 2048 = 10.77Hz.

If "bass" is 100Hz or less, 100 / 10.77 = 9.29; the first 9 indices are the bass freqs.

In shader texture sampler terms, bass up to 100Hz is 0.0 to 0.0929.

Technically the FFT functions return 1025 elements, not 1024:
* https://github.com/swharden/FftSharp/issues/68
* https://github.com/swharden/FftSharp/issues/69
