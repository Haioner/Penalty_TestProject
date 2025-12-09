using UnityEngine;
using Fusion;
using System.Collections.Generic;

public class CharacterAnimator : NetworkBehaviour
{
    [Header("Animation Settings")]
    [SerializeField] private Animator animator;
    [SerializeField] private string kickTrigger = "Kick";
    [SerializeField] private string diveTrigger = "Dive";
    [SerializeField] private string diveDirectionParam = "DiveDirection";

    [Header("Beater Character")]
    [SerializeField] private Avatar beaterAvatar;
    [SerializeField] private List<GameObject> beaterModels;

    [Header("GoalKeeper Character")]
    [SerializeField] private Avatar goalKeeperAvatar;
    [SerializeField] private List<GameObject> goalKeeperModels;

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

    public void SetCharacterForRole(PlayerRole role)
    {
        if (Object.HasStateAuthority)
        {
            RPC_UpdateCharacterModel(role);
        }
        else
        {
            UpdateCharacterModelLocal(role);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_UpdateCharacterModel(PlayerRole role)
    {
        UpdateCharacterModelLocal(role);
    }

    private void UpdateCharacterModelLocal(PlayerRole role)
    {
        Debug.Log($"<color=magenta>[🎨 ANIMATOR] Updating character model for role: {role}</color>");
        
        if (role == PlayerRole.Beater)
        {
            ActivateBeaterModel();
            Debug.Log("<color=magenta>[🎨 ANIMATOR] Beater model activated</color>");
        }
        else if (role == PlayerRole.GoalKeeper)
        {
            ActivateGoalKeeperModel();
            Debug.Log("<color=magenta>[🎨 ANIMATOR] GoalKeeper model activated</color>");
        }
    }

    private void ActivateBeaterModel()
    {
        if (animator != null && beaterAvatar != null)
        {
            animator.avatar = beaterAvatar;
        }

        if (beaterModels != null)
        {
            foreach (GameObject model in beaterModels)
            {
                if (model != null)
                    model.SetActive(true);
            }
        }

        if (goalKeeperModels != null)
        {
            foreach (GameObject model in goalKeeperModels)
            {
                if (model != null)
                    model.SetActive(false);
            }
        }

        if (animator != null)
        {
            animator.Rebind();
            animator.Update(0f);
        }
    }

    private void ActivateGoalKeeperModel()
    {
        if (animator != null && goalKeeperAvatar != null)
        {
            animator.avatar = goalKeeperAvatar;
        }

        if (goalKeeperModels != null)
        {
            foreach (GameObject model in goalKeeperModels)
            {
                if (model != null)
                    model.SetActive(true);
            }
        }

        if (beaterModels != null)
        {
            foreach (GameObject model in beaterModels)
            {
                if (model != null)
                    model.SetActive(false);
            }
        }

        if (animator != null)
        {
            animator.Rebind();
            animator.Update(0f);
        }
    }
}
