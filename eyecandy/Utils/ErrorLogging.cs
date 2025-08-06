
using Microsoft.Extensions.Logging;
using OpenTK.Audio.OpenAL;
using OpenTK.Graphics.OpenGL;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace eyecandy
{
    /// <summary/>
    public static class ErrorLogging
    {
        /// <summary>
        /// By default, an ILogger is used, otherwise output to the console
        /// when a debugger is attached, or store when no debugger is attached.
        /// </summary>
        public static LoggingStrategy Strategy = LoggingStrategy.Automatic;

        /// <summary>
        /// Generally the library should use the EyecandyError method instead of directly
        /// calling the logger.
        /// </summary>
        public static ILogger Logger = null;

        /// <summary>
        /// Any error messages collected by calls to OpenGLErrorCheck (if LoggingStrategy calls for storage).
        /// </summary>
        public static List<string> OpenGLErrors = new();

        /// <summary>
        /// Any error messages collected by calls to OpenALErrorCheck (if LoggingStrategy calls for storage).
        /// </summary>
        public static List<string> OpenALErrors = new();

        /// <summary>
        /// Any errors internal to the library, such as file-loading or shader compilation errors (if LoggingStrategy calls for storage). 
        /// </summary>
        public static List<string> EyecandyErrors = new();

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
        /// True if either OpenGL or OpenAL errors have been collected.
        /// </summary>
        public static bool HasErrors = (OpenGLErrors.Count > 0 || OpenALErrors.Count > 0 || EyecandyErrors.Count > 0);

        [Obsolete("GL.GetError calls are no longer needed since the KHR DebugMessageCallback is in use.")]
        /// <summary>
        /// Writes or stores outstanding OpenGL error messages (depending on the StoreErrors flag).
        /// </summary>
        public static void OpenGLErrorCheck(string programStage = "unspecified")
            => ErrorCheck(programStage, GL.GetError, OpenGLErrors, ErrorCode.NoError);

        /// <summary>
        /// Writes or stores outstanding OpenAL error messages (depending on the StoreErrors flag).
        /// </summary>
        public static void OpenALErrorCheck(string programStage = "unspecified")
            => ErrorCheck(programStage, AL.GetError, OpenALErrors, ALError.NoError);

        /// <summary>
        /// Outputs and optionally purges any stored OpenGL and OpenAL errors.
        /// </summary>
        public static void WriteToConsole(bool purge = true)
        {
            if (!HasErrors) return;
            Console.WriteLine("  (Stored errors not necessarily in the sequence listed.)");
            foreach (var e in OpenGLErrors) Console.WriteLine(e);
            foreach (var e in EyecandyErrors) Console.WriteLine(e);
            foreach (var e in OpenALErrors) Console.WriteLine(e);
            if (purge)
            {
                OpenGLErrors.Clear();
                EyecandyErrors.Clear();
                OpenALErrors.Clear();
            }
        }

        // The OpenGL and OpenAL processes are identical except for the enum returned...
        private static void ErrorCheck<T>(string programStage, Func<T> errorMethod, List<string> storage, T noError)
        where T : Enum
        {
            bool consoleOutput = Strategy == LoggingStrategy.AlwaysOutputToConsole
                || (Strategy == LoggingStrategy.Automatic && Logger is null);

            var err = errorMethod.Invoke();
            while (!err.Equals(noError))
            {
                var message = $"  Program stage \"{programStage}\": {err}";
                Logger?.LogError(message.Trim());

                if (Strategy == LoggingStrategy.AlwaysStore)
                {
                    storage.Add(message);
                }

                if(consoleOutput)
                {
                    foreach (var e in storage) Console.WriteLine(e);
                    storage.Clear();
                    Console.WriteLine(message);
                }
                err = errorMethod.Invoke();
            }
        }

        internal static void EyecandyError(string programStage, string err, LogLevel logLevel = LogLevel.Error)
        {
            var message = $"[{logLevel}] at program stage \"{programStage}\": {err}";
            LogMessage(logLevel, message);
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
            if (IgnoredErrorCallbackIDs.Contains(id)) return;

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

            LogMessage(logLevel, logMessage);
        }

        private static void LogMessage(LogLevel logLevel, string message)
        {
            Logger?.Log(logLevel, message.Trim());

            if (Strategy == LoggingStrategy.AlwaysStore)
            {
                EyecandyErrors.Add(message);
            }

            bool consoleOutput = Strategy == LoggingStrategy.AlwaysOutputToConsole
                || (Strategy == LoggingStrategy.Automatic && Logger is null);

            if (consoleOutput)
            {
                foreach (var e in EyecandyErrors) Console.WriteLine(e);
                EyecandyErrors.Clear();
                Console.WriteLine(message);
            }
        }
    }
}
