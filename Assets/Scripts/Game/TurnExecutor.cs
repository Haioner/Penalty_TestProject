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

    [Header("Timing")]
    [SerializeField] private float kickDelay = 0.5f;
    [SerializeField][Range(0f, 1f)] private float diveProgressThreshold = 0.4f;
    [SerializeField] private float resultDelay = 2f;

    [Networked] private TurnState currentState { get; set; }
    [Networked] private TickTimer stateTimer { get; set; }

    private ShotHorizontalPos shotHorizontal;
    private ShotVerticalPos shotVertical;
    private PrecisionZone shotPrecision;
    private ShotHorizontalPos diveHorizontal;
    private ShotVerticalPos diveVertical;

    public TurnState GetCurrentState()
    {
        return currentState;
    }

    public void ExecuteTurn(ShotHorizontalPos shotH, ShotVerticalPos shotV, PrecisionZone precision,
                           ShotHorizontalPos diveH, ShotVerticalPos diveV)
    {
        if (!Object.HasStateAuthority)
            return;

        shotHorizontal = shotH;
        shotVertical = shotV;
        shotPrecision = precision;
        diveHorizontal = diveH;
        diveVertical = diveV;

        currentState = TurnState.ExecutingKick;
        stateTimer = TickTimer.CreateFromSeconds(Runner, kickDelay);

        RPC_NotifyKickStart();
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
                    ShootBall();
                    currentState = TurnState.ExecutingDive;
                }
                break;

            case TurnState.ExecutingDive:
                if (ball != null && ball.GetTravelProgress() >= diveProgressThreshold)
                {
                    PlayGoalkeeperAnimation();
                    currentState = TurnState.ShowingResult;
                    stateTimer = TickTimer.CreateFromSeconds(Runner, resultDelay);
                }
                break;

            case TurnState.ShowingResult:
                if (stateTimer.Expired(Runner))
                {
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
            Vector3 missTarget = targetManager.GetTargetPosition(ShotHorizontalPos.Right, ShotVerticalPos.Top);
            return missTarget + Vector3.up * 2f;
        }

        if (shotPrecision == PrecisionZone.Perfect)
        {
            return targetManager.GetTargetPosition(shotHorizontal, shotVertical);
        }

        if (shotPrecision == PrecisionZone.Medium)
        {
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

    private void ResetTurn()
    {
        if (ball != null)
            ball.ResetBall();

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
    }
}
