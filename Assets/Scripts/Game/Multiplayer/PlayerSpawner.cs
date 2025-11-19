using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class PlayerSpawner : NetworkBehaviour
{
    [Header("Player Prefabs")]
    [SerializeField] private GameObject beaterPrefab;
    [SerializeField] private GameObject goalKeeperPrefab;

    [Header("Spawn Positions")]
    [SerializeField] private Transform beaterSpawnPoint;
    [SerializeField] private Transform goalKeeperSpawnPoint;

    private bool hasSpawned = false;

    public void SpawnPlayersWithClasses(Dictionary<PlayerRef, PlayerClass> assignments)
    {
        if (!Object.HasStateAuthority || hasSpawned)
            return;

        hasSpawned = true;

        foreach (var assignment in assignments)
        {
            PlayerRef player = assignment.Key;
            PlayerClass playerClass = assignment.Value;

            GameObject prefabToSpawn = null;
            Vector3 spawnPosition = Vector3.zero;

            if (playerClass == PlayerClass.Beater)
            {
                prefabToSpawn = beaterPrefab;
                spawnPosition = beaterSpawnPoint != null ? beaterSpawnPoint.position : Vector3.zero;
            }
            else if (playerClass == PlayerClass.GoalKeeper)
            {
                prefabToSpawn = goalKeeperPrefab;
                spawnPosition = goalKeeperSpawnPoint != null ? goalKeeperSpawnPoint.position : Vector3.zero;
            }

            if (prefabToSpawn != null)
            {
                NetworkObject playerNetworkObject = Runner.Spawn(
                    prefabToSpawn,
                    spawnPosition,
                    Quaternion.identity,
                    player
                );

                Runner.SetPlayerObject(player, playerNetworkObject);
            }
        }
    }
}
