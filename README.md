# eyecandy [![NuGet](https://img.shields.io/nuget/v/eyecandy.svg)](https://nuget.org/packages/eyecandy)
**.NET library for processing OpenAL audio captures as OpenGL textures**

If you're interested in audio visualization similar to the old WinAmp plugins (Milkdrop!), or more recently, websites like [Shadertoy](https://www.shadertoy.com/) or [VertexShaderArt](https://www.vertexshaderart.com/), then you've come to the right place. Although this is a fully independent library, it is the foundation for the [monkey-hi-hat](https://github.com/MV10/monkey-hi-hat) music visualization application, and the accompanying shaders in my [Volt's Laboratory](https://github.com/MV10/volts-laboratory) repository.

This library does all the hard work of capturing live-playback audio and producing different representations of the sound data as OpenGL textures. It performs well enough that I have used it on a 32-bit Raspberry Pi4B, in some cases attaining 200+ FPS (though usually much less). On modern Windows 10 64-bit hardware, 4-digit frame rates are not unusual.

Please refer to the repository [wiki](https://github.com/MV10/eyecandy/wiki) for usage, configuration, and other details. The `demo` project is also a good reference, and the library's public API is fully documented. There is even more information in the [monkey-hi-hat](https://github.com/MV10/monkey-hi-hat) wiki that will be of interest to library consumers.

## Demos

The repository's demo project has a lot of useful utilities, and illustrates different ways to use the library. Here is the help output (run the demo program without args to see this):

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
webaudio        Compares WebAudio pseudo-Decibels to pure FFT Decibels
info            OpenAL information (devices, defaults, extensions, etc.)

[options]
F               Full-screen mode
P               Output Process ID
```
