#version 320 es
layout (location = 0) in float vertexId;
uniform vec2 resolution;
uniform float vertexCount;
uniform float time;
uniform sampler2D wave;
uniform sampler2D volume;
out vec4 v_color;

// lolengine.net/blog/2013/07/27/rgb-to-hsv-in-glsl
vec3 hsv2rgb(vec3 c)
{
    c = vec3(c.x, clamp(c.yz, 0.0, 1.0));
    vec4 K = vec4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
    vec3 p = abs(fract(c.xxx + K.xyz) * 6.0 - K.www);
    return c.z * mix(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y);
}

void main () {
  
  // same as Sound Basics 1
  float norm = (vertexId / vertexCount);
  float x = (norm - 0.5) * 2.0;
  
  // show the entire real-time wave-audio sample
  float y = (texture(wave,  vec2(norm, 0.0)).g * 2.0);

  // grab the volume (single value, no frequency) and use that as the
  // color hue; small multiplier, volume almost never gets near 1.0
  float vol = texture(volume, vec2(0.0, 0.0)).g * 2.0;
  vec3 hsv = vec3(vol, 1.0, 1.0);
  
  gl_Position = vec4(x, y, 0.0, 1.0);
  v_color = vec4(hsv2rgb(hsv), 1.0);

  gl_PointSize = 3.0;

}
