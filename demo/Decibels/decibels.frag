#version 320 es
precision highp float;

in vec2 fragCoord;
uniform vec2 iResolution;
uniform sampler2D fftdb;
uniform sampler2D webaudio;
out vec4 fragColor;

float plot(float uvy, float liney, float lineWidth)
{
    float pos = smoothstep(uvy - lineWidth, uvy, liney) - smoothstep(uvy, uvy + lineWidth, liney);
	return pos;
}

void main()
{
	vec2 uv = fragCoord;
    float fft = texture(fftdb, vec2(uv.x, 0.25)).g;
    float web = texture(webaudio, vec2(uv.x, 0.25)).g;
    float f = plot(uv.y*2.0 - 1.0, fft, 0.01);
    float w = plot(uv.y*2.0 - 0.3, web, 0.01);
 	fragColor = vec4(f, w, 0.0, 1.0);
}
