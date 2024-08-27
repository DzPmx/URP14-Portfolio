#ifndef OIT_INCLUDED
#define OIT_INLLUDED

//Depth Peeling
//////////////////////////////////////////
inline float4 EncodeFloatRGBA(float v)
{
    float4 kEncodeMul = float4(1.0, 255.0, 65025.0, 16581375.0);
    float kEncodeBit = 1.0 / 255.0;
    float4 enc = kEncodeMul * v;
    enc = frac(enc);
    enc -= enc.yzww * kEncodeBit;
    return enc;
}

inline float DecodeFloatRGBA(float4 enc)
{
    float4 kDecodeDot = float4(1.0, 1 / 255.0, 1 / 65025.0, 1 / 16581375.0);
    return dot(enc, kDecodeDot);
}

//////////////////////////////////////////


//Weighted Blended
/////////////////////////////////////////
float DepthWeightedBlended(float z, float alpha)
{
    #ifdef _WEIGHTED_FUNTION_1
    return alpha*alpha*pow(z, -6);
    #elif _WEIGHTED_FUNTION_2
    return alpha * max(1e-2, min(3 * 1e3, 10.0/(1e-5 + pow(z/5, 2) + pow(z/200, 6))));
    #elif _WEIGHTED_FUNTION_3
    return alpha * max(1e-2, min(3 * 1e3, 0.03/(1e-5 + pow(z/200, 4))));
    #elif _No_WEIGHTED
    return 1.0;
    #endif
    return 1.0;
}
/////////////////////////////////////////
#endif
