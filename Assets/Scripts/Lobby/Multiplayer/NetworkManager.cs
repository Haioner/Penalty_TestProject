using UnityEngine.SceneManagement;
using System.Collections.Generic;
using Fusion.Sockets;
using System.Linq;
using UnityEngine;
using Fusion;
using System;

public class NetworkManager : MonoBehaviour, INetworkRunnerCallbacks
{
    public static NetworkManager Instance;

    public string LobbyName = "default";
    public int GameSceneIndex = 2;
    public static NetworkRunner runnerInstance;
    public string RegionToken;
    public string PlayerName;
    public static bool ReconnectToRegion;

    //public GameObject playerPrefab;
    public static List<SessionInfo> CurrentSessionList = new List<SessionInfo>();

    public static event Action OnPlayerJoinedEvent;
    public static event Action OnPlayerLeftEvent;
    public static event Action OnShutDownEvent;

    private void Awake()
    {
        InstanceNetworkManager();

        ReconnectToRegion = PlayerPrefs.GetInt("Reconnect", 0) == 1;
        GetRunnerInstance();
        LoadRegionToken();
        LoadName();
    }

    private void InstanceNetworkManager()
    {
        if (Instance == null)
        {
            DontDestroyOnLoad(gameObject);
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void GetRunnerInstance()
    {
        runnerInstance = gameObject.GetComponent<NetworkRunner>();
        if (runnerInstance == null)
            runnerInstance = gameObject.AddComponent<NetworkRunner>();
    }

    public void JoinLobby()
    {
        runnerInstance.JoinSessionLobby(SessionLobby.Shared, LobbyName);
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        //SpawnPlayer(runner, player, Vector3.zero);
        OnPlayerJoinedEvent?.Invoke();
    }

    public void SpawnPlayer(NetworkRunner runner, PlayerRef player, Vector3 spawnPos)
    {
        if (player == runner.LocalPlayer)
        {
            //NetworkObject playerNetworkObject = runner.Spawn(playerPrefab, spawnPos, Quaternion.identity, player);
            //runner.SetPlayerObject(player, playerNetworkObject);
        }
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        OnPlayerLeftEvent?.Invoke();
    }

    public static void ShutDown()
    {
        runnerInstance.Shutdown();
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        SceneManager.LoadScene("Lobby");
        OnShutDownEvent?.Invoke();
    }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        CurrentSessionList = sessionList;
        //if (SceneManager.GetActiveScene().name != "Lobby")
        //    SceneManager.LoadScene("Lobby");
    }

    public static void CreateOrJoinRoom(string roomName = "defaultRoom", bool visibility = true)
    {
        runnerInstance.StartGame(new StartGameArgs()
        {
            Scene = SceneRef.FromIndex(Instance.GameSceneIndex),
            SessionName = roomName,
            IsVisible = visibility,
            GameMode = GameMode.Shared,
        });
    }

    public void CreateRandomSession()
    {
        int randomInt = UnityEngine.Random.Range(1000, 9999);
        string randomSessionName = "Room-" + randomInt.ToString();

        runnerInstance.StartGame(new StartGameArgs()
        {
            Scene = SceneRef.FromIndex(Instance.GameSceneIndex),
            SessionName = randomSessionName,
            GameMode = GameMode.Shared,
        });
    }

    public static async void CreateOrJoinRandomPublicRoom()
    {
        await runnerInstance.JoinSessionLobby(SessionLobby.Shared);

        var sessions = CurrentSessionList
            .Where(x => x.IsOpen && x.IsVisible)
            .OrderByDescending(x => x.PlayerCount)
            .ToList();

        if (sessions.Count > 0)
        {
            await runnerInstance.StartGame(new StartGameArgs()
            {
                Scene = SceneRef.FromIndex(Instance.GameSceneIndex),
                SessionName = sessions[0].Name,
                GameMode = GameMode.Shared,
            });
        }
        else
        {
            string randomSessionName = GenerateRandomRoomName(6);

            await runnerInstance.StartGame(new StartGameArgs()
            {
                Scene = SceneRef.FromIndex(Instance.GameSceneIndex),
                SessionName = randomSessionName,
                GameMode = GameMode.Shared,
                IsVisible = true,
            });
        }
    }

    private static string GenerateRandomRoomName(int length)
    {
        //const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        const string chars = "0123456789";
        System.Text.StringBuilder result = new System.Text.StringBuilder(length);

        for (int i = 0; i < length; i++)
        {
            int index = UnityEngine.Random.Range(0, chars.Length);
            result.Append(chars[index]);
        }

        return result.ToString();
    }

    public void SaveRegionToken(string token)
    {
        RegionToken = token;
        PlayerPrefs.SetString("Region", token);
    }

    public void SaveReconnect(bool state)
    {
        ReconnectToRegion = state;

        if (state)
            PlayerPrefs.SetInt("Reconnect", 1);
        else
            PlayerPrefs.SetInt("Reconnect", 0);
    }

    #region Load

    private void LoadRegionToken()
    {
        if (PlayerPrefs.HasKey("Region"))
        {
            RegionToken = PlayerPrefs.GetString("Region");
        }
    }

    private void LoadName()
    {
        if (PlayerPrefs.HasKey("PlayerName"))
        {
            PlayerName = PlayerPrefs.GetString("PlayerName");
        }
        else
        {
            PlayerName = "Player" + UnityEngine.Random.Range(1000, 9999);
            PlayerPrefs.SetString("PlayerName", PlayerName);
        }
    }

    #endregion

    #region Callbacks
    public void OnConnectedToServer(NetworkRunner runner) { }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }

    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }

    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }

    public void OnInput(NetworkRunner runner, NetworkInput input) { }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }

    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }

    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }

    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }

    public void OnSceneLoadDone(NetworkRunner runner) { }

    public void OnSceneLoadStart(NetworkRunner runner) { }

    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    #endregion
}