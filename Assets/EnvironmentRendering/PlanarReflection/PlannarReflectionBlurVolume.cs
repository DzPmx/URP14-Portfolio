namespace UnityEngine.Rendering.Universal
{
    [VolumeComponentMenu("Reflection/Planner Reflection Blur")]
    public class PlannarReflectionBlurVolume : VolumeComponent, IPostProcessComponent
    {
        public BoolParameter enableEffect = new BoolParameter(false);
        public ClampedIntParameter blurTimes = new ClampedIntParameter(3, 0, 5);
        public ClampedFloatParameter blurRadius = new ClampedFloatParameter(3f, 0f, 4f);
        public  bool IsActive() => enableEffect == true;
        public  bool IsTileCompatible() => false;
    }
}