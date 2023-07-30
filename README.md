# eyecandy [![NuGet](https://img.shields.io/nuget/v/eyecandy.svg)](https://nuget.org/packages/eyecandy)
**.NET library for processing OpenAL audio captures as OpenGL textures**

If you're interested in audio visualization similar to the old WinAmp plugins (Milkdrop!), or more recently, websites like [Shadertoy](https://www.shadertoy.com/) or [VertexShaderArt](https://www.vertexshaderart.com/), then you've come to the right place. See my work-in-progress at the [monkey-hi-hat](https://github.com/MV10/monkey-hi-hat) repository for a complete application using this library, and the accompanying shaders in my [Volt's Laboratory](https://github.com/MV10/volts-laboratory) repository.

This library does all the hard work of capturing live-playback audio and producing different representations of the sound data as OpenGL textures. It performs well enough that I have used it on a 32-bit Raspberry Pi4B, in some cases attaining 200+ FPS (though usually much less). On modern Windows 10 64-bit hardware, 4-digit frame rates are not unusual.

Please refer to the repository [wiki]() for usage, configuration, and other details. The `demo` project is also a good reference, and the library's public API is fully documented.

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
