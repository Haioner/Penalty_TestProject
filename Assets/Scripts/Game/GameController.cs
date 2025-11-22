using System.Collections.Generic;
using UnityEngine;
using Fusion;
using TMPro;

public class GameController : NetworkBehaviour
{
    private const int ROUNDS_PER_SIDE = 5;
    private const float TURN_TIME = 10f;

    [Header("Player Positions")]
    [SerializeField] private Transform beaterPosition;
    [SerializeField] private Transform goalKeeperPosition;

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI roundText;

    [Networked] private TickTimer turnTimer { get; set; }
    [Networked] private int currentTurn { get; set; }
    [Networked] private int player1Score { get; set; }
    [Networked] private int player2Score { get; set; }
    [Networked] private NetworkBool gameStarted { get; set; }
    [Networked] private NetworkBool suddenDeath { get; set; }

    private struct PlayerChoice : INetworkStruct
    {
        public ShotHorizontalPos horizontalPos;
        public ShotVerticalPos verticalPos;
        public PrecisionZone precision;
        public NetworkBool hasChosen;
    }

    [Networked] private PlayerChoice beaterChoice { get; set; }
    [Networked] private PlayerChoice goalKeeperChoice { get; set; }

    private class PlayerData
    {
        public PlayerRef playerRef;
        public string playerName;
        public PlayerController playerController;
    }

    private List<PlayerData> players = new List<PlayerData>();

    public Vector3 GetPositionForRole(PlayerRole role)
    {
        if (role == PlayerRole.Beater && beaterPosition != null)
            return beaterPosition.position;
        else if (role == PlayerRole.GoalKeeper && goalKeeperPosition != null)
            return goalKeeperPosition.position;

        return Vector3.zero;
    }

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            currentTurn = 0;
            player1Score = 0;
            player2Score = 0;
            gameStarted = false;
            suddenDeath = false;
        }
    }

    public void StartGame()
    {
        if (!Object.HasStateAuthority)
            return;

        gameStarted = true;
        StartNewTurn();
    }

    public void RegisterPlayer(PlayerRef playerRef, string name, PlayerController controller)
    {
        RPC_RegisterPlayer(playerRef, name, controller);
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RPC_RegisterPlayer(PlayerRef playerRef, string name, PlayerController controller)
    {
        if (players.FindIndex(p => p.playerRef == playerRef) == -1)
        {
            players.Add(new PlayerData
            {
                playerRef = playerRef,
                playerName = name,
                playerController = controller
            });
            Debug.Log($"[GameController] Registered player {playerRef} - Name: {name}, Controller: {controller != null}");
        }
    }

    private void StartNewTurn()
    {
        if (!Object.HasStateAuthority)
            return;

        beaterChoice = new PlayerChoice 
        { 
            horizontalPos = ShotHorizontalPos.Middle, 
            verticalPos = ShotVerticalPos.Middle, 
            precision = PrecisionZone.Medium, 
            hasChosen = false 
        };
        goalKeeperChoice = new PlayerChoice 
        { 
            horizontalPos = ShotHorizontalPos.Middle, 
            verticalPos = ShotVerticalPos.Middle, 
            precision = PrecisionZone.Medium, 
            hasChosen = false 
        };

        turnTimer = TickTimer.CreateFromSeconds(Runner, TURN_TIME);
    }

    private PlayerRef GetCurrentBeaterPlayer()
    {
        foreach (var playerData in players)
        {
            if (playerData.playerController != null && playerData.playerController.Role == PlayerRole.Beater)
            {
                return playerData.playerRef;
            }
        }
        return PlayerRef.None;
    }

    private PlayerRef GetCurrentGoalKeeperPlayer()
    {
        foreach (var playerData in players)
        {
            if (playerData.playerController != null && playerData.playerController.Role == PlayerRole.GoalKeeper)
            {
                return playerData.playerRef;
            }
        }
        return PlayerRef.None;
    }

    public void SubmitBeaterChoice(PlayerRef player, ShotHorizontalPos horizontalPos, ShotVerticalPos verticalPos, PrecisionZone precision)
    {
        RPC_SubmitBeaterChoice(player, horizontalPos, verticalPos, precision);
    }

    public void SubmitGoalKeeperChoice(PlayerRef player, ShotHorizontalPos horizontalPos, ShotVerticalPos verticalPos)
    {
        RPC_SubmitGoalKeeperChoice(player, horizontalPos, verticalPos);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_SubmitBeaterChoice(PlayerRef player, ShotHorizontalPos horizontalPos, ShotVerticalPos verticalPos, PrecisionZone precision)
    {
        if (beaterChoice.hasChosen)
            return;

        beaterChoice = new PlayerChoice
        {
            horizontalPos = horizontalPos,
            verticalPos = verticalPos,
            precision = precision,
            hasChosen = true
        };

        CheckBothPlayersChosen();
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_SubmitGoalKeeperChoice(PlayerRef player, ShotHorizontalPos horizontalPos, ShotVerticalPos verticalPos)
    {
        if (goalKeeperChoice.hasChosen)
            return;

        goalKeeperChoice = new PlayerChoice
        {
            horizontalPos = horizontalPos,
            verticalPos = verticalPos,
            precision = PrecisionZone.Medium,
            hasChosen = true
        };

        CheckBothPlayersChosen();
    }

    private void CheckBothPlayersChosen()
    {
        if (beaterChoice.hasChosen && goalKeeperChoice.hasChosen)
        {
            ProcessTurn();
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!gameStarted || !Object.HasStateAuthority)
            return;

        if (turnTimer.Expired(Runner))
        {
            if (!beaterChoice.hasChosen || !goalKeeperChoice.hasChosen)
            {
                AssignRandomChoices();
            }
            ProcessTurn();
        }
    }

    private void AssignRandomChoices()
    {
        if (!beaterChoice.hasChosen)
        {
            ShotHorizontalPos[] horizontalOptions = { ShotHorizontalPos.Left, ShotHorizontalPos.Middle, ShotHorizontalPos.Right };
            ShotVerticalPos[] verticalOptions = { ShotVerticalPos.Top, ShotVerticalPos.Middle, ShotVerticalPos.Bottom };

            beaterChoice = new PlayerChoice
            {
                horizontalPos = horizontalOptions[Random.Range(0, horizontalOptions.Length)],
                verticalPos = verticalOptions[Random.Range(0, verticalOptions.Length)],
                precision = (PrecisionZone)Random.Range(0, 3),
                hasChosen = true
            };
        }

        if (!goalKeeperChoice.hasChosen)
        {
            ShotHorizontalPos[] horizontalOptions = { ShotHorizontalPos.Left, ShotHorizontalPos.Middle, ShotHorizontalPos.Right };
            ShotVerticalPos[] verticalOptions = { ShotVerticalPos.Top, ShotVerticalPos.Middle, ShotVerticalPos.Bottom };

            goalKeeperChoice = new PlayerChoice
            {
                horizontalPos = horizontalOptions[Random.Range(0, horizontalOptions.Length)],
                verticalPos = verticalOptions[Random.Range(0, verticalOptions.Length)],
                precision = PrecisionZone.Medium,
                hasChosen = true
            };
        }
    }

    private void ProcessTurn()
    {
        if (!Object.HasStateAuthority)
            return;

        bool isMiss = beaterChoice.precision == PrecisionZone.Miss;
        bool isSaved = isMiss ? false : CheckSave(beaterChoice, goalKeeperChoice);
        bool isGoal = !isMiss && !isSaved;

        PlayerRef beaterPlayer = GetCurrentBeaterPlayer();
        PlayerRef goalkeeperPlayer = GetCurrentGoalKeeperPlayer();

        Debug.Log($"[ProcessTurn] === ROUND {currentTurn + 1} ===");
        Debug.Log($"[ProcessTurn] Beater: {GetPlayerNameByRef(beaterPlayer)} ({beaterPlayer}) | Goalkeeper: {GetPlayerNameByRef(goalkeeperPlayer)} ({goalkeeperPlayer})");
        Debug.Log($"[ProcessTurn] Shot: {beaterChoice.horizontalPos}/{beaterChoice.verticalPos} (Precision: {beaterChoice.precision})");
        Debug.Log($"[ProcessTurn] Dive: {goalKeeperChoice.horizontalPos}/{goalKeeperChoice.verticalPos}");
        Debug.Log($"[ProcessTurn] Result - Miss: {isMiss}, Saved: {isSaved}, GOAL: {isGoal}");

        if (isGoal)
        {
            int beaterPlayerIndex = players.FindIndex(p => p.playerRef == beaterPlayer);
            
            int scoreBefore1 = player1Score;
            int scoreBefore2 = player2Score;
            
            if (beaterPlayerIndex == 0)
            {
                player1Score++;
                Debug.Log($"<color=green>[⚽ GOAL!] {GetPlayerNameByRef(beaterPlayer)} scored! Score: {scoreBefore1}-{scoreBefore2} → {player1Score}-{player2Score}</color>");
            }
            else
            {
                player2Score++;
                Debug.Log($"<color=green>[⚽ GOAL!] {GetPlayerNameByRef(beaterPlayer)} scored! Score: {scoreBefore1}-{scoreBefore2} → {player1Score}-{player2Score}</color>");
            }
        }
        else
        {
            string reason = isMiss ? "MISSED" : "SAVED";
            Debug.Log($"<color=yellow>[❌ NO GOAL] {reason}! Score unchanged: {player1Score}-{player2Score}</color>");
        }

        RPC_ShowTurnResult(beaterChoice.horizontalPos, beaterChoice.verticalPos, beaterChoice.precision, 
                          goalKeeperChoice.horizontalPos, goalKeeperChoice.verticalPos, isGoal, isSaved, isMiss);

        currentTurn++;

        if (currentTurn >= (ROUNDS_PER_SIDE * 2))
        {
            CheckGameEnd();
        }
        else
        {
            if (currentTurn == ROUNDS_PER_SIDE)
            {
                Debug.Log($"<color=cyan>[🔄 SWAP] After {ROUNDS_PER_SIDE} rounds, players are swapping roles!</color>");
                RPC_SwapRoles();
            }
            else
            {
                RPC_RestartTurnForPlayers();
            }
            
            StartNewTurn();
        }
    }

    private bool CheckSave(PlayerChoice shot, PlayerChoice dive)
    {
        if (shot.precision == PrecisionZone.Miss)
            return false;

        if (shot.precision == PrecisionZone.Perfect)
        {
            return shot.horizontalPos == dive.horizontalPos && shot.verticalPos == dive.verticalPos;
        }
        else if (shot.precision == PrecisionZone.Medium)
        {
            return shot.horizontalPos == dive.horizontalPos;
        }

        return false;
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowTurnResult(ShotHorizontalPos shotH, ShotVerticalPos shotV, PrecisionZone precision, 
                                    ShotHorizontalPos diveH, ShotVerticalPos diveV, bool isGoal, bool isSaved, bool isMiss)
    {
        Debug.Log($"Turn {currentTurn} - Shot: {shotH}/{shotV} ({precision}), Dive: {diveH}/{diveV}, Goal: {isGoal}, Saved: {isSaved}, Miss: {isMiss}");
    }

    private void CheckGameEnd()
    {
        if (player1Score == player2Score)
        {
            suddenDeath = true;
            StartNewTurn();
        }
        else
        {
            RPC_EndGame(player1Score > player2Score ? 1 : 2);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_EndGame(int winnerPlayerNumber)
    {
        Debug.Log($"Game Over! Player {winnerPlayerNumber} wins with score {player1Score}-{player2Score}!");
        gameStarted = false;
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_RestartTurnForPlayers()
    {
        PlayerController[] allPlayers = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        foreach (PlayerController player in allPlayers)
        {
            if (player.Role != PlayerRole.None)
            {
                RoleController roleController = player.GetComponent<RoleController>();
                if (roleController != null)
                {
                    roleController.RestartRole();
                }
            }
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_SwapRoles()
    {
        PlayerController[] allPlayers = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        foreach (PlayerController player in allPlayers)
        {
            if (player.Role == PlayerRole.Beater)
            {
                player.SetRole(PlayerRole.GoalKeeper);
            }
            else if (player.Role == PlayerRole.GoalKeeper)
            {
                player.SetRole(PlayerRole.Beater);
            }
        }
    }

    public override void Render()
    {
        if (!gameStarted)
            return;

        UpdateUI();
    }

    private void UpdateUI()
    {
        if (timerText != null && turnTimer.RemainingTime(Runner).HasValue)
        {
            float remainingTime = (float)turnTimer.RemainingTime(Runner).Value;
            timerText.text = Mathf.CeilToInt(remainingTime).ToString();
        }

        if (scoreText != null)
        {
            string player1Name = GetPlayerName(0);
            string player2Name = GetPlayerName(1);

            scoreText.text = $"{player1Name} ({player1Score}) x {player2Name} ({player2Score})";
        }

        if (roundText != null)
        {
            int currentRoundDisplay = currentTurn + 1;
            string roundLabel = suddenDeath ? "SUDDEN DEATH" : $"Round {currentRoundDisplay}";
            roundText.text = roundLabel;
        }
    }

    private string GetPlayerName(int playerIndex)
    {
        if (players.Count <= playerIndex || playerIndex < 0)
        {
            return $"Player{playerIndex + 1}";
        }

        PlayerData playerData = players[playerIndex];
        
        if (!string.IsNullOrEmpty(playerData.playerName))
        {
            return playerData.playerName;
        }

        return $"Player{playerIndex + 1}";
    }

    private string GetPlayerNameByRef(PlayerRef playerRef)
    {
        PlayerData playerData = players.Find(p => p.playerRef == playerRef);
        
        if (playerData != null && !string.IsNullOrEmpty(playerData.playerName))
        {
            return playerData.playerName;
        }
        
        return "Unknown";
    }
}
