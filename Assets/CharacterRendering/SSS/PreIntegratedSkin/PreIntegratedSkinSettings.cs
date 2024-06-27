using UnityEngine;
using UnityEngine.Serialization;

[System.Serializable]
[CreateAssetMenu(fileName = "PreIntegratedSSS Settings", menuName = "PreIntegratedSSS Settings")]
public class PreIntegratedSkinSettings : ScriptableObject
{
    [SerializeField] public Color subsurfaceColor;
    [SerializeField] public float radius;
}