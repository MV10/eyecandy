# eyecandy [![NuGet](https://img.shields.io/nuget/v/eyecandy.svg)](https://nuget.org/packages/eyecandy)
**.NET library for processing OpenAL audio captures as OpenGL textures**

If you're interested in audio visualization similar to the old WinAmp plugins (Milkdrop!), or more recently, websites like [Shadertoy](https://www.shadertoy.com/) or [VertexShaderArt](https://www.vertexshaderart.com/), then you've come to the right place. See my work-in-progress at the [monkey-hi-hat](https://github.com/MV10/monkey-hi-hat) repository for a complete application using this library, and the accompanying shaders in my [Volt's Laboratory](https://github.com/MV10/volts-laboratory) repository.

This library does all the hard work of capturing live-playback audio and producing different representations of the sound data as OpenGL textures. It performs well enough that I have used it on a 32-bit Raspberry Pi4B, in some cases attaining 200+ FPS (though usually much less). On modern Windows 10 64-bit hardware, 4-digit frame rates are not unusual.

There are many other features and options not covered in this README, but the demos cover all of the important parts, and the public API is fully documented.

#### 2023-07-27 v1.0.6
* HideMousePointer option
* Converted storage class to record
* Improved error logging, added some debug-level log output
* Engine-level silence detection support

#### 2023-07-23 v1.0.3
* Squash a Linux bug: can't call AL.GetError after closing all devices...

#### 2023-07-22 v1.0.2
* Added silence-detection settings to config (DetectSilence, MaximumSilenceRMS)
* Added silence-detection to buffers (SilenceStarted timestamp)
* Added "silence" demo
* Changed Shader to log unknown uniform names as Info level rather than Warn

#### 2023-07-20 v1.0.1
* Improved / expanded error-handling and reporting
* Added `AverageFramesPerSecond` to `BaseWindow.CalculateFPS` method with configurable period
* Optional standard `ILogger` support (populate via `ErrorLogging.Logger` static field)
* `BaseWindow.Shader` creation is optional (via `createShaderFromConfig` constructor parameter)
* OpenGL ES 3.2 made optional (`BaseWindow.ForceOpenGLES3dot2` static field is `true` by default)
* Demo project forces all error logging to the console and all GL demos output average FPS upon exit

# Demos

In terms of the demo project in this repo, here is the help output (run it without args):

```
demo [type] [options]

[type]
peaks           Peak audio capture values (use for configuration)
text            Text-based audio visualizations
history         Raw history-texture dumps
wave            Raw PCM wave audio visualization
freq            Frequency magnitude and volume history (multiple shaders)
vert            VertexShaderArt-style integer-array vertex shader (no audio)
frag            Shadertoy-style pixel fragment shader
info            OpenAL information (devices, defaults, extensions, etc.)

[options]
F               Full-screen mode
P               Output Process ID
```

# Major Dependencies
TODO: Create Wiki entries to assist with OS configuration details (in particular, loopback audio on Windows, and GLFW support on Raspberry Pi).

* [OpenTK](https://github.com/opentk/opentk)
* OpenGL ES 3.2
* GLFW
* [OpenAL Soft](https://github.com/kcat/openal-soft)
* [FftSharp](https://github.com/swharden/FftSharp)
* .NET 6.x

# Windowed Usage

At a high level, these are the extra steps compared to a regular OpenTK `GameWindow` scenario:

* Create an `EyeCandyCaptureConfig` object to define audio capture parameters
* Create an `EyeCandyWindowConfig` object to define your window characteristics
    * Set the size: `config.OpenTKNativeWindowSettings.Size = (960, 540);`
    * Set the vert: `config.VertexShaderPathname = "Subdir/filename.vert";`
    * Set the frag: `config.FragmentShaderPathname = "Subdir/filename.frag";`
* Create a window object derived from `BaseWindow`
* In the window constructor, create an `AudioTextureEngine` object
* Call `engine.Create<AudioTextureXXXX>(...)` to define the textures you'll use
* Among your other OpenGL calls, in the `OnRenderFrame` event...
    * Call `engine.UpdateTextures();`
    * Call `engine.SetTextureUniforms(Shader);`

Refer to the Wave or Frag demos for relatively easy-to-follow examples of this. The Wave demo works similarly to VertexShaderArt (the work is performed in a vertex shader based on an array of sequential integers), and the Frag demo works similarly to Shadertoy (the work is performed in a frag shader on a quad that simply covers the entire display surface).

# Audio-Only Usage

It is possible to use the audio portion separately:

* Create an `EyeCandyCaptureConfig` object to define audio capture parameters
* Create an `AudioCaptureProcesor` object
* For post-processing, assign an `AudioProcessingRequirements` struct to `audio.Requirements`
* Create an `Action`-style callback (no arguments, returns `void`)
* Create a `CancellationTokenSource`
* Start capturing with `Task.Run(() => audio.Capture(callback, cancellationToken))`
* In the callback, reference the sample and processed data arrays in `audio.Buffers`
* Stop capturing:
    * Cancel the token
    * `await` the `Task` returned from the `Task.Run` invocation
    * Call `Dispose` (or re-start capture)

The "Peaks" demo is a good example of this (purely console-based analysis of data maximums).

# Available Audio Textures

Currently there are six types of audio textures available. The "History" textures have current data in row zero, and each time the texture is updated, the data is "scrolled" upwards before the new data is generated. Thus, increasing the y offset is going backwards in time.

All currently available audio textures are 4-channel (RGBA) 32-bit floats. With one exception, the data is in the green channel. The `AudioTexture4ChannelHistory` class uses all four RGBA channels.

Most texture widths match the sample count, defined in the audio config, and the config also defines row counts for history textures. Of course, once they're in the shader, they're normalized 0-1.

* `AudioTextureVolumeHistory` - Represents volume with a Root Mean Square (RMS) calculation. This is a 1-pixel-wide history texture.
* `AudioTextureWaveHistory` - The raw 16-bit PCM wave sample data provided by the OpenAL library. This contains both positive and negative values. This is a full-width history texture.
* `AudioTextureFrequencyMagnitudeHistory` - A frequency analysis represented by a magnitude calculation. This data is "subtle" and will probably benefit from magnification (apply a multiplier). This is a full-width history texture.
* `AudioTextureFrequencyDecibelHistory` - A frequency analysis represented by decibel level. This data has strong signals compared to the FFT magnitude calculation. This is a full-width history texture.
* `AudioTextureShadertoy` - A non-history texture similar to audio data provided by the Shadertoy website. The y=0 row represents frequency magnitude data, and the y=1 row represents the raw PCM wave data.
* `AudioTexture4ChannelHistory` - This combines all currenly available data into a single texture. Unlike the other textures, all four RGBA channels are used. Red is volume, Green is raw PCM, Blue is frequency magnitude, and Alpha is frequency decibels. This is a full-width history texture.

Note that you can subclass the `AudioTexture` abstract class and create your own audio interpretations. There are only a few requirements in the constructor, then you just populate the `ChannelBuffer` array with data from the `AudioCaptureProcessor.Buffers` arrays.

# Differences vs. Shader Websites

With a little tweaking, I've had success converting single-pass Shadertoy code and VertexShaderArt code to run under this library. Some of the demos use shaders I've written on those sites (check the code comments for links). Currently this library doesn't support multi-pass rendering which is an option on Shadertoy, but this is something I'd like to add.

Shadertoy and VertexShaderArt both rely on the browser WebAudio API, and both PCM and frequency data generated by WebAudio is pretty different in comparison to what this library captures with OpenAL. In fact, most of my testing relies on Spotify playback, and the data the demo program captures on my Windows machine using the official native Spotify client is also pretty different from what is captured on my Raspberry Pi using the [spotifyd](https://github.com/Spotifyd/spotifyd) client.

Also, this library seems _much_ more sensitive to playback volume versus the WebAudio-based sites, and that also varies from machine to machine.

I'm hoping to find somebody who understands audio better than I do (which is "not at all"). I'd like to add more textures and processing that more closely matches what WebAudio produces, if only to simplify conversion of Shadertoy and VertexShaderArt shaders.

Those sites rely on WebGL, which (like everything in browser-land) is kind of a stunted mutant half-assed version of OpenGL. Thus, you may run into the need to tweak certain keywords (for example, `texture2d` sampling should be replaced with the overloaded `texture` call). But by and large, a few `#define` directives is usually adequate to convert many of those examples.

> IMPORTANT: As noted previously, everything in this library uses 32-bit RGBA floats and the data is usually in the green `g` channel. VertexShaderArt uses the alpha channel and 16-bit integers (except their `floatSound` texture, which is rarely used), and Shadertoy uses 16-bit integers in the `x` or red `r` channel. 

Both sites hide some aspects of shader programming. VertexShaderArt "injects" the `in`/`out` and `uniform` declarations, for example. For reasons I don't quite understand, Shadertoy uses a `mainImage` function to pass the `in`/`out` args. You'll see in the demos that none of this applies here -- you're just writing fully normal shaders however you want.

Finally, Shadertoy apparently passes `fragCoord` as actual (X,Y) values matching the true resolution, so almost every shader divides by `iResolution` to create a normalized 0-1 range. This program passes `fragCoord` as normalized value so the resolution isn't needed.

# Notes

Random notes from the old exploratory code. Maybe it'll get stashed into the Wiki section at some point, or something like that:

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
