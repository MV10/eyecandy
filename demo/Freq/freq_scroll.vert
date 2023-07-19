#version 320 es

layout (location = 0) in float vertexId;
uniform vec2 resolution;
uniform float vertexCount;
uniform float time;
uniform sampler2D sound;
uniform sampler2D volume;
out vec4 v_color;

// This is something I wrote on VertexShaderArt.com, except that
// this program uses the green channel instead of alpha, and the
// frequency data is quite a bit more subtle, requiring a large
// multiplier.

void main () {
  
  // normalize the ID; 0.0 to 1.0 across the range of input IDs
  float norm = (vertexId / vertexCount);
  
  // expand x position to cover entire display area
  // we're shifting norm 0.0 to 1.0 to cover display -1.0 to +1.0
  // mathematically identical: norm * 2.0 - 1.0
  float x = (norm - 0.5) * 2.0;
  
  // sample the texture history data (0.0 to 1.0 via norm)
  // this will be the point's y-pos with offsets to separate them
  // note the first array index is frequency; x=0.015 is all bass
  float ySnd = (texture(sound,  vec2(0.01, norm)).g * 3.0);
  float yVol = (texture(volume, vec2(0.0, norm)).g - 0.7);

  // even/odd IDs alternate between textures and colors
  // it would be more readable and flexible to do this with arrays
  // but WebGL can't handle variable array indexing (yeah WTF?)
  // https://stackoverflow.com/a/30648046/152997
  float a = mod(vertexId, 2.0);
  float b = step(a, 0.0);

  // one y is multiplied by 1, the other is multiplied by 0
  float y = (ySnd * a) + (yVol * b);
  
  // same for colors, while = sound tex, green = volume tex
  vec3 color = (vec3(1.0) * a) + (vec3(0.0, 1.0, 0.0) * b);

  gl_Position = vec4(x, y, 0.0, 1.0);
  v_color = vec4(color, 1.0);

  // fat points easier to see at hi-res full-screen
  gl_PointSize = 5.0;

}
