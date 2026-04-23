using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SmartCampus.Dialogue
{
    [DisallowMultipleComponent]
    public sealed class DialogueView : MonoBehaviour
    {
        [Header("Root")]
        [SerializeField] private GameObject rootPanel;

        [Header("Labels")]
        [SerializeField] private Text characterLabel;
        [SerializeField] private Text actOrLocationLabel;
        [SerializeField] private Text bodyLabel;
        [SerializeField] private Text progressLabel;

        [Header("Buttons")]
        [SerializeField] private Button previousButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private Button skipButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Text nextButtonLabel;
        [SerializeField] private Text skipButtonLabel;

        [Header("Text Reveal")]
        [SerializeField] [Min(1f)] private float charactersPerSecond = 36f;
        [SerializeField] private bool useUnscaledTime = true;

        [Header("Button Labels")]
        [SerializeField] private string revealNowButtonText = "Mostrar";
        [SerializeField] private string nextButtonText = "Siguiente";
        [SerializeField] private string finishButtonText = "Cerrar";
        [SerializeField] private string skipButtonText = "Omitir";

        private Coroutine revealCoroutine;
        private DialogueSequenceSnapshot currentSnapshot;
        private string currentFullText = string.Empty;
        private string currentCharacterName = string.Empty;

        public event Action NextRequested;
        public event Action PreviousRequested;
        public event Action SkipRequested;
        public event Action CloseRequested;
        public event Action<string, char, int> VisibleCharacterRevealed;

        public bool IsRevealing => revealCoroutine != null;

        private void Awake()
        {
            rootPanel ??= gameObject;
            RegisterButtons();
        }

        private void OnDisable()
        {
            StopRevealCoroutine();
        }

        private void OnDestroy()
        {
            UnregisterButtons();
        }

        public void SetVisible(bool visible)
        {
            var targetPanel = rootPanel != null ? rootPanel : gameObject;
            targetPanel.SetActive(visible);
        }

        public void Bind(DialogueSequenceSnapshot snapshot, string localizedText)
        {
            currentSnapshot = snapshot;
            currentCharacterName = snapshot.CurrentLine == null ? string.Empty : snapshot.CurrentLine.Character;
            currentFullText = localizedText ?? string.Empty;

            if (characterLabel != null)
            {
                characterLabel.text = currentCharacterName;
            }

            if (actOrLocationLabel != null)
            {
                var actOrLocation = snapshot.CurrentLine == null ? string.Empty : snapshot.CurrentLine.ActOrLocation;
                actOrLocationLabel.text = actOrLocation;
                actOrLocationLabel.gameObject.SetActive(!string.IsNullOrWhiteSpace(actOrLocation));
            }

            if (progressLabel != null)
            {
                progressLabel.text = snapshot.HasLine ? $"{snapshot.DisplayIndex}/{snapshot.LineCount}" : string.Empty;
            }

            StartReveal();
        }

        public void CompleteCurrentReveal()
        {
            StopRevealCoroutine();

            if (bodyLabel != null)
            {
                bodyLabel.text = currentFullText;
            }

            RefreshButtons();
        }

        private void StartReveal()
        {
            StopRevealCoroutine();

            if (bodyLabel != null)
            {
                bodyLabel.text = string.Empty;
            }

            if (string.IsNullOrEmpty(currentFullText) || charactersPerSecond <= 0f)
            {
                CompleteCurrentReveal();
                return;
            }

            revealCoroutine = StartCoroutine(RevealTextRoutine());
            RefreshButtons();
        }

        private IEnumerator RevealTextRoutine()
        {
            var visibleCharacterCount = 0;
            var targetVisibleCharacters = 0f;

            while (visibleCharacterCount < currentFullText.Length)
            {
                var deltaTime = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                targetVisibleCharacters += charactersPerSecond * deltaTime;
                var nextVisibleCharacterCount = Mathf.Min(currentFullText.Length, Mathf.FloorToInt(targetVisibleCharacters));

                if (nextVisibleCharacterCount <= visibleCharacterCount)
                {
                    yield return null;
                    continue;
                }

                while (visibleCharacterCount < nextVisibleCharacterCount)
                {
                    var visibleCharacter = currentFullText[visibleCharacterCount];
                    VisibleCharacterRevealed?.Invoke(currentCharacterName, visibleCharacter, visibleCharacterCount);
                    visibleCharacterCount++;
                }

                if (bodyLabel != null)
                {
                    bodyLabel.text = currentFullText.Substring(0, visibleCharacterCount);
                }

                yield return null;
            }

            revealCoroutine = null;
            RefreshButtons();
        }

        private void StopRevealCoroutine()
        {
            if (revealCoroutine == null)
            {
                return;
            }

            StopCoroutine(revealCoroutine);
            revealCoroutine = null;
        }

        private void RegisterButtons()
        {
            if (previousButton != null)
            {
                previousButton.onClick.AddListener(HandlePreviousPressed);
            }

            if (nextButton != null)
            {
                nextButton.onClick.AddListener(HandleNextPressed);
            }

            if (skipButton != null)
            {
                skipButton.onClick.AddListener(HandleSkipPressed);
            }

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(HandleClosePressed);
            }
        }

        private void UnregisterButtons()
        {
            if (previousButton != null)
            {
                previousButton.onClick.RemoveListener(HandlePreviousPressed);
            }

            if (nextButton != null)
            {
                nextButton.onClick.RemoveListener(HandleNextPressed);
            }

            if (skipButton != null)
            {
                skipButton.onClick.RemoveListener(HandleSkipPressed);
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(HandleClosePressed);
            }
        }

        private void RefreshButtons()
        {
            if (previousButton != null)
            {
                previousButton.interactable = !IsRevealing && currentSnapshot.CanMovePrevious;
            }

            if (nextButton != null)
            {
                nextButton.interactable = currentSnapshot.HasLine;
            }

            if (skipButton != null)
            {
                skipButton.interactable = currentSnapshot.HasLine;
            }

            if (nextButtonLabel != null)
            {
                nextButtonLabel.text = IsRevealing
                    ? revealNowButtonText
                    : (currentSnapshot.CanMoveNext ? nextButtonText : finishButtonText);
            }

            if (skipButtonLabel != null)
            {
                skipButtonLabel.text = IsRevealing ? revealNowButtonText : skipButtonText;
            }
        }

        private void HandlePreviousPressed()
        {
            if (!IsRevealing)
            {
                PreviousRequested?.Invoke();
            }
        }

        private void HandleNextPressed()
        {
            if (IsRevealing)
            {
                CompleteCurrentReveal();
                return;
            }

            NextRequested?.Invoke();
        }

        private void HandleSkipPressed()
        {
            if (IsRevealing)
            {
                CompleteCurrentReveal();
                return;
            }

            SkipRequested?.Invoke();
        }

        private void HandleClosePressed()
        {
            CloseRequested?.Invoke();
        }
    }
}
