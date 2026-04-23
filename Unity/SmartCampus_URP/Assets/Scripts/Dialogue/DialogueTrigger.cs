using System;
using UnityEngine;

namespace SmartCampus.Dialogue
{
    public enum DialogueTriggerMode
    {
        ActOrLocation,
        SingleLine,
        LineSequence
    }

    [DisallowMultipleComponent]
    public sealed class DialogueTrigger : MonoBehaviour
    {
        [SerializeField] private DialogueUIController dialogueController;
        [SerializeField] private DialogueTriggerMode triggerMode = DialogueTriggerMode.ActOrLocation;
        [SerializeField] private string actOrLocation;
        [SerializeField] private string singleLineId;
        [SerializeField] private string[] lineIds = Array.Empty<string>();

        [Header("Optional Trigger Collider")]
        [SerializeField] private bool playOnTriggerEnter;
        [SerializeField] private bool playOnlyOnce = true;
        [SerializeField] private string requiredTag = "Player";

        private bool hasPlayed;

        private void Awake()
        {
            dialogueController ??= FindFirstObjectByType<DialogueUIController>(FindObjectsInactive.Include);
        }

        public void Play()
        {
            if (dialogueController == null)
            {
                Debug.LogWarning("DialogueTrigger necesita una DialogueUIController en la escena.", this);
                return;
            }

            if (playOnlyOnce && hasPlayed)
            {
                return;
            }

            hasPlayed = true;
            switch (triggerMode)
            {
                case DialogueTriggerMode.ActOrLocation:
                    dialogueController.PlayActOrLocation(actOrLocation);
                    break;
                case DialogueTriggerMode.SingleLine:
                    dialogueController.PlayLine(singleLineId);
                    break;
                case DialogueTriggerMode.LineSequence:
                    dialogueController.PlayLineIds(lineIds);
                    break;
                default:
                    Debug.LogWarning($"Modo de dialogo no soportado: {triggerMode}.", this);
                    break;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!playOnTriggerEnter || other == null)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(requiredTag) && !other.CompareTag(requiredTag))
            {
                return;
            }

            Play();
        }
    }
}
