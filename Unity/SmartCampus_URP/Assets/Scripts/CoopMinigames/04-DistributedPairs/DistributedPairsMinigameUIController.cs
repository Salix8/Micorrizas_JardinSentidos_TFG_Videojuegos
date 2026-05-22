using UnityEngine;
using TMPro;
using UnityEngine.UI;
using SmartCampus.Coop.Minigames;

namespace SmartCampus.Coop.Minigames.DistributedPairs
{
    [DisallowMultipleComponent]
    public sealed class DistributedPairsMinigameUIController : MinigameUIControllerBase
    {
        [SerializeField] private DistributedPairsMinigameSession distributedPairsMinigameSession;
        [SerializeField] private DistributedPairsHandView localHandView;
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text progressLabel;
        [SerializeField] private TMP_Text sharedStatusLabel;
        [SerializeField] private TMP_Text localSelectionLabel;
        [SerializeField] private bool showRuntimePileHud;
        [SerializeField] private RectTransform drawPileAnchor;
        [SerializeField] private TMP_Text drawPileCountLabel;
        [SerializeField] private TMP_Text discardPileCountLabel;
        [SerializeField] private Button mismatchResetButton;
        [SerializeField] private TMP_Text mismatchResetLabel;

        private DistributedPairsMinigameSession TypedSession => distributedPairsMinigameSession != null
            ? distributedPairsMinigameSession
            : Session as DistributedPairsMinigameSession;

        protected override void Awake()
        {
            distributedPairsMinigameSession ??= FindFirstObjectByType<DistributedPairsMinigameSession>(FindObjectsInactive.Include);
            EnsureRuntimeHud();
            base.Awake();
        }

        protected override void OnEnable()
        {
            distributedPairsMinigameSession ??= FindFirstObjectByType<DistributedPairsMinigameSession>(FindObjectsInactive.Include);
            if (TypedSession != null)
            {
                TypedSession.StateChanged += HandleStateChanged;
            }

            EnsureRuntimeHud();
            BindRuntimeHud();
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            if (TypedSession != null)
            {
                TypedSession.StateChanged -= HandleStateChanged;
            }

            if (mismatchResetButton != null)
            {
                mismatchResetButton.onClick.RemoveListener(HandleMismatchResetRequested);
            }

            base.OnDisable();
        }

        protected override string BuildWaitingMessage()
        {
            if (TypedSession == null || !TypedSession.HasLocalTutorialBeenDismissed)
            {
                return base.BuildWaitingMessage();
            }

            return $"Esperando a que el resto cierre el tutorial: {TypedSession.TutorialDismissedCount}/{TypedSession.ParticipantCount}";
        }

        protected override void RefreshGameplay()
        {
            if (TypedSession == null)
            {
                return;
            }

            var config = TypedSession.MinigameConfig as DistributedPairsMinigameConfig;
            if (config == null)
            {
                return;
            }

            if (titleLabel != null)
            {
                titleLabel.text = config.DisplayName;
            }

            if (progressLabel != null)
            {
                progressLabel.text = $"Parejas: {TypedSession.MatchedPairCount}/{TypedSession.TotalPairCount}   Errores: {TypedSession.FailedAttemptCount}";
            }

            if (sharedStatusLabel != null)
            {
                sharedStatusLabel.text = TypedSession.SharedStatusMessage;
            }

            if (localSelectionLabel != null)
            {
                var localSelectedCard = TypedSession.GetLocalSelectedCard();
                if (TypedSession.HasPendingMismatch)
                {
                    localSelectionLabel.text = "El intento no coincide. Toca la pantalla para girar ambas cartas y seguir jugando.";
                }
                else
                {
                    localSelectionLabel.text = localSelectedCard.HasValue
                        ? $"Carta activa: {config.GetPairDefinition(localSelectedCard.Value.PairId)?.Title}"
                        : "Solo puedes tener una carta activa. Espera a que otro dispositivo revele la segunda carta.";
                }
            }

            if (localHandView != null)
            {
                localHandView.SetDrawPileAnchor(drawPileAnchor);
                localHandView.Render(
                    TypedSession.GetLocalHandStates(),
                    config,
                    TypedSession.Stage == CooperativeMinigameStage.Playing && !TypedSession.HasPendingMismatch,
                    TypedSession.Stage == CooperativeMinigameStage.Playing && TypedSession.HasPendingMismatch,
                    TypedSession.TryToggleLocalCardSelection);
            }

            if (drawPileCountLabel != null)
            {
                drawPileCountLabel.text = TypedSession.GetDrawPileCount().ToString();
            }

            if (discardPileCountLabel != null)
            {
                discardPileCountLabel.text = TypedSession.GetDiscardPileCount().ToString();
            }

            if (mismatchResetButton != null)
            {
                mismatchResetButton.gameObject.SetActive(TypedSession.Stage == CooperativeMinigameStage.Playing && TypedSession.HasPendingMismatch);
            }
        }

        private void HandleStateChanged()
        {
            RefreshGameplay();
        }

        private void HandleMismatchResetRequested()
        {
            TypedSession?.TryAcknowledgePendingMismatch();
        }

        private void BindRuntimeHud()
        {
            ApplyMismatchOverlayPresentation();

            if (mismatchResetButton != null)
            {
                mismatchResetButton.onClick.RemoveListener(HandleMismatchResetRequested);
                mismatchResetButton.onClick.AddListener(HandleMismatchResetRequested);
            }
        }

        private void EnsureRuntimeHud()
        {
            var hasPileHudReferences = drawPileCountLabel != null && discardPileCountLabel != null && drawPileAnchor != null;
            if ((!showRuntimePileHud || hasPileHudReferences) && mismatchResetButton != null)
            {
                return;
            }

            var rootRect = transform as RectTransform;
            if (rootRect == null)
            {
                return;
            }

            if (!showRuntimePileHud)
            {
                drawPileAnchor = null;
                drawPileCountLabel = null;
                discardPileCountLabel = null;
            }
            else if (drawPileCountLabel == null || discardPileCountLabel == null || drawPileAnchor == null)
            {
                var pileHudRoot = CreateUiObject("RuntimePileHud", rootRect, typeof(Image));
                var pileHudRect = pileHudRoot.GetComponent<RectTransform>();
                pileHudRect.anchorMin = new Vector2(1f, 1f);
                pileHudRect.anchorMax = new Vector2(1f, 1f);
                pileHudRect.pivot = new Vector2(1f, 1f);
                pileHudRect.anchoredPosition = new Vector2(-40f, -44f);
                pileHudRect.sizeDelta = new Vector2(260f, 150f);

                var pileHudImage = pileHudRoot.GetComponent<Image>();
                pileHudImage.color = new Color(0.12f, 0.17f, 0.21f, 0.18f);
                pileHudImage.raycastTarget = false;

                var deckPanel = CreatePilePanel("DeckPanel", pileHudRoot.transform, "Mazo");
                var deckRect = deckPanel.GetComponent<RectTransform>();
                deckRect.anchorMin = new Vector2(0f, 0f);
                deckRect.anchorMax = new Vector2(0.48f, 1f);
                deckRect.offsetMin = new Vector2(0f, 0f);
                deckRect.offsetMax = new Vector2(-8f, 0f);

                var discardPanel = CreatePilePanel("DiscardPanel", pileHudRoot.transform, "Descartes");
                var discardRect = discardPanel.GetComponent<RectTransform>();
                discardRect.anchorMin = new Vector2(0.52f, 0f);
                discardRect.anchorMax = new Vector2(1f, 1f);
                discardRect.offsetMin = new Vector2(8f, 0f);
                discardRect.offsetMax = new Vector2(0f, 0f);

                drawPileAnchor = deckPanel.transform.Find("CardAnchor") as RectTransform;
                drawPileCountLabel = deckPanel.transform.Find("CountLabel")?.GetComponent<TMP_Text>();
                discardPileCountLabel = discardPanel.transform.Find("CountLabel")?.GetComponent<TMP_Text>();
            }

            if (mismatchResetButton == null)
            {
                var overlay = CreateUiObject("MismatchResetOverlay", rootRect, typeof(Image), typeof(Button));
                var overlayRect = overlay.GetComponent<RectTransform>();
                overlayRect.anchorMin = Vector2.zero;
                overlayRect.anchorMax = Vector2.one;
                overlayRect.offsetMin = Vector2.zero;
                overlayRect.offsetMax = Vector2.zero;

                var overlayImage = overlay.GetComponent<Image>();
                overlayImage.color = new Color(0.09f, 0.11f, 0.14f, 0.08f);

                mismatchResetButton = overlay.GetComponent<Button>();
                mismatchResetButton.targetGraphic = overlayImage;
                mismatchResetButton.transition = Selectable.Transition.None;

                mismatchResetLabel = CreateText("Label", overlay.transform, "No coinciden. Toca para girarlas de nuevo.", 26, TextAnchor.MiddleCenter);
                var labelRect = mismatchResetLabel.rectTransform;
                labelRect.anchorMin = new Vector2(0.5f, 0f);
                labelRect.anchorMax = new Vector2(0.5f, 0f);
                labelRect.pivot = new Vector2(0.5f, 0.5f);
                labelRect.anchoredPosition = new Vector2(0f, 92f);
                labelRect.sizeDelta = new Vector2(760f, 72f);
                mismatchResetLabel.color = Color.white;
                mismatchResetButton.gameObject.SetActive(false);
            }
        }

        private void ApplyMismatchOverlayPresentation()
        {
            if (mismatchResetButton == null)
            {
                return;
            }

            mismatchResetButton.transition = Selectable.Transition.None;

            var overlayImage = mismatchResetButton.targetGraphic as Image;
            if (overlayImage != null)
            {
                overlayImage.color = new Color(0.09f, 0.11f, 0.14f, 0.08f);
                overlayImage.raycastTarget = true;
            }

            if (mismatchResetLabel == null)
            {
                return;
            }

            mismatchResetLabel.alignment = TextAlignmentOptions.Center;
            mismatchResetLabel.color = new Color(1f, 1f, 1f, 0.94f);
            mismatchResetLabel.fontSize = 22f;
            mismatchResetLabel.text = "No coinciden. Toca cualquier parte para girarlas.";

            var labelRect = mismatchResetLabel.rectTransform;
            labelRect.anchorMin = new Vector2(0.5f, 0f);
            labelRect.anchorMax = new Vector2(0.5f, 0f);
            labelRect.pivot = new Vector2(0.5f, 0.5f);
            labelRect.anchoredPosition = new Vector2(0f, 92f);
            labelRect.sizeDelta = new Vector2(760f, 72f);
        }

        private static GameObject CreatePilePanel(string name, Transform parent, string title)
        {
            var panel = CreateUiObject(name, parent, typeof(Image));
            var image = panel.GetComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.75f);
            image.raycastTarget = false;

            var cardAnchor = CreateUiObject("CardAnchor", panel.transform, typeof(Image));
            var cardAnchorRect = cardAnchor.GetComponent<RectTransform>();
            cardAnchorRect.anchorMin = new Vector2(0.5f, 1f);
            cardAnchorRect.anchorMax = new Vector2(0.5f, 1f);
            cardAnchorRect.pivot = new Vector2(0.5f, 1f);
            cardAnchorRect.anchoredPosition = new Vector2(0f, -16f);
            cardAnchorRect.sizeDelta = new Vector2(72f, 104f);
            var cardAnchorImage = cardAnchor.GetComponent<Image>();
            cardAnchorImage.color = new Color(0.16f, 0.29f, 0.35f, 1f);
            cardAnchorImage.raycastTarget = false;

            var titleLabel = CreateText("TitleLabel", panel.transform, title, 18, TextAnchor.MiddleCenter);
            var titleRect = titleLabel.rectTransform;
            titleRect.anchorMin = new Vector2(0.5f, 1f);
            titleRect.anchorMax = new Vector2(0.5f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0f, -126f);
            titleRect.sizeDelta = new Vector2(110f, 24f);

            var countLabel = CreateText("CountLabel", panel.transform, "0", 22, TextAnchor.UpperCenter);
            var countRect = countLabel.rectTransform;
            countRect.anchorMin = new Vector2(0.5f, 0f);
            countRect.anchorMax = new Vector2(0.5f, 0f);
            countRect.pivot = new Vector2(0.5f, 0f);
            countRect.anchoredPosition = new Vector2(0f, 12f);
            countRect.sizeDelta = new Vector2(120f, 54f);

            return panel;
        }

        private static GameObject CreateUiObject(string name, Transform parent, params System.Type[] components)
        {
            var objectComponents = new System.Type[components.Length + 1];
            objectComponents[0] = typeof(RectTransform);
            for (var index = 0; index < components.Length; index++)
            {
                objectComponents[index + 1] = components[index];
            }

            var gameObject = new GameObject(name, objectComponents);
            gameObject.layer = 5;
            gameObject.transform.SetParent(parent, false);
            return gameObject;
        }

        private static TMP_Text CreateText(string name, Transform parent, string value, int fontSize, TextAnchor alignment)
        {
            var textObject = CreateUiObject(name, parent, typeof(TextMeshProUGUI));
            var text = textObject.GetComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = fontSize;
            text.alignment = ConvertAlignment(alignment);
            text.color = new Color(0.12f, 0.15f, 0.17f, 1f);
            text.enableWordWrapping = true;
            text.overflowMode = TextOverflowModes.Overflow;
            text.raycastTarget = false;
            return text;
        }

        private static TextAlignmentOptions ConvertAlignment(TextAnchor alignment)
        {
            return alignment switch
            {
                TextAnchor.UpperCenter => TextAlignmentOptions.Top,
                TextAnchor.MiddleCenter => TextAlignmentOptions.Center,
                _ => TextAlignmentOptions.Center
            };
        }
    }
}
