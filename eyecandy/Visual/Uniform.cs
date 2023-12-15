
using OpenTK.Graphics.OpenGL;

namespace eyecandy;

/// <summary>
/// Stores information about a shader uniform. Populated in the Shader class constructor.
/// </summary>
public readonly record struct Uniform(string Name, int Location, int Size, ActiveUniformType DataType, object DefaultValue);
