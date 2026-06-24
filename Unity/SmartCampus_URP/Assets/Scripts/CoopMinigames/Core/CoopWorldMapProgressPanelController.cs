using UnityEngine;
using TMPro;
using System.Collections.Generic;

namespace SmartCampus.Coop.Minigames
{
    [DisallowMultipleComponent]
    public sealed class CoopWorldMapProgressPanelController : MonoBehaviour
    {
        [SerializeField] private CoopMinigameTopPanelView topPanelView;
        [SerializeField] private CoopSessionProgressSync sessionProgressSync;
        [SerializeField] private CoopSessionCoordinator sessionCoordinator;
        [SerializeField] private RelayConnectionService relayConnectionService;
        [SerializeField] private string mapTitle = "MAPA DE QUESTS";

        [Header("Layout")]
        [SerializeField] private RectTransform panelRectTransform;
        [SerializeField] private Vector2 panelAnchorMin = new(0.5f, 1f);
        [SerializeField] private Vector2 panelAnchorMax = new(0.5f, 1f);
        [SerializeField] private Vector2 panelPivot = new(0.5f, 1f);
        [SerializeField] private Vector2 panelAnchoredPosition = new(0f, -24f);
        [SerializeField] private Vector2 panelSize = new(560f, 150f);
        [SerializeField] private List<GameObject> hiddenElements = new();

        [Header("Progress Label Backgrounds")]
        [SerializeField] private Color progressLabelBackgroundColor = new(0.96f, 0.93f, 0.83f, 0.92f);
        [SerializeField] private Color progressLabelBorderColor = new(0.76f, 0.70f, 0.52f, 0.85f);
        [SerializeField] private float progressLabelCornerRadius = 12f;
        [SerializeField] private float progressLabelBorderWidth = 1.5f;
        [SerializeField] private List<ProgressLabelBackgroundBinding> progressLabelBackgrounds = new();

        private void Awake()
        {
            ResolveReferences();
            ApplySceneLayout();
            RefreshPanel();
        }

        private void OnEnable()
        {
            ResolveReferences();
            ApplySceneLayout();
            Subscribe();
            RefreshPanel();
        }

        private void OnValidate()
        {
            ApplySceneLayout();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        public void RefreshPanel()
        {
            ResolveReferences();
            if (topPanelView == null)
            {
                return;
            }

            ApplySceneLayout();
            topPanelView.Bind(
                mapTitle,
                CalculateProgress01(),
                sessionCoordinator == null ? string.Empty : sessionCoordinator.TeamName,
                relayConnectionService == null ? string.Empty : relayConnectionService.CurrentJoinCode);
        }

        private float CalculateProgress01()
        {
            if (sessionProgressSync == null || sessionProgressSync.ConfiguredMinigameCount <= 0)
            {
                return 0f;
            }

            return Mathf.Clamp01((float)sessionProgressSync.CompletedCount / sessionProgressSync.ConfiguredMinigameCount);
        }

        private void ResolveReferences()
        {
            topPanelView ??= GetComponentInChildren<CoopMinigameTopPanelView>(true);
            panelRectTransform ??= topPanelView == null ? null : topPanelView.GetComponent<RectTransform>();
            sessionCoordinator ??= FindFirstObjectByType<CoopSessionCoordinator>(FindObjectsInactive.Include);
            sessionProgressSync ??= sessionCoordinator != null
                ? sessionCoordinator.SessionProgressSync
                : FindFirstObjectByType<CoopSessionProgressSync>(FindObjectsInactive.Include);
            relayConnectionService ??= FindFirstObjectByType<RelayConnectionService>(FindObjectsInactive.Include);
        }

        private void ApplySceneLayout()
        {
            if (panelRectTransform != null)
            {
                panelRectTransform.anchorMin = panelAnchorMin;
                panelRectTransform.anchorMax = panelAnchorMax;
                panelRectTransform.pivot = panelPivot;
                panelRectTransform.anchoredPosition = panelAnchoredPosition;
                panelRectTransform.sizeDelta = panelSize;
                panelRectTransform.localScale = Vector3.one;
            }

            foreach (var element in hiddenElements)
            {
                if (element != null)
                {
                    element.SetActive(false);
                }
            }

            foreach (var binding in progressLabelBackgrounds)
            {
                if (binding == null || binding.Label == null || binding.Background == null)
                {
                    continue;
                }

                var labelRect = binding.Label.rectTransform;
                var backgroundRect = binding.Background.rectTransform;
                if (labelRect != null && backgroundRect != null)
                {
                    backgroundRect.SetParent(labelRect.parent, false);
                    backgroundRect.anchorMin = labelRect.anchorMin;
                    backgroundRect.anchorMax = labelRect.anchorMax;
                    backgroundRect.pivot = labelRect.pivot;
                    backgroundRect.anchoredPosition = labelRect.anchoredPosition;
                    backgroundRect.sizeDelta = labelRect.sizeDelta;
                    backgroundRect.localScale = Vector3.one;
                    backgroundRect.SetSiblingIndex(Mathf.Max(0, labelRect.GetSiblingIndex() - 1));
                }

                binding.Background.Configure(
                    progressLabelBackgroundColor,
                    progressLabelBorderColor,
                    progressLabelCornerRadius,
                    progressLabelBorderWidth);
            }
        }

        private void Subscribe()
        {
            if (sessionProgressSync != null)
            {
                sessionProgressSync.ProgressChanged -= HandleProgressChanged;
                sessionProgressSync.ProgressChanged += HandleProgressChanged;
            }

            if (sessionCoordinator != null)
            {
                sessionCoordinator.TeamNameChanged -= HandleTeamNameChanged;
                sessionCoordinator.TeamNameChanged += HandleTeamNameChanged;
            }

            if (relayConnectionService != null)
            {
                relayConnectionService.JoinCodeChanged -= HandleJoinCodeChanged;
                relayConnectionService.JoinCodeChanged += HandleJoinCodeChanged;
            }
        }

        private void Unsubscribe()
        {
            if (sessionProgressSync != null)
            {
                sessionProgressSync.ProgressChanged -= HandleProgressChanged;
            }

            if (sessionCoordinator != null)
            {
                sessionCoordinator.TeamNameChanged -= HandleTeamNameChanged;
            }

            if (relayConnectionService != null)
            {
                relayConnectionService.JoinCodeChanged -= HandleJoinCodeChanged;
            }
        }

        private void HandleProgressChanged()
        {
            RefreshPanel();
        }

        private void HandleTeamNameChanged(string _)
        {
            RefreshPanel();
        }

        private void HandleJoinCodeChanged(string _)
        {
            RefreshPanel();
        }

        [System.Serializable]
        private sealed class ProgressLabelBackgroundBinding
        {
            [SerializeField] private TMP_Text label;
            [SerializeField] private RoundedPanelGraphic background;

            public TMP_Text Label => label;
            public RoundedPanelGraphic Background => background;
        }
    }
}
