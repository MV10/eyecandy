# eyecandy [![NuGet](https://img.shields.io/nuget/v/eyecandy.svg)](https://nuget.org/packages/eyecandy)
**.NET library for processing audio playback as OpenGL textures**

If you're interested in audio visualization similar to the old WinAmp plugins (Milkdrop!), or more
recently, websites like [Shadertoy](https://www.shadertoy.com/) or [VertexShaderArt](https://www.vertexshaderart.com/), then you've come to the right
place. Although this is a fully independent library, it is the foundation for the [monkey-hi-hat](https://github.com/MV10/monkey-hi-hat)
music visualization application, and the accompanying shaders in my [Volt's Laboratory](https://github.com/MV10/volts-laboratory) repository.

This library does all the hard work of capturing live-playback audio and producing different representations
of the sound data as OpenGL textures. On modern Windows 64-bit hardware, 4-digit frame rates are not unusual.
As of October 2025, Linux support is back! (It performs well enough that I have used it on a 32-bit Raspberry Pi 4B,
in some cases attaining 200+ FPS, although the Pi will not be supported due to GPU and driver limitations and quality.) 

Please refer to the repository [wiki](https://github.com/MV10/eyecandy/wiki) for usage, configuration, and other details. The `demo` project is also
a good reference, and the library's public API is fully documented. There is even more information in the
[monkey-hi-hat](https://github.com/MV10/monkey-hi-hat) wiki that will be of interest to library consumers.

> Version 3+ _does not_ require audio loopback drivers for Windows! Loopback is internal!

## Demos

The repository's demo project has a lot of useful utilities, and illustrates different ways to use the library. Here is the help output (run the demo program without args to see this):

```
demo [type] [options]

[type]
peaks           Peak audio capture values (use for configuration)
text            Text-based audio visualizations
silence         Silence-detection testing
history         Raw history-texture dumps
wave            Raw PCM wave audio visualization
freq            Frequency magnitude and volume history (multiple shaders)
vert            VertexShaderArt-style integer-array vertex shader (no audio)
frag            Shadertoy-style pixel fragment shader
webaudio        Compares WebAudio pseudo-Decibels to pure FFT Decibels
modes           Different OpenGL drawing modes (points, lines, tris, etc)
uniforms        Testing the Shader.ResetUniforms call

info            List known audio devices (Windows: WASAPI, Linux: OpenAL)
info O          Windows: Use OpenAL instead of WASAPI (requires loopback)

[options]
F               Full-screen mode
S               Simulate audio with the SyntheticData wave sample source
E               Simulate OpenGL errors (currently only "freq" does this)
D               Show Debug log messages (default is Error/Critical)
V               Show Verbose log messages (default is Error/Critical)
O               Capture audio with OpenAL (requires loopback driver)
```
