using System.Collections.Generic;
using Fusion.Photon.Realtime;
using Fusion.Sockets;
using UnityEngine.UI;
using UnityEngine;
using Fusion;
using System;
using TMPro;

public class LobbyController : MonoBehaviour, INetworkRunnerCallbacks
{
    [SerializeField] private PhotonAppSettings photonAppSettings;

    [Header("UI")]
    [SerializeField] private GameObject regionsHolder;
    [SerializeField] private GameObject loadingHolder;
    [SerializeField] private GameObject menuHolder;
    [SerializeField] private GameObject roomLoadingHolder;
    [SerializeField] private Button quickButton;
    [SerializeField] private TMP_InputField playerText_Input;
    [SerializeField] private TextMeshProUGUI regionText;

    private NetworkManager networkManager;

    private void Start()
    {
        if(NetworkManager.Instance != null)
        {
            networkManager = NetworkManager.Instance;
            NetworkManager.runnerInstance.AddCallbacks(this);
            InitLobbyCanvas();
        }
    }

    private void InitLobbyCanvas()
    {
        if (NetworkManager.ReconnectToRegion)
        {
            regionsHolder.SetActive(false);
            loadingHolder.SetActive(true);
            networkManager.JoinLobby();
        }
        else
        {
            regionsHolder.SetActive(true);
            loadingHolder.SetActive(false);
        }

        playerText_Input.SetTextWithoutNotify(NetworkManager.Instance.PlayerName); //Player Name
        UpdateRegionText();
    }

    private void UpdateRegionText()
    {
        regionText.text = "Region " + NetworkManager.Instance.RegionToken;
    }

    public void SavePlayerName(string newName)
    {
        NetworkManager.Instance.PlayerName = newName;

        if (newName.Length > 0 && !string.IsNullOrEmpty(newName))
            PlayerPrefs.SetString("PlayerName", newName);
    }

    public void QuickPlay()
    {
        NetworkManager.CreateOrJoinRandomPublicRoom();
        WaitRoom();
    }

    private void WaitRoom()
    {
        quickButton.interactable = false;
        roomLoadingHolder.SetActive(true);
    }

    public void LeaveLobbyAndRegion()
    {
        NetworkManager.ShutDown();
        NetworkManager.Instance.SaveReconnect(false);
    }

    public void SelectRegion(string regionTOKEN)
    {
        photonAppSettings.AppSettings.FixedRegion = regionTOKEN;

        networkManager.JoinLobby();
        NetworkManager.Instance.SaveRegionToken(regionTOKEN);
        NetworkManager.Instance.SaveReconnect(true);

        regionsHolder.SetActive(false);
        loadingHolder.SetActive(true);
        UpdateRegionText();
    }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) 
    {
        loadingHolder.SetActive(false); 
        menuHolder.SetActive(true); 
    }

    #region Callbacks
    public void OnConnectedToServer(NetworkRunner runner) { }

    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }

    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player){}

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player){}

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player){}

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason){}

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason){}

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token){}

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason){}

    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message){}

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data){}

    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress){}

    public void OnInput(NetworkRunner runner, NetworkInput input){}

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input){}



    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data){}

    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken){}

    public void OnSceneLoadDone(NetworkRunner runner){}

    public void OnSceneLoadStart(NetworkRunner runner){}
    #endregion
}
