
using eyecandy.Utils;
using Microsoft.Extensions.Logging;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace eyecandy;

/// <summary>
/// A helper class for managing OpenGL shaders.
/// </summary>
public class Shader : IDisposable
{
    /// <summary>
    /// OpenGL handle to the linked and ready-to-use shaders.
    /// </summary>
    public int Handle = -1;

    /// <summary>
    /// Querying uniforms is slow, so they are cached at startup.
    /// </summary>
    public Dictionary<string, Uniform> Uniforms;

    /// <summary>
    /// True if no load, compile, or link errors occurred.
    /// </summary>
    public bool IsValid { private set; get; } = true;

    // used for logging
    private string SourceFiles;

    // avoid blasting the log with "ignored" messages from every render pass!
    private List<string> IgnoredUniformNames = new();

    /// <summary>
    /// The constructor compiles a new vertex / fragment shader pair. One or more ShaderLibrary
    /// objects may also be provided to link into the program (these are detached after linking
    /// but not deleted; deletion occurs in the ShaderLibrary.Dispose method).
    /// </summary>
    public Shader(string vertexPathname, string fragmentPathname, params ShaderLibrary[] libs)
    {
        SourceFiles = $"{Path.GetFileName(vertexPathname)} / {Path.GetFileName(fragmentPathname)}";
        var loggerInfo = $"{nameof(Shader)} ctor ({SourceFiles})";

        ErrorLogging.Logger?.LogDebug($"{loggerInfo} loading with {libs.Length} libraries");

        int VertexShader = 0;
        int FragmentShader = 0;

        // check library validity
        foreach(var lib in libs)
        {
            if(!lib.IsValid)
            {
                IsValid = false;
                ErrorLogging.EyecandyError($"{loggerInfo} Library Validation", $"Library {lib.Pathname} is not valid");
                return;
            }
        }

        // load
        try
        {
            string VertexShaderSource = File.ReadAllText(vertexPathname);
            string FragmentShaderSource = File.ReadAllText(fragmentPathname);
            VertexShader = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(VertexShader, VertexShaderSource);
            FragmentShader = GL.CreateShader(ShaderType.FragmentShader);
            //ErrorLogging.OpenGLErrorCheck($"{loggerInfo} after loading vertex shader source file");
            GL.ShaderSource(FragmentShader, FragmentShaderSource);
            //ErrorLogging.OpenGLErrorCheck($"{loggerInfo} after loading fragment shader source file");
            //ErrorLogging.Logger?.LogDebug($"{loggerInfo} file-read completed");
        }
        catch (Exception ex)
        {
            IsValid = false;
            ErrorLogging.EyecandyError($"{loggerInfo} Read File", $"{ex}: {ex.Message}");
            return;
        }

        try
        {
            // create the program object and attach all shader objects; this outer
            // try/finally block ensures detach and deletion in the event of a failure
            Handle = GL.CreateProgram();
            foreach (var lib in libs) GL.AttachShader(Handle, lib.Handle);
            GL.AttachShader(Handle, VertexShader);
            GL.AttachShader(Handle, FragmentShader);
            //ErrorLogging.OpenGLErrorCheck($"{loggerInfo} after program created and shaders attached");

            // compile
            try
            {
                GL.CompileShader(VertexShader);
                GL.GetShader(VertexShader, ShaderParameter.CompileStatus, out int vertOk);
                if (vertOk == 0)
                {
                    ErrorLogging.EyecandyError($"{loggerInfo} Compile Vert", GL.GetShaderInfoLog(VertexShader));
                    IsValid = false;
                    return;
                }

                GL.CompileShader(FragmentShader);
                GL.GetShader(FragmentShader, ShaderParameter.CompileStatus, out int fragOk);
                if (fragOk == 0)
                {
                    ErrorLogging.EyecandyError($"{loggerInfo} Compile Frag", GL.GetShaderInfoLog(FragmentShader));
                    IsValid = false;
                    foreach (var lib in libs) GL.DetachShader(Handle, lib.Handle);
                    GL.DeleteProgram(Handle);
                    Handle = -1;
                    return;
                }

                ErrorLogging.Logger?.LogDebug($"{loggerInfo} compilation completed");
            }
            catch (Exception ex)
            {
                IsValid = false;
                ErrorLogging.EyecandyError($"{loggerInfo} Compile", $"{ex}: {ex.Message}");
                return;
            }
            //ErrorLogging.OpenGLErrorCheck($"{loggerInfo} after compiling shader program");

            // attach and link
            try
            {
                GL.LinkProgram(Handle);
                GL.GetProgram(Handle, GetProgramParameterName.LinkStatus, out int linkOk);
                if (linkOk == 0)
                {
                    IsValid = false;
                    ErrorLogging.EyecandyError($"{loggerInfo} Linking", GL.GetProgramInfoLog(Handle));
                    return;
                }
                ErrorLogging.Logger?.LogDebug($"{loggerInfo} linking completed");
            }
            catch (Exception ex)
            {
                IsValid = false;
                ErrorLogging.EyecandyError($"{loggerInfo} Linking", $"{ex}: {ex.Message}");
                return;
            }
            //ErrorLogging.OpenGLErrorCheck($"{loggerInfo} after linking programs");
        }
        finally
        {
            // cleanup
            GL.DetachShader(Handle, VertexShader);
            GL.DetachShader(Handle, FragmentShader);
            foreach (var lib in libs) GL.DetachShader(Handle, lib.Handle);

            GL.DeleteShader(FragmentShader);
            GL.DeleteShader(VertexShader);
            // libraries are resuable, so deletion occurs in their Dispose moethod

            if (!IsValid)
            {
                GL.DeleteProgram(Handle);
                Handle = -1;
            }
        }
        //ErrorLogging.OpenGLErrorCheck($"{loggerInfo} after cleanup");

        // cache uniform locations; note that uniforms not used by the shader code
        // are not "active" and will not be listed even though they're declared, thus
        // the SetUniform overrides ignore any uniform key-name not in the cache
        GL.GetProgram(Handle, GetProgramParameterName.ActiveUniforms, out var uniformCount);
        ErrorLogging.Logger?.LogDebug($"{loggerInfo} {uniformCount} active uniforms reported");
        Uniforms = new(uniformCount);
        for (var i = 0; i < uniformCount; i++)
        {
            var key = GL.GetActiveUniform(Handle, i, out var size, out var type);
            var location = GL.GetUniformLocation(Handle, key);
            var value = GetUniform(location, type);
            var uniform = new Uniform(key, location, size, type, value);
            Uniforms.Add(key, uniform);
            ErrorLogging.Logger?.LogDebug($"{loggerInfo} caching uniform {key} at location {location}, type {type}, default value {value}");
        }
        //ErrorLogging.OpenGLErrorCheck($"{loggerInfo} after caching uniforms");
    }

    /// <summary>
    /// Wraps the GL.UseProgram call.
    /// </summary>
    public void Use()
    {
        if (!IsValid || IsDisposed)
            throw new InvalidOperationException($"{nameof(Shader)} is invalid (check log output or ErrorLogger properties), or has been disposed.");

        if(ActiveShader != this)
        {
            GL.UseProgram(Handle);
            ActiveShader = this;
        }
    }
    private static Shader ActiveShader;

    /// <summary>
    /// Returns the location handle for a given attribute.
    /// </summary>
    public int GetAttribLocation(string attribName)
    {
        Use();
        return GL.GetAttribLocation(Handle, attribName);
    }

    /// <summary>
    /// Assigns a texture to a shader uniform.
    /// </summary>
    public void SetTexture(AudioTexture audioTexture)
        => SetTexture(audioTexture.UniformName, audioTexture.Handle, audioTexture.AssignedTextureUnit);

    /// <summary>
    /// Assigns a texture to a shader uniform.
    /// </summary>
    public void SetTexture(string name, int handle, TextureUnit unit)
        => SetTexture(name, handle, unit.ToOrdinal());

    /// <summary>
    /// Assigns a texture to a shader uniform.
    /// </summary>
    public void SetTexture(string name, int handle, int unit)
    {
        if (!Uniforms.ContainsKey(name))
        {
            if (!IgnoredUniformNames.Contains(name))
            {
                ErrorLogging.EyecandyError($"{SourceFiles} {nameof(SetTexture)}", $"No uniform named \"{name}\"; ignoring request.", LogLevel.Trace);
                IgnoredUniformNames.Add(name);
            }
            return;
        }
        Use();

        GL.ActiveTexture(unit.ToTextureUnitEnum());
        GL.BindTexture(TextureTarget.Texture2D, handle);
        GL.Uniform1(Uniforms[name].Location, unit);
    }

    /// <summary>
    /// Assigns a value to a shader uniform.
    /// </summary>
    public void SetUniform(string name, int data)
    {
        if (!Uniforms.ContainsKey(name))
        {
            if (!IgnoredUniformNames.Contains(name))
            {
                ErrorLogging.EyecandyError($"{SourceFiles} {nameof(SetUniform)}", $"No uniform named \"{name}\"; ignoring request.", LogLevel.Trace);
                IgnoredUniformNames.Add(name);
            }
            return;
        }
        Use();
        GL.Uniform1(Uniforms[name].Location, data);
    }

    /// <summary>
    /// Assigns a value to a shader uniform.
    /// </summary>
    public void SetUniform(string name, float data)
    {
        if (!Uniforms.ContainsKey(name))
        {
            if (!IgnoredUniformNames.Contains(name))
            {
                ErrorLogging.EyecandyError($"{SourceFiles} {nameof(SetUniform)}", $"No uniform named \"{name}\"; ignoring request.", LogLevel.Trace);
                IgnoredUniformNames.Add(name);
            }
            return;
        }
        Use();
        GL.Uniform1(Uniforms[name].Location, data);
    }

    /// <summary>
    /// Assigns a value to a shader uniform.
    /// </summary>
    public void SetUniform(string name, Matrix4 data)
    {
        if (!Uniforms.ContainsKey(name))
        {
            if (!IgnoredUniformNames.Contains(name))
            {
                ErrorLogging.EyecandyError($"{SourceFiles} {nameof(SetUniform)}", $"No uniform named \"{name}\"; ignoring request.", LogLevel.Trace);
                IgnoredUniformNames.Add(name);
            }
            return;
        }
        Use();
        GL.UniformMatrix4(Uniforms[name].Location, transpose: true, ref data);
    }

    /// <summary>
    /// Assigns a value to a shader uniform.
    /// </summary>
    public void SetUniform(string name, Vector2 data)
    {
        if (!Uniforms.ContainsKey(name))
        {
            if (!IgnoredUniformNames.Contains(name))
            {
                ErrorLogging.EyecandyError($"{SourceFiles} {nameof(SetUniform)}", $"No uniform named \"{name}\"; ignoring request.", LogLevel.Trace);
                IgnoredUniformNames.Add(name);
            }
            return;
        }
        Use();
        GL.Uniform2(Uniforms[name].Location, data);
    }

    /// <summary>
    /// Assigns a value to a shader uniform.
    /// </summary>
    public void SetUniform(string name, Vector2i data)
    {
        if (!Uniforms.ContainsKey(name))
        {
            if (!IgnoredUniformNames.Contains(name))
            {
                ErrorLogging.EyecandyError($"{SourceFiles} {nameof(SetUniform)}", $"No uniform named \"{name}\"; ignoring request.", LogLevel.Trace);
                IgnoredUniformNames.Add(name);
            }
            return;
        }
        Use();
        GL.Uniform2(Uniforms[name].Location, data);
    }

    /// <summary>
    /// Assigns a value to a shader uniform.
    /// </summary>
    public void SetUniform(string name, Vector3 data)
    {
        if (!Uniforms.ContainsKey(name))
        {
            if (!IgnoredUniformNames.Contains(name))
            {
                ErrorLogging.EyecandyError($"{SourceFiles} {nameof(SetUniform)}", $"No uniform named \"{name}\"; ignoring request.", LogLevel.Trace);
                IgnoredUniformNames.Add(name);
            }
            return;
        }
        Use();
        GL.Uniform3(Uniforms[name].Location, data);
    }

    /// <summary>
    /// Assigns a value to a shader uniform.
    /// </summary>
    public void SetUniform(string name, Vector4 data)
    {
        if (!Uniforms.ContainsKey(name))
        {
            if (!IgnoredUniformNames.Contains(name))
            {
                ErrorLogging.EyecandyError($"{SourceFiles} {nameof(SetUniform)}", $"No uniform named \"{name}\"; ignoring request.", LogLevel.Trace);
                IgnoredUniformNames.Add(name);
            }
            return;
        }
        Use();
        GL.Uniform4(Uniforms[name].Location, data);
    }

    /// <summary>
    /// Retrieves the current value of a shader uniform. Currently only
    /// float data types are supported by this functionality.
    /// </summary>
    public object GetUniform(string name)
    {
        if (!Uniforms.ContainsKey(name) || Uniforms[name].DataType != ActiveUniformType.Float) return null;
        return GetUniform(Uniforms[name].Location, Uniforms[name].DataType);
    }

    /// <summary>
    /// Retrieves the current value of a shader uniform. Currently only
    /// float data types are supported by this functionality.
    /// </summary>
    public object GetUniform(int location, ActiveUniformType type)
    {
        switch(type)
        {
            case ActiveUniformType.Float:
                GL.GetUniform(Handle, location, out float f);
                return f;

            default:
                return null;
        }
    }

    /// <summary>
    /// Resets uniforms to their original values loaded in the constructor after linking.
    /// Currently only floats are supported.
    /// </summary>
    public void ResetUniforms()
    {
        foreach(var kvp in Uniforms)
        {
            var u = kvp.Value;
            if(u.DefaultValue is not null)
            {
                switch (u.DataType)
                {
                    case ActiveUniformType.Float:
                        SetUniform(u.Name, (float)u.DefaultValue);
                        break;

                    default:
                        break;
                }
            }
        }
    }

    /// <summary/>
    public void Dispose()
    {
        if (IsDisposed) return;
        ErrorLogging.Logger?.LogTrace($"{GetType()}.Dispose() ----------------------------");

        ErrorLogging.Logger?.LogTrace($"  {GetType()}.Dispose() DeleteProgram for {SourceFiles}");
        GL.DeleteProgram(Handle);

        if (ActiveShader == this) ActiveShader = null;

        IsDisposed = true;
        GC.SuppressFinalize(this);
    }
    private bool IsDisposed = false;
}
