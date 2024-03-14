#ifndef SPHERICAL_GAUSSIAN_SSS
#define SPHERICAL_GAUSSIAN_SSS

struct SG
{
  float3 Amplitude;  // float3或者float皆可，按需求设定
  float3 Axis;
  float Sharpness;
}

float3 EvaluateSG( SG sg, float3 dir)
{
  float cosAngle = dot(dir, sg.Axis);
  return sg.Amplitude * exp(sg.Sharpness * (cosAngle - 1.0f));
}

// SG Kernel
SG MakeNormalizedSG(float3 LightDir, half Sharpness, half3 LightIntensity)
{
    // 归一化的SG
    SphericalGaussian SG;
    SG.Axis = float3(0, 1, 0); // 任意方向
    SG.Sharpness = Sharpness; // (1 / ScatterAmt.element)
    SG.Amplitude = SG.Sharpness / ((2 * PI) - (2 * PI) * exp(-2 * SG.Sharpness)); // 归一化处理

    // 对齐轴向，乘上光源强度(颜色)
    SG.Axis = LightDir;
    SG.Amplitude *= LightIntensity;
    return SG;
}

float3 SGIrradianceFitted(in SG lightingLobe, in float3 normal)
{
    const float muDotN = dot(lightingLobe.Axis, normal);
    const float lambda = lightingLobe.Sharpness;
 
    const float c0 = 0.36f;
    const float c1 = 1.0f / (4.0f * c0);
 
    float eml  = exp(-lambda);
    float em2l = eml * eml;
    float rl   = rcp(lambda);
 
    float scale = 1.0f + 2.0f * em2l - rl;
    float bias  = (eml - em2l) * rl - em2l;
 
    float x  = sqrt(1.0f - scale);
    float x0 = c0 * muDotN;
    float x1 = c1 * x;
 
    float n = x0 + x1;
 
    float y = saturate(muDotN);
    if(abs(x0) <= x1)
        y = n * n / x;
 
    float result = scale * y + bias;
 
    return result;
}

// Inner product with cosine lobe
    // Assumes G is normalized
float3 DotCosineLobe( FSphericalGaussian G, float3 N )
{
    const float muDotN = dot( G.Axis, N );

    const float c0 = 0.36;
    const float c1 = 0.25 / c0;

    float eml  = exp( -G.Sharpness );
    float em2l = eml * eml;
    float rl   = rcp( G.Sharpness );
    
    float scale = 1.0f + 2.0f * em2l - rl;
    float bias  = (eml - em2l) * rl - em2l;

    float x = sqrt( 1.0 - scale );
    float x0 = c0 * muDotN;
    float x1 = c1 * x;

    float n = x0 + x1;
    float y = ( abs( x0 ) <= x1 ) ? n * n / x : saturate( muDotN );

    return scale * y + bias;
}

// Compute the irradiance that would result from convolving a punctual light source
// with the SG filtering kernels
float3 SGSGDiffuseLighting (float3 N, float3 lightDir, float3 ScatterAmt)
{
    FSphericalGaussian redKernel   = MakeNormalizedSG(lightDir, 1.0f / max(ScatterAmt.x, 0.0001f));
    FSphericalGaussian greenKernel = MakeNormalizedSG(lightDir, 1.0f / max(ScatterAmt.y, 0.0001f));
    FSphericalGaussian blueKernel  = MakeNormalizedSG(lightDir, 1.0f / max(ScatterAmt.z, 0.0001f));

    float3 SGDiffuse = float3(DotCosineLobe(redKernel, N).x,
    DotCosineLobe(greenKernel, N).x,
    DotCosineLobe(blueKernel, N).x);

    // Below is Diffuse Tonemapping, without it, sss will be purple or grey.
    // Cuz too much red radiance scatter out from skin.
    // Tonemap_Filmic_UC2DefaultToGamma
    // Uncharted II fixed tonemapping formula.
    // The linear to sRGB conversion is baked in.
    half3 x = max(0, (SGDiffuse - 0.004));
    half3 DiffuseTonemapping = \
    lerp(SGDiffuse, (x * (6.2 * x + 0.5)) / (x * (6.2 * x + 1.7) + 0.06), 1);
    SGDiffuse = lerp(SGDiffuse, DiffuseTonemapping, 1); // 1 is complete toonmapping.

    return SGDiffuse;
}

#endif 