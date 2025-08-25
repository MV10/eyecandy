
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

    private readonly ILogger Logger;

    /// <summary>
    /// Produces a compiled shader suitable for linking to one or more shader programs via
    /// the Shader class. This should be attached and detached during program compilation,
    /// but not deleted. The compiled OpenGL object will be deleted when this eyecandy
    /// object is disposed.
    /// </summary>
    public ShaderLibrary(string pathname, ShaderType type = ShaderType.FragmentShader)
    {
        Logger = ErrorLogging.LoggerFactory?.CreateLogger("Eyecandy." + nameof(ShaderLibrary));
        Logger?.LogDebug($"Constructor loading {type} from {pathname}");

        Pathname = pathname;
        SourceFile = Path.GetFileName(pathname);

        // load
        try
        {
            string shaderSource = File.ReadAllText(pathname);
            Handle = GL.CreateShader(type);
            GL.ShaderSource(Handle, shaderSource);
            Logger?.LogDebug($"Constructor completed reading {SourceFile}");
        }
        catch (Exception ex)
        {
            IsValid = false;
            Logger?.LogError($"Constructor reading {SourceFile} {ex}: {ex.Message}");
            return;
        }

        // compile
        try
        {
            GL.CompileShader(Handle);
            GL.GetShader(Handle, ShaderParameter.CompileStatus, out int ok);
            if (ok == 0)
            {
                Logger?.LogError($"Constructor compiling {SourceFile} {GL.GetShaderInfoLog(Handle)}");
                IsValid = false;
                return;
            }

            Logger?.LogDebug($"Constructor compilation completed for {SourceFile}");
        }
        catch (Exception ex)
        {
            IsValid = false;
            Logger?.LogError($"Constructor compiling {SourceFile} {ex}: {ex.Message}");
            return;
        }
    }

    /// <summary/>
    public void Dispose()
    {
        if (IsDisposed) return;
        Logger?.LogTrace("Dispose() ----------------------------");

        Logger?.LogTrace($"  Dispose() DeleteShader for {SourceFile}");
        GL.DeleteShader(Handle);

        IsDisposed = true;
        GC.SuppressFinalize(this);
    }
    private bool IsDisposed = false;
}
