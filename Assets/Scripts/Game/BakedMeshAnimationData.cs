using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "BakedAnimation", menuName = "Crowd/Baked Mesh Animation Data")]
public class BakedMeshAnimationData : ScriptableObject
{
    public string animationName;
    public Mesh[] bakedFrames;
    public float frameRate = 20f;
    public bool loop = true;
    
    public float Duration => bakedFrames != null && bakedFrames.Length > 0 
        ? bakedFrames.Length / frameRate 
        : 0f;
    
    public int FrameCount => bakedFrames != null ? bakedFrames.Length : 0;
    
    public Mesh GetFrameAtTime(float time)
    {
        if (bakedFrames == null || bakedFrames.Length == 0) return null;
        
        float wrappedTime = loop ? Mathf.Repeat(time, Duration) : Mathf.Clamp(time, 0f, Duration);
        int frameIndex = Mathf.FloorToInt(wrappedTime * frameRate);
        frameIndex = Mathf.Clamp(frameIndex, 0, bakedFrames.Length - 1);
        
        return bakedFrames[frameIndex];
    }
}
