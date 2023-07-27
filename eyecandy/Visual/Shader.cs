
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
        public int Handle;

        /// <summary>
        /// Querying uniforms is slow, so they are cached at startup.
        /// </summary>
        public Dictionary<string, int> UniformLocations = new();

        /// <summary>
        /// True if no load, compile, or link errors occurred.
        /// </summary>
        public bool IsValid { private set; get; } = true;

        private bool IsDisposed = false;

        // avoid blasting the log with "ignored" messages from every render pass!
        private List<string> IgnoredUniformNames = new();

        /// <summary>
        /// The constructor compiles a new vertex / fragment shader pair.
        /// </summary>
        public Shader(string vertexPathname, string fragmentPathname)
        {
            ErrorLogging.Logger?.LogDebug($"Shader constructor loading:\n  {vertexPathname}\n  {fragmentPathname}");
            ErrorLogging.Logger?.LogDebug($"");

            int VertexShader = 0;
            int FragmentShader = 0;

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
            }

            // compile
            try
            {
                GL.CompileShader(VertexShader);
                GL.GetShader(VertexShader, ShaderParameter.CompileStatus, out int vertOk);
                if (vertOk == 0)
                {
                    ErrorLogging.LibraryError($"{nameof(Shader)} ctor Compile Vert", GL.GetShaderInfoLog(VertexShader));
                    IsValid = false;

                }

                GL.CompileShader(FragmentShader);
                GL.GetShader(FragmentShader, ShaderParameter.CompileStatus, out int fragOk);
                if (fragOk == 0)
                {
                    ErrorLogging.LibraryError($"{nameof(Shader)} ctor Compile Frag", GL.GetShaderInfoLog(VertexShader));
                    IsValid = false;
                }

                ErrorLogging.Logger?.LogDebug($"Shader constructor: compilation completed");
            }
            catch (Exception ex)
            {
                IsValid = false;
                ErrorLogging.LibraryError($"{nameof(Shader)} ctor Compile", $"{ex}: {ex.Message}");
            }

            // link
            try
            {
                Handle = GL.CreateProgram();
                GL.AttachShader(Handle, VertexShader);
                GL.AttachShader(Handle, FragmentShader);
                GL.LinkProgram(Handle);
                GL.GetProgram(Handle, GetProgramParameterName.LinkStatus, out int linkOk);
                if (linkOk == 0)
                {
                    IsValid = false;
                    ErrorLogging.LibraryError($"{nameof(Shader)} ctor Linking", GL.GetProgramInfoLog(Handle));
                }
                ErrorLogging.Logger?.LogDebug($"Shader constructor: linking completed");
            }
            catch (Exception ex)
            {
                IsValid = false;
                ErrorLogging.LibraryError($"{nameof(Shader)} ctor Linking", $"{ex}: {ex.Message}");
            }

            // cleanup
            GL.DetachShader(Handle, VertexShader);
            GL.DetachShader(Handle, FragmentShader);
            GL.DeleteShader(FragmentShader);
            GL.DeleteShader(VertexShader);

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
        {
            if (!UniformLocations.ContainsKey(name))
            {
                if(!IgnoredUniformNames.Contains(name))
                {
                    ErrorLogging.LibraryError($"{nameof(SetTexture)}", $"No uniform named \"{name}\"; ignoring request.", LogLevel.Information);
                    IgnoredUniformNames.Add(name);
                }
                return;
            }
            Use();

            GL.ActiveTexture(unit);
            GL.BindTexture(TextureTarget.Texture2D, handle);

            // Texture0 is 33984, Texture1 is 33985 etc; we want 0, 1, 2, etc.
            int unitOrdinal = (int)unit - (int)TextureUnit.Texture0;

            GL.Uniform1(UniformLocations[name], unitOrdinal);
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
                    ErrorLogging.LibraryError($"{nameof(SetUniform)}", $"No uniform named \"{name}\"; ignoring request.", LogLevel.Information);
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
                    ErrorLogging.LibraryError($"{nameof(SetUniform)}", $"No uniform named \"{name}\"; ignoring request.", LogLevel.Information);
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
                    ErrorLogging.LibraryError($"{nameof(SetUniform)}", $"No uniform named \"{name}\"; ignoring request.", LogLevel.Information);
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
                    ErrorLogging.LibraryError($"{nameof(SetUniform)}", $"No uniform named \"{name}\"; ignoring request.", LogLevel.Information);
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
                    ErrorLogging.LibraryError($"{nameof(SetUniform)}", $"No uniform named \"{name}\"; ignoring request.", LogLevel.Information);
                    IgnoredUniformNames.Add(name);
                }
                return;
            }
            Use();
            GL.Uniform3(UniformLocations[name], data);
        }

        /// <summary/>
        protected virtual void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                GL.DeleteProgram(Handle);
                IsDisposed = true;
            }
        }

        /// <summary/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary/>
        ~Shader()
        {
            if (!IsDisposed)
            {
                throw new InvalidOperationException($"Finalizer: {nameof(Shader)} was not Disposed!");
            }
        }
    }
}
