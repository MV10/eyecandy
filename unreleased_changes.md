
# Yo! Pay Attention

> 3.3.0 publicly released 2025-08-17

### Changelog
* https://github.com/MV10/eyecandy/wiki/5.-Changelog

* v4.0.0 unreleased
* Init texture buffers separately from `AudioTexture.GenerateTexture`
* Pin the buffer in `AudioTexture.GenerateTexture`
* Change `AudioTexture.GenerateTexture` to use `GL.TexSubImage2D` (non-allocating)
* Remove obsolete `AudioTextureEngine.EndAudioProcessing_SynchronousHack`
* Standarized and improved logging
    * Emits messages with correct log categories
    * Eliminated log message string interpolation
    * Added `ErrorLogging.LoggerFactory`
    * Removed `ErrorLogging.LoggingStrategy` (consumer can configure console output)
    * Removed `ErrorLogging.Logger` in favor of loggers with categories
    * Demo changes to support/demonstrate new logging features

### Work In Progress

*


### TODO

* Post v4.0.0 remove obsolete `ErrorLogging` fields


### Terminal Path

Debug bin:
* C:\Source\eyecandy\demo\bin\Debug\net8.0

Release package:
* C:\Source\eyecandy\eyecandy\bin\Release








