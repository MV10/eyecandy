#version 460
precision highp float;

in vec2 fragCoord;
uniform float yCoord = 0.5;
out vec4 fragColor;

#define threshold 0.001

void main()
{
	fragColor = (fragCoord.y >= yCoord - threshold && fragCoord.y <= yCoord + threshold) ? vec4(1) : vec4(0);
}
