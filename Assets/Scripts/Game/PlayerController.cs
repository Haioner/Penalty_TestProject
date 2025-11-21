using UnityEngine;
using Fusion;
using TMPro;

public enum PlayerRole
{
    None, Beater, GoalKeeper
}

public class PlayerController : NetworkBehaviour
{
    [Networked] public PlayerRole Role { get; set; }
    [Networked] public string playerName { get; set; }
    [SerializeField] private TextMeshPro playerNameText;
    [SerializeField] private GameObject pinObject;

    private GameController gameController;
    private RoleController roleController;
    private bool isLocalPlayer = false;

    public override void Spawned()
    {
        gameController = FindFirstObjectByType<GameController>();
        roleController = GetComponent<RoleController>();
        
        if (pinObject != null)
            pinObject.SetActive(false);
    }

    public void InitializePlayer()
    {
        isLocalPlayer = true;
        string name = NetworkManager.Instance.PlayerName;
        playerName = name;
        RPC_UpdatePlayerName(name);
        gameController.RegisterPlayerName(Runner.LocalPlayer, name);
        pinObject.SetActive(true);
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RPC_UpdatePlayerName(string name)
    {
        playerNameText.text = name;
    }

    public void SetRole(PlayerRole newRole)
    {
        if (!Object.HasStateAuthority)
            return;

        Role = newRole;
        RPC_UpdateRole(newRole);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_UpdateRole(PlayerRole newRole)
    {
        UpdateControllers(newRole);
        MoveToPosition(newRole);
    }

    private void UpdateControllers(PlayerRole role)
    {
        if (isLocalPlayer)
        {
            roleController.StartRole(role);
        }
    }

    private void MoveToPosition(PlayerRole role)
    {
        if (gameController == null)
            return;

        Vector3 targetPosition = gameController.GetPositionForRole(role);
        transform.position = targetPosition;
    }
}
