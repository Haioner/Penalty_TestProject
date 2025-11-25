using UnityEngine;
using Fusion;

public class Ball : NetworkBehaviour
{
    [Header("Ball Settings")]
    [SerializeField] private float shotSpeed = 15f;
    [SerializeField] private Vector3 initialPosition;

    [Networked] private NetworkBool isMoving { get; set; }
    [Networked] private Vector3 targetPosition { get; set; }

    private Vector3 startPosition;
    private float travelProgress = 0f;

    private void Start()
    {
        initialPosition = transform.position;
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
        if (!Object.HasStateAuthority)
            return;

        isMoving = false;
        travelProgress = 0f;
        RPC_ResetBallPosition();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ResetBallPosition()
    {
        transform.position = initialPosition;
    }

    public override void FixedUpdateNetwork()
    {
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
