using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using SmartCampus.Coop;

[DisallowMultipleComponent]
public sealed class RelayConnectionService : MonoBehaviour
{
    [Header("Scene Setup")]
    [SerializeField] private NetworkManager networkManager;
    [SerializeField] private UnityTransport unityTransport;
    [SerializeField] private bool persistAcrossScenes = true;

    [Header("Relay")]
    [SerializeField] [Range(CoopSessionRules.DefaultMinimumPlayers, CoopSessionRules.DefaultMaximumPlayers)] private int minPlayersToStart = CoopSessionRules.DefaultMinimumPlayers;
    [SerializeField] [Range(CoopSessionRules.DefaultMinimumPlayers, CoopSessionRules.DefaultMaximumPlayers)] private int maxPlayers = CoopSessionRules.DefaultMaximumPlayers;
    [SerializeField] private RelayConnectionProtocol connectionProtocol = RelayConnectionProtocol.Dtls;

    [Header("Flow")]
    [FormerlySerializedAs("gameplaySceneName")]
    [SerializeField] private string mainMapSceneName = "UJI";
    [SerializeField] private bool autoLoadMainMapOnHostStart;

    private bool servicesInitialized;

    public string CurrentJoinCode { get; private set; } = string.Empty;
    public bool IsBusy { get; private set; }
    public int ConnectedPlayerCount => networkManager == null ? 0 : networkManager.ConnectedClientsIds.Count;
    public bool IsSessionActive => networkManager != null && networkManager.IsListening;
    public bool IsHost => networkManager != null && networkManager.IsHost;
    public int MinimumPlayersToStart => minPlayersToStart;
    public int MaximumPlayers => maxPlayers;
    public string MainMapSceneName => mainMapSceneName;
    public CoopSessionRules SessionRules => new(minPlayersToStart, maxPlayers);
    public bool CanStartMainMap => IsHost && SessionRules.CanStart(ConnectedPlayerCount);

    public event Action<string> StatusChanged;
    public event Action<string> JoinCodeChanged;
    public event Action<int> PlayerCountChanged;

    private void Awake()
    {
        var rules = SessionRules;
        minPlayersToStart = rules.MinimumPlayers;
        maxPlayers = rules.MaximumPlayers;

        networkManager ??= GetComponent<NetworkManager>();
        unityTransport ??= GetComponent<UnityTransport>();

        if (networkManager == null)
        {
            Debug.LogError($"{nameof(RelayConnectionService)} requires a {nameof(NetworkManager)} reference.", this);
            enabled = false;
            return;
        }

        unityTransport ??= networkManager.GetComponent<UnityTransport>();

        if (unityTransport == null)
        {
            Debug.LogError($"{nameof(RelayConnectionService)} requires a {nameof(UnityTransport)} reference.", this);
            enabled = false;
            return;
        }

        if (persistAcrossScenes)
        {
            PersistMultiplayerBootstrap();
        }
    }

    private void PersistMultiplayerBootstrap()
    {
        DontDestroyOnLoad(gameObject);

        if (networkManager != null)
        {
            DontDestroyOnLoad(networkManager.gameObject);
        }

        if (unityTransport != null &&
            networkManager != null &&
            unityTransport.gameObject != networkManager.gameObject)
        {
            DontDestroyOnLoad(unityTransport.gameObject);
        }
    }

    private void OnEnable()
    {
        if (networkManager == null)
        {
            return;
        }

        networkManager.OnClientConnectedCallback += HandleClientConnected;
        networkManager.OnClientDisconnectCallback += HandleClientDisconnected;
        networkManager.OnServerStarted += HandleServerStarted;
    }

    private void OnDisable()
    {
        if (networkManager == null)
        {
            return;
        }

        networkManager.OnClientConnectedCallback -= HandleClientConnected;
        networkManager.OnClientDisconnectCallback -= HandleClientDisconnected;
        networkManager.OnServerStarted -= HandleServerStarted;
    }

    public async Task<string> StartHostAsync()
    {
        if (IsBusy)
        {
            return CurrentJoinCode;
        }

        try
        {
            IsBusy = true;
            PublishStatus("Initializing Unity Services...");
            await EnsureServicesReadyAsync();

            ShutdownNetworkIfNeeded();

            PublishStatus("Creating Relay allocation...");
            var allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers - 1);

            unityTransport.UseWebSockets = connectionProtocol == RelayConnectionProtocol.Wss;
            unityTransport.SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, GetConnectionType()));

            CurrentJoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            JoinCodeChanged?.Invoke(CurrentJoinCode);

            if (!networkManager.StartHost())
            {
                throw new InvalidOperationException("NetworkManager could not start as host.");
            }

            PublishStatus($"Co-op lobby started. Share join code {CurrentJoinCode}. Waiting for {SessionRules.DescribeRequirements()}.");

            if (autoLoadMainMapOnHostStart)
            {
                LoadMainMapScene();
            }

            return CurrentJoinCode;
        }
        catch (Exception exception)
        {
            PublishStatus($"Host startup failed: {exception.Message}");
            Debug.LogException(exception, this);
            return string.Empty;
        }
        finally
        {
            IsBusy = false;
            NotifyPlayerCountChanged();
        }
    }

    public async Task<bool> JoinAsClientAsync(string joinCode)
    {
        if (IsBusy)
        {
            return false;
        }

        var sanitizedJoinCode = (joinCode ?? string.Empty).Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(sanitizedJoinCode))
        {
            PublishStatus("Enter a valid join code.");
            return false;
        }

        try
        {
            IsBusy = true;
            PublishStatus("Initializing Unity Services...");
            await EnsureServicesReadyAsync();

            ShutdownNetworkIfNeeded();

            PublishStatus($"Joining session {sanitizedJoinCode}...");
            var allocation = await RelayService.Instance.JoinAllocationAsync(sanitizedJoinCode);

            unityTransport.UseWebSockets = connectionProtocol == RelayConnectionProtocol.Wss;
            unityTransport.SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, GetConnectionType()));

            if (!networkManager.StartClient())
            {
                throw new InvalidOperationException("NetworkManager could not start as client.");
            }

            CurrentJoinCode = sanitizedJoinCode;
            JoinCodeChanged?.Invoke(CurrentJoinCode);
            PublishStatus("Joined the co-op lobby through Relay.");
            return true;
        }
        catch (Exception exception)
        {
            PublishStatus($"Join failed: {exception.Message}");
            Debug.LogException(exception, this);
            return false;
        }
        finally
        {
            IsBusy = false;
            NotifyPlayerCountChanged();
        }
    }

    public void LoadMainMapScene()
    {
        if (networkManager == null || !networkManager.IsHost)
        {
            PublishStatus("Only the host can move the group from the lobby into the main map.");
            return;
        }

        if (!CanStartMainMap)
        {
            PublishStatus(SessionRules.GetStartBlocker(ConnectedPlayerCount));
            return;
        }

        if (string.IsNullOrWhiteSpace(mainMapSceneName))
        {
            PublishStatus("Main map scene name is not configured.");
            return;
        }

        var sceneLoadStatus = networkManager.SceneManager.LoadScene(mainMapSceneName, LoadSceneMode.Single);
        PublishStatus($"Loading co-op main map '{mainMapSceneName}' ({sceneLoadStatus}).");
    }

    public void ShutdownSession()
    {
        ShutdownNetworkIfNeeded();
        CurrentJoinCode = string.Empty;
        JoinCodeChanged?.Invoke(CurrentJoinCode);
        PublishStatus("Session closed.");
        NotifyPlayerCountChanged();
    }

    private async Task EnsureServicesReadyAsync()
    {
        if (!servicesInitialized)
        {
            await UnityServices.InitializeAsync();
            servicesInitialized = true;
        }

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            PublishStatus($"Signed in anonymously as {AuthenticationService.Instance.PlayerId}.");
        }
    }

    private void ShutdownNetworkIfNeeded()
    {
        if (networkManager != null && networkManager.IsListening)
        {
            networkManager.Shutdown();
        }
    }

    private string GetConnectionType()
    {
        return connectionProtocol switch
        {
            RelayConnectionProtocol.Udp => "udp",
            RelayConnectionProtocol.Wss => "wss",
            _ => "dtls"
        };
    }

    private void HandleServerStarted()
    {
        NotifyPlayerCountChanged();
    }

    private void HandleClientConnected(ulong clientId)
    {
        if (networkManager != null &&
            networkManager.IsServer &&
            ConnectedPlayerCount > SessionRules.MaximumPlayers)
        {
            var disconnectReason = $"Lobby full. Maximum supported players: {SessionRules.MaximumPlayers}.";
            networkManager.DisconnectClient(clientId, disconnectReason);
            PublishStatus($"Rejected client {clientId}. {disconnectReason}");
            NotifyPlayerCountChanged();
            return;
        }

        PublishStatus($"Player connected. Lobby population: {ConnectedPlayerCount}/{SessionRules.MaximumPlayers}.");
        NotifyPlayerCountChanged();
    }

    private void HandleClientDisconnected(ulong _)
    {
        PublishStatus($"Player disconnected. Lobby population: {ConnectedPlayerCount}/{SessionRules.MaximumPlayers}.");
        NotifyPlayerCountChanged();
    }

    private void NotifyPlayerCountChanged()
    {
        PlayerCountChanged?.Invoke(ConnectedPlayerCount);
    }

    private void PublishStatus(string message)
    {
        StatusChanged?.Invoke(message);
        Debug.Log(message, this);
    }
}
