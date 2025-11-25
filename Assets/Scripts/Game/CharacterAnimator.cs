using UnityEngine;
using Fusion;

public class CharacterAnimator : NetworkBehaviour
{
    [Header("Animation Settings")]
    [SerializeField] private Animator animator;
    [SerializeField] private string kickTrigger = "Kick";
    [SerializeField] private string diveTrigger = "Dive";
    [SerializeField] private string diveDirectionParam = "DiveDirection";

    [Networked] private NetworkBool isAnimating { get; set; }

    //[Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_PlayKick()
    {
        if (animator != null)
        {
            isAnimating = true;
            animator.SetTrigger(kickTrigger);
        }
    }

    //[Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_PlayDive(int direction)
    {
        if (animator != null)
        {
            isAnimating = true;
            animator.SetFloat(diveDirectionParam, direction);
            animator.SetTrigger(diveTrigger);
        }
    }

    //[Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_ResetAnimator()
    {
        if (animator != null)
        {
            isAnimating = false;
            animator.Rebind();
            animator.Update(0f);
        }
    }

    public int GetDiveDirection(ShotHorizontalPos horizontalPos, ShotVerticalPos verticalPos)
    {
        if (horizontalPos == ShotHorizontalPos.Left && verticalPos == ShotVerticalPos.Top)
            return 0;
        if (horizontalPos == ShotHorizontalPos.Left && verticalPos == ShotVerticalPos.Bottom)
            return 1;
        if (horizontalPos == ShotHorizontalPos.Middle)
            return 2;
        if (horizontalPos == ShotHorizontalPos.Right && verticalPos == ShotVerticalPos.Top)
            return 3;
        if (horizontalPos == ShotHorizontalPos.Right && verticalPos == ShotVerticalPos.Bottom)
            return 4;
        
        return 2;
    }

    public bool IsAnimating()
    {
        return isAnimating;
    }
}
