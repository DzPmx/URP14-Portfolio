#ifndef BURLEY_NORMALIZED_SSS_INCLUDED
#define BURLEY_NORMALIZED_SSS_INCLUDED


//4像素一个采样点
#define SSS_PIXELS_PER_SAMPLE 4

void SampleBurleyDiffusionProfile(float u, float rcpS, out float r, out float rcpPdf)
{
    u = 1 - u; // Convert CDF to CCDF

    float g = 1 + (4 * u) * (2 * u + sqrt(1 + (4 * u) * u));
    float n = exp2(log2(g) * (-1.0 / 3.0)); // g^(-1/3)
    float p = (g * n) * n; // g^(+1/3)
    float c = 1 + p + n; // 1 + g^(+1/3) + g^(-1/3)
    float d = (3 / LOG2_E * 2) + (3 / LOG2_E) * log2(u); // 3 * Log[4 * u]
    float x = (3 / LOG2_E) * log2(c) - d; // 3 * Log[c / (4 * u)]

    // x      = s * r
    // exp_13 = Exp[-x/3] = Exp[-1/3 * 3 * Log[c / (4 * u)]]
    // exp_13 = Exp[-Log[c / (4 * u)]] = (4 * u) / c
    // exp_1  = Exp[-x] = exp_13 * exp_13 * exp_13
    // expSum = exp_1 + exp_13 = exp_13 * (1 + exp_13 * exp_13)
    // rcpExp = rcp(expSum) = c^3 / ((4 * u) * (c^2 + 16 * u^2))
    float rcpExp = ((c * c) * c) * rcp((4 * u) * ((c * c) + (4 * u) * (4 * u)));

    r = x * rcpS;
    rcpPdf = (8 * PI * rcpS) * rcpExp; // (8 * Pi) / s / (Exp[-s * r / 3] + Exp[-s * r])
}

float3 EvalBurleyDiffusionProfile(float r, float3 S)
{
    float3 exp_13 = exp2(((LOG2_E * (-1.0 / 3.0)) * r) * S); // Exp[-S * r / 3]
    float3 expSum = exp_13 * (1 + exp_13 * exp_13); // Exp[-S * r / 3] + Exp[-S * r]

    return (S * rcp(8 * PI)) * expSum; // S / (8 * Pi) * (Exp[-S * r / 3] + Exp[-S * r])
}

float3 ComputeBilateralWeight(float xy2, float z, float mmPerUnit, float3 S, float rcpPdf)
{
    //如果想简化计算z可以不考虑
    // z = 0;
    float r = sqrt(xy2 + (z * mmPerUnit) * (z * mmPerUnit));
    float area = rcpPdf;
    return saturate(EvalBurleyDiffusionProfile(r, S) * area);
}

#endif
