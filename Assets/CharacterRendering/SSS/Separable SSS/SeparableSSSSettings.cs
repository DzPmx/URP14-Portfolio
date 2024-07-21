using UnityEngine;

[System.Serializable]
[CreateAssetMenu(fileName = "SeparableSSS Settings", menuName = "SeparableSSS Settings")]
public class SeparableSSSSettings : ScriptableObject
{
    [SerializeField] [Range(0, 5)] public float subsurfaceScaler = 0.25f;
    [SerializeField] public Color subsurfaceColor;
    [SerializeField] public Color subsurfaceFalloff;
}