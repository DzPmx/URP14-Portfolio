using UnityEngine;

[ExecuteAlways]
public class PreIntegratedSkinController : MonoBehaviour
{
    public PreIntegratedSkinSettings settings;
    public float radius;
    public Color Color;
    private Vector4[] gussianWeigt = new Vector4[6];
    
    float[] _Variance = new[] { 0.0064f, 0.0484f, 0.187f, 0.567f, 1.99f, 7.41f };
    float[] _Weight = new[] { 0.233f, 0.100f, 0.118f, 0.113f, 0.358f, 0.078f };
    
    private int varianceID= Shader.PropertyToID("_Variance");
    private int weightID= Shader.PropertyToID("_Weight");
    private int gaussianArray = Shader.PropertyToID("_GaussianArray");

    void Update()
    {
        gussianWeigt = PreIntegratedProfile(radius, Color);
        _Variance = ConvertVarianceToWeight(_Variance);
        Shader.SetGlobalFloatArray(varianceID,_Variance);
        Shader.SetGlobalFloatArray(weightID,_Weight);
        Shader.SetGlobalVectorArray(gaussianArray, gussianWeigt);
    }

    Vector3 Gaussian(float variance, float r, Color color)
    {
        Vector3 g;

        float rr1 = r / (0.001f + color.r);
        g.x = Mathf.Exp((-(rr1 * rr1)) / (2.0f * variance)) / (2.0f * 3.14f * variance);

        float rr2 = r / (0.001f + color.g);
        g.y = Mathf.Exp((-(rr2 * rr2)) / (2.0f * variance)) / (2.0f * 3.14f * variance);

        float rr3 = r / (0.001f + color.b);
        g.z = Mathf.Exp((-(rr3 * rr3)) / (2.0f * variance)) / (2.0f * 3.14f * variance);

        return g;
    }


    Vector4[] PreIntegratedProfile(float r, Color color)
    {
        Vector4[] profile = new Vector4[6];
        profile[0] = Gaussian(0.0064f, r, color);
        profile[1] = Gaussian(0.0484f, r, color);
        profile[2] = Gaussian(0.187f, r, color);
        profile[3] = Gaussian(0.567f, r, color);
        profile[4] = Gaussian(1.99f, r, color);
        profile[5] = Gaussian(7.41f, r, color);
        return profile;
    }
    
    
    float[] ConvertVarianceToWeight(float[] variances)
    {
        float[] weights = new float[variances.Length];
        float sumInverseVariance = 0.0f;

        // 计算方差倒数的和
        for (int i = 0; i < variances.Length; i++)
        {
            sumInverseVariance += 1.0f / variances[i];
        }

        // 计算权重并标准化
        for (int i = 0; i < variances.Length; i++)
        {
            weights[i] = (1.0f / variances[i]) / sumInverseVariance;
        }

        return weights;
    }
}