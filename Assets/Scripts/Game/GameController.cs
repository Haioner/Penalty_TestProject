using System.Collections.Generic;
using UnityEngine;
using Fusion;
using TMPro;

public class GameController : NetworkBehaviour
{
    private const int ROUNDS_PER_SIDE = 5;

    [Header("Role Swap Settings")]
    [SerializeField] private int turnsBeforeRoleSwap = 1;

    [Header("Player Positions")]
    [SerializeField] private Transform beaterPosition;
    [SerializeField] private Transform goalKeeperPosition;

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI roundText;
    [SerializeField] private TextMeshProUGUI waitingText;
    [SerializeField] private TextMeshProUGUI countdownText;
    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private GameObject finalResultScreen;
    [SerializeField] private TextMeshProUGUI finalResultText;
    [SerializeField] private TextMeshProUGUI finalScoreText;
    [SerializeField] private TextMeshProUGUI coinsEarnedText;
    [SerializeField] private TextMeshProUGUI rematchWaitingText;
    [SerializeField] private UnityEngine.UI.Button playAgainButton;
    [SerializeField] private UnityEngine.UI.Button leaveRoomButton;

    [Header("Turn Execution")]
    [SerializeField] private float TURN_TIME = 10f;
    [SerializeField] private TurnExecutor turnExecutor;

    [Networked] private TickTimer turnTimer { get; set; }
    [Networked] private TickTimer countdownTimer { get; set; }
    [Networked] private TickTimer roleSwapDelayTimer { get; set; }
    [Networked] private int countdownValue { get; set; }
    [Networked] private NetworkBool isCountingDown { get; set; }
    [Networked] private NetworkBool isWaitingRoleSwap { get; set; }
    [Networked] private int currentTurn { get; set; }
    [Networked] private int player1Score { get; set; }
    [Networked] private int player2Score { get; set; }
    [Networked] private NetworkBool gameStarted { get; set; }
    [Networked] private NetworkBool suddenDeath { get; set; }
    [Networked] private NetworkBool player1WantsRematch { get; set; }
    [Networked] private NetworkBool player2WantsRematch { get; set; }
    [Networked] private int suddenDeathRoundStartTurn { get; set; }
    [Networked] private int player1SuddenDeathScore { get; set; }
    [Networked] private int player2SuddenDeathScore { get; set; }

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

    public void LeaveRoom() 
    {
        Debug.Log("<color=orange>[🚪 LEAVE ROOM] Player leaving the match...</color>");
        NetworkManager.ShutDown();
    }

    public void RestartMatch()
    {
        RPC_RequestRematch(Runner.LocalPlayer);

        if (playAgainButton != null)
        {
            playAgainButton.interactable = false;
        }

    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestRematch(PlayerRef playerRef)
    {
        int playerIndex = players.FindIndex(p => p.playerRef == playerRef);
        string playerName = GetPlayerNameByRef(playerRef);
        
        Debug.Log($"<color=cyan>[🔄 REMATCH REQUEST] {playerName} (Player {playerIndex + 1}) wants to play again!</color>");
        
        if (playerIndex == 0)
        {
            player1WantsRematch = true;
        }
        else if (playerIndex == 1)
        {
            player2WantsRematch = true;
        }
        
        RPC_UpdateRematchUI(playerRef, playerName);
        
        if (player1WantsRematch && player2WantsRematch)
        {
            Debug.Log("<color=green>[🔄 REMATCH] Both players ready! Starting new match...</color>");
            ExecuteRematch();
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_UpdateRematchUI(PlayerRef requestingPlayer, NetworkString<_64> playerName)
    {

        if (Runner.LocalPlayer != requestingPlayer)
        {
            if (rematchWaitingText != null)
            {
                rematchWaitingText.gameObject.SetActive(true);
                rematchWaitingText.text = $"{playerName} wants to play again";
            }
        }
    }

    private void ExecuteRematch()
    {
        Debug.Log("<color=cyan>[🔄 REMATCH] Executing rematch - resetting game state...</color>");
        
        currentTurn = 0;
        player1Score = 0;
        player2Score = 0;
        suddenDeath = false;
        isCountingDown = false;
        isWaitingRoleSwap = false;
        gameStarted = false;
        player1WantsRematch = false;
        player2WantsRematch = false;
        suddenDeathRoundStartTurn = 0;
        player1SuddenDeathScore = 0;
        player2SuddenDeathScore = 0;
        
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
        
        if (turnExecutor != null)
        {
            turnExecutor.ResetForNewTurn();
            Debug.Log("<color=cyan>[🔄 REMATCH] TurnExecutor reset complete</color>");
        }
        
        RPC_HideFinalResultScreen();
        
        Debug.Log($"<color=cyan>[🔄 REMATCH] Swapping player roles... Players registered: {players.Count}</color>");
        RPC_SwapRoles();
        
        Debug.Log("<color=cyan>[🔄 REMATCH] Calling StartGame...</color>");
        StartGame();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_HideFinalResultScreen()
    {
        if (finalResultScreen != null)
        {
            finalResultScreen.SetActive(false);
        }
        
        if (rematchWaitingText != null)
        {
            rematchWaitingText.gameObject.SetActive(false);
        }
        
        if (playAgainButton != null)
        {
            playAgainButton.interactable = true;
        }
        
        if (leaveRoomButton != null)
        {
            leaveRoomButton.interactable = true;
        }
    }

    public Vector3 GetPositionForRole(PlayerRole role)
    {
        if (role == PlayerRole.Beater && beaterPosition != null)
            return beaterPosition.position;
        else if (role == PlayerRole.GoalKeeper && goalKeeperPosition != null)
            return goalKeeperPosition.position;

        return Vector3.zero;
    }

    public Quaternion GetRotationForRole(PlayerRole role)
    {
        if (role == PlayerRole.Beater && beaterPosition != null)
            return beaterPosition.rotation;
        else if (role == PlayerRole.GoalKeeper && goalKeeperPosition != null)
            return goalKeeperPosition.rotation;

        return Quaternion.identity;
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
            isCountingDown = false;
            isWaitingRoleSwap = false;
            countdownValue = 3;
            player1WantsRematch = false;
            player2WantsRematch = false;
            suddenDeathRoundStartTurn = 0;
            player1SuddenDeathScore = 0;
            player2SuddenDeathScore = 0;
        }

        if (countdownText != null)
            countdownText.gameObject.SetActive(false);

        if (resultText != null)
            resultText.gameObject.SetActive(false);

        if (finalResultScreen != null)
            finalResultScreen.SetActive(false);
        
        if (rematchWaitingText != null)
            rematchWaitingText.gameObject.SetActive(false);
    }

    public void StartGame()
    {
        Debug.Log($"<color=yellow>[🎮 START GAME] Called - HasStateAuthority: {Object.HasStateAuthority}, Runner: {Runner != null}</color>");
        
        if (!Object.HasStateAuthority)
        {
            Debug.Log("<color=orange>[🎮 START GAME] Skipped - Not StateAuthority</color>");
            return;
        }

        Debug.Log("<color=green>[🎮 START GAME] Starting countdown...</color>");
        isCountingDown = true;
        countdownValue = 3;
        countdownTimer = TickTimer.CreateFromSeconds(Runner, 1f);
        RPC_ShowCountdown(countdownValue);
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

        Debug.Log($"<color=cyan>[🎯 NEW TURN] Starting turn {currentTurn + 1}</color>");

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

        if (turnExecutor != null)
            turnExecutor.ResetForNewTurn();

        turnTimer = TickTimer.CreateFromSeconds(Runner, TURN_TIME);
        
        Debug.Log($"<color=cyan>[🎯 NEW TURN] Turn timer started for {TURN_TIME} seconds</color>");
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

    public PlayerController GetCurrentGoalkeeper()
    {
        foreach (var playerData in players)
        {
            if (playerData.playerController != null && playerData.playerController.Role == PlayerRole.GoalKeeper)
            {
                return playerData.playerController;
            }
        }
        return null;
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

        CheckBothPlayersChosen(player);
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

        CheckBothPlayersChosen(player);
    }

    private void CheckBothPlayersChosen(PlayerRef playerWhoChose)
    {
        if (beaterChoice.hasChosen && goalKeeperChoice.hasChosen)
        {
            RPC_HideWaitingText();
            ProcessTurn();
        }
        else
        {
            RPC_ShowWaitingTextForPlayer(playerWhoChose);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowWaitingTextForPlayer(PlayerRef playerWhoChose)
    {
        if (Runner.LocalPlayer == playerWhoChose)
        {
            if (waitingText != null)
            {
                waitingText.gameObject.SetActive(true);
                waitingText.text = "Waiting other player...";
            }
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowCountdown(int value)
    {
        Debug.Log($"<color=yellow>[RPC COUNTDOWN] Showing countdown: {value}</color>");
        
        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(true);
            
            if (value > 0)
                countdownText.text = value.ToString();
            else
                countdownText.text = "GO!";
                
            Debug.Log($"<color=yellow>[RPC COUNTDOWN] Text set to: {countdownText.text}</color>");
        }
        else
        {
            Debug.LogWarning("<color=red>[RPC COUNTDOWN] countdownText is NULL!</color>");
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_HideCountdown()
    {
        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(false);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_EnablePlayerControls()
    {
        PlayerController[] allPlayers = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        foreach (PlayerController player in allPlayers)
        {
            RoleController roleController = player.GetComponent<RoleController>();
            if (roleController != null)
            {
                roleController.EnableRoleControlsAfterCountdown();
            }
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_HideWaitingText()
    {
        if (waitingText != null)
        {
            waitingText.gameObject.SetActive(false);
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority)
            return;

        if (isCountingDown)
        {
            if (countdownTimer.Expired(Runner))
            {
                countdownValue--;
                Debug.Log($"<color=yellow>[⏱️ COUNTDOWN] Tick - countdownValue: {countdownValue}</color>");

                if (countdownValue > 0)
                {
                    RPC_ShowCountdown(countdownValue);
                    countdownTimer = TickTimer.CreateFromSeconds(Runner, 1f);
                }
                else if (countdownValue == 0)
                {
                    RPC_ShowCountdown(0);
                    countdownTimer = TickTimer.CreateFromSeconds(Runner, 1f);
                }
                else
                {
                    Debug.Log("<color=green>[⏱️ COUNTDOWN] Countdown finished! Starting game...</color>");
                    RPC_HideCountdown();
                    isCountingDown = false;
                    gameStarted = true;
                    RPC_EnablePlayerControls();
                    StartNewTurn();
                }
            }
            return;
        }

        if (isWaitingRoleSwap)
        {
            if (roleSwapDelayTimer.Expired(Runner))
            {
                isWaitingRoleSwap = false;
                RPC_EnablePlayerControlsAfterSwap();
                StartNewTurn();
            }
            return;
        }

        if (!gameStarted)
            return;

        if (turnExecutor != null && turnExecutor.GetCurrentState() == TurnState.Completed)
        {
            OnTurnExecutionComplete();
            return;
        }

        if (turnTimer.Expired(Runner) && (turnExecutor == null || turnExecutor.GetCurrentState() == TurnState.WaitingChoices))
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

        PlayerRef beaterPlayer = GetCurrentBeaterPlayer();
        PlayerRef goalkeeperPlayer = GetCurrentGoalKeeperPlayer();
        string beaterName = GetPlayerNameByRef(beaterPlayer);
        string goalkeeperName = GetPlayerNameByRef(goalkeeperPlayer);

        bool isMiss = beaterChoice.precision == PrecisionZone.Miss;
        bool isSaved = isMiss ? false : CheckSave(beaterChoice, goalKeeperChoice);
        bool isGoal = !isMiss && !isSaved;

        if (turnExecutor != null)
        {
            turnExecutor.ExecuteTurn(
                beaterChoice.horizontalPos, beaterChoice.verticalPos, beaterChoice.precision,
                goalKeeperChoice.horizontalPos, goalKeeperChoice.verticalPos,
                beaterName, goalkeeperName, isGoal
            );
        }

        Debug.Log($"[ProcessTurn] === ROUND {currentTurn + 1} ===");
        Debug.Log($"[ProcessTurn] Beater: {beaterName} ({beaterPlayer}) | Goalkeeper: {goalkeeperName} ({goalkeeperPlayer})");
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
                Debug.Log($"<color=green>[⚽ GOAL!] {beaterName} scored! Score: {scoreBefore1}-{scoreBefore2} → {player1Score}-{player2Score}</color>");
            }
            else
            {
                player2Score++;
                Debug.Log($"<color=green>[⚽ GOAL!] {beaterName} scored! Score: {scoreBefore1}-{scoreBefore2} → {player1Score}-{player2Score}</color>");
            }
        }
        else
        {
            string reason = isMiss ? "MISSED" : "SAVED";
            Debug.Log($"<color=yellow>[❌ NO GOAL] {reason}! Score unchanged: {player1Score}-{player2Score}</color>");
        }

        RPC_ShowTurnResult(beaterChoice.horizontalPos, beaterChoice.verticalPos, beaterChoice.precision, 
                          goalKeeperChoice.horizontalPos, goalKeeperChoice.verticalPos, isGoal, isSaved, isMiss);
    }

    public void OnTurnExecutionComplete()
    {
        if (!Object.HasStateAuthority)
            return;

        RPC_HideResultMessage();

        currentTurn++;

        if (suddenDeath)
        {
            int turnsInCurrentRound = currentTurn - suddenDeathRoundStartTurn;
            
            Debug.Log($"<color=cyan>[SUDDEN DEATH] Turn {currentTurn}, Turns in round: {turnsInCurrentRound}</color>");
            
            if (turnsInCurrentRound < 2)
            {
                Debug.Log("<color=cyan>[SUDDEN DEATH] First player shot, swapping roles for second player...</color>");
                RPC_SwapRoles();
                isWaitingRoleSwap = true;
                roleSwapDelayTimer = TickTimer.CreateFromSeconds(Runner, 0.5f);
            }
            else
            {
                int player1RoundScore = player1Score - player1SuddenDeathScore;
                int player2RoundScore = player2Score - player2SuddenDeathScore;
                
                Debug.Log($"<color=cyan>[SUDDEN DEATH] Round complete! P1: {player1RoundScore}, P2: {player2RoundScore}</color>");
                
                if (player1RoundScore > player2RoundScore)
                {
                    Debug.Log("<color=green>[SUDDEN DEATH] Player 1 scored and Player 2 missed - Player 1 wins!</color>");
                    RPC_EndGame(1);
                }
                else if (player2RoundScore > player1RoundScore)
                {
                    Debug.Log("<color=green>[SUDDEN DEATH] Player 2 scored and Player 1 missed - Player 2 wins!</color>");
                    RPC_EndGame(2);
                }
                else
                {
                    Debug.Log("<color=yellow>[SUDDEN DEATH] Both players had same result - continuing sudden death!</color>");
                    player1SuddenDeathScore = player1Score;
                    player2SuddenDeathScore = player2Score;
                    suddenDeathRoundStartTurn = currentTurn;
                    
                    RPC_SwapRoles();
                    isWaitingRoleSwap = true;
                    roleSwapDelayTimer = TickTimer.CreateFromSeconds(Runner, 0.5f);
                }
            }
        }
        else if (currentTurn >= (ROUNDS_PER_SIDE * 2))
        {
            CheckGameEnd();
        }
        else
        {
            if (turnsBeforeRoleSwap > 0 && currentTurn % turnsBeforeRoleSwap == 0 && currentTurn < (ROUNDS_PER_SIDE * 2))
            {
                Debug.Log($"<color=cyan>[🔄 SWAP] After {turnsBeforeRoleSwap} turn(s), players are swapping roles! (Turn {currentTurn})</color>");
                RPC_SwapRoles();
                isWaitingRoleSwap = true;
                roleSwapDelayTimer = TickTimer.CreateFromSeconds(Runner, 0.5f);
            }
            else
            {
                RPC_RestartTurnForPlayers();
                StartNewTurn();
            }
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

    public void ShowTurnResultMessage(string beaterName, string goalkeeperName, bool isGoal)
    {
        RPC_ShowResultMessage(beaterName, goalkeeperName, isGoal);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowResultMessage(string beaterName, string goalkeeperName, bool isGoal)
    {
        if (resultText != null)
        {
            resultText.gameObject.SetActive(true);
            if (isGoal)
            {
                resultText.text = $"{beaterName} GOAL !!!";
            }
            else
            {
                resultText.text = $"{goalkeeperName} Defended !!!";
            }
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_HideResultMessage()
    {
        if (resultText != null)
        {
            resultText.gameObject.SetActive(false);
        }
    }

    private void CheckGameEnd()
    {
        if (player1Score == player2Score)
        {
            Debug.Log("<color=magenta>[⚡ SUDDEN DEATH] Score tied! Starting sudden death mode!</color>");
            suddenDeath = true;
            suddenDeathRoundStartTurn = currentTurn;
            player1SuddenDeathScore = player1Score;
            player2SuddenDeathScore = player2Score;
            RPC_SwapRoles();
            isWaitingRoleSwap = true;
            roleSwapDelayTimer = TickTimer.CreateFromSeconds(Runner, 0.5f);
        }
        else
        {
            RPC_EndGame(player1Score > player2Score ? 1 : 2);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_EndGame(int winnerPlayerNumber)
    {
        Debug.Log($"<color=green>[🏆 GAME OVER] Player {winnerPlayerNumber} wins with score {player1Score}-{player2Score}!</color>");
        gameStarted = false;
        
        if (finalResultScreen != null)
        {
            finalResultScreen.SetActive(true);
            
            string player1Name = GetPlayerName(0);
            string player2Name = GetPlayerName(1);
            
            int localPlayerIndex = -1;
            for (int i = 0; i < players.Count; i++)
            {
                if (players[i].playerRef == Runner.LocalPlayer)
                {
                    localPlayerIndex = i;
                    break;
                }
            }
            
            bool isLocalPlayerWinner = (localPlayerIndex + 1) == winnerPlayerNumber;
            
            Debug.Log($"[RPC_EndGame] LocalPlayer: {Runner.LocalPlayer}, LocalPlayerIndex: {localPlayerIndex}, WinnerPlayerNumber: {winnerPlayerNumber}, IsWinner: {isLocalPlayerWinner}");
            
            if (finalResultText != null)
            {
                finalResultText.text = isLocalPlayerWinner ? "VICTORY!" : "DEFEAT!";
            }
            
            if (finalScoreText != null)
            {
                finalScoreText.text = $"{player1Name} {player1Score} x {player2Score} {player2Name}";
            }
            
            int coinsEarned = 0;
            if (NetworkManager.Instance != null)
            {
                if (isLocalPlayerWinner)
                {
                    coinsEarned = 10;
                    NetworkManager.Instance.AddCoins(coinsEarned);
                    Debug.Log("<color=yellow>[💰 COINS] Victory! +10 coins awarded!</color>");
                }
                else
                {
                    coinsEarned = 3;
                    NetworkManager.Instance.AddCoins(coinsEarned);
                    Debug.Log("<color=yellow>[💰 COINS] Defeat! +3 coins awarded!</color>");
                }
            }
            
            if (coinsEarnedText != null)
            {
                coinsEarnedText.text = $"+{coinsEarned} Coins";
            }
        }
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
        Debug.Log("<color=magenta>[🔄 SWAP ROLES] Swapping player roles...</color>");
        
        PlayerController[] allPlayers = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        Debug.Log($"<color=magenta>[🔄 SWAP ROLES] Found {allPlayers.Length} players to swap</color>");
        
        foreach (PlayerController player in allPlayers)
        {
            PlayerRole oldRole = player.Role;
            
            if (player.Role == PlayerRole.Beater)
            {
                player.SetRole(PlayerRole.GoalKeeper);
                Debug.Log($"<color=magenta>[🔄 SWAP ROLES] Player swapped: {oldRole} → {player.Role}</color>");
            }
            else if (player.Role == PlayerRole.GoalKeeper)
            {
                player.SetRole(PlayerRole.Beater);
                Debug.Log($"<color=magenta>[🔄 SWAP ROLES] Player swapped: {oldRole} → {player.Role}</color>");
            }
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_EnablePlayerControlsAfterSwap()
    {
        PlayerController[] allPlayers = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        foreach (PlayerController player in allPlayers)
        {
            RoleController roleController = player.GetComponent<RoleController>();
            if (roleController != null)
            {
                roleController.EnableRoleControlsAfterCountdown();
            }
        }
    }

    public override void Render()
    {
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (gameStarted && timerText != null && turnTimer.RemainingTime(Runner).HasValue)
        {
            float remainingTime = (float)turnTimer.RemainingTime(Runner).Value;
            timerText.text = Mathf.CeilToInt(remainingTime).ToString() + "s";
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
