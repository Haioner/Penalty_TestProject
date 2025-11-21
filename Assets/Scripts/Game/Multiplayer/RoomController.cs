using UnityEngine;
using Fusion;

public class RoomController : NetworkBehaviour, IPlayerJoined, IPlayerLeft
{
    [SerializeField] private GameObject waitingCanvasHolder;
    [SerializeField] private PlayerSpawner playerSpawner;
    [SerializeField] private GameController gameController;

    [Networked] private NetworkBool allPlayersReady { get; set; }
    private const int MAX_PLAYERS = 2;

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
            allPlayersReady = false;

        SetActiveWaitingScreen(true);
        CheckPlayerCount();
    }

    public void PlayerJoined(PlayerRef player)
    {
        CheckPlayerCount();
    }

    public void PlayerLeft(PlayerRef player)
    {
        CheckPlayerCount();
    }

    private void CheckPlayerCount()
    {
        if (Runner == null || Runner.ActivePlayers == null)
            return;

        int playerCount = Runner.SessionInfo.PlayerCount;
        if (playerCount >= MAX_PLAYERS && !allPlayersReady)
        {
            if (Object.HasStateAuthority)
            {
                allPlayersReady = true;
                SpawnPlayers();
            }
        }
        else if (playerCount < MAX_PLAYERS)
        {
            SetActiveWaitingScreen(true);
        }
    }

    private void SpawnPlayers()
    {
        if (playerSpawner != null)
            playerSpawner.SpawnPlayers();

        RPC_SetActiveWaitingScreen(false);
        RPC_StartGame();
    }

    private void SetActiveWaitingScreen(bool state)
    {
        if (waitingCanvasHolder != null)
            waitingCanvasHolder.SetActive(state);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_SetActiveWaitingScreen(bool state)
    {
        if (waitingCanvasHolder != null)
            waitingCanvasHolder.SetActive(state);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_StartGame()
    {
        if (gameController != null)
        {
            gameController.StartGame();
        }
    }
}
