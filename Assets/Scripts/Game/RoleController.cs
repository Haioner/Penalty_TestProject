using Fusion;

public class RoleController : NetworkBehaviour
{
    private PrecisionBar precisionBar;
    private ShotTargetManager targetManager;
    private PlayerController playerController;
    private PlayerRole currentRole = PlayerRole.None;
    private bool waitingForGameStart = false;

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
    }

    public void StartRole(PlayerRole newRole)
    {
        currentRole = newRole;

        if (precisionBar == null)
            precisionBar = FindFirstObjectByType<PrecisionBar>();

        if (targetManager == null)
            targetManager = FindFirstObjectByType<ShotTargetManager>();

        waitingForGameStart = true;
    }

    public void EnableRoleControlsAfterCountdown()
    {
        if (!waitingForGameStart)
            return;

        waitingForGameStart = false;

        if (currentRole == PlayerRole.Beater)
            StartBeater();
        else if (currentRole == PlayerRole.GoalKeeper)
            StartGoalKeeper();
    }

    public void RestartRole()
    {
        if (currentRole != PlayerRole.None)
        {
            if (currentRole == PlayerRole.Beater)
                StartBeater();
            else if (currentRole == PlayerRole.GoalKeeper)
                StartGoalKeeper();
        }
    }

    private void StartBeater()
    {
        if (precisionBar != null)
            precisionBar.StartScaling();

        if (targetManager != null)
            targetManager.EnableTargets(playerController);
    }

    private void StartGoalKeeper()
    {
        if (precisionBar != null)
            precisionBar.StopScaling();

        if (targetManager != null)
            targetManager.EnableTargets(playerController);
    }

    public void StopRole()
    {
        if (precisionBar != null)
            precisionBar.StopScaling();

        if (targetManager != null)
            targetManager.DisableTargets();
    }
}
