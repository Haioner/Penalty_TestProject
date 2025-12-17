using UnityEngine;
using System.Collections.Generic;

public class CrowdBakedManager : MonoBehaviour
{
    [Header("Crowd References")]
    [SerializeField] private Transform[] crowdSections;
    
    [Header("Performance Settings")]
    [SerializeField] private bool useLOD = true;
    [SerializeField] private float maxVisibleDistance = 50f;
    [SerializeField] private int updateBatchSize = 50;
    
    private List<BakedMeshAnimator> allCrowdMembers = new List<BakedMeshAnimator>();
    private Camera mainCamera;
    private int currentUpdateIndex = 0;
    
    private void Awake()
    {
        mainCamera = Camera.main;
        CollectCrowdMembers();
        OptimizeCrowd();
    }
    
    private void CollectCrowdMembers()
    {
        if (crowdSections == null || crowdSections.Length == 0)
        {
            crowdSections = new Transform[] { transform };
        }
        
        foreach (Transform section in crowdSections)
        {
            if (section == null) continue;
            
            BakedMeshAnimator[] animators = section.GetComponentsInChildren<BakedMeshAnimator>(true);
            allCrowdMembers.AddRange(animators);
        }
        
        Debug.Log($"CrowdBakedManager: Found {allCrowdMembers.Count} crowd members");
    }
    
    private void OptimizeCrowd()
    {
        foreach (BakedMeshAnimator member in allCrowdMembers)
        {
            if (member == null) continue;
            
            MeshRenderer[] renderers = member.GetComponentsInChildren<MeshRenderer>();
            foreach (MeshRenderer renderer in renderers)
            {
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
                renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
                renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
                
                if (renderer.sharedMaterial != null)
                {
                    renderer.sharedMaterial.enableInstancing = true;
                }
            }
        }
    }
    
    private void Update()
    {
        if (!useLOD || mainCamera == null || allCrowdMembers.Count == 0) return;
        
        UpdateCrowdLOD();
    }
    
    private void UpdateCrowdLOD()
    {
        int membersToUpdate = Mathf.Min(updateBatchSize, allCrowdMembers.Count);
        Vector3 cameraPosition = mainCamera.transform.position;
        
        for (int i = 0; i < membersToUpdate; i++)
        {
            currentUpdateIndex = (currentUpdateIndex + 1) % allCrowdMembers.Count;
            BakedMeshAnimator member = allCrowdMembers[currentUpdateIndex];
            
            if (member == null) continue;
            
            float distance = Vector3.Distance(cameraPosition, member.transform.position);
            bool shouldBeActive = distance <= maxVisibleDistance;
            
            if (member.gameObject.activeSelf != shouldBeActive)
            {
                member.gameObject.SetActive(shouldBeActive);
            }
        }
    }
    
    public void TriggerWinAnimation()
    {
        foreach (BakedMeshAnimator member in allCrowdMembers)
        {
            if (member != null && member.gameObject.activeSelf)
            {
                member.PlayWin();
            }
        }
    }
    
    public void TriggerIdleAnimation()
    {
        foreach (BakedMeshAnimator member in allCrowdMembers)
        {
            if (member != null && member.gameObject.activeSelf)
            {
                member.PlayIdle();
            }
        }
    }
}
