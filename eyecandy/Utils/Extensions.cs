
using OpenTK.Graphics.OpenGL;

namespace eyecandy.Utils;

internal static class Extensions
{
    public static TextureUnit ToTextureUnitEnum(this int intTextureUnit)
        => (TextureUnit)(intTextureUnit + (int)TextureUnit.Texture0);

    public static int ToOrdinal(this TextureUnit textureUnit)
        => (int)textureUnit - (int)TextureUnit.Texture0;
}
