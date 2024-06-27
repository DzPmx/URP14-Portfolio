using UnityEngine;

[System.Serializable]
[CreateAssetMenu(fileName = "Burley Normalized SSS Settings", menuName = "Burley Normalized SSS Settings")]
public class BurleyNormalizedSSSSettings : ScriptableObject
{
    [SerializeField][ColorUsage(false, true)] public Color scatteringDistance = Color.black;
    [SerializeField]public float worldScale = 1f;
}