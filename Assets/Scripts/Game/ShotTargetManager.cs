using UnityEngine;
using Fusion;

public class ShotTargetManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ShotItem[] shotTargets;

    private GameController gameController;
    private PrecisionBar precisionBar;
    private PlayerController localPlayerController;
    private NetworkRunner runner;
    private bool canInteract = false;

    private void Start()
    {
        SetTargetsActive(false);
        FindReferences();
    }

    private void FindReferences()
    {
        if (gameController == null)
            gameController = FindFirstObjectByType<GameController>();
        
        if (precisionBar == null)
            precisionBar = FindFirstObjectByType<PrecisionBar>();
        
        if (runner == null)
            runner = FindFirstObjectByType<NetworkRunner>();
    }

    #region Targets
    public void EnableTargets(PlayerController playerController)
    {
        FindReferences();
        
        localPlayerController = playerController;
        canInteract = true;
        SetTargetsActive(true);
    }

    public void DisableTargets()
    {
        canInteract = false;
        SetTargetsActive(false);
        localPlayerController = null;
    }

    private void SetTargetsActive(bool active)
    {
        if (shotTargets == null)
            return;

        foreach (ShotItem target in shotTargets)
        {
            if (target != null)
            {
                target.SetActive(active);
            }
        }
    }
    #endregion

    #region Role Interaction
    public bool CanInteract()
    {
        return canInteract && localPlayerController != null;
    }

    public void OnTargetSelected(ShotItem shotItem)
    {
        PlayerRole currentRole = localPlayerController.Role;
        DisableTargets();

        if (currentRole == PlayerRole.Beater) //Beater
        {
            PrecisionZone precision = precisionBar != null 
                ? precisionBar.GetCurrentPrecision() 
                : PrecisionZone.Medium;

            if (precisionBar != null)
                precisionBar.StopScaling();

            gameController.SubmitBeaterChoice(runner.LocalPlayer, shotItem.HorizontalPosition, shotItem.VerticalPosition, precision);
        }
        else if (currentRole == PlayerRole.GoalKeeper) //GoalKeeper
        {
            gameController.SubmitGoalKeeperChoice(runner.LocalPlayer, shotItem.HorizontalPosition, shotItem.VerticalPosition);
        }
    }
    #endregion
}
