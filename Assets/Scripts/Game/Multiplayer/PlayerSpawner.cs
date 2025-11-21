using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class PlayerSpawner : NetworkBehaviour
{
    [Header("Player Prefab")]
    [SerializeField] private GameObject playerPrefab;

    private bool hasSpawned = false;
    private List<PlayerController> spawnedPlayers = new List<PlayerController>();

    public void SpawnPlayers()
    {
        if (!Object.HasStateAuthority || hasSpawned)
            return;
        hasSpawned = true;

        List<PlayerRef> players = new List<PlayerRef>(Runner.ActivePlayers);
        for (int i = 0; i < players.Count; i++)
        {
            PlayerRef player = players[i];
            NetworkObject playerNetworkObject = Runner.Spawn(
                playerPrefab, 
                transform.position, 
                Quaternion.identity, 
                player
            );
            RPC_NotifyPlayerSpawned(player, playerNetworkObject);
        }

        AssignRandomRoles();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_NotifyPlayerSpawned(PlayerRef player, NetworkObject playerNetworkObject)
    {
        PlayerController playerController = playerNetworkObject.GetComponent<PlayerController>();
        if (playerController != null)
        {
            spawnedPlayers.Add(playerController);
        }

        if (player == Runner.LocalPlayer)
        {
            Runner.SetPlayerObject(player, playerNetworkObject);
            
            if (playerController != null)
                playerController.InitializePlayer();
        }
    }

    private void AssignRandomRoles()
    {
        if (spawnedPlayers.Count < 2)
            return;

        int beaterIndex = Random.Range(0, 2);
        int goalKeeperIndex = 1 - beaterIndex;

        spawnedPlayers[beaterIndex].SetRole(PlayerRole.Beater);
        spawnedPlayers[goalKeeperIndex].SetRole(PlayerRole.GoalKeeper);

        Debug.Log($"Roles assigned: Player {beaterIndex} = Beater, Player {goalKeeperIndex} = GoalKeeper");
    }
}
