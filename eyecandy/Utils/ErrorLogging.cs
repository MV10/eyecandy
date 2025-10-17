
using Microsoft.Extensions.Logging;
using OpenTK.Audio.OpenAL;
using OpenTK.Graphics.OpenGL;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace eyecandy;

/// <summary>
/// OpenGL and OpenAL error logging. Note this will handle OpenGL errors for the entire
/// application, not just eyecandy. Requires use of the OpenTK BaseWindow class for
/// OpenGL logging, otherwise none of this will be correctly "wired up" for output.
/// </summary>
public static class ErrorLogging
{
    /// <summary>
    /// Optional log factory.
    /// </summary>
    public static ILoggerFactory LoggerFactory
    {
        get;
        set
        {
            if (value is null)
            {
                OpenALLogger = null;
                OpenGLLogger = null;
            }
            else
            {
                OpenALLogger = value.CreateLogger("Eyecandy.OpenAL");
                OpenGLLogger = value.CreateLogger("Eyecandy.OpenGL");
            }
            field = value;
        }
    } = null;

    /// <summary>
    /// Set by BaseWindow constructor based on the EyeCandyWindowConfig 
    /// setting by the same name.
    /// </summary>
    public static bool DebugBreakOnGLError = false;

    /// <summary>
    /// Controls how OpenGL error logging works. Refer to the flags for more information.
    /// </summary>
    public static OpenGLErrorLogFlags LoggingMode = OpenGLErrorLogFlags.Normal;

    /// <summary>
    /// Limits the frequency at which a given error message will be logged.
    /// </summary>
    public static long LogInterval = 0;

    // Automatically generated whenever LoggerFactory is set.
    private static ILogger OpenALLogger = null;

    // Automatically generated whenever LoggerFactory is set.
    private static ILogger OpenGLLogger = null;

    private static Dictionary<uint, (string Message, long Counter)> IntervalTracking = new();

    // These are widely recognized as unimportant "noise" messages when the OpenGL
    // Debug Message error callback is wired up. For example:
    // https://deccer.github.io/OpenGL-Getting-Started/02-debugging/02-debug-callback/
    private static readonly List<int> IgnoredErrorCallbackIDs =
    [
        0,              // gl{Push,Pop}DebugGroup calls
        131169, 131185, // NVIDIA buffer allocated to use video memory
        131218, 131204, // texture cannot be used for texture mapping
        131222, 131154, // NVIDIA pixel transfer is syncrhonized with 3D rendering
    ];

    /// <summary>
    /// Writes or stores outstanding OpenAL error messages (depending on the StoreErrors flag).
    /// </summary>
    public static void OpenALErrorCheck(string programStage = "unspecified")
    {
        if (OpenALLogger is null) return;
        var err = AL.GetError();
        while (!err.Equals(ALError.NoError))
        {
            var message = $"  Program stage \"{programStage}\": {err}";
            OpenALLogger.LogError(message.Trim());
            err = AL.GetError();
        }
    }

    /// <summary>
    /// The BaseWindow.Dispose method calls this to report all interval-suppressed errors and total counts.
    /// </summary>
    internal static void FlushOpenGLErrors()
    {
        if (OpenGLLogger is null) return;
        OpenGLLogger.LogInformation($"\n\nErrors were suppressed at interval {LogInterval}; final tallies:\n");
        foreach(var kvp in IntervalTracking)
        {
            OpenGLLogger.LogInformation($"\nLogged {kvp.Value.Counter} times:\n{kvp.Value.Message}");
        }
    }

    // This is wired up in the BaseWindow constructor
    internal static void OpenGLErrorCallback(
        DebugSource source,     // API, WINDOW_SYSTEM, SHADER_COMPILER, THIRD_PARTY, APPLICATION, OTHER
        DebugType type,         // ERROR, DEPRECATED_BEHAVIOR, UNDEFINED_BEHAVIOR, PORTABILITY, PERFORMANCE, MARKER, OTHER
        int id,                 // ID associated with the message (driver specific; see IgnoredErrorCallbackIDs list above)
        DebugSeverity severity, // NOTIFICATION, LOW, MEDIUM, HIGH ... (others defined too?)
        int length,             // length of the string in pMessage
        IntPtr pMessage,        // pointer to message string
        IntPtr pUserParam)      // not used here
    {
        if (OpenGLLogger is null || IgnoredErrorCallbackIDs.Contains(id)) return;

        var message = Marshal.PtrToStringAnsi(pMessage, length);
        var errSource = source.ToString().Substring("DebugSource".Length);
        var errType = type.ToString().Substring("DebugType".Length);
        var errSev = severity.ToString().Substring("DebugSeverity".Length);
        var stack = new StackTrace(true).ToString();

        var appState = string.Empty;
        if(!string.IsNullOrEmpty(GLErrorAppState.AppState))
        {
            appState = $"AppState:\n{GLErrorAppState.AppState}\n";
        }
        if(!string.IsNullOrEmpty(GLErrorAppState.MethodState.Args))
        {
            appState = $"{appState}MethodState:\n{GLErrorAppState.MethodState.Name}({GLErrorAppState.MethodState.Args})\n";
        }

        var logMessage = string.Empty;

        switch (LoggingMode)
        {
            case OpenGLErrorLogFlags.Normal:
            case OpenGLErrorLogFlags.DebugContext:
                logMessage = $"OpenGL Error:\n[{errSev}] source={errSource} type={errType} id={id}\n{message}\n{appState}{stack}";
                break;

            case OpenGLErrorLogFlags.LowDetail:
                logMessage = $"OpenGL [{errSev} / {errSource}] {message}";
                break;

            default: // disabled, but this should not even be wired up
                return;
        }

        if (LogInterval > 0)
        {
            var hash = StringHashing.Hash(logMessage);
            if (IntervalTracking.TryGetValue(hash, out var entry))
            {
                if (++entry.Counter % LogInterval != 0) return;
                logMessage = $"{logMessage}\n(Suppressing duplicate errors at interval {LogInterval}; this is number {entry.Counter})";
            }
            else
            {
                IntervalTracking.Add(hash, (logMessage, 1));
            }
        }

        var logLevel = LogLevel.Information;
        switch(severity)
        {
            case DebugSeverity.DebugSeverityHigh:
                logLevel = LogLevel.Error;
                break;

            case DebugSeverity.DebugSeverityMedium:
            case DebugSeverity.DebugSeverityLow:
                logLevel = LogLevel.Warning;
                break;

            default:
                break;
        }

        if (Debugger.IsAttached && DebugBreakOnGLError) Debugger.Break();

        OpenGLLogger.Log(logLevel, logMessage);
    }
}
