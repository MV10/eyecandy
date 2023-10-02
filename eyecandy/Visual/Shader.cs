
using eyecandy.Utils;
using Microsoft.Extensions.Logging;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace eyecandy
{
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
        public Dictionary<string, int> UniformLocations = new();

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
            ErrorLogging.Logger?.LogDebug($"Shader constructor loading:\n  {vertexPathname}\n  {fragmentPathname}\n  {libs.Length} libraries");

            SourceFiles = $"{Path.GetFileName(vertexPathname)} / {Path.GetFileName(fragmentPathname)}";

            int VertexShader = 0;
            int FragmentShader = 0;

            // check library validity
            foreach(var lib in libs)
            {
                if(!lib.IsValid)
                {
                    IsValid = false;
                    ErrorLogging.LibraryError($"{nameof(Shader)} ctor Library Validation", $"Library {lib.Pathname} is not valid");
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
                GL.ShaderSource(FragmentShader, FragmentShaderSource);
                ErrorLogging.Logger?.LogDebug($"Shader constructor: file-read completed");
            }
            catch (Exception ex)
            {
                IsValid = false;
                ErrorLogging.LibraryError($"{nameof(Shader)} ctor Read File", $"{ex}: {ex.Message}");
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

                // compile
                try
                {
                    GL.CompileShader(VertexShader);
                    GL.GetShader(VertexShader, ShaderParameter.CompileStatus, out int vertOk);
                    if (vertOk == 0)
                    {
                        ErrorLogging.LibraryError($"{nameof(Shader)} ctor Compile Vert", GL.GetShaderInfoLog(VertexShader));
                        IsValid = false;
                        return;
                    }

                    GL.CompileShader(FragmentShader);
                    GL.GetShader(FragmentShader, ShaderParameter.CompileStatus, out int fragOk);
                    if (fragOk == 0)
                    {
                        ErrorLogging.LibraryError($"{nameof(Shader)} ctor Compile Frag", GL.GetShaderInfoLog(FragmentShader));
                        IsValid = false;
                        foreach (var lib in libs) GL.DetachShader(Handle, lib.Handle);
                        GL.DeleteProgram(Handle);
                        Handle = -1;
                        return;
                    }

                    ErrorLogging.Logger?.LogDebug($"Shader constructor: compilation completed");
                }
                catch (Exception ex)
                {
                    IsValid = false;
                    ErrorLogging.LibraryError($"{nameof(Shader)} ctor Compile", $"{ex}: {ex.Message}");
                    return;
                }

                // attach and link
                try
                {
                    GL.LinkProgram(Handle);
                    GL.GetProgram(Handle, GetProgramParameterName.LinkStatus, out int linkOk);
                    if (linkOk == 0)
                    {
                        IsValid = false;
                        ErrorLogging.LibraryError($"{nameof(Shader)} ctor Linking", GL.GetProgramInfoLog(Handle));
                        return;
                    }
                    ErrorLogging.Logger?.LogDebug($"Shader constructor: linking completed");
                }
                catch (Exception ex)
                {
                    IsValid = false;
                    ErrorLogging.LibraryError($"{nameof(Shader)} ctor Linking", $"{ex}: {ex.Message}");
                    return;
                }
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

            // cache uniform locations; note that uniforms not used by the shader code
            // are not "active" and will not be listed even though they're declared, thus
            // the SetUniform overrides ignore any uniform key-name not in the cache
            GL.GetProgram(Handle, GetProgramParameterName.ActiveUniforms, out var uniformCount);
            ErrorLogging.Logger?.LogDebug($"Shader constructor: {uniformCount} active uniforms reported");
            for (var i = 0; i < uniformCount; i++)
            {
                var key = GL.GetActiveUniform(Handle, i, out _, out _);
                var location = GL.GetUniformLocation(Handle, key);
                UniformLocations.Add(key, location);
                ErrorLogging.Logger?.LogDebug($"Shader constructor: caching uniform {key} at location {location}");
            }

            ErrorLogging.OpenGLErrorCheck($"{nameof(Shader)} ctor");
        }

        /// <summary>
        /// Wraps the GL.UseProgram call.
        /// </summary>
        public void Use()
        {
            if (!IsValid || IsDisposed)
                throw new InvalidOperationException($"{nameof(Shader)} is invalid (check log output or ErrorLogger properties), or has been disposed.");

            GL.UseProgram(Handle);
        }

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
            if (!UniformLocations.ContainsKey(name))
            {
                if (!IgnoredUniformNames.Contains(name))
                {
                    ErrorLogging.LibraryError($"{SourceFiles} {nameof(SetTexture)}", $"No uniform named \"{name}\"; ignoring request.", LogLevel.Trace);
                    IgnoredUniformNames.Add(name);
                }
                return;
            }
            Use();

            GL.ActiveTexture(unit.ToTextureUnitEnum());
            GL.BindTexture(TextureTarget.Texture2D, handle);
            GL.Uniform1(UniformLocations[name], unit);
        }

        /// <summary>
        /// Assigns a value to a shader uniform.
        /// </summary>
        public void SetUniform(string name, int data)
        {
            if (!UniformLocations.ContainsKey(name))
            {
                if (!IgnoredUniformNames.Contains(name))
                {
                    ErrorLogging.LibraryError($"{SourceFiles} {nameof(SetUniform)}", $"No uniform named \"{name}\"; ignoring request.", LogLevel.Trace);
                    IgnoredUniformNames.Add(name);
                }
                return;
            }
            Use();
            GL.Uniform1(UniformLocations[name], data);
        }

        /// <summary>
        /// Assigns a value to a shader uniform.
        /// </summary>
        public void SetUniform(string name, float data)
        {
            if (!UniformLocations.ContainsKey(name))
            {
                if (!IgnoredUniformNames.Contains(name))
                {
                    ErrorLogging.LibraryError($"{SourceFiles} {nameof(SetUniform)}", $"No uniform named \"{name}\"; ignoring request.", LogLevel.Trace);
                    IgnoredUniformNames.Add(name);
                }
                return;
            }
            Use();
            GL.Uniform1(UniformLocations[name], data);
        }

        /// <summary>
        /// Assigns a value to a shader uniform.
        /// </summary>
        public void SetUniform(string name, Matrix4 data)
        {
            if (!UniformLocations.ContainsKey(name))
            {
                if (!IgnoredUniformNames.Contains(name))
                {
                    ErrorLogging.LibraryError($"{SourceFiles} {nameof(SetUniform)}", $"No uniform named \"{name}\"; ignoring request.", LogLevel.Trace);
                    IgnoredUniformNames.Add(name);
                }
                return;
            }
            Use();
            GL.UniformMatrix4(UniformLocations[name], transpose: true, ref data);
        }

        /// <summary>
        /// Assigns a value to a shader uniform.
        /// </summary>
        public void SetUniform(string name, Vector2 data)
        {
            if (!UniformLocations.ContainsKey(name))
            {
                if (!IgnoredUniformNames.Contains(name))
                {
                    ErrorLogging.LibraryError($"{SourceFiles} {nameof(SetUniform)}", $"No uniform named \"{name}\"; ignoring request.", LogLevel.Trace);
                    IgnoredUniformNames.Add(name);
                }
                return;
            }
            Use();
            GL.Uniform2(UniformLocations[name], data);
        }

        /// <summary>
        /// Assigns a value to a shader uniform.
        /// </summary>
        public void SetUniform(string name, Vector3 data)
        {
            if (!UniformLocations.ContainsKey(name))
            {
                if (!IgnoredUniformNames.Contains(name))
                {
                    ErrorLogging.LibraryError($"{SourceFiles} {nameof(SetUniform)}", $"No uniform named \"{name}\"; ignoring request.", LogLevel.Trace);
                    IgnoredUniformNames.Add(name);
                }
                return;
            }
            Use();
            GL.Uniform3(UniformLocations[name], data);
        }

        /// <summary>
        /// Assigns a value to a shader uniform.
        /// </summary>
        public void SetUniform(string name, Vector4 data)
        {
            if (!UniformLocations.ContainsKey(name))
            {
                if (!IgnoredUniformNames.Contains(name))
                {
                    ErrorLogging.LibraryError($"{SourceFiles} {nameof(SetUniform)}", $"No uniform named \"{name}\"; ignoring request.", LogLevel.Trace);
                    IgnoredUniformNames.Add(name);
                }
                return;
            }
            Use();
            GL.Uniform4(UniformLocations[name], data);
        }

        /// <summary/>
        public void Dispose()
        {
            if (IsDisposed) return;
            ErrorLogging.Logger?.LogTrace($"{GetType()}.Dispose() ----------------------------");

            ErrorLogging.Logger?.LogTrace($"  {GetType()}.Dispose() DeleteProgram for {SourceFiles}");
            GL.DeleteProgram(Handle);

            IsDisposed = true;
            GC.SuppressFinalize(this);
        }
        private bool IsDisposed = false;
    }
}
