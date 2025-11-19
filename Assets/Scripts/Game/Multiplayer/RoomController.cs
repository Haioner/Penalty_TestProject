using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using Fusion;
using TMPro;

public enum PlayerClass
{
    None, Beater, GoalKeeper
}

public class RoomController : NetworkBehaviour, IPlayerJoined, IPlayerLeft
{
    private const int MAX_PLAYERS = 2;
    private const float CLASS_SELECTION_TIME = 15f;

    [Header("UI - Waiting Screen")]
    [SerializeField] private GameObject waitingCanvasHolder;

    [Header("UI - Class Selection Screen")]
    [SerializeField] private GameObject classSelectionCanvasHolder;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private Button beaterButton;
    [SerializeField] private Button goalKeeperButton;
    [SerializeField] private Transform beaterPlayersContent;
    [SerializeField] private Transform goalKeeperPlayersContent;
    [SerializeField] private GameObject playerNamePrefab;

    [Header("References")]
    [SerializeField] private PlayerSpawner playerSpawner;

    [Networked, OnChangedRender(nameof(OnTimerChanged))] private TickTimer classSelectionTimer { get; set; }
    [Networked] private NetworkBool allPlayersReady { get; set; }
    [Networked] private int readyPlayerCount { get; set; }

    private Dictionary<PlayerRef, PlayerClassChoice> playerChoices = new Dictionary<PlayerRef, PlayerClassChoice>();
    private Dictionary<PlayerRef, GameObject> beaterNameObjects = new Dictionary<PlayerRef, GameObject>();
    private Dictionary<PlayerRef, GameObject> goalKeeperNameObjects = new Dictionary<PlayerRef, GameObject>();
    private bool hasLocalPlayerChosen = false;
    private PlayerClass localPlayerChoice = PlayerClass.None;

    private struct PlayerClassChoice : INetworkStruct
    {
        public PlayerClass chosenClass;
        public NetworkBool hasChosen;
        public NetworkString<_16> playerName;
    }

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            Debug.Log("TEM AUTHORITY");
            readyPlayerCount = 0;
            allPlayersReady = false;
        }

        if (beaterButton != null)
            beaterButton.onClick.AddListener(OnBeaterButtonClicked);

        if (goalKeeperButton != null)
            goalKeeperButton.onClick.AddListener(OnGoalKeeperButtonClicked);

        ShowWaitingScreen();
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

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (beaterButton != null)
            beaterButton.onClick.RemoveListener(OnBeaterButtonClicked);

        if (goalKeeperButton != null)
            goalKeeperButton.onClick.RemoveListener(OnGoalKeeperButtonClicked);
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
                classSelectionTimer = TickTimer.CreateFromSeconds(Runner, CLASS_SELECTION_TIME);
                RPC_ShowClassSelectionScreen();
            }
        }
        else if (playerCount < MAX_PLAYERS)
        {
            ShowWaitingScreen();
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowClassSelectionScreen()
    {
        ShowClassSelectionScreen();
    }

    private void ShowWaitingScreen()
    {
        if (waitingCanvasHolder != null)
            waitingCanvasHolder.SetActive(true);

        if (classSelectionCanvasHolder != null)
            classSelectionCanvasHolder.SetActive(false);
    }

    private void ShowClassSelectionScreen()
    {
        if (waitingCanvasHolder != null)
            waitingCanvasHolder.SetActive(false);

        if (classSelectionCanvasHolder != null)
            classSelectionCanvasHolder.SetActive(true);

        hasLocalPlayerChosen = false;
        localPlayerChoice = PlayerClass.None;

        if (beaterButton != null)
            beaterButton.interactable = true;

        if (goalKeeperButton != null)
            goalKeeperButton.interactable = true;

        UpdateTimerUI();
    }

    public void OnBeaterButtonClicked()
    {
        SelectClass(PlayerClass.Beater);
    }

    public void OnGoalKeeperButtonClicked()
    {
        SelectClass(PlayerClass.GoalKeeper);
    }

    private void SelectClass(PlayerClass selectedClass)
    {
        if (hasLocalPlayerChosen)
            return;

        hasLocalPlayerChosen = true;
        localPlayerChoice = selectedClass;

        RPC_SendClassChoice(Runner.LocalPlayer, selectedClass, NetworkManager.Instance.PlayerName);

        if (beaterButton != null)
            beaterButton.interactable = false;

        if (goalKeeperButton != null)
            goalKeeperButton.interactable = false;
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_SendClassChoice(PlayerRef player, PlayerClass chosenClass, string playerName)
    {
        if (!playerChoices.ContainsKey(player))
        {
            PlayerClassChoice choice = new PlayerClassChoice
            {
                chosenClass = chosenClass,
                hasChosen = true,
                playerName = playerName
            };
            playerChoices[player] = choice;
            readyPlayerCount++;
        }

        RPC_UpdateClassUI(player, chosenClass, playerName);

        if (readyPlayerCount >= MAX_PLAYERS)
        {
            ProcessClassSelections();
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_UpdateClassUI(PlayerRef player, PlayerClass chosenClass, string playerName)
    {
        UpdatePlayerClassUI(player, chosenClass, playerName);
    }

    private void UpdatePlayerClassUI(PlayerRef player, PlayerClass chosenClass, string playerName)
    {
        RemovePlayerFromAllLists(player);

        if (chosenClass == PlayerClass.Beater && beaterPlayersContent != null)
        {
            GameObject nameObj = Instantiate(playerNamePrefab, beaterPlayersContent);
            TextMeshProUGUI nameText = nameObj.GetComponent<TextMeshProUGUI>();
            if (nameText != null)
                nameText.text = playerName;
            beaterNameObjects[player] = nameObj;
        }
        else if (chosenClass == PlayerClass.GoalKeeper && goalKeeperPlayersContent != null)
        {
            GameObject nameObj = Instantiate(playerNamePrefab, goalKeeperPlayersContent);
            TextMeshProUGUI nameText = nameObj.GetComponent<TextMeshProUGUI>();
            if (nameText != null)
                nameText.text = playerName;
            goalKeeperNameObjects[player] = nameObj;
        }
    }

    private void RemovePlayerFromAllLists(PlayerRef player)
    {
        if (beaterNameObjects.ContainsKey(player))
        {
            Destroy(beaterNameObjects[player]);
            beaterNameObjects.Remove(player);
        }

        if (goalKeeperNameObjects.ContainsKey(player))
        {
            Destroy(goalKeeperNameObjects[player]);
            goalKeeperNameObjects.Remove(player);
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!allPlayersReady)
            return;

        if (Object.HasStateAuthority && classSelectionTimer.Expired(Runner))
        {
            ProcessClassSelections();
        }
    }

    public override void Render()
    {
        if (allPlayersReady && classSelectionCanvasHolder != null && classSelectionCanvasHolder.activeSelf)
        {
            UpdateTimerUI();
        }
    }

    private void UpdateTimerUI()
    {
        if (timerText == null)
            return;

        if (classSelectionTimer.RemainingTime(Runner).HasValue)
        {
            float remainingTime = (float)classSelectionTimer.RemainingTime(Runner).Value;
            timerText.text = Mathf.CeilToInt(remainingTime).ToString();
        }
    }

    private void OnTimerChanged()
    {
        UpdateTimerUI();
    }

    private void ProcessClassSelections()
    {
        if (!Object.HasStateAuthority)
            return;

        List<PlayerRef> allPlayers = new List<PlayerRef>(Runner.ActivePlayers);

        foreach (PlayerRef player in allPlayers)
        {
            if (!playerChoices.ContainsKey(player))
            {
                PlayerClassChoice choice = new PlayerClassChoice
                {
                    chosenClass = PlayerClass.None,
                    hasChosen = false,
                    playerName = "Player"
                };
                playerChoices[player] = choice;
            }
        }

        List<PlayerRef> batedorPlayers = new List<PlayerRef>();
        List<PlayerRef> goleiroPlayers = new List<PlayerRef>();
        List<PlayerRef> noChoicePlayers = new List<PlayerRef>();

        foreach (var kvp in playerChoices)
        {
            if (kvp.Value.chosenClass == PlayerClass.Beater)
                batedorPlayers.Add(kvp.Key);
            else if (kvp.Value.chosenClass == PlayerClass.GoalKeeper)
                goleiroPlayers.Add(kvp.Key);
            else
                noChoicePlayers.Add(kvp.Key);
        }

        Dictionary<PlayerRef, PlayerClass> finalAssignments = new Dictionary<PlayerRef, PlayerClass>();

        if (batedorPlayers.Count == 1 && goleiroPlayers.Count == 1)
        {
            finalAssignments[batedorPlayers[0]] = PlayerClass.Beater;
            finalAssignments[goleiroPlayers[0]] = PlayerClass.GoalKeeper;
        }
        else if (batedorPlayers.Count == 2)
        {
            int randomIndex = Random.Range(0, 2);
            finalAssignments[batedorPlayers[randomIndex]] = PlayerClass.Beater;
            finalAssignments[batedorPlayers[1 - randomIndex]] = PlayerClass.GoalKeeper;
        }
        else if (goleiroPlayers.Count == 2)
        {
            int randomIndex = Random.Range(0, 2);
            finalAssignments[goleiroPlayers[randomIndex]] = PlayerClass.GoalKeeper;
            finalAssignments[goleiroPlayers[1 - randomIndex]] = PlayerClass.Beater;
        }
        else if (noChoicePlayers.Count == 2)
        {
            int randomIndex = Random.Range(0, 2);
            finalAssignments[noChoicePlayers[randomIndex]] = PlayerClass.Beater;
            finalAssignments[noChoicePlayers[1 - randomIndex]] = PlayerClass.GoalKeeper;
        }
        else if (noChoicePlayers.Count == 1)
        {
            if (batedorPlayers.Count == 1)
            {
                finalAssignments[batedorPlayers[0]] = PlayerClass.Beater;
                finalAssignments[noChoicePlayers[0]] = PlayerClass.GoalKeeper;
            }
            else if (goleiroPlayers.Count == 1)
            {
                finalAssignments[goleiroPlayers[0]] = PlayerClass.GoalKeeper;
                finalAssignments[noChoicePlayers[0]] = PlayerClass.Beater;
            }
        }

        if (playerSpawner != null)
        {
            playerSpawner.SpawnPlayersWithClasses(finalAssignments);
        }

        RPC_HideClassSelection();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_HideClassSelection()
    {
        if (classSelectionCanvasHolder != null)
            classSelectionCanvasHolder.SetActive(false);
    }
}
