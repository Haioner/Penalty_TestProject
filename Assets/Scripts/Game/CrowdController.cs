using UnityEngine;
using System.Collections;

public class CrowdController : MonoBehaviour
{
    [Header("Configuração")]
    [SerializeField] private float winAnimationDuration = 2f;
    
    private BakedMeshAnimator[] allAnimators;
    
    private void Awake()
    {
        allAnimators = GetComponentsInChildren<BakedMeshAnimator>(true);
        Debug.Log($"[CrowdController] Encontrados {allAnimators.Length} personagens na multidão");
    }
    
    public void PlayWin()
    {
        Debug.Log($"[CrowdController] PlayWin chamado! Animando {allAnimators.Length} personagens");
        StopAllCoroutines();
        StartCoroutine(PlayWinAndReturnToIdle());
    }
    
    public void PlayIdle()
    {
        Debug.Log($"[CrowdController] PlayIdle chamado! Resetando {allAnimators.Length} personagens");
        StopAllCoroutines();
        
        foreach (BakedMeshAnimator animator in allAnimators)
        {
            if (animator != null)
            {
                animator.PlayIdle();
            }
        }
    }
    
    private IEnumerator PlayWinAndReturnToIdle()
    {
        Debug.Log("[CrowdController] Iniciando animação Win");
        
        foreach (BakedMeshAnimator animator in allAnimators)
        {
            if (animator != null)
            {
                animator.PlayWin();
            }
        }
        
        Debug.Log($"[CrowdController] Esperando {winAnimationDuration} segundos...");
        yield return new WaitForSeconds(winAnimationDuration);
        
        Debug.Log("[CrowdController] Voltando para Idle");
        foreach (BakedMeshAnimator animator in allAnimators)
        {
            if (animator != null)
            {
                animator.PlayIdle();
            }
        }
    }
}
