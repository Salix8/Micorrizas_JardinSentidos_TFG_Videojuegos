using UnityEngine;
using TMPro;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class MultiplayerMenuController : MonoBehaviour
{
    private static readonly string[] RequiredLobbyButtonPaths =
    {
        "SafeAreaRoot/SurfacePanel/ScrollFrame/PanelScrollView/Viewport/Content/HomePanel/CreateSessionButton",
        "SafeAreaRoot/SurfacePanel/ScrollFrame/PanelScrollView/Viewport/Content/HomePanel/OpenJoinPanelButton",
        "SafeAreaRoot/SurfacePanel/ScrollFrame/PanelScrollView/Viewport/Content/JoinPanel/JoinSessionButton",
        "SafeAreaRoot/SurfacePanel/ScrollFrame/PanelScrollView/Viewport/Content/JoinPanel/BackButton",
        "SafeAreaRoot/SurfacePanel/ScrollFrame/PanelScrollView/Viewport/Content/SessionPanel/ActionsRoot/CopyJoinCodeButton",
        "SafeAreaRoot/SurfacePanel/ScrollFrame/PanelScrollView/Viewport/Content/SessionPanel/ActionsRoot/StartMatchButton",
        "SafeAreaRoot/SurfacePanel/ScrollFrame/PanelScrollView/Viewport/Content/SessionPanel/ActionsRoot/LeaveSessionButton"
    };

    [Header("References")]
    [SerializeField] private RelayConnectionService relayConnectionService;
    [SerializeField] private CoopSessionCoordinator coopSessionCoordinator;

    [Header("Panels")]
    [SerializeField] private GameObject homePanel;
    [SerializeField] private GameObject hostPanel;
    [SerializeField] private GameObject joinPanel;
    [SerializeField] private GameObject sessionPanel;

    [Header("UI")]
    [SerializeField] private TMP_InputField joinCodeInput;
    [SerializeField] private TMP_InputField teamNameInput;
    [SerializeField] private TMP_Text statusLabel;
    [SerializeField] private TMP_Text joinCodeLabel;
    [SerializeField] private TMP_Text playerCountLabel;
    [SerializeField] private TMP_Text sessionRequirementsLabel;
    [SerializeField] private Button hostButton;
    [SerializeField] private Button joinButton;
    [SerializeField] private Button startMatchButton;
    [SerializeField] private Button leaveSessionButton;
    [SerializeField] private Button copyJoinCodeButton;

    private bool suppressTeamNameInputCallback;

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
        ResolveReferences();

        if (!ValidateButtonWiring())
        {
            Debug.LogError($"{nameof(MultiplayerMenuController)} detected invalid lobby button wiring. Open the Lobby scene setup utility and repair the scene listeners.", this);
        }

        relayConnectionService.StatusChanged += HandleStatusChanged;
        relayConnectionService.JoinCodeChanged += HandleJoinCodeChanged;
        relayConnectionService.PlayerCountChanged += HandlePlayerCountChanged;

        if (teamNameInput != null)
        {
            teamNameInput.onValueChanged.AddListener(HandleTeamNameChanged);
        }

        if (coopSessionCoordinator != null)
        {
            coopSessionCoordinator.TeamNameChanged -= HandleSessionTeamNameChanged;
            coopSessionCoordinator.TeamNameChanged += HandleSessionTeamNameChanged;
        }

        ShowHomePanel();
        RefreshState();
    }

    private void OnDisable()
    {
        if (relayConnectionService != null)
        {
            relayConnectionService.StatusChanged -= HandleStatusChanged;
            relayConnectionService.JoinCodeChanged -= HandleJoinCodeChanged;
            relayConnectionService.PlayerCountChanged -= HandlePlayerCountChanged;
        }

        if (teamNameInput != null)
        {
            teamNameInput.onValueChanged.RemoveListener(HandleTeamNameChanged);
        }

        if (coopSessionCoordinator != null)
        {
            coopSessionCoordinator.TeamNameChanged -= HandleSessionTeamNameChanged;
        }
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

            ApplyTeamNameInputToSession();
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
        ApplyTeamNameInputToSession();

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
        RefreshTeamNameInput();
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

        if (teamNameInput != null)
        {
            teamNameInput.interactable = isHost && !relayConnectionService.IsBusy;
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

        var contentRoot = transform.Find("SafeAreaRoot/SurfacePanel/ScrollFrame/PanelScrollView/Viewport/Content");
        if (contentRoot == null)
        {
            return;
        }

        homePanel ??= contentRoot.Find("HomePanel")?.gameObject;
        hostPanel ??= contentRoot.Find("HostPanel")?.gameObject;
        joinPanel ??= contentRoot.Find("JoinPanel")?.gameObject;
        sessionPanel ??= contentRoot.Find("SessionPanel")?.gameObject;

        joinCodeInput ??= contentRoot.Find("JoinPanel/JoinCodeInput")?.GetComponent<TMP_InputField>();
        statusLabel ??= contentRoot.Find("SessionPanel/StatusLabel")?.GetComponent<TMP_Text>();
        joinCodeLabel ??= contentRoot.Find("SessionPanel/JoinCodeLabel")?.GetComponent<TMP_Text>();
        playerCountLabel ??= contentRoot.Find("SessionPanel/PlayerCountLabel")?.GetComponent<TMP_Text>();
        sessionRequirementsLabel ??= contentRoot.Find("SessionPanel/SessionRequirementsLabel")?.GetComponent<TMP_Text>();

        hostButton ??= contentRoot.Find("HomePanel/CreateSessionButton")?.GetComponent<Button>();
        joinButton ??= contentRoot.Find("JoinPanel/JoinSessionButton")?.GetComponent<Button>();
        startMatchButton ??= contentRoot.Find("SessionPanel/ActionsRoot/StartMatchButton")?.GetComponent<Button>();
        leaveSessionButton ??= contentRoot.Find("SessionPanel/ActionsRoot/LeaveSessionButton")?.GetComponent<Button>();
        copyJoinCodeButton ??= contentRoot.Find("SessionPanel/ActionsRoot/CopyJoinCodeButton")?.GetComponent<Button>();
        teamNameInput ??= contentRoot.Find("SessionPanel/TeamNameInput")?.GetComponent<TMP_InputField>();
    }

    private void HandleTeamNameChanged(string _)
    {
        if (suppressTeamNameInputCallback)
        {
            return;
        }

        ApplyTeamNameInputToSession();
    }

    private void HandleSessionTeamNameChanged(string _)
    {
        RefreshTeamNameInput();
    }

    private void RefreshTeamNameInput()
    {
        if (teamNameInput == null)
        {
            return;
        }

        ResolveReferences();
        if (coopSessionCoordinator == null)
        {
            return;
        }

        teamNameInput.characterLimit = coopSessionCoordinator.MaxTeamNameLength;
        var resolvedTeamName = coopSessionCoordinator.TeamName;
        if (string.Equals(teamNameInput.text, resolvedTeamName))
        {
            return;
        }

        suppressTeamNameInputCallback = true;
        teamNameInput.SetTextWithoutNotify(resolvedTeamName);
        suppressTeamNameInputCallback = false;
    }

    private void ApplyTeamNameInputToSession()
    {
        ResolveReferences();

        if (teamNameInput == null ||
            coopSessionCoordinator == null ||
            !coopSessionCoordinator.IsSpawned ||
            !coopSessionCoordinator.IsServer)
        {
            return;
        }

        coopSessionCoordinator.SetTeamNameServer(teamNameInput.text);
        RefreshTeamNameInput();
    }

    private bool ValidateButtonWiring()
    {
        var isValid = true;
        for (var index = 0; index < RequiredLobbyButtonPaths.Length; index++)
        {
            var buttonTransform = this.transform.Find(RequiredLobbyButtonPaths[index]);
            if (buttonTransform == null)
            {
                Debug.LogError($"Missing required lobby button at path '{RequiredLobbyButtonPaths[index]}'.", this);
                isValid = false;
                continue;
            }

            var button = buttonTransform.GetComponent<Button>();
            if (button == null)
            {
                Debug.LogError($"GameObject '{buttonTransform.name}' is missing a Button component.", buttonTransform);
                isValid = false;
                continue;
            }

            if (!HasValidPersistentListener(button))
            {
                Debug.LogError($"Lobby button '{buttonTransform.name}' does not have a valid persistent onClick target.", button);
                isValid = false;
            }
        }

        return isValid;
    }

    private static bool HasValidPersistentListener(Button button)
    {
        var persistentCallCount = button.onClick.GetPersistentEventCount();
        if (persistentCallCount == 0)
        {
            return false;
        }

        for (var index = 0; index < persistentCallCount; index++)
        {
            if (button.onClick.GetPersistentTarget(index) != null &&
                !string.IsNullOrWhiteSpace(button.onClick.GetPersistentMethodName(index)))
            {
                return true;
            }
        }

        return false;
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
