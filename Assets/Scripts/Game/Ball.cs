using UnityEngine;
using Fusion;

public class Ball : NetworkBehaviour
{
    [Header("Ball Settings")]
    [SerializeField] private float shotSpeed = 15f;
    [SerializeField] private Vector3 initialPosition;

    [Networked] private NetworkBool isMoving { get; set; }
    [Networked] private NetworkBool isAttachedToGoalkeeper { get; set; }
    [Networked] private Vector3 targetPosition { get; set; }
    [Networked] private float travelProgress { get; set; }

    private Vector3 startPosition;
    private GameController gameController;

    private void Start()
    {
        initialPosition = transform.position;
        gameController = FindFirstObjectByType<GameController>();
    }

    public void ShootBall(Vector3 target)
    {
        if (!Object.HasStateAuthority)
            return;

        startPosition = transform.position;
        targetPosition = target;
        isMoving = true;
        travelProgress = 0f;
    }

    public void ResetBall()
    {
        Debug.Log("<color=red>[BALL RESET] ResetBall called!</color>");
        
        if (!Object.HasStateAuthority)
        {
            Debug.LogWarning("[BALL RESET] No state authority!");
            return;
        }

        isMoving = false;
        isAttachedToGoalkeeper = false;
        travelProgress = 0f;
        
        Debug.Log("<color=red>[BALL RESET] Calling RPC_DetachAndReset</color>");
        RPC_DetachAndReset();
    }

    public void AttachToGoalkeeper()
    {
        Debug.Log("<color=magenta>[BALL] AttachToGoalkeeper called!</color>");
        
        if (!Object.HasStateAuthority)
        {
            Debug.LogWarning("[BALL] No state authority, returning");
            return;
        }

        if (gameController == null)
        {
            gameController = FindFirstObjectByType<GameController>();
            Debug.Log($"[BALL] GameController found: {gameController != null}");
        }

        PlayerController goalkeeper = gameController.GetCurrentGoalkeeper();
        Debug.Log($"<color=magenta>[BALL] Goalkeeper found: {goalkeeper != null}</color>");
        
        if (goalkeeper != null)
        {
            RPC_AttachToGoalkeeperHand(goalkeeper);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_AttachToGoalkeeperHand(PlayerController goalkeeper)
    {
        Debug.Log("<color=magenta>[BALL RPC] RPC_AttachToGoalkeeperHand called!</color>");
        
        if (goalkeeper == null)
        {
            Debug.LogError("[BALL RPC] Goalkeeper is NULL!");
            return;
        }

        Transform handPivot = goalkeeper.GetHandPivot();
        Debug.Log($"<color=magenta>[BALL RPC] HandPivot found: {handPivot != null}</color>");
        
        if (handPivot != null)
        {
            Debug.Log($"<color=green>[BALL RPC] Setting parent to {handPivot.name}</color>");
            transform.SetParent(handPivot);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            isAttachedToGoalkeeper = true;
            isMoving = false;
            travelProgress = 1f;
            Debug.Log("<color=green>[BALL RPC] travelProgress set to 1.0 (caught!)</color>");
        }
        else
        {
            Debug.LogError("[BALL RPC] HandPivot is NULL! Please assign it in the Inspector.");
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_DetachAndReset()
    {
        Debug.Log("<color=orange>[BALL RESET] Detaching from parent and resetting position</color>");
        
        if (transform.parent != null)
        {
            Debug.Log($"<color=orange>[BALL RESET] Removing parent: {transform.parent.name}</color>");
            transform.SetParent(null);
        }
        
        transform.position = initialPosition;
        Debug.Log($"<color=orange>[BALL RESET] Reset to position: {initialPosition}</color>");
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_DetachFromGoalkeeper()
    {
        if (transform.parent != null)
        {
            transform.SetParent(null);
        }
        isAttachedToGoalkeeper = false;
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ResetBallPosition()
    {
        transform.position = initialPosition;
    }

    public override void FixedUpdateNetwork()
    {
        if (isAttachedToGoalkeeper)
            return;

        if (!isMoving)
            return;

        travelProgress += Runner.DeltaTime * shotSpeed / Vector3.Distance(startPosition, targetPosition);
        transform.position = Vector3.Lerp(startPosition, targetPosition, travelProgress);

        if (travelProgress >= 1f)
        {
            isMoving = false;
        }
    }

    public bool IsMoving()
    {
        return isMoving;
    }

    public float GetTravelProgress()
    {
        return travelProgress;
    }
}
