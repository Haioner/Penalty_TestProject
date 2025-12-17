using UnityEngine;

public class BakedMeshAnimator : MonoBehaviour
{
    [Header("Animation Data")]
    [SerializeField] private BakedMeshAnimationData idleAnimation;
    [SerializeField] private BakedMeshAnimationData winAnimation;
    
    [Header("Settings")]
    [SerializeField] private MeshFilter meshFilter;
    [SerializeField] private bool randomizeStartTime = true;
    [SerializeField] private float speedVariation = 0.2f;
    
    private BakedMeshAnimationData currentAnimation;
    private float animationTime;
    private float playbackSpeed = 1f;
    private bool isPlaying = true;
    
    private void Awake()
    {
        if (meshFilter == null)
        {
            meshFilter = GetComponent<MeshFilter>();
        }
        
        if (speedVariation > 0f)
        {
            playbackSpeed = Random.Range(1f - speedVariation, 1f + speedVariation);
        }
        
        PlayIdle();
        
        if (randomizeStartTime && currentAnimation != null)
        {
            animationTime = Random.Range(0f, currentAnimation.Duration);
        }
    }
    
    private void Update()
    {
        if (!isPlaying || currentAnimation == null || meshFilter == null) return;
        
        animationTime += Time.deltaTime * playbackSpeed;
        
        if (!currentAnimation.loop && animationTime >= currentAnimation.Duration)
        {
            animationTime = currentAnimation.Duration - 0.01f;
        }
        
        Mesh currentFrame = currentAnimation.GetFrameAtTime(animationTime);
        if (currentFrame != null)
        {
            meshFilter.sharedMesh = currentFrame;
        }
    }
    
    public void PlayIdle()
    {
        if (idleAnimation != null)
        {
            currentAnimation = idleAnimation;
            animationTime = 0f;
            isPlaying = true;
        }
        else
        {
            Debug.LogWarning($"[BakedMeshAnimator] Idle animation não configurada em {gameObject.name}!");
        }
    }
    
    public void PlayWin()
    {
        if (winAnimation != null)
        {
            currentAnimation = winAnimation;
            animationTime = 0f;
            isPlaying = true;
        }
        else
        {
            Debug.LogWarning($"[BakedMeshAnimator] Win animation não configurada em {gameObject.name}!");
        }
    }
    
    public void Pause()
    {
        isPlaying = false;
    }
    
    public void Resume()
    {
        isPlaying = true;
    }
}
