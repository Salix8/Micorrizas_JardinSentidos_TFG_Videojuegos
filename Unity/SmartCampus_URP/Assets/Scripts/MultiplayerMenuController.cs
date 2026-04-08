using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class MultiplayerMenuController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RelayConnectionService relayConnectionService;
    [SerializeField] private CoopSessionCoordinator coopSessionCoordinator;

    [Header("Panels")]
    [SerializeField] private GameObject homePanel;
    [SerializeField] private GameObject hostPanel;
    [SerializeField] private GameObject joinPanel;
    [SerializeField] private GameObject sessionPanel;

    [Header("UI")]
    [SerializeField] private InputField joinCodeInput;
    [SerializeField] private Text statusLabel;
    [SerializeField] private Text joinCodeLabel;
    [SerializeField] private Text playerCountLabel;
    [SerializeField] private Text sessionRequirementsLabel;
    [SerializeField] private Button hostButton;
    [SerializeField] private Button joinButton;
    [SerializeField] private Button startMatchButton;
    [SerializeField] private Button leaveSessionButton;
    [SerializeField] private Button copyJoinCodeButton;

    private void Awake()
    {
        ResolveReferences();

        if (relayConnectionService == null)
        {
            Debug.LogError($"{nameof(MultiplayerMenuController)} requires a {nameof(RelayConnectionService)} reference.", this);
            enabled = false;
        }
    }

    private void OnEnable()
    {
        relayConnectionService.StatusChanged += HandleStatusChanged;
        relayConnectionService.JoinCodeChanged += HandleJoinCodeChanged;
        relayConnectionService.PlayerCountChanged += HandlePlayerCountChanged;

        ShowHomePanel();
        RefreshState();
    }

    private void OnDisable()
    {
        if (relayConnectionService == null)
        {
            return;
        }

        relayConnectionService.StatusChanged -= HandleStatusChanged;
        relayConnectionService.JoinCodeChanged -= HandleJoinCodeChanged;
        relayConnectionService.PlayerCountChanged -= HandlePlayerCountChanged;
    }

    public void ShowHomePanel()
    {
        SetActivePanel(homePanel);
    }

    public void ShowHostPanel()
    {
        SetActivePanel(hostPanel);
    }

    public void ShowJoinPanel()
    {
        SetActivePanel(joinPanel);
    }

    public void ShowSessionPanel()
    {
        SetActivePanel(sessionPanel);
    }

    public async void HostSession()
    {
        SetInteractable(false);

        try
        {
            ShowSessionPanel();
            var joinCode = await relayConnectionService.StartHostAsync();
            if (string.IsNullOrWhiteSpace(joinCode) && !relayConnectionService.IsSessionActive)
            {
                relayConnectionService.ShutdownSession();
                ShowHomePanel();
            }
        }
        finally
        {
            RefreshState();
        }
    }

    public async void JoinSession()
    {
        SetInteractable(false);

        try
        {
            ShowSessionPanel();
            var joined = await relayConnectionService.JoinAsClientAsync(joinCodeInput != null ? joinCodeInput.text : string.Empty);
            if (!joined && !relayConnectionService.IsSessionActive)
            {
                relayConnectionService.ShutdownSession();
                ShowJoinPanel();
                FocusJoinCodeInput();
            }
        }
        finally
        {
            RefreshState();
        }
    }

    public void StartMatch()
    {
        ResolveReferences();

        if (coopSessionCoordinator != null && coopSessionCoordinator.IsSpawned && coopSessionCoordinator.IsServer)
        {
            coopSessionCoordinator.StartMainMap();
        }
        else
        {
            relayConnectionService.LoadMainMapScene();
        }

        RefreshState();
    }

    public void LeaveSession()
    {
        relayConnectionService.ShutdownSession();
        ShowHomePanel();
        RefreshState();
    }

    public void CopyJoinCode()
    {
        if (string.IsNullOrWhiteSpace(relayConnectionService.CurrentJoinCode))
        {
            return;
        }

        GUIUtility.systemCopyBuffer = relayConnectionService.CurrentJoinCode;
        HandleStatusChanged($"Join code {relayConnectionService.CurrentJoinCode} copied to clipboard.");
    }

    private void RefreshState()
    {
        HandleJoinCodeChanged(relayConnectionService.CurrentJoinCode);
        HandlePlayerCountChanged(relayConnectionService.ConnectedPlayerCount);
        RefreshButtonStates();
    }

    private void RefreshButtonStates()
    {
        var isHost = relayConnectionService.IsHost;

        if (startMatchButton != null)
        {
            startMatchButton.gameObject.SetActive(isHost);
            startMatchButton.interactable = isHost &&
                                            relayConnectionService.CanStartMainMap &&
                                            !relayConnectionService.IsBusy;
        }

        if (copyJoinCodeButton != null)
        {
            copyJoinCodeButton.interactable = !string.IsNullOrWhiteSpace(relayConnectionService.CurrentJoinCode);
        }

        if (leaveSessionButton != null)
        {
            leaveSessionButton.interactable = relayConnectionService.IsSessionActive || !string.IsNullOrWhiteSpace(relayConnectionService.CurrentJoinCode);
        }

        if (hostButton != null)
        {
            hostButton.interactable = !relayConnectionService.IsBusy;
        }

        if (joinButton != null)
        {
            joinButton.interactable = !relayConnectionService.IsBusy;
        }

        if (sessionRequirementsLabel != null)
        {
            sessionRequirementsLabel.text =
                $"Lobby rule: {relayConnectionService.MinimumPlayersToStart}-{relayConnectionService.MaximumPlayers} players";
        }
    }

    private void SetActivePanel(GameObject targetPanel)
    {
        SetPanelVisibility(homePanel, targetPanel == homePanel);
        SetPanelVisibility(hostPanel, targetPanel == hostPanel);
        SetPanelVisibility(joinPanel, targetPanel == joinPanel);
        SetPanelVisibility(sessionPanel, targetPanel == sessionPanel);
    }

    private void SetPanelVisibility(GameObject panel, bool visible)
    {
        if (panel != null)
        {
            panel.SetActive(visible);
        }
    }

    private void SetInteractable(bool interactable)
    {
        if (hostButton != null)
        {
            hostButton.interactable = interactable;
        }

        if (joinButton != null)
        {
            joinButton.interactable = interactable;
        }

        if (startMatchButton != null)
        {
            startMatchButton.interactable = interactable;
        }

        if (leaveSessionButton != null)
        {
            leaveSessionButton.interactable = interactable;
        }

        if (copyJoinCodeButton != null)
        {
            copyJoinCodeButton.interactable = interactable;
        }
    }

    private void HandleStatusChanged(string message)
    {
        if (statusLabel != null)
        {
            statusLabel.text = message;
        }
    }

    private void HandleJoinCodeChanged(string joinCode)
    {
        if (joinCodeLabel != null)
        {
            joinCodeLabel.text = string.IsNullOrWhiteSpace(joinCode)
                ? "Join code: -"
                : $"Join code: {joinCode}";
        }

        if (copyJoinCodeButton != null)
        {
            copyJoinCodeButton.interactable = !string.IsNullOrWhiteSpace(joinCode);
        }
    }

    private void HandlePlayerCountChanged(int playerCount)
    {
        if (playerCountLabel != null)
        {
            playerCountLabel.text =
                $"Players: {playerCount}/{relayConnectionService.MaximumPlayers} (minimum {relayConnectionService.MinimumPlayersToStart})";
        }

        RefreshButtonStates();
    }

    private void ResolveReferences()
    {
        relayConnectionService ??= FindFirstObjectByType<RelayConnectionService>();
        coopSessionCoordinator ??= FindFirstObjectByType<CoopSessionCoordinator>(FindObjectsInactive.Include);
    }

    private void FocusJoinCodeInput()
    {
        if (joinCodeInput == null)
        {
            return;
        }

        joinCodeInput.ActivateInputField();
        joinCodeInput.Select();
    }
}
