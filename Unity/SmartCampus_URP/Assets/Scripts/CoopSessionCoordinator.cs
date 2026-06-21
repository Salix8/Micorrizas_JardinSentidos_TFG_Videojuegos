using System;
using SmartCampus.Coop;
using SmartCampus.Coop.Minigames;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using SmartCampus.Dialogue;

[DisallowMultipleComponent]
public sealed class CoopSessionCoordinator : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private RelayConnectionService relayConnectionService;
    [SerializeField] private CoopSessionProgressSync sessionProgressSync;
    [SerializeField] private DialogueFlowSync dialogueFlowSync;
    [SerializeField] private bool persistAcrossScenes = true;

    [Header("Co-op Rules")]
    [SerializeField] [Range(CoopSessionRules.DefaultMinimumPlayers, CoopSessionRules.DefaultMaximumPlayers)] private int minPlayersToStart = CoopSessionRules.DefaultMinimumPlayers;
    [SerializeField] [Range(CoopSessionRules.DefaultMinimumPlayers, CoopSessionRules.DefaultMaximumPlayers)] private int maxPlayers = CoopSessionRules.DefaultMaximumPlayers;

    [Header("Scene Flow")]
    [SerializeField] private string lobbySceneName = "Lobby";
    [SerializeField] private string mainMapSceneName = "UJI";
    [SerializeField] private string sessionSummarySceneName = "CoopFinalResults";
    [SerializeField] private string[] miniGameSceneNames = new string[6];

    private readonly NetworkVariable<CoopGamePhase> currentPhase = new(CoopGamePhase.Lobby);
    private readonly NetworkVariable<int> activeMiniGameIndex = new(-1);
    private readonly NetworkList<ulong> playerSlots = new();

    public CoopGamePhase CurrentPhase => currentPhase.Value;
    public int ActiveMiniGameIndex => activeMiniGameIndex.Value;
    public int MinimumPlayersToStart => minPlayersToStart;
    public int MaximumPlayers => maxPlayers;
    public CoopSessionRules SessionRules => new(minPlayersToStart, maxPlayers);
    public CoopSessionProgressSync SessionProgressSync => sessionProgressSync;
    public int ConnectedPlayerCount => NetworkManager == null ? 0 : NetworkManager.ConnectedClientsIds.Count;
    public int RegisteredPlayerCount => playerSlots.Count;
    public int ConfiguredMinigameCount => miniGameSceneNames == null ? 0 : miniGameSceneNames.Length;
    public bool CanStartMainMap => IsServer && SessionRules.CanStart(ConnectedPlayerCount);

    public event Action<CoopGamePhase> PhaseChanged;
    public event Action SlotsChanged;

    private void Awake()
    {
        ResolveReferences();

        if (persistAcrossScenes)
        {
            DontDestroyOnLoad(gameObject);
        }

        ValidateMiniGameConfiguration(logWarnings: true);
    }

    public override void OnNetworkSpawn()
    {
        ResolveReferences();
        relayConnectionService ??= FindFirstObjectByType<RelayConnectionService>(FindObjectsInactive.Include);

        if (relayConnectionService != null)
        {
            minPlayersToStart = relayConnectionService.MinimumPlayersToStart;
            maxPlayers = relayConnectionService.MaximumPlayers;

            if (string.IsNullOrWhiteSpace(mainMapSceneName))
            {
                mainMapSceneName = relayConnectionService.MainMapSceneName;
            }
        }

        var rules = SessionRules;
        minPlayersToStart = rules.MinimumPlayers;
        maxPlayers = rules.MaximumPlayers;

        currentPhase.OnValueChanged += HandlePhaseChanged;
        playerSlots.OnListChanged += HandlePlayerSlotsChanged;
        sessionProgressSync?.SynchronizeConfigurationServer(resetProgress: sessionProgressSync.ConfiguredMinigameCount == 0);

        if (IsServer && NetworkManager != null)
        {
            NetworkManager.OnClientConnectedCallback += HandleClientConnected;
            NetworkManager.OnClientDisconnectCallback += HandleClientDisconnected;
            RebuildPlayerSlots();
        }

        SlotsChanged?.Invoke();
        PhaseChanged?.Invoke(currentPhase.Value);
    }

    public override void OnNetworkDespawn()
    {
        currentPhase.OnValueChanged -= HandlePhaseChanged;
        playerSlots.OnListChanged -= HandlePlayerSlotsChanged;

        if (IsServer && NetworkManager != null)
        {
            NetworkManager.OnClientConnectedCallback -= HandleClientConnected;
            NetworkManager.OnClientDisconnectCallback -= HandleClientDisconnected;
        }
    }

    public void StartMainMap()
    {
        ResolveReferences();

        if (!IsServer)
        {
            return;
        }

        if (!CanStartMainMap)
        {
            Debug.LogWarning($"Cannot leave lobby. {SessionRules.GetStartBlocker(ConnectedPlayerCount)}", this);
            return;
        }

        sessionProgressSync?.ResetProgressServer();
        dialogueFlowSync?.ResetSessionServer();
        dialogueFlowSync?.BeginOpeningTransitionServer();
        activeMiniGameIndex.Value = -1;
        currentPhase.Value = CoopGamePhase.WorldMap;
        LoadScene(mainMapSceneName);
    }

    public void StartMiniGame(int miniGameIndex)
    {
        ResolveReferences();

        if (!IsServer)
        {
            return;
        }

        if (miniGameIndex < 0 || miniGameIndex >= miniGameSceneNames.Length)
        {
            Debug.LogWarning($"Mini-game index {miniGameIndex} is out of range.", this);
            return;
        }

        if (!CanLaunchMiniGame(miniGameIndex))
        {
            Debug.LogWarning($"Mini-game index {miniGameIndex} has already been completed in this cooperative session.", this);
            return;
        }

        var sceneName = miniGameSceneNames[miniGameIndex];
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning($"Mini-game scene slot {miniGameIndex} is not configured.", this);
            return;
        }

        Debug.Log($"Starting mini-game index {miniGameIndex} -> scene '{sceneName}'.", this);
        activeMiniGameIndex.Value = miniGameIndex;
        currentPhase.Value = CoopGamePhase.MiniGame;
        LoadScene(sceneName);
    }

    public bool IsMiniGameConfigured(int miniGameIndex)
    {
        if (miniGameSceneNames == null || miniGameIndex < 0 || miniGameIndex >= miniGameSceneNames.Length)
        {
            return false;
        }

        return !string.IsNullOrWhiteSpace(miniGameSceneNames[miniGameIndex]);
    }

    public bool CanLaunchMiniGame(int miniGameIndex)
    {
        ResolveReferences();
        var isConfigured = IsMiniGameConfigured(miniGameIndex);
        var isAlreadyCompleted = sessionProgressSync != null && sessionProgressSync.IsMinigameCompleted(miniGameIndex);
        return CoopSessionFlowService.CanLaunchConfiguredMinigame(isConfigured, isAlreadyCompleted);
    }

    public bool TryGetMiniGameSceneName(int miniGameIndex, out string sceneName)
    {
        sceneName = string.Empty;
        if (miniGameSceneNames == null || miniGameIndex < 0 || miniGameIndex >= miniGameSceneNames.Length)
        {
            return false;
        }

        sceneName = miniGameSceneNames[miniGameIndex];
        return !string.IsNullOrWhiteSpace(sceneName);
    }

    public void ValidateMiniGameConfiguration(bool logWarnings)
    {
        if (!logWarnings || miniGameSceneNames == null)
        {
            return;
        }

        for (var index = 0; index < miniGameSceneNames.Length; index++)
        {
            if (string.IsNullOrWhiteSpace(miniGameSceneNames[index]))
            {
                Debug.LogWarning($"Mini-game scene slot {index} is empty in CoopSessionCoordinator.", this);
            }
        }
    }

    public void ReturnToMainMap()
    {
        if (!IsServer)
        {
            return;
        }

        activeMiniGameIndex.Value = -1;
        currentPhase.Value = CoopGamePhase.WorldMap;
        LoadScene(mainMapSceneName);
    }

    public void ContinueAfterMinigameResults()
    {
        ResolveReferences();

        if (!IsServer)
        {
            return;
        }

        activeMiniGameIndex.Value = -1;
        var nextPhase = CoopSessionFlowService.ResolvePostMinigamePhase(
            sessionProgressSync != null && sessionProgressSync.AreAllMinigamesCompleted);
        currentPhase.Value = nextPhase;

        if (nextPhase == CoopGamePhase.SessionSummary)
        {
            if (string.IsNullOrWhiteSpace(sessionSummarySceneName))
            {
                Debug.LogWarning("Session summary scene is not configured. Falling back to the main map.", this);
                currentPhase.Value = CoopGamePhase.WorldMap;
                LoadScene(mainMapSceneName);
                return;
            }

            LoadScene(sessionSummarySceneName);
            return;
        }

        LoadScene(mainMapSceneName);
    }

    public void RestartSessionToMainMap()
    {
        ResolveReferences();

        if (!IsServer)
        {
            return;
        }

        sessionProgressSync?.ResetProgressServer();
        dialogueFlowSync?.ResetSessionServer();
        dialogueFlowSync?.BeginOpeningTransitionServer();
        activeMiniGameIndex.Value = -1;
        currentPhase.Value = CoopGamePhase.WorldMap;
        LoadScene(mainMapSceneName);
    }

    public void ReturnToLobby()
    {
        ResolveReferences();

        if (!IsServer)
        {
            return;
        }

        sessionProgressSync?.ResetProgressServer();
        dialogueFlowSync?.ResetSessionServer();
        activeMiniGameIndex.Value = -1;
        currentPhase.Value = CoopGamePhase.Lobby;
        LoadScene(lobbySceneName);
    }

    public bool AreAllConfiguredMinigamesCompleted()
    {
        ResolveReferences();
        return sessionProgressSync != null && sessionProgressSync.AreAllMinigamesCompleted;
    }

    public string GetResultsContinueButtonLabel()
    {
        return AreAllConfiguredMinigamesCompleted() ? "Ver puntuacion final" : "Continuar";
    }

    public bool TryRegisterMiniGameResult(MinigameResultData result)
    {
        ResolveReferences();

        if (!IsServer || sessionProgressSync == null || activeMiniGameIndex.Value < 0)
        {
            return false;
        }

        return sessionProgressSync.TryRegisterResultServer(activeMiniGameIndex.Value, result);
    }

    public int GetLocalPlayerSlot()
    {
        if (NetworkManager == null)
        {
            return -1;
        }

        return GetPlayerSlot(NetworkManager.LocalClientId);
    }

    public int GetPlayerSlot(ulong clientId)
    {
        for (var index = 0; index < playerSlots.Count; index++)
        {
            if (playerSlots[index] == clientId)
            {
                return index;
            }
        }

        return -1;
    }

    public int GetLocalInformationChannel(int channelCount)
    {
        if (channelCount <= 0)
        {
            return -1;
        }

        var localSlot = GetLocalPlayerSlot();
        return localSlot < 0 ? -1 : localSlot % channelCount;
    }

    public bool TryGetPlayerClientIdAtSlot(int slotIndex, out ulong clientId)
    {
        if (slotIndex >= 0 && slotIndex < playerSlots.Count)
        {
            clientId = playerSlots[slotIndex];
            return true;
        }

        clientId = default;
        return false;
    }

    private void HandleClientConnected(ulong _)
    {
        RebuildPlayerSlots();
    }

    private void HandleClientDisconnected(ulong _)
    {
        RebuildPlayerSlots();
    }

    private void RebuildPlayerSlots()
    {
        if (!IsServer)
        {
            return;
        }

        playerSlots.Clear();

        if (NetworkManager == null)
        {
            return;
        }

        foreach (var clientId in NetworkManager.ConnectedClientsIds)
        {
            if (playerSlots.Count >= maxPlayers)
            {
                break;
            }

            playerSlots.Add(clientId);
        }
    }

    private void ResolveReferences()
    {
        relayConnectionService ??= FindFirstObjectByType<RelayConnectionService>(FindObjectsInactive.Include);
        sessionProgressSync ??= FindFirstObjectByType<CoopSessionProgressSync>(FindObjectsInactive.Include);
        dialogueFlowSync ??= GetComponent<DialogueFlowSync>();
        dialogueFlowSync ??= FindFirstObjectByType<DialogueFlowSync>(FindObjectsInactive.Include);
    }

    private void LoadScene(string sceneName)
    {
        if (NetworkManager == null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("A synchronized scene transition was requested without a valid scene name.", this);
            return;
        }

        var sceneLoadStatus = NetworkManager.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
        Debug.Log($"Loading co-op phase '{currentPhase.Value}' scene '{sceneName}' ({sceneLoadStatus}).", this);
    }

    private void HandlePhaseChanged(CoopGamePhase _, CoopGamePhase current)
    {
        PhaseChanged?.Invoke(current);
    }

    private void HandlePlayerSlotsChanged(NetworkListEvent<ulong> _)
    {
        SlotsChanged?.Invoke();
    }
}
