using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Serialization;

namespace RenderFeature.PostProcessSystem.Glitch
{
    [VolumeComponentMenu("DZ Post Processing/Glitch/Digital Stripe")]
    public class DigitalStripe : global::PostProcessSystem
    {
        public BoolParameter enableEffect = new BoolParameter(false);
        public ClampedFloatParameter intensity = new ClampedFloatParameter(0.1f, 0, 1);
        public ClampedIntParameter frequency = new ClampedIntParameter(25, 0, 100);
        public ClampedFloatParameter stripeLength = new ClampedFloatParameter(0.5f, 0 ,0.99f);
        public ClampedIntParameter noiseTextureWidth = new ClampedIntParameter(20, 8, 256);
        public ClampedIntParameter noiseTextureHeight = new ClampedIntParameter(20, 8, 256);
        public BoolParameter needStripColorAdjust = new BoolParameter(false);
        public ColorParameter stripColorAdjustColor = new ColorParameter(new Color(1f, 0f, 1f),true,true,false);
        public ClampedFloatParameter stripColorAdjustIntensity = new ClampedFloatParameter(2f, 0, 10f);
        public override bool IsActive() => enableEffect == true;
        public override bool IsTileCompatible() => false;
        public override int OrderInInjectionPoint => 6;
        public override CustomPostProcessInjectPoint injectPoint => CustomPostProcessInjectPoint.BeforePostProcess;

        private Material material;
        private const string shaderName = "MyURPShader/PostProcessing/URP_PostProcessing_Glitch";
        Texture2D noiseTexture;
        RenderTexture trashFrame1;
        RenderTexture trashFrame2;
        private int digitalStripeIntensityID = Shader.PropertyToID("_DigitalStripeIntensity");
        private int noiseTexID = Shader.PropertyToID("_NoiseTex");
        private int stripColorAdjustColorID = Shader.PropertyToID("_DigitalStripeColorAdjustColor");
        private int stripColorAdjustIntensityID = Shader.PropertyToID("_DigitalStripeColorAdjustIntensity");
        public override void Setup()
        {
            if (IsActive())
            {
                if (material == null) material = CoreUtils.CreateEngineMaterial(shaderName);
            }
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            UpdateNoiseTexture(frequency.value, noiseTextureWidth.value,noiseTextureHeight.value, stripeLength.value);
            material.SetFloat(digitalStripeIntensityID,intensity.value);
            if (noiseTexture != null)
            {
                material.SetTexture(noiseTexID, noiseTexture);
            }
            if (needStripColorAdjust.value)
            {
                material.EnableKeyword("NEED_TRASH_FRAME");
                material.SetColor(stripColorAdjustColorID, stripColorAdjustColor.value);
                material.SetFloat(stripColorAdjustIntensityID, stripColorAdjustIntensity.value);
            }
            else
            {
                material.DisableKeyword("NEED_TRASH_FRAME");
            }
            
        }

        public override void Render(CommandBuffer cmd, ref RenderingData renderingData, RTHandle source, RTHandle dest)
        {
            Blitter.BlitCameraTexture(cmd,source,dest,material,(int)GlitchPass.DigitalStripe);
        }

        public override void Dispose(bool disposing)
        {
            CoreUtils.Destroy(material);
        }
        
        void UpdateNoiseTexture(int frame, int noiseTexWidth, int noiseTexHeight, float stripLength)
        {
            int frameCount = Time.frameCount;
            if (frameCount % frame != 0)
            {
                return;
            }

            noiseTexture = new Texture2D(noiseTexWidth, noiseTexHeight, TextureFormat.ARGB32, false);
            noiseTexture.wrapMode = TextureWrapMode.Clamp;
            noiseTexture.filterMode = FilterMode.Point;

            trashFrame1 = new RenderTexture(Screen.width, Screen.height, 0);
            trashFrame2 = new RenderTexture(Screen.width, Screen.height, 0);
            trashFrame1.hideFlags = HideFlags.DontSave;
            trashFrame2.hideFlags = HideFlags.DontSave;

            Color32 color = RandomColor();

            for (int y = 0; y < noiseTexture.height; y++)
            {
                for (int x = 0; x < noiseTexture.width; x++)
                {
                    //随机值若大于给定strip随机阈值，重新随机颜色
                    if (UnityEngine.Random.value > stripLength)
                    {
                        color = RandomColor();
                    }
                    //设置贴图像素值
                    noiseTexture.SetPixel(x, y, color);
                }
            }
            noiseTexture.Apply();
            var bytes = noiseTexture.EncodeToPNG();
        }
        
        public static Color RandomColor()
        {
            return new Color(Random.value, Random.value, Random.value, Random.value);
        }
    }
}