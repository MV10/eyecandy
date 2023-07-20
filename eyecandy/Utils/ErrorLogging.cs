
using Microsoft.Extensions.Logging;
using OpenTK.Audio.OpenAL;
using OpenTK.Graphics.OpenGL;
using System.Diagnostics;

namespace eyecandy
{
    public static class ErrorLogging
    {
        /// <summary>
        /// By default, an ILogger is used, otherwise output to the console
        /// when a debugger is attached, or store when no debugger is attached.
        /// </summary>
        public static LoggingStrategy Strategy = LoggingStrategy.Automatic;

        /// <summary>
        /// Generally the library should use the LibraryError method instead of directly
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
        public static List<string> LibraryErrors = new();

        /// <summary>
        /// True if either OpenGL or OpenAL errors have been collected.
        /// </summary>
        public static bool HasErrors = (OpenGLErrors.Count > 0 || OpenALErrors.Count > 0 || LibraryErrors.Count > 0);

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
            ConsoleMarkerOpen();
            Console.WriteLine("  (Stored errors not necessarily in the sequence listed.)");
            foreach (var e in OpenGLErrors) Console.WriteLine(e);
            foreach (var e in LibraryErrors) Console.WriteLine(e);
            foreach (var e in OpenALErrors) Console.WriteLine(e);
            ConsoleMarkerClose();
            if (purge)
            {
                OpenGLErrors.Clear();
                LibraryErrors.Clear();
                OpenALErrors.Clear();
            }
        }

        // The OpenGL and OpenAL processes are identical except for the enum returned...
        private static void ErrorCheck<T>(string programStage, Func<T> errorMethod, List<string> storage, T noError)
        where T : Enum
        {
            bool consoleMarker = false;
            var err = errorMethod.Invoke();
            while (!err.Equals(noError))
            {
                var message = $"  Program stage \"{programStage}\": {err}";
                Logger?.LogError(message.Trim());

                if (Strategy == LoggingStrategy.AlwaysStore || (Strategy == LoggingStrategy.Automatic && !Debugger.IsAttached))
                {
                    storage.Add(message);
                }

                if(Strategy == LoggingStrategy.AlwaysOutputToConsole || (Strategy == LoggingStrategy.Automatic && Debugger.IsAttached))
                {
                    if (!consoleMarker)
                    {
                        ConsoleMarkerOpen();
                        foreach (var e in storage) Console.WriteLine(e);
                        storage.Clear();
                        consoleMarker = true;
                    }
                    Console.WriteLine(message);
                }
                err = errorMethod.Invoke();
            }
            if (consoleMarker) ConsoleMarkerClose();
        }

        internal static void LibraryError(string programStage, string err, LogLevel logLevel = LogLevel.Error)
        {
            var message = $"  Program stage \"{programStage}\": {err}";
            Logger?.Log(logLevel, message.Trim());

            if (Strategy == LoggingStrategy.AlwaysStore || (Strategy == LoggingStrategy.Automatic && !Debugger.IsAttached))
            {
                LibraryErrors.Add(message);
            }

            if (Strategy == LoggingStrategy.AlwaysOutputToConsole || (Strategy == LoggingStrategy.Automatic && Debugger.IsAttached))
            {
                ConsoleMarkerOpen();
                foreach (var e in LibraryErrors) Console.WriteLine(e);
                LibraryErrors.Clear();
                Console.WriteLine(message);
                ConsoleMarkerClose();
            }
        }

        private static void ConsoleMarkerOpen()
            => Console.WriteLine("\n\nERROR ".PadRight(54, '*'));

        private static void ConsoleMarkerClose()
            => Console.WriteLine("\n\n".PadLeft(60, '*'));
    }
}
