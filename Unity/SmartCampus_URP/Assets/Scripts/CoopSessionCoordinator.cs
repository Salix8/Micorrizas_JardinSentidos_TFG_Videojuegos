using System;
using SmartCampus.Coop;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class CoopSessionCoordinator : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private RelayConnectionService relayConnectionService;
    [SerializeField] private bool persistAcrossScenes = true;

    [Header("Co-op Rules")]
    [SerializeField] [Range(CoopSessionRules.DefaultMinimumPlayers, CoopSessionRules.DefaultMaximumPlayers)] private int minPlayersToStart = CoopSessionRules.DefaultMinimumPlayers;
    [SerializeField] [Range(CoopSessionRules.DefaultMinimumPlayers, CoopSessionRules.DefaultMaximumPlayers)] private int maxPlayers = CoopSessionRules.DefaultMaximumPlayers;

    [Header("Scene Flow")]
    [SerializeField] private string lobbySceneName = "Lobby";
    [SerializeField] private string mainMapSceneName = "UJI";
    [SerializeField] private string[] miniGameSceneNames = new string[5];

    private readonly NetworkVariable<CoopGamePhase> currentPhase = new(CoopGamePhase.Lobby);
    private readonly NetworkVariable<int> activeMiniGameIndex = new(-1);
    private readonly NetworkList<ulong> playerSlots = new();

    public CoopGamePhase CurrentPhase => currentPhase.Value;
    public int ActiveMiniGameIndex => activeMiniGameIndex.Value;
    public int MinimumPlayersToStart => minPlayersToStart;
    public int MaximumPlayers => maxPlayers;
    public CoopSessionRules SessionRules => new(minPlayersToStart, maxPlayers);
    public int ConnectedPlayerCount => NetworkManager == null ? 0 : NetworkManager.ConnectedClientsIds.Count;
    public int RegisteredPlayerCount => playerSlots.Count;
    public bool CanStartMainMap => IsServer && SessionRules.CanStart(ConnectedPlayerCount);

    public event Action<CoopGamePhase> PhaseChanged;
    public event Action SlotsChanged;

    private void Awake()
    {
        if (persistAcrossScenes)
        {
            DontDestroyOnLoad(gameObject);
        }
    }

    public override void OnNetworkSpawn()
    {
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
        if (!IsServer)
        {
            return;
        }

        if (!CanStartMainMap)
        {
            Debug.LogWarning($"Cannot leave lobby. {SessionRules.GetStartBlocker(ConnectedPlayerCount)}", this);
            return;
        }

        activeMiniGameIndex.Value = -1;
        currentPhase.Value = CoopGamePhase.WorldMap;
        LoadScene(mainMapSceneName);
    }

    public void StartMiniGame(int miniGameIndex)
    {
        if (!IsServer)
        {
            return;
        }

        if (miniGameIndex < 0 || miniGameIndex >= miniGameSceneNames.Length)
        {
            Debug.LogWarning($"Mini-game index {miniGameIndex} is out of range.", this);
            return;
        }

        var sceneName = miniGameSceneNames[miniGameIndex];
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning($"Mini-game scene slot {miniGameIndex} is not configured.", this);
            return;
        }

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

    public void ReturnToLobby()
    {
        if (!IsServer)
        {
            return;
        }

        activeMiniGameIndex.Value = -1;
        currentPhase.Value = CoopGamePhase.Lobby;
        LoadScene(lobbySceneName);
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
