// 4D musgrave noise (fractal perlin noise), none of which I understand :D
// 
// This is directly copied from Blender (thank you open source <3)
// E.g.:
// - https://github.com/blender/blender/blob/9c0bffcc89f174f160805de042b00ae7c201c40b/source/blender/gpu/shaders/material/gpu_shader_material_noise.glsl#L196
// - https://github.com/blender/blender/blob/9c0bffcc89f174f160805de042b00ae7c201c40b/source/blender/gpu/shaders/material/gpu_shader_material_tex_musgrave.glsl#L692C39-L692C39
// 
// As such this file has a GPL license

float fade(float t) {
    return t * t * t * (t * (t * 6.0 - 15.0) + 10.0);
}

#define rot(x, k) (((x) << (k)) | ((x) >> (32 - (k))))

// We can't do the above mix define, so I took this from:
// https://github.com/blender/blender/blob/9c0bffcc89f174f160805de042b00ae7c201c40b/source/blender/blenlib/intern/noise.cc#L254
float mix(float v0, float v1, float x) {
  return (1 - x) * v0 + x * v1;
}

#define final(a, b, c) \
  { \
    c ^= b; \
    c -= rot(b, 14); \
    a ^= c; \
    a -= rot(c, 11); \
    b ^= a; \
    b -= rot(a, 25); \
    c ^= b; \
    c -= rot(b, 16); \
    a ^= c; \
    a -= rot(c, 4); \
    b ^= a; \
    b -= rot(a, 14); \
    c ^= b; \
    c -= rot(b, 24); \
  }

#define FLOORFRAC(x, x_int, x_fract) { float x_floor = floor(x); x_int = int(x_floor); x_fract = x - x_floor; }

uint hash_uint4(uint kx, uint ky, uint kz, uint kw) {
    uint a, b, c;
    a = b = c = 0xdeadbeefu + (4u << 2u) + 13u;

    a += kx;
    b += ky;
    c += kz;
    mix(a, b, c);

    a += kw;
    final(a, b, c);

    return c;
}

uint hash_int4(int kx, int ky, int kz, int kw) {
    return hash_uint4(uint(kx), uint(ky), uint(kz), uint(kw));
}

float negate_if(float value, uint condition) {
    return (condition != 0u) ? -value : value;
}

float tri_mix(
    float v0,
    float v1,
    float v2,
    float v3,
    float v4,
    float v5,
    float v6,
    float v7,
    float x,
    float y,
    float z
) {
    float x1 = 1.0 - x;
    float y1 = 1.0 - y;
    float z1 = 1.0 - z;
    return z1 * (y1 * (v0 * x1 + v1 * x) + y * (v2 * x1 + v3 * x)) +
         z * (y1 * (v4 * x1 + v5 * x) + y * (v6 * x1 + v7 * x));
}

float quad_mix(
    float v0,
    float v1,
    float v2,
    float v3,
    float v4,
    float v5,
    float v6,
    float v7,
    float v8,
    float v9,
    float v10,
    float v11,
    float v12,
    float v13,
    float v14,
    float v15,
    float x,
    float y,
    float z,
    float w
) {
    return mix(
        tri_mix(v0, v1, v2, v3, v4, v5, v6, v7, x, y, z),
        tri_mix(v8, v9, v10, v11, v12, v13, v14, v15, x, y, z),
        w
    );
}

float noise_scale4(float result) {
    return 0.8344 * result;
}

float noise_grad(uint hash, float x, float y, float z, float w) {
    uint h = hash & 31u;
    float u = h < 24u ? x : y;
    float v = h < 16u ? y : z;
    float s = h < 8u ? z : w;
    return negate_if(u, h & 1u) + negate_if(v, h & 2u) + negate_if(s, h & 4u);
}

float noise_perlin(float4 vec) {
    int X, Y, Z, W;
    float fx, fy, fz, fw;

    FLOORFRAC(vec.x, X, fx);
    FLOORFRAC(vec.y, Y, fy);
    FLOORFRAC(vec.z, Z, fz);
    FLOORFRAC(vec.w, W, fw);

    float u = fade(fx);
    float v = fade(fy);
    float t = fade(fz);
    float s = fade(fw);

    float r = quad_mix(
      noise_grad(hash_int4(X, Y, Z, W), fx, fy, fz, fw),
      noise_grad(hash_int4(X + 1, Y, Z, W), fx - 1.0, fy, fz, fw),
      noise_grad(hash_int4(X, Y + 1, Z, W), fx, fy - 1.0, fz, fw),
      noise_grad(hash_int4(X + 1, Y + 1, Z, W), fx - 1.0, fy - 1.0, fz, fw),
      noise_grad(hash_int4(X, Y, Z + 1, W), fx, fy, fz - 1.0, fw),
    // float offset,
    // float gain,
      noise_grad(hash_int4(X + 1, Y, Z + 1, W), fx - 1.0, fy, fz - 1.0, fw),
      noise_grad(hash_int4(X, Y + 1, Z + 1, W), fx, fy - 1.0, fz - 1.0, fw),
      noise_grad(hash_int4(X + 1, Y + 1, Z + 1, W), fx - 1.0, fy - 1.0, fz - 1.0, fw),
      noise_grad(hash_int4(X, Y, Z, W + 1), fx, fy, fz, fw - 1.0),
      noise_grad(hash_int4(X + 1, Y, Z, W + 1), fx - 1.0, fy, fz, fw - 1.0),
      noise_grad(hash_int4(X, Y + 1, Z, W + 1), fx, fy - 1.0, fz, fw - 1.0),
      noise_grad(hash_int4(X + 1, Y + 1, Z, W + 1), fx - 1.0, fy - 1.0, fz, fw - 1.0),
      noise_grad(hash_int4(X, Y, Z + 1, W + 1), fx, fy, fz - 1.0, fw - 1.0),
      noise_grad(hash_int4(X + 1, Y, Z + 1, W + 1), fx - 1.0, fy, fz - 1.0, fw - 1.0),
      noise_grad(hash_int4(X, Y + 1, Z + 1, W + 1), fx, fy - 1.0, fz - 1.0, fw - 1.0),
      noise_grad(hash_int4(X + 1, Y + 1, Z + 1, W + 1), fx - 1.0, fy - 1.0, fz - 1.0, fw - 1.0),
      u,
      v,
      t,
      s
    );

    return r;
}

float snoise(float4 p) {
    float r = noise_perlin(p);
    return (isinf(r)) ? 0.0 : noise_scale4(r);
}

void node_tex_musgrave_fBm_4d_float(
    float3 co,
    float w,
    float scale,
    float detail,
    float dimension,
    float lac,
    out float fac
) {
    float4 p = float4(co, w) * scale;
    float H = max(dimension, 1e-5);
    float octaves = clamp(detail, 0.0, 15.0);
    float lacunarity = max(lac, 1e-5);

    float value = 0.0;
    float pwr = 1.0;
    float pwHL = pow(lacunarity, -H);

    for (int i = 0; i < int(octaves); i++) {
        value += snoise(p) * pwr;
        pwr *= pwHL;
        p *= lacunarity;
    }

    float rmd = octaves - floor(octaves);
    if (rmd != 0.0) {
        value += rmd * snoise(p) * pwr;
    }

    fac = value;
}

void MusgraveTexture_float(
    float3 uv,
    float w,
    float scale,
    float detail,
    float dimension,
    float lacunarity,
    out float Out
) {
    node_tex_musgrave_fBm_4d_float(
        uv,
        w,
        scale,
        detail,
        dimension,
        lacunarity,
        Out
    );
}
