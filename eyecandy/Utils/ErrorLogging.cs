using OpenTK.Audio.OpenAL;
using OpenTK.Graphics.OpenGL;

namespace eyecandy
{
    public static class ErrorLogging
    {
        /// <summary>
        /// If false, errors are immediately written to the Console instead of stored. In Release
        /// mode this defaults to true. In Debug mode it defaults to false.
        /// </summary>
#if DEBUG
        public static bool StoreErrors = false;
#else
        public static bool StoreErrors = true;
#endif

        /// <summary>
        /// Any error messages collected by calls to OpenGLErrorCheck (assuming StoreErrors is true).
        /// </summary>
        public static List<string> OpenGLErrors = new();

        /// <summary>
        /// Any error messages collected by calls to OpenALErrorCheck (assuming StoreErrors is true).
        /// </summary>
        public static List<string> OpenALErrors = new();

        /// <summary>
        /// Any file-loading or shader compilation errors (assuming StoreErrors is true). 
        /// </summary>
        public static List<string> ShaderErrors = new();

        /// <summary>
        /// True if either OpenGL or OpenAL errors have been collected.
        /// </summary>
        public static bool HasErrors = (OpenGLErrors.Count > 0 || OpenALErrors.Count > 0 || ShaderErrors.Count > 0);

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
            foreach (var e in ShaderErrors) Console.WriteLine(e);
            foreach (var e in OpenALErrors) Console.WriteLine(e);
            ConsoleMarkerClose();
            if (purge)
            {
                OpenGLErrors.Clear();
                ShaderErrors.Clear();
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
                if (StoreErrors)
                {
                    storage.Add(message);
                }
                else
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

        internal static void ShaderError(string programStage, string err)
        {
            var message = $"  Program stage \"{programStage}\": {err}";
            if (StoreErrors)
            {
                ShaderErrors.Add(message);
            }
            else
            {
                ConsoleMarkerOpen();
                foreach (var e in ShaderErrors) Console.WriteLine(e);
                ShaderErrors.Clear();
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
