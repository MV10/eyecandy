
using Microsoft.Extensions.Logging;
using OpenTK.Audio.OpenAL;
using OpenTK.Graphics.OpenGL;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace eyecandy;

/// <summary/>
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

    // Automatically generated whenever LoggerFactory is set.
    private static ILogger OpenALLogger = null;

    // Automatically generated whenever LoggerFactory is set.
    private static ILogger OpenGLLogger = null;

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

        var logMessage = $"OpenGL Error:\n[{errSev}] source={errSource} type={errType} id={id}\n{message}\n{stack}";

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

        OpenGLLogger.Log(logLevel, logMessage);
    }

    [Obsolete("Set the LoggerFactory property and configure the logger for the desired output.")]
    public static LoggingStrategy Strategy = LoggingStrategy.Automatic;

    [Obsolete("Set the LoggerFactory property and configure the logger for the desired output.")]
    public static ILogger Logger = null;

    [Obsolete("Set the LoggerFactory property and configure the logger for the desired output.")]
    public static List<string> OpenGLErrors = new();

    [Obsolete("Set the LoggerFactory property and configure the logger for the desired output.")]
    public static List<string> OpenALErrors = new();

    [Obsolete("Set the LoggerFactory property and configure the logger for the desired output.")]
    public static List<string> EyecandyErrors = new();

    [Obsolete("Set the LoggerFactory property and configure the logger for the desired output.")]
    public static bool HasErrors = false;

    [Obsolete("GL.GetError calls are no longer needed since the KHR DebugMessageCallback is in use.")]
    public static void OpenGLErrorCheck(string programStage = "unspecified") { }

    [Obsolete("Use the version of OpenALErrorCheck that does not rely on the obsolete storage collections")]
    private static void ErrorCheck<T>(string programStage, Func<T> errorMethod, List<string> storage, T noError)
    where T : Enum
    { }
}

[Obsolete("Set the ErrorLogging.LoggerFactory property and configure the logger for the desired output.")]
public enum LoggingStrategy
{
    [Obsolete("Set the ErrorLogging.LoggerFactory property and configure the logger for the desired output.")]
    Automatic = 0,

    [Obsolete("Set the ErrorLogging.LoggerFactory property and configure the logger for the desired output.")]
    AlwaysOutputToConsole = 1,

    [Obsolete("Set the ErrorLogging.LoggerFactory property and configure the logger for the desired output.")]
    AlwaysStore = 2,
}
