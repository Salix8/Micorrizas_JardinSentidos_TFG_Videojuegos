using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SmartCampus.Dialogue
{
    [DisallowMultipleComponent]
    public sealed class DialogueDemoUIController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private DialogueSystemConfig dialogueSystemConfig;
        [SerializeField] private DialogueUIController dialogueUIController;
        [SerializeField] private TMP_Dropdown sequenceDropdown;
        [SerializeField] private Button playSequenceButton;
        [SerializeField] private TMP_InputField lineIdInput;
        [SerializeField] private Button playLineButton;
        [SerializeField] private TMP_Text feedbackLabel;

        private DialogueCatalogService fallbackCatalog;

        private void Awake()
        {
            dialogueUIController ??= FindFirstObjectByType<DialogueUIController>(FindObjectsInactive.Include);
            BuildFallbackCatalogIfPossible();
        }

        private void Start()
        {
            PopulateSequenceDropdown();
        }

        private void OnEnable()
        {
            PopulateSequenceDropdown();

            if (playSequenceButton != null)
            {
                playSequenceButton.onClick.AddListener(PlaySelectedSequence);
            }

            if (playLineButton != null)
            {
                playLineButton.onClick.AddListener(PlayRequestedLine);
            }
        }

        private void OnDisable()
        {
            if (playSequenceButton != null)
            {
                playSequenceButton.onClick.RemoveListener(PlaySelectedSequence);
            }

            if (playLineButton != null)
            {
                playLineButton.onClick.RemoveListener(PlayRequestedLine);
            }
        }

        public void PlaySelectedSequence()
        {
            if (dialogueUIController == null || sequenceDropdown == null || sequenceDropdown.options.Count == 0)
            {
                SetFeedback("No hay secuencias cargadas.");
                return;
            }

            var sequenceKey = sequenceDropdown.options[sequenceDropdown.value].text;
            var started = dialogueUIController.PlaySequence(sequenceKey);
            SetFeedback(started
                ? $"Secuencia reproducida: {sequenceKey}"
                : $"No se pudo reproducir la secuencia: {sequenceKey}");
        }

        public void PlayRequestedLine()
        {
            if (dialogueUIController == null)
            {
                SetFeedback("No se encontro el controlador de dialogo.");
                return;
            }

            var lineId = lineIdInput == null ? string.Empty : lineIdInput.text;
            if (string.IsNullOrWhiteSpace(lineId))
            {
                SetFeedback("Introduce un String ID para probar PlayLine.");
                return;
            }

            var started = dialogueUIController.PlayLine(lineId);
            SetFeedback(started
                ? $"Linea reproducida: {lineId}"
                : $"No se pudo reproducir la linea: {lineId}");
        }

        private void PopulateSequenceDropdown()
        {
            if (sequenceDropdown == null)
            {
                return;
            }

            sequenceDropdown.ClearOptions();
            var keys = GetAvailableSequenceKeys();
            if (keys.Count == 0)
            {
                SetFeedback("No se han encontrado secuencias en el CSV configurado.");
                return;
            }

            var options = new System.Collections.Generic.List<string>(keys.Count);
            for (var index = 0; index < keys.Count; index++)
            {
                options.Add(keys[index]);
            }

            sequenceDropdown.AddOptions(options);
            sequenceDropdown.RefreshShownValue();
        }

        private System.Collections.Generic.IReadOnlyList<string> GetAvailableSequenceKeys()
        {
            if (dialogueUIController != null && dialogueUIController.AvailableSequenceKeys.Count > 0)
            {
                return dialogueUIController.AvailableSequenceKeys;
            }

            if (fallbackCatalog != null)
            {
                return fallbackCatalog.SequenceKeys;
            }

            return System.Array.Empty<string>();
        }

        private void BuildFallbackCatalogIfPossible()
        {
            if (dialogueSystemConfig == null || dialogueSystemConfig.DialogueCsvAsset == null)
            {
                return;
            }

            if (DialogueCatalogService.TryCreate(dialogueSystemConfig.DialogueCsvAsset.text, out var catalog, out _))
            {
                fallbackCatalog = catalog;
            }
        }

        private void SetFeedback(string message)
        {
            if (feedbackLabel != null)
            {
                feedbackLabel.text = message ?? string.Empty;
            }
        }
    }
}
