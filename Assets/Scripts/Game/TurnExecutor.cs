using UnityEngine;
using Fusion;
using System.Collections;

public enum TurnState
{
    WaitingChoices,
    ExecutingKick,
    ExecutingDive,
    ShowingResult,
    Completed
}

public class TurnExecutor : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private Ball ball;
    [SerializeField] private ShotTargetManager targetManager;
    [SerializeField] private GameController gameController;

    [Header("Timing")]
    [SerializeField] private float kickDelay = 0.5f;
    [SerializeField][Range(0f, 1f)] private float catchProgressThreshold = 0.7f;
    [SerializeField][Range(0f, 1f)] private float goalProgressThreshold = 0.8f;
    [SerializeField] private float resultDelay = 2f;

    [Networked] private TurnState currentState { get; set; }
    [Networked] private TickTimer stateTimer { get; set; }
    [Networked] private PlayerRef beaterPlayerRef { get; set; }
    [Networked] private NetworkString<_64> beaterName { get; set; }
    [Networked] private NetworkString<_64> goalkeeperName { get; set; }
    [Networked] private NetworkBool isGoal { get; set; }
    [Networked] private NetworkBool isSaved { get; set; }
    [Networked] private NetworkBool ballCaught { get; set; }
    [Networked] private NetworkBool goalCounted { get; set; }

    private ShotHorizontalPos shotHorizontal;
    private ShotVerticalPos shotVertical;
    private PrecisionZone shotPrecision;
    private ShotHorizontalPos diveHorizontal;
    private ShotVerticalPos diveVertical;
    private bool resultMessageShown = false;

    public TurnState GetCurrentState()
    {
        return currentState;
    }

    public void ExecuteTurn(ShotHorizontalPos shotH, ShotVerticalPos shotV, PrecisionZone precision,
                           ShotHorizontalPos diveH, ShotVerticalPos diveV,
                           PlayerRef beaterPlayer, string beaterPlayerName, string goalkeeperPlayerName, bool goalScored)
    {
        if (!Object.HasStateAuthority)
            return;

        shotHorizontal = shotH;
        shotVertical = shotV;
        shotPrecision = precision;
        diveHorizontal = diveH;
        diveVertical = diveV;
        beaterPlayerRef = beaterPlayer;
        beaterName = beaterPlayerName;
        goalkeeperName = goalkeeperPlayerName;
        isGoal = goalScored;
        
        bool isMiss = precision == PrecisionZone.Miss;
        isSaved = !isMiss && !goalScored;
        
        resultMessageShown = false;
        ballCaught = false;
        goalCounted = false;

        Debug.Log($"<color=yellow>[TURN EXECUTOR] ExecuteTurn called - IsGoal: {goalScored}, IsSaved: {isSaved}, IsMiss: {isMiss}</color>");

        if (gameController == null)
            gameController = FindFirstObjectByType<GameController>();

        currentState = TurnState.ExecutingKick;
        stateTimer = TickTimer.CreateFromSeconds(Runner, kickDelay);

        RPC_NotifyKickStart();
        PlayGoalkeeperAnimation();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_NotifyKickStart()
    {
        PlayerController[] players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        foreach (var player in players)
        {
            if (player.Role == PlayerRole.Beater)
            {
                CharacterAnimator animator = player.GetComponent<CharacterAnimator>();
                if (animator != null)
                    animator.RPC_PlayKick();
            }
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority)
            return;

        switch (currentState)
        {
            case TurnState.ExecutingKick:
                if (stateTimer.Expired(Runner))
                {
                    Debug.Log("<color=yellow>[STATE] Transitioning to ExecutingDive</color>");
                    ShootBall();
                    currentState = TurnState.ExecutingDive;
                }
                break;

            case TurnState.ExecutingDive:
                if (ball != null)
                {
                    float progress = ball.GetTravelProgress();
                    
                    if (progress >= catchProgressThreshold && !ballCaught && isSaved)
                    {
                        Debug.Log($"<color=cyan>[ATTACH] Progress: {progress:F2}, Threshold: {catchProgressThreshold}, BallCaught: {ballCaught}, IsSaved: {isSaved}</color>");
                        ballCaught = true;
                        AttachBallToGoalkeeper();
                    }
                    
                    if (progress >= goalProgressThreshold && !goalCounted)
                    {
                        goalCounted = true;
                        Debug.Log($"<color=green>[GOAL COUNT] Progress: {progress:F2}, IsGoal: {isGoal}</color>");
                        
                        if (isGoal)
                        {
                            if (gameController != null)
                            {
                                gameController.IncrementScore(beaterPlayerRef, beaterName.ToString());
                            }
                            RPC_TriggerCrowdCelebration();
                        }
                    }

                    if (progress >= 1f && !resultMessageShown)
                    {
                        resultMessageShown = true;
                        if (gameController != null)
                        {
                            gameController.ShowTurnResultMessage(beaterName.ToString(), goalkeeperName.ToString(), isGoal);
                        }
                        currentState = TurnState.ShowingResult;
                        stateTimer = TickTimer.CreateFromSeconds(Runner, resultDelay);
                    }
                }
                break;

            case TurnState.ShowingResult:
                if (ball != null && ball.GetTravelProgress() >= 1f && !resultMessageShown)
                {
                    resultMessageShown = true;
                    if (gameController != null)
                    {
                        gameController.ShowTurnResultMessage(beaterName.ToString(), goalkeeperName.ToString(), isGoal);
                    }
                }

                if (stateTimer.Expired(Runner))
                {
                    Debug.Log("<color=red>[STATE] ShowingResult timer expired, calling ResetTurn()</color>");
                    ResetTurn();
                    currentState = TurnState.Completed;
                }
                break;
        }
    }

    private void ShootBall()
    {
        Vector3 target = GetBallTarget();
        if (ball != null)
            ball.ShootBall(target);
    }

    private Vector3 GetBallTarget()
    {
        if (targetManager == null)
        {
            targetManager = FindFirstObjectByType<ShotTargetManager>();
        }

        if (shotPrecision == PrecisionZone.Miss)
        {
            ShotHorizontalPos missHorizontal = shotHorizontal;
            if (shotHorizontal == ShotHorizontalPos.Middle)
            {
                missHorizontal = Random.value > 0.5f ? ShotHorizontalPos.Left : ShotHorizontalPos.Right;
            }
            
            Vector3 missTarget = targetManager.GetTargetPosition(missHorizontal, ShotVerticalPos.Top);
            return missTarget + Vector3.up * 2f;
        }

        if (shotPrecision == PrecisionZone.Perfect)
        {
            return targetManager.GetTargetPosition(shotHorizontal, shotVertical);
        }

        if (shotPrecision == PrecisionZone.Medium)
        {
            if (shotHorizontal == ShotHorizontalPos.Middle)
            {
                return targetManager.GetTargetPosition(shotHorizontal, shotVertical);
            }
            
            Vector3 topPos = targetManager.GetTargetPosition(shotHorizontal, ShotVerticalPos.Top);
            Vector3 bottomPos = targetManager.GetTargetPosition(shotHorizontal, ShotVerticalPos.Bottom);
            return Vector3.Lerp(topPos, bottomPos, 0.5f);
        }

        return targetManager.GetTargetPosition(shotHorizontal, shotVertical);
    }

    private void PlayGoalkeeperAnimation()
    {
        PlayerController[] players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        foreach (var player in players)
        {
            if (player.Role == PlayerRole.GoalKeeper)
            {
                CharacterAnimator animator = player.GetComponent<CharacterAnimator>();
                if (animator != null)
                {
                    int direction = animator.GetDiveDirection(diveHorizontal, diveVertical);
                    RPC_PlayGoalkeeperDive(direction);
                }
                break;
            }
        }
    }

    private void AttachBallToGoalkeeper()
    {
        Debug.Log("<color=green>[ATTACH] AttachBallToGoalkeeper called!</color>");
        
        if (!Object.HasStateAuthority)
        {
            Debug.LogWarning("[ATTACH] No state authority, returning");
            return;
        }

        if (ball != null)
        {
            Debug.Log("<color=green>[ATTACH] Calling ball.AttachToGoalkeeper()</color>");
            ball.AttachToGoalkeeper();
        }
        else
        {
            Debug.LogError("[ATTACH] Ball is NULL!");
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayGoalkeeperDive(int direction)
    {
        PlayerController[] players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        foreach (var player in players)
        {
            if (player.Role == PlayerRole.GoalKeeper)
            {
                CharacterAnimator animator = player.GetComponent<CharacterAnimator>();
                if (animator != null)
                    animator.RPC_PlayDive(direction);
            }
        }
    }
    
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_TriggerCrowdCelebration()
    {
        Debug.Log("<color=green>[RPC CROWD] Gol contado em 80%! Tocando animação da multidão...</color>");
        
        CrowdController crowdController = FindFirstObjectByType<CrowdController>();
        if (crowdController != null)
        {
            crowdController.PlayWin();
        }
        else
        {
            Debug.LogWarning("[RPC CROWD] CrowdController não encontrado!");
        }
    }

    private void ResetTurn()
    {
        Debug.Log("<color=red>[TURN EXECUTOR] ResetTurn called!</color>");
        
        if (ball != null)
        {
            Debug.Log("<color=red>[TURN EXECUTOR] Calling ball.ResetBall()</color>");
            ball.ResetBall();
        }
        else
        {
            Debug.LogError("[TURN EXECUTOR] Ball is NULL in ResetTurn!");
        }

        RPC_ResetCharacters();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ResetCharacters()
    {
        PlayerController[] players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        foreach (var player in players)
        {
            CharacterAnimator animator = player.GetComponent<CharacterAnimator>();
            if (animator != null)
                animator.RPC_ResetAnimator();
        }
    }

    public void ResetForNewTurn()
    {
        if (!Object.HasStateAuthority)
            return;
            
        currentState = TurnState.WaitingChoices;
        resultMessageShown = false;
        ballCaught = false;
        goalCounted = false;
    }
}
