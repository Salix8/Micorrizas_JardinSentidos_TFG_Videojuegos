using System;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using SmartCampus.Coop.Minigames;

namespace SmartCampus.Coop.Minigames.AudioWordConsensus
{
    [DisallowMultipleComponent]
    public sealed class AudioWordConsensusMinigameUIController : MinigameUIControllerBase
    {
        private enum EditorPreviewRole
        {
            Receiver = 0,
            Emitter = 1
        }

        [Serializable]
        private sealed class EditorPreviewSettings
        {
            [SerializeField] private bool enableIsolatedScenePreview = true;
            [SerializeField] private EditorPreviewRole role = EditorPreviewRole.Receiver;
            [SerializeField] [Min(1)] private int previewRoundIndex = 1;
            [SerializeField] [Min(1)] private int previewTotalRounds = 3;
            [SerializeField] [Min(0)] private int previewCorrectRoundCount;
            [SerializeField] [Min(0)] private int previewIncorrectRoundCount;
            [SerializeField] [Min(0f)] private float previewRemainingTimeSeconds = 45f;
            [SerializeField] private string previewStatusMessage = "Vista previa local del flujo de juego.";
            [SerializeField] private int previewWordShuffleSeed = 1024;
            [SerializeField] private List<string> fallbackReceiverWords = new() { "Raiz", "Tallo", "Hoja" };

            public bool EnableIsolatedScenePreview => enableIsolatedScenePreview;
            public EditorPreviewRole Role => role;
            public int PreviewRoundIndex => Mathf.Max(1, previewRoundIndex);
            public int PreviewTotalRounds => Mathf.Max(1, previewTotalRounds);
            public int PreviewCorrectRoundCount => Mathf.Max(0, previewCorrectRoundCount);
            public int PreviewIncorrectRoundCount => Mathf.Max(0, previewIncorrectRoundCount);
            public float PreviewRemainingTimeSeconds => Mathf.Max(0f, previewRemainingTimeSeconds);
            public string PreviewStatusMessage => string.IsNullOrWhiteSpace(previewStatusMessage)
                ? "Vista previa local del flujo de juego."
                : previewStatusMessage.Trim();
            public int PreviewWordShuffleSeed => previewWordShuffleSeed;
            public IReadOnlyList<string> FallbackReceiverWords => fallbackReceiverWords;
        }

        [Serializable]
        private sealed class CanvasVisibilitySettings
        {
            [SerializeField] private bool forceScreenSpaceCamera = true;
            [SerializeField] [Min(0.3f)] private float planeDistance = 1f;
            [SerializeField] private bool overrideSorting = true;
            [SerializeField] private int sortingOrder = 500;

            public bool ForceScreenSpaceCamera => forceScreenSpaceCamera;
            public float PlaneDistance => Mathf.Max(0.3f, planeDistance);
            public bool OverrideSorting => overrideSorting;
            public int SortingOrder => sortingOrder;
        }

        private const float WordOptionButtonPreferredHeight = 76f;
        private const float WordOptionContainerPadding = 12f;
        private const float WordOptionContainerSpacing = 12f;
        private const float WordOptionContainerMinimumHeight = 200f;
        private static readonly Vector2 WordOptionLabelOffsetMin = new(18f, 10f);
        private static readonly Vector2 WordOptionLabelOffsetMax = new(-18f, -10f);

        [SerializeField] private AudioWordConsensusMinigameSession audioWordConsensusMinigameSession;
        [SerializeField] private AudioSource localAudioSource;
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text roundLabel;
        [SerializeField] private TMP_Text timerLabel;
        [SerializeField] private TMP_Text scoreLabel;
        [SerializeField] private TMP_Text statusLabel;
        [SerializeField] private TMP_Text roleLabel;
        [SerializeField] private TMP_Text localWordLabel;
        [SerializeField] private Button playSoundButton;
        [SerializeField] private TMP_Text playSoundButtonLabel;
        [SerializeField] private Button restartSoundButton;
        [SerializeField] private TMP_Text restartSoundButtonLabel;
        [SerializeField] private Transform wordOptionsContainer;
        [SerializeField] private Button wordOptionButtonTemplate;
        [SerializeField] private Button submitWordButton;
        [SerializeField] private TMP_Text submitWordButtonLabel;
        [SerializeField] private EditorPreviewSettings editorPreviewSettings = new();
        [SerializeField] private CanvasVisibilitySettings canvasVisibilitySettings = new();

        private readonly List<Button> activeWordOptionButtons = new();
        private bool localPlaybackPaused;
        private int lastPreparedRoundIndex = -1;
        private string lastPreviewSelection = string.Empty;
        private Canvas rootCanvas;
        private Camera cachedMainCamera;

        private AudioWordConsensusMinigameSession TypedSession => audioWordConsensusMinigameSession != null
            ? audioWordConsensusMinigameSession
            : Session as AudioWordConsensusMinigameSession;

        private EditorPreviewSettings PreviewSettings => editorPreviewSettings ??= new EditorPreviewSettings();

        protected override void Awake()
        {
            audioWordConsensusMinigameSession ??= FindFirstObjectByType<AudioWordConsensusMinigameSession>(FindObjectsInactive.Include);
            ResolveCanvasReferences();
            base.Awake();
        }

        protected override void OnEnable()
        {
            audioWordConsensusMinigameSession ??= FindFirstObjectByType<AudioWordConsensusMinigameSession>(FindObjectsInactive.Include);
            ResolveCanvasReferences();
            ApplyCanvasVisibilityContract();
            if (TypedSession != null)
            {
                TypedSession.StateChanged += HandleStateChanged;
            }

            RegisterLocalButtons();
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            if (TypedSession != null)
            {
                TypedSession.StateChanged -= HandleStateChanged;
            }

            if (playSoundButton != null)
            {
                playSoundButton.onClick.RemoveListener(HandlePlaySoundPressed);
            }

            if (restartSoundButton != null)
            {
                restartSoundButton.onClick.RemoveListener(HandleRestartSoundPressed);
            }

            StopAndResetLocalAudio(clearClip: false);
            ClearGeneratedWordOptionButtons();
            base.OnDisable();
        }

        protected override string BuildWaitingMessage()
        {
            if (IsIsolatedEditorPreviewActive())
            {
                return "Vista previa local activa. La sesion cooperativa no esta inicializada.";
            }

            if (TypedSession == null || !TypedSession.HasLocalTutorialBeenDismissed)
            {
                return base.BuildWaitingMessage();
            }

            return $"Esperando a que el resto cierre el tutorial: {TypedSession.TutorialDismissedCount}/{TypedSession.ParticipantCount}";
        }

        protected override bool TryResolveViewStateOverride(CooperativeMinigameConfigBase config, out MinigameUIViewState viewState)
        {
            if (IsIsolatedEditorPreviewActive())
            {
                viewState = new MinigameUIViewState(
                    showTutorialPopup: false,
                    showWaiting: false,
                    showGameplay: true,
                    showResults: false);
                return true;
            }

            viewState = default;
            return false;
        }

        protected override void RefreshGameplay()
        {
            ApplyCanvasVisibilityContract();

            if (IsIsolatedEditorPreviewActive())
            {
                RefreshIsolatedEditorPreview();
                return;
            }

            if (TypedSession == null)
            {
                return;
            }

            var config = TypedSession.MinigameConfig as AudioWordConsensusMinigameConfig;
            if (config == null)
            {
                return;
            }

            if (titleLabel != null)
            {
                titleLabel.text = config.DisplayName;
            }

            if (roundLabel != null)
            {
                var currentRoundDisplay = Mathf.Max(1, TypedSession.ActiveRoundIndex + 1);
                roundLabel.text = $"Ronda: {currentRoundDisplay}/{Mathf.Max(1, TypedSession.TotalScheduledRounds)}";
            }

            if (timerLabel != null)
            {
                var remainingSeconds = Mathf.CeilToInt(TypedSession.RemainingTimeSeconds);
                timerLabel.text = $"Tiempo restante: {remainingSeconds / 60:00}:{remainingSeconds % 60:00}";
            }

            if (scoreLabel != null)
            {
                scoreLabel.text = $"Aciertos: {TypedSession.CorrectRoundCount}   Fallos: {TypedSession.IncorrectRoundCount}";
            }

            if (statusLabel != null)
            {
                statusLabel.text = TypedSession.SharedStatusMessage;
            }

            var isEmitter = TypedSession.IsLocalEmitter;
            var hasAssignedWords = TypedSession.TryGetAssignedWordsForLocalPlayer(out var assignedWords);
            var isAwaitingAssignedWords = TypedSession.IsAwaitingLocalAssignedWords();
            var roundDefinition = TypedSession.GetCurrentRoundDefinition();
            SyncLocalAudioState(isEmitter, roundDefinition);
            EnsureRestartSoundButton();

            if (roleLabel != null)
            {
                roleLabel.text = isEmitter
                    ? "Tu rol en esta ronda: reproducir el sonido para el grupo."
                    : "Tu rol en esta ronda: mostrar tu palabra y decidir en grupo quien debe pulsar.";
            }

            if (localWordLabel != null)
            {
                localWordLabel.text = isEmitter
                    ? "En este turno no recibes palabra. Tu tarea es reproducir el sonido cuando el grupo lo necesite."
                    : (hasAssignedWords
                        ? "Opciones disponibles en tu dispositivo:"
                        : (isAwaitingAssignedWords
                            ? "Recibiendo opciones para esta ronda..."
                            : "No hay opciones disponibles para esta ronda."));
            }

            if (playSoundButton != null)
            {
                playSoundButton.gameObject.SetActive(isEmitter);
                playSoundButton.interactable = isEmitter && roundDefinition != null && roundDefinition.SoundClip != null;
            }

            if (playSoundButtonLabel != null)
            {
                playSoundButtonLabel.text = BuildPlayPauseButtonLabel(roundDefinition, config);
            }

            if (restartSoundButton != null)
            {
                restartSoundButton.gameObject.SetActive(isEmitter);
                restartSoundButton.interactable = isEmitter && roundDefinition != null && roundDefinition.SoundClip != null;
            }

            if (restartSoundButtonLabel != null)
            {
                restartSoundButtonLabel.text = "Reiniciar pista";
            }

            RefreshWordOptionButtons(isEmitter, hasAssignedWords ? assignedWords : null);
        }

        private void RegisterLocalButtons()
        {
            if (playSoundButton != null)
            {
                playSoundButton.onClick.RemoveListener(HandlePlaySoundPressed);
                playSoundButton.onClick.AddListener(HandlePlaySoundPressed);
            }

            EnsureRestartSoundButton();
            if (restartSoundButton != null)
            {
                restartSoundButton.onClick.RemoveListener(HandleRestartSoundPressed);
                restartSoundButton.onClick.AddListener(HandleRestartSoundPressed);
            }
        }

        private void RefreshWordOptionButtons(bool isEmitter, IReadOnlyList<string> assignedWords)
        {
            var templateButton = ResolveWordOptionTemplate();
            if (templateButton == null)
            {
                return;
            }

            EnsureWordOptionsContainerContract();
            var hasAssignedWords = assignedWords != null && assignedWords.Count > 0;
            if (isEmitter || !hasAssignedWords)
            {
                HideWordOptionButtons();
                RebuildWordOptionsLayout();
                return;
            }

            ShowWordOptionButtons(
                assignedWords,
                TypedSession.CanLocalSubmitAssignedWord(),
                HandleSubmitWordPressed);
            RebuildWordOptionsLayout();
        }

        private void EnsureWordOptionButtonPool(int requiredButtonCount)
        {
            var templateButton = ResolveWordOptionTemplate();
            if (templateButton == null)
            {
                return;
            }

            while (activeWordOptionButtons.Count < requiredButtonCount)
            {
                var instance = Instantiate(templateButton, templateButton.transform.parent);
                instance.name = $"{templateButton.name}_{activeWordOptionButtons.Count + 1}";
                instance.gameObject.SetActive(false);
                EnsureWordOptionVisualContract(instance);
                activeWordOptionButtons.Add(instance);
            }
        }

        private Button ResolveWordOptionTemplate()
        {
            if (wordOptionButtonTemplate == null)
            {
                wordOptionButtonTemplate = submitWordButton;
            }

            if (wordOptionButtonTemplate != null && wordOptionsContainer == null)
            {
                wordOptionsContainer = wordOptionButtonTemplate.transform.parent;
            }

            if (wordOptionButtonTemplate != null)
            {
                EnsureTemplateIsPrepared(wordOptionButtonTemplate);
            }

            return wordOptionButtonTemplate;
        }

        private static TMP_Text GetButtonLabel(Button button)
        {
            return button == null ? null : button.GetComponentInChildren<TMP_Text>(true);
        }

        private void ClearGeneratedWordOptionButtons()
        {
            for (var index = activeWordOptionButtons.Count - 1; index >= 0; index--)
            {
                var button = activeWordOptionButtons[index];
                if (button == null)
                {
                    continue;
                }

                button.onClick.RemoveAllListeners();
                Destroy(button.gameObject);
            }

            activeWordOptionButtons.Clear();
        }

        private void HideWordOptionButtons()
        {
            var templateButton = ResolveWordOptionTemplate();
            if (templateButton == null)
            {
                return;
            }

            if (activeWordOptionButtons.Count == 0)
            {
                EnsureTemplateIsPrepared(templateButton);
            }

            for (var index = 0; index < activeWordOptionButtons.Count; index++)
            {
                var button = activeWordOptionButtons[index];
                if (button == null)
                {
                    continue;
                }

                button.onClick.RemoveAllListeners();
                button.gameObject.SetActive(false);
            }

            templateButton.onClick.RemoveAllListeners();
            templateButton.gameObject.SetActive(false);
        }

        private void HandlePlaySoundPressed()
        {
            var roundDefinition = ResolveCurrentRoundDefinitionForPlayback();
            if (roundDefinition == null || roundDefinition.SoundClip == null || localAudioSource == null)
            {
                return;
            }

            EnsurePreparedRoundClip(roundDefinition);

            if (localAudioSource.isPlaying)
            {
                localAudioSource.Pause();
                localPlaybackPaused = true;
            }
            else if (localPlaybackPaused && localAudioSource.clip == roundDefinition.SoundClip)
            {
                localAudioSource.UnPause();
                localPlaybackPaused = false;
            }
            else
            {
                localAudioSource.time = 0f;
                localAudioSource.Play();
                localPlaybackPaused = false;
            }

            RefreshGameplay();
        }

        private void HandleRestartSoundPressed()
        {
            var roundDefinition = ResolveCurrentRoundDefinitionForPlayback();
            if (roundDefinition == null || roundDefinition.SoundClip == null || localAudioSource == null)
            {
                return;
            }

            EnsurePreparedRoundClip(roundDefinition);
            localAudioSource.Stop();
            localAudioSource.time = 0f;
            localAudioSource.Play();
            localPlaybackPaused = false;
            RefreshGameplay();
        }

        private void HandleSubmitWordPressed(string selectedWord)
        {
            TypedSession?.SubmitLocalAssignedWord(selectedWord);
        }

        private void HandlePreviewWordPressed(string selectedWord)
        {
            lastPreviewSelection = selectedWord;
            RefreshIsolatedEditorPreview();
        }

        private void HandleStateChanged()
        {
            RefreshGameplay();
        }

        private void RebuildWordOptionsLayout()
        {
            Canvas.ForceUpdateCanvases();

            if (wordOptionsContainer is RectTransform containerRectTransform)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(containerRectTransform);
                if (containerRectTransform.parent is RectTransform parentRectTransform)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(parentRectTransform);
                }
            }

            Canvas.ForceUpdateCanvases();
        }

        private void EnsureRestartSoundButton()
        {
            if (restartSoundButton != null || playSoundButton == null)
            {
                return;
            }

            restartSoundButton = Instantiate(playSoundButton, playSoundButton.transform.parent);
            restartSoundButton.name = "RestartSoundButton";
            restartSoundButton.transform.SetSiblingIndex(playSoundButton.transform.GetSiblingIndex() + 1);
            restartSoundButtonLabel = restartSoundButton.GetComponentInChildren<TMP_Text>(true);
        }

        private void ResolveCanvasReferences()
        {
            rootCanvas ??= GetComponentInParent<Canvas>();
            if (cachedMainCamera == null)
            {
                cachedMainCamera = Camera.main;
                if (cachedMainCamera == null)
                {
                    cachedMainCamera = FindFirstObjectByType<Camera>(FindObjectsInactive.Include);
                }
            }
        }

        private void ApplyCanvasVisibilityContract()
        {
            ResolveCanvasReferences();
            if (rootCanvas == null || cachedMainCamera == null || !canvasVisibilitySettings.ForceScreenSpaceCamera)
            {
                return;
            }

            rootCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            rootCanvas.worldCamera = cachedMainCamera;
            rootCanvas.planeDistance = canvasVisibilitySettings.PlaneDistance;
            rootCanvas.overrideSorting = canvasVisibilitySettings.OverrideSorting;
            rootCanvas.sortingOrder = canvasVisibilitySettings.SortingOrder;
        }

        private void SyncLocalAudioState(bool isEmitter, AudioWordConsensusRoundDefinition roundDefinition)
        {
            if (localAudioSource == null)
            {
                return;
            }

            var targetClip = isEmitter ? roundDefinition?.SoundClip : null;
            var activeRoundIndex = ResolveDisplayedRoundIndex();
            var hasRoundChanged = activeRoundIndex != lastPreparedRoundIndex;
            var hasClipChanged = localAudioSource.clip != targetClip;
            if (!hasRoundChanged && !hasClipChanged)
            {
                return;
            }

            StopAndResetLocalAudio(clearClip: false);
            localAudioSource.clip = targetClip;
            lastPreparedRoundIndex = activeRoundIndex;
        }

        private void EnsurePreparedRoundClip(AudioWordConsensusRoundDefinition roundDefinition)
        {
            if (localAudioSource == null)
            {
                return;
            }

            var targetClip = roundDefinition == null ? null : roundDefinition.SoundClip;
            if (localAudioSource.clip == targetClip)
            {
                return;
            }

            StopAndResetLocalAudio(clearClip: false);
            localAudioSource.clip = targetClip;
            lastPreparedRoundIndex = ResolveDisplayedRoundIndex();
        }

        private string BuildPlayPauseButtonLabel(AudioWordConsensusRoundDefinition roundDefinition, AudioWordConsensusMinigameConfig config)
        {
            if (roundDefinition == null || roundDefinition.SoundClip == null)
            {
                return config == null ? string.Empty : config.MissingAudioClipLabel;
            }

            if (localAudioSource != null && localAudioSource.isPlaying)
            {
                return "Pausar sonido";
            }

            return localPlaybackPaused ? "Continuar sonido" : "Reproducir sonido";
        }

        private void StopAndResetLocalAudio(bool clearClip)
        {
            if (localAudioSource == null)
            {
                return;
            }

            localAudioSource.Stop();
            localAudioSource.time = 0f;
            if (clearClip)
            {
                localAudioSource.clip = null;
            }

            localPlaybackPaused = false;
        }

        private bool IsIsolatedEditorPreviewActive()
        {
#if UNITY_EDITOR
            // This preview is only meant for direct scene Play Mode without any active NGO session.
            return Application.isPlaying &&
                   PreviewSettings.EnableIsolatedScenePreview &&
                   TypedSession != null &&
                   !TypedSession.IsSpawned &&
                   NetworkManager.Singleton == null;
#else
            return false;
#endif
        }

        private void RefreshIsolatedEditorPreview()
        {
            EnsureRestartSoundButton();

            var config = TypedSession != null ? TypedSession.MinigameConfig as AudioWordConsensusMinigameConfig : null;
            var roundDefinition = ResolvePreviewRoundDefinition(config);
            var isEmitter = PreviewSettings.Role == EditorPreviewRole.Emitter;
            SyncLocalAudioState(isEmitter, roundDefinition);

            if (titleLabel != null)
            {
                titleLabel.text = config == null ? "Sonido y consenso" : config.DisplayName;
            }

            if (roundLabel != null)
            {
                roundLabel.text = $"Ronda: {PreviewSettings.PreviewRoundIndex}/{PreviewSettings.PreviewTotalRounds}";
            }

            if (timerLabel != null)
            {
                var remainingSeconds = Mathf.CeilToInt(PreviewSettings.PreviewRemainingTimeSeconds);
                timerLabel.text = $"Tiempo restante: {remainingSeconds / 60:00}:{remainingSeconds % 60:00}";
            }

            if (scoreLabel != null)
            {
                scoreLabel.text = $"Aciertos: {PreviewSettings.PreviewCorrectRoundCount}   Fallos: {PreviewSettings.PreviewIncorrectRoundCount}";
            }

            if (statusLabel != null)
            {
                statusLabel.text = string.IsNullOrWhiteSpace(lastPreviewSelection)
                    ? PreviewSettings.PreviewStatusMessage
                    : $"Vista previa: seleccionada '{lastPreviewSelection}'.";
            }

            if (roleLabel != null)
            {
                roleLabel.text = isEmitter
                    ? "Vista previa local: emisor del sonido."
                    : "Vista previa local: receptor con opciones visibles.";
            }

            if (localWordLabel != null)
            {
                localWordLabel.text = isEmitter
                    ? "En la vista previa del emisor se muestran los controles de audio y se ocultan las opciones de palabra."
                    : "Opciones simuladas para depurar el layout del dispositivo receptor.";
            }

            if (playSoundButton != null)
            {
                playSoundButton.gameObject.SetActive(isEmitter);
                playSoundButton.interactable = isEmitter && roundDefinition != null && roundDefinition.SoundClip != null;
            }

            if (playSoundButtonLabel != null)
            {
                playSoundButtonLabel.text = BuildPlayPauseButtonLabel(roundDefinition, config);
            }

            if (restartSoundButton != null)
            {
                restartSoundButton.gameObject.SetActive(isEmitter);
                restartSoundButton.interactable = playSoundButton != null && playSoundButton.interactable;
            }

            if (restartSoundButtonLabel != null)
            {
                restartSoundButtonLabel.text = "Reiniciar pista";
            }

            if (isEmitter)
            {
                HideWordOptionButtons();
            }
            else
            {
                ShowWordOptionButtons(BuildPreviewWordOptions(roundDefinition), interactable: true, HandlePreviewWordPressed);
            }

            RebuildWordOptionsLayout();
        }

        private AudioWordConsensusRoundDefinition ResolveCurrentRoundDefinitionForPlayback()
        {
            if (IsIsolatedEditorPreviewActive())
            {
                var config = TypedSession != null ? TypedSession.MinigameConfig as AudioWordConsensusMinigameConfig : null;
                return ResolvePreviewRoundDefinition(config);
            }

            return TypedSession == null ? null : TypedSession.GetCurrentRoundDefinition();
        }

        private int ResolveDisplayedRoundIndex()
        {
            return IsIsolatedEditorPreviewActive()
                ? PreviewSettings.PreviewRoundIndex - 1
                : (TypedSession == null ? -1 : TypedSession.ActiveRoundIndex);
        }

        private AudioWordConsensusRoundDefinition ResolvePreviewRoundDefinition(AudioWordConsensusMinigameConfig config)
        {
            if (config == null || config.ActiveRoundCount <= 0)
            {
                return null;
            }

            var clampedRoundIndex = Mathf.Clamp(PreviewSettings.PreviewRoundIndex - 1, 0, config.ActiveRoundCount - 1);
            return config.GetRoundDefinition(clampedRoundIndex);
        }

        private IReadOnlyList<string> BuildPreviewWordOptions(AudioWordConsensusRoundDefinition roundDefinition)
        {
            if (roundDefinition != null)
            {
                var configuredWords = AudioWordConsensusWordAssignmentService.BuildShuffledOptionWords(
                    roundDefinition.CorrectWord,
                    roundDefinition.DistractorWords,
                    PreviewSettings.PreviewWordShuffleSeed);
                if (configuredWords.Count > 0)
                {
                    return configuredWords;
                }
            }

            return AudioWordConsensusWordAssignmentService.BuildDistinctOptionWords(
                "Palabra correcta",
                PreviewSettings.FallbackReceiverWords);
        }

        private void ShowWordOptionButtons(IReadOnlyList<string> optionWords, bool interactable, Action<string> onSelected)
        {
            var templateButton = ResolveWordOptionTemplate();
            if (templateButton == null || optionWords == null || optionWords.Count == 0)
            {
                HideWordOptionButtons();
                return;
            }

            EnsureTemplateIsPrepared(templateButton);
            EnsureWordOptionButtonPool(optionWords.Count);
            for (var index = 0; index < activeWordOptionButtons.Count; index++)
            {
                var button = activeWordOptionButtons[index];
                var shouldShow = index < optionWords.Count;
                button.gameObject.SetActive(shouldShow);

                if (!shouldShow)
                {
                    continue;
                }

                var optionWord = optionWords[index];
                EnsureWordOptionVisualContract(button);
                var label = GetButtonLabel(button);
                if (label != null)
                {
                    label.text = optionWord;
                }

                button.interactable = interactable;
                button.onClick.RemoveAllListeners();
                if (interactable && onSelected != null)
                {
                    button.onClick.AddListener(() => onSelected(optionWord));
                }
            }

            ApplyManualWordOptionLayout(optionWords.Count);
        }

        private void EnsureTemplateIsPrepared(Button templateButton)
        {
            if (templateButton == null)
            {
                return;
            }

            EnsureWordOptionsContainerContract();
            EnsureWordOptionVisualContract(templateButton);
            templateButton.onClick.RemoveAllListeners();
            templateButton.gameObject.SetActive(false);
        }

        private void EnsureWordOptionsContainerContract()
        {
            if (wordOptionsContainer == null)
            {
                return;
            }

            if (wordOptionsContainer.TryGetComponent<LayoutElement>(out var layoutElement) == false)
            {
                layoutElement = wordOptionsContainer.gameObject.AddComponent<LayoutElement>();
            }

            layoutElement.minHeight = 200f;
            layoutElement.preferredHeight = 200f;
            layoutElement.flexibleHeight = 0f;
            layoutElement.flexibleWidth = 1f;
            layoutElement.minWidth = 0f;
            layoutElement.preferredWidth = 0f;

            if (wordOptionsContainer.TryGetComponent<VerticalLayoutGroup>(out var layoutGroup) == false)
            {
                layoutGroup = wordOptionsContainer.gameObject.AddComponent<VerticalLayoutGroup>();
            }

            layoutGroup.padding = new RectOffset(12, 12, 12, 12);
            layoutGroup.spacing = 12f;
            layoutGroup.childControlWidth = true;
            layoutGroup.childControlHeight = true;
            layoutGroup.childForceExpandWidth = true;
            layoutGroup.childForceExpandHeight = false;
            layoutGroup.childAlignment = TextAnchor.UpperLeft;
            layoutGroup.enabled = false;

            if (wordOptionsContainer.TryGetComponent<ContentSizeFitter>(out var fitter) == false)
            {
                fitter = wordOptionsContainer.gameObject.AddComponent<ContentSizeFitter>();
            }

            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.enabled = false;
        }

        private void EnsureWordOptionVisualContract(Button button)
        {
            if (button == null)
            {
                return;
            }

            if (button.TryGetComponent<LayoutElement>(out var layoutElement) == false)
            {
                layoutElement = button.gameObject.AddComponent<LayoutElement>();
            }

            layoutElement.minHeight = WordOptionButtonPreferredHeight;
            layoutElement.preferredHeight = WordOptionButtonPreferredHeight;
            layoutElement.flexibleHeight = 0f;
            layoutElement.minWidth = 0f;
            layoutElement.preferredWidth = 0f;
            layoutElement.flexibleWidth = 1f;
            layoutElement.ignoreLayout = true;

            if (button.transform is RectTransform buttonRectTransform)
            {
                buttonRectTransform.anchorMin = new Vector2(0f, 1f);
                buttonRectTransform.anchorMax = new Vector2(1f, 1f);
                buttonRectTransform.pivot = new Vector2(0.5f, 1f);
                buttonRectTransform.sizeDelta = new Vector2(0f, WordOptionButtonPreferredHeight);
            }

            var label = GetButtonLabel(button);
            if (label == null)
            {
                return;
            }

            if (label.transform is RectTransform labelRectTransform)
            {
                labelRectTransform.anchorMin = Vector2.zero;
                labelRectTransform.anchorMax = Vector2.one;
                labelRectTransform.pivot = new Vector2(0.5f, 0.5f);
                labelRectTransform.offsetMin = WordOptionLabelOffsetMin;
                labelRectTransform.offsetMax = WordOptionLabelOffsetMax;
            }

            label.textWrappingMode = TextWrappingModes.Normal;
            label.overflowMode = TextOverflowModes.Ellipsis;
        }

        private void ApplyManualWordOptionLayout(int visibleButtonCount)
        {
            if (wordOptionsContainer is not RectTransform containerRectTransform)
            {
                return;
            }

            var totalHeight = visibleButtonCount <= 0
                ? WordOptionContainerMinimumHeight
                : Mathf.Max(
                    WordOptionContainerMinimumHeight,
                    (WordOptionContainerPadding * 2f) +
                    (visibleButtonCount * WordOptionButtonPreferredHeight) +
                    ((visibleButtonCount - 1) * WordOptionContainerSpacing));

            if (wordOptionsContainer.TryGetComponent<LayoutElement>(out var containerLayoutElement))
            {
                containerLayoutElement.minHeight = totalHeight;
                containerLayoutElement.preferredHeight = totalHeight;
            }

            for (var index = 0; index < activeWordOptionButtons.Count; index++)
            {
                var button = activeWordOptionButtons[index];
                if (button == null || !button.gameObject.activeSelf || button.transform is not RectTransform buttonRectTransform)
                {
                    continue;
                }

                var topOffset = WordOptionContainerPadding + index * (WordOptionButtonPreferredHeight + WordOptionContainerSpacing);
                buttonRectTransform.anchorMin = new Vector2(0f, 1f);
                buttonRectTransform.anchorMax = new Vector2(1f, 1f);
                buttonRectTransform.pivot = new Vector2(0.5f, 1f);
                buttonRectTransform.offsetMin = new Vector2(WordOptionContainerPadding, -(topOffset + WordOptionButtonPreferredHeight));
                buttonRectTransform.offsetMax = new Vector2(-WordOptionContainerPadding, -topOffset);
            }
        }
    }
}
