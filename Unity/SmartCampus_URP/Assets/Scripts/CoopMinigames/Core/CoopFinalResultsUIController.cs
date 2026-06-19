using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SmartCampus.Coop.Minigames
{
    [DisallowMultipleComponent]
    public sealed class CoopFinalResultsUIController : MonoBehaviour
    {
        [Header("Runtime Session References")]
        [SerializeField] private CoopSessionCoordinator coopSessionCoordinator;
        [SerializeField] private CoopSessionProgressSync coopSessionProgressSync;
        [SerializeField] private RelayConnectionService relayConnectionService;
        [SerializeField] private CoopPlayerProfileSync playerProfileSync;

        [Header("Scene UI References")]
        [SerializeField] private TMP_Text headerLabel;
        [SerializeField] private TMP_Text helperLabel;
        [SerializeField] private TMP_Text teamIdentityLabel;
        [SerializeField] private TMP_Text scoreCardTitleLabel;
        [SerializeField] private TMP_Text averageScoreLabel;
        [SerializeField] private TMP_Text membersTitleLabel;
        [SerializeField] private RectTransform membersContainer;
        [SerializeField] private Button restartButton;
        [SerializeField] private TMP_Text restartButtonLabel;
        [SerializeField] private Button exitButton;
        [SerializeField] private TMP_Text exitButtonLabel;
        [SerializeField] private TMP_Text waitingHostLabel;

        [Header("Labels")]
        [SerializeField] private string headerText = "Gracias por jugar";
        [SerializeField] private bool useSceneAuthoredHelperText = true;
        [SerializeField] [TextArea(2, 4)] private string helperText =
            "Habeis completado el recorrido cooperativo.\nEsta es la nota final del equipo.";
        [SerializeField] private string scoreCardTitleText = "Nota global del equipo";
        [SerializeField] private string averageTextFormat = "{0:0.0}/10";
        [SerializeField] private string teamName = "EQUIPO";
        [SerializeField] private string teamIdentityFormat = "{0} · SALA {1}";
        [SerializeField] private string membersTitleText = "AVENTUREROS DEL EQUIPO";
        [SerializeField] private string unavailableRoomCodeText = "SIN CÓDIGO";
        [SerializeField] private string emptyMembersText = "No hay aventureros conectados";
        [SerializeField] private string restartButtonText = "Reiniciar partida";
        [SerializeField] private string exitButtonText = "Salir del juego";
        [SerializeField] private string waitingHostText = "Esperando a que el host reinicie la partida.";

        [Header("Member Row Style")]
        [SerializeField] [Min(48f)] private float memberRowHeight = 76f;
        [SerializeField] [Min(32f)] private float memberAvatarSize = 54f;
        [SerializeField] [Min(14f)] private float memberNameFontSize = 27f;
        [SerializeField] [Min(0f)] private float memberRowCornerRadius = 24f;
        [SerializeField] [Min(0f)] private float memberRowBorderWidth = 1.5f;
        [SerializeField] private Color memberRowFillColor = new(0.98f, 0.975f, 0.93f, 0.96f);
        [SerializeField] private Color memberRowBorderColor = new(0.78f, 0.75f, 0.58f, 0.75f);
        [SerializeField] private Color memberNameColor = new(0.08f, 0.30f, 0.10f, 1f);

        private void Awake()
        {
            ResolveRuntimeReferences();
        }

        private void OnEnable()
        {
            ResolveRuntimeReferences();

            if (coopSessionProgressSync != null)
            {
                coopSessionProgressSync.ProgressChanged += HandleProgressChanged;
            }

            if (playerProfileSync != null)
            {
                playerProfileSync.ProfilesChanged += HandleProfilesChanged;
            }

            if (relayConnectionService != null)
            {
                relayConnectionService.JoinCodeChanged += HandleJoinCodeChanged;
            }

            if (restartButton != null)
            {
                restartButton.onClick.RemoveListener(HandleRestartClicked);
                restartButton.onClick.AddListener(HandleRestartClicked);
            }

            if (exitButton != null)
            {
                exitButton.onClick.RemoveListener(HandleExitClicked);
                exitButton.onClick.AddListener(HandleExitClicked);
            }

            RefreshView();
        }

        private void OnDisable()
        {
            if (coopSessionProgressSync != null)
            {
                coopSessionProgressSync.ProgressChanged -= HandleProgressChanged;
            }

            if (playerProfileSync != null)
            {
                playerProfileSync.ProfilesChanged -= HandleProfilesChanged;
            }

            if (relayConnectionService != null)
            {
                relayConnectionService.JoinCodeChanged -= HandleJoinCodeChanged;
            }

            if (restartButton != null)
            {
                restartButton.onClick.RemoveListener(HandleRestartClicked);
            }

            if (exitButton != null)
            {
                exitButton.onClick.RemoveListener(HandleExitClicked);
            }
        }

        private void HandleProgressChanged()
        {
            ResolveRuntimeReferences();
            RefreshView();
        }

        private void HandleProfilesChanged()
        {
            RefreshMembers();
        }

        private void HandleJoinCodeChanged(string _)
        {
            RefreshTeamIdentity();
        }

        private void HandleRestartClicked()
        {
            ResolveRuntimeReferences();
            coopSessionCoordinator?.RestartSessionToMainMap();
        }

        private void HandleExitClicked()
        {
            ResolveRuntimeReferences();
            relayConnectionService?.ShutdownSession();

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void ResolveRuntimeReferences()
        {
            // These systems live in the persistent co-op bootstrap and are not authored inside the summary scene asset.
            coopSessionCoordinator ??= FindFirstObjectByType<CoopSessionCoordinator>(FindObjectsInactive.Include);
            coopSessionProgressSync ??= coopSessionCoordinator != null
                ? coopSessionCoordinator.SessionProgressSync
                : FindFirstObjectByType<CoopSessionProgressSync>(FindObjectsInactive.Include);
            relayConnectionService ??= FindFirstObjectByType<RelayConnectionService>(FindObjectsInactive.Include);
            playerProfileSync ??= FindFirstObjectByType<CoopPlayerProfileSync>(FindObjectsInactive.Include);
        }

        private void RefreshView()
        {
            ResolveRuntimeReferences();

            if (headerLabel != null)
            {
                headerLabel.text = headerText;
            }

            if (!useSceneAuthoredHelperText && helperLabel != null)
            {
                helperLabel.text = helperText;
            }

            if (scoreCardTitleLabel != null)
            {
                scoreCardTitleLabel.text = scoreCardTitleText;
            }

            RefreshTeamIdentity();

            if (averageScoreLabel != null)
            {
                var averageScore = coopSessionProgressSync == null ? 0f : coopSessionProgressSync.AverageScoreOutOfTen;
                averageScoreLabel.text = string.Format(averageTextFormat, averageScore);
            }

            if (membersTitleLabel != null)
            {
                membersTitleLabel.text = membersTitleText;
            }

            RefreshMembers();

            var canRestart = coopSessionCoordinator != null &&
                             coopSessionCoordinator.IsSpawned &&
                             coopSessionCoordinator.IsServer &&
                             coopSessionProgressSync != null &&
                             coopSessionProgressSync.AreAllMinigamesCompleted;

            if (restartButtonLabel != null)
            {
                restartButtonLabel.text = restartButtonText;
            }

            if (exitButtonLabel != null)
            {
                exitButtonLabel.text = exitButtonText;
            }

            if (restartButton != null)
            {
                restartButton.gameObject.SetActive(canRestart);
                restartButton.interactable = canRestart;
            }

            if (exitButton != null)
            {
                exitButton.gameObject.SetActive(true);
                exitButton.interactable = true;
            }

            if (waitingHostLabel != null)
            {
                waitingHostLabel.gameObject.SetActive(!canRestart);
                waitingHostLabel.text = waitingHostText;
            }
        }

        private void RefreshTeamIdentity()
        {
            if (teamIdentityLabel == null)
            {
                return;
            }

            var resolvedTeamName = string.IsNullOrWhiteSpace(teamName) ? "EQUIPO" : teamName.Trim();
            var roomCode = relayConnectionService == null || string.IsNullOrWhiteSpace(relayConnectionService.CurrentJoinCode)
                ? unavailableRoomCodeText
                : relayConnectionService.CurrentJoinCode.Trim().ToUpperInvariant();
            teamIdentityLabel.text = string.Format(teamIdentityFormat, resolvedTeamName, roomCode);
        }

        private void RefreshMembers()
        {
            if (membersContainer == null)
            {
                return;
            }

            ClearMembers();
            if (playerProfileSync == null || playerProfileSync.PlayerProfiles.Count == 0)
            {
                CreateMemberRow(emptyMembersText, null);
                return;
            }

            var profiles = new List<CoopPlayerProfileNetworkState>();
            for (var index = 0; index < playerProfileSync.PlayerProfiles.Count; index++)
            {
                profiles.Add(playerProfileSync.PlayerProfiles[index]);
            }

            profiles.Sort((left, right) =>
            {
                var leftSlot = coopSessionCoordinator == null ? int.MaxValue : coopSessionCoordinator.GetPlayerSlot(left.ClientId);
                var rightSlot = coopSessionCoordinator == null ? int.MaxValue : coopSessionCoordinator.GetPlayerSlot(right.ClientId);
                return leftSlot.CompareTo(rightSlot);
            });

            foreach (var profile in profiles)
            {
                Sprite avatarSprite = null;
                var catalog = playerProfileSync.AppearanceCatalog;
                if (catalog != null &&
                    catalog.TryGetAvatar(profile.AvatarId.ToString(), out var avatarDefinition) &&
                    avatarDefinition != null)
                {
                    avatarSprite = avatarDefinition.AvatarSprite;
                }

                CreateMemberRow(profile.DisplayName.ToString(), avatarSprite);
            }
        }

        private void ClearMembers()
        {
            for (var index = membersContainer.childCount - 1; index >= 0; index--)
            {
                var child = membersContainer.GetChild(index).gameObject;
                child.SetActive(false);
                Destroy(child);
            }
        }

        private void CreateMemberRow(string displayName, Sprite avatarSprite)
        {
            var rowObject = new GameObject(
                "PlayerRow",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(RoundedPanelGraphic),
                typeof(HorizontalLayoutGroup),
                typeof(LayoutElement),
                typeof(CoopFinalResultsPlayerRowView));
            rowObject.transform.SetParent(membersContainer, false);

            var rowBackground = rowObject.GetComponent<RoundedPanelGraphic>();
            rowBackground.Configure(
                memberRowFillColor,
                memberRowBorderColor,
                memberRowCornerRadius,
                memberRowBorderWidth);
            rowBackground.raycastTarget = false;

            var rowLayout = rowObject.GetComponent<HorizontalLayoutGroup>();
            rowLayout.padding = new RectOffset(18, 18, 10, 10);
            rowLayout.spacing = 18f;
            rowLayout.childAlignment = TextAnchor.MiddleLeft;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = false;

            var rowSize = rowObject.GetComponent<LayoutElement>();
            rowSize.preferredHeight = memberRowHeight;
            rowSize.flexibleWidth = 1f;

            var avatarObject = new GameObject("Avatar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement));
            avatarObject.transform.SetParent(rowObject.transform, false);
            var avatarImage = avatarObject.GetComponent<Image>();
            avatarImage.color = Color.white;
            avatarImage.raycastTarget = false;
            var avatarSize = avatarObject.GetComponent<LayoutElement>();
            avatarSize.preferredWidth = memberAvatarSize;
            avatarSize.preferredHeight = memberAvatarSize;

            var nameObject = new GameObject("PlayerName", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI), typeof(LayoutElement));
            nameObject.transform.SetParent(rowObject.transform, false);
            var nameLabel = nameObject.GetComponent<TextMeshProUGUI>();
            nameLabel.fontSize = memberNameFontSize;
            nameLabel.fontStyle = FontStyles.Bold;
            nameLabel.color = memberNameColor;
            nameLabel.alignment = TextAlignmentOptions.MidlineLeft;
            nameLabel.textWrappingMode = TextWrappingModes.NoWrap;
            nameLabel.raycastTarget = false;
            var nameSize = nameObject.GetComponent<LayoutElement>();
            nameSize.flexibleWidth = 1f;
            nameSize.preferredHeight = memberAvatarSize;

            var rowView = rowObject.GetComponent<CoopFinalResultsPlayerRowView>();
            rowView.Initialize(avatarImage, nameLabel);
            rowView.Bind(displayName, avatarSprite);
        }
    }
}
