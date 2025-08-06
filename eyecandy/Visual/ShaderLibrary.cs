
using Microsoft.Extensions.Logging;
using OpenTK.Graphics.OpenGL;

namespace eyecandy;

/// <summary>
/// A helper class for managing compiled (but not linked) OpenGL shaders.
/// </summary>
public class ShaderLibrary : IDisposable
{
    /// <summary>
    /// OpenGL handle to the compiled and ready to attach/link shader object.
    /// </summary>
    public int Handle;

    /// <summary>
    /// True if no load or compile errors occurred.
    /// </summary>
    public bool IsValid { private set; get; } = true;

    /// <summary>
    /// Path to the library source file.
    /// </summary>
    public string Pathname { private set; get; }

    // used for logging
    private string SourceFile;

    /// <summary>
    /// Produces a compiled shader suitable for linking to one or more shader programs via
    /// the Shader class. This should be attached and detached during program compilation,
    /// but not deleted. The compiled OpenGL object will be deleted when this eyecandy
    /// object is disposed.
    /// </summary>
    public ShaderLibrary(string pathname, ShaderType type = ShaderType.FragmentShader)
    {
        ErrorLogging.Logger?.LogDebug($"{nameof(ShaderLibrary)} constructor loading {type}:\n  {pathname}");

        Pathname = pathname;
        SourceFile = Path.GetFileName(pathname);

        // load
        try
        {
            string shaderSource = File.ReadAllText(pathname);
            Handle = GL.CreateShader(type);
            GL.ShaderSource(Handle, shaderSource);
            ErrorLogging.Logger?.LogDebug($"Shader constructor: file-read completed");
        }
        catch (Exception ex)
        {
            IsValid = false;
            ErrorLogging.EyecandyError($"{nameof(ShaderLibrary)} ctor Read File", $"{ex}: {ex.Message}");
            return;
        }

        // compile
        try
        {
            GL.CompileShader(Handle);
            GL.GetShader(Handle, ShaderParameter.CompileStatus, out int ok);
            if (ok == 0)
            {
                ErrorLogging.EyecandyError($"{nameof(ShaderLibrary)} ctor Compile ", GL.GetShaderInfoLog(Handle));
                IsValid = false;
                return;
            }

            ErrorLogging.Logger?.LogDebug($"{nameof(ShaderLibrary)} constructor: compilation completed");
        }
        catch (Exception ex)
        {
            IsValid = false;
            ErrorLogging.EyecandyError($"{nameof(ShaderLibrary)} ctor Compile", $"{ex}: {ex.Message}");
            return;
        }
    }

    /// <summary/>
    public void Dispose()
    {
        if (IsDisposed) return;
        ErrorLogging.Logger?.LogTrace($"{GetType()}.Dispose() ----------------------------");

        ErrorLogging.Logger?.LogTrace($"  {GetType()}.Dispose() DeleteShader for {SourceFile}");
        GL.DeleteShader(Handle);

        IsDisposed = true;
        GC.SuppressFinalize(this);
    }
    private bool IsDisposed = false;
}
