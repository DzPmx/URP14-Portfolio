#ifndef POST_PROCESS_RANDOM
#define  POST_PROCESS_RANDOM

#define NOISE_SIMPLEX_1_DIV_289 0.00346020761245674740484429065744f

float mod289(float x)
{
    return x - floor(x * NOISE_SIMPLEX_1_DIV_289) * 289.0;
}

float2 mod289(float2 x)
{
    return x - floor(x * NOISE_SIMPLEX_1_DIV_289) * 289.0;
}

float3 mod289(float3 x)
{
    return x - floor(x * NOISE_SIMPLEX_1_DIV_289) * 289.0;
}

float4 mod289(float4 x)
{
    return x - floor(x * NOISE_SIMPLEX_1_DIV_289) * 289.0;
}

float permute(float x)
{
    return mod289(x * x * 34.0 + x);
}

float3 permute(float3 x)
{
    return mod289(x * x * 34.0 + x);
}

float4 permute(float4 x)
{
    return mod289(x * x * 34.0 + x);
}

float3 taylorInvSqrt(float3 r)
{
    return 1.79284291400159 - 0.85373472095314 * r;
}

float Rand(float2 n)
{
    return sin(dot(n, half2(1233.224, 1743.335)));
}

inline float randomNoise(float seed)
{
    return Rand(float2(seed, 1.0));
}

inline float randomNoise(float2 seed,float speed)
{
    return frac(sin(dot(seed * floor(_Time.y * speed), float2(17.13, 3.71))) * 43758.5453123);
}

float randomNoise(float2 c)
{
    return frac(sin(dot(c.xy, float2(12.9898, 78.233))) * 43758.5453);
}

float randomNoise(float x, float y)
{
    return frac(sin(dot(float2(x, y), float2(12.9898, 78.233))) * 43758.5453);
}

float snoise(float2 v)
{
    const float4 C = float4(0.211324865405187, // (3.0-sqrt(3.0))/6.0
    0.366025403784439, // 0.5*(sqrt(3.0)-1.0)
    - 0.577350269189626, // -1.0 + 2.0 * C.x
    0.024390243902439); // 1.0 / 41.0
    // First corner
    float2 i = floor(v + dot(v, C.yy));
    float2 x0 = v - i + dot(i, C.xx);
	
    // Other corners
    float2 i1;
    i1.x = step(x0.y, x0.x);
    i1.y = 1.0 - i1.x;
	
    // x1 = x0 - i1  + 1.0 * C.xx;
    // x2 = x0 - 1.0 + 2.0 * C.xx;
    float2 x1 = x0 + C.xx - i1;
    float2 x2 = x0 + C.zz;
	
    // Permutations
    i = mod289(i); // Avoid truncation effects in permutation
    float3 p = permute(permute(i.y + float3(0.0, i1.y, 1.0))
    + i.x + float3(0.0, i1.x, 1.0));
	
    float3 m = max(0.5 - float3(dot(x0, x0), dot(x1, x1), dot(x2, x2)), 0.0);
    m = m * m;
    m = m * m;
	
    // Gradients: 41 points uniformly over a line, mapped onto a diamond.
    // The ring size 17*17 = 289 is close to a multiple of 41 (41*7 = 287)
    float3 x = 2.0 * frac(p * C.www) - 1.0;
    float3 h = abs(x) - 0.5;
    float3 ox = floor(x + 0.5);
    float3 a0 = x - ox;
	
    // Normalise gradients implicitly by scaling m
    m *= taylorInvSqrt(a0 * a0 + h * h);
	
    // Compute final noise value at P
    float3 g;
    g.x = a0.x * x0.x + h.x * x0.y;
    g.y = a0.y * x1.x + h.y * x1.y;
    g.z = a0.z * x2.x + h.z * x2.y;
    return 130.0 * dot(m, g);
}

float trunc(float x, float num_levels)
{
    return floor(x * num_levels) / num_levels;
}
	
float2 trunc(float2 x, float2 num_levels)
{
    return floor(x * num_levels) / num_levels;
}
	
float3 rgb2yuv(float3 rgb)
{
    float3 yuv;
    yuv.x = dot(rgb, float3(0.299, 0.587, 0.114));
    yuv.y = dot(rgb, float3(-0.14713, -0.28886, 0.436));
    yuv.z = dot(rgb, float3(0.615, -0.51499, -0.10001));
    return yuv;
}
	
float3 yuv2rgb(float3 yuv)
{
    float3 rgb;
    rgb.r = yuv.x + yuv.z * 1.13983;
    rgb.g = yuv.x + dot(float2(-0.39465, -0.58060), yuv.yz);
    rgb.b = yuv.x + yuv.y * 2.03211;
    return rgb;
}




#endif