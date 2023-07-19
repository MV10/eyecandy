#version 320 es
precision highp float;

in vec2 textureCoords;
uniform sampler2D audioTexture;
out vec4 outputColor;

void main()
{
    outputColor = texture(audioTexture, textureCoords);
}
