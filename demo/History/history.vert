#version 320 es

layout(location = 0) in vec3 vertices;
layout(location = 1) in vec2 vertexTexCoords;
out vec2 textureCoords;

void main(void)
{
    textureCoords = vertexTexCoords;
    gl_Position = vec4(vertices, 1.0);
}
