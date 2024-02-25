using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Renderfeature.VolumeStack
{
    [Serializable, VolumeComponentMenu("VolumeTest/Test")]
    public class ColorTint : VolumeComponent, IPostProcessComponent
    {
        private BoolParameter enableEffect = new(true);
        public bool IsActive() => enableEffect == true;
        public bool IsTileCompatible() => false;
        public ColorParameter color = new ColorParameter(Color.white);
    }
}