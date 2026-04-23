using System;
using System.Collections.Generic;

namespace SmartCampus.Dialogue
{
    public sealed class DialogueSequenceService : IDialogueService
    {
        private readonly List<DialogueLine> activeLines = new();
        private int currentIndex = -1;

        public event Action<DialogueSequenceSnapshot> SnapshotChanged;
        public event Action SequenceCompleted;

        public bool IsPlaying => currentIndex >= 0 && currentIndex < activeLines.Count;
        public DialogueSequenceSnapshot CurrentSnapshot => CreateSnapshot();

        public void Start(IReadOnlyList<DialogueLine> sequenceLines)
        {
            activeLines.Clear();
            if (sequenceLines != null)
            {
                activeLines.AddRange(sequenceLines);
            }

            currentIndex = activeLines.Count > 0 ? 0 : -1;
            PublishSnapshot();

            if (!IsPlaying)
            {
                SequenceCompleted?.Invoke();
            }
        }

        public bool MoveNext()
        {
            if (!IsPlaying)
            {
                return false;
            }

            if (currentIndex >= activeLines.Count - 1)
            {
                Complete();
                return false;
            }

            currentIndex++;
            PublishSnapshot();
            return true;
        }

        public bool MovePrevious()
        {
            if (!IsPlaying || currentIndex <= 0)
            {
                return false;
            }

            currentIndex--;
            PublishSnapshot();
            return true;
        }

        public void Complete()
        {
            if (!IsPlaying)
            {
                return;
            }

            currentIndex = -1;
            PublishSnapshot();
            SequenceCompleted?.Invoke();
        }

        private void PublishSnapshot()
        {
            SnapshotChanged?.Invoke(CreateSnapshot());
        }

        private DialogueSequenceSnapshot CreateSnapshot()
        {
            var currentLine = IsPlaying ? activeLines[currentIndex] : null;
            return new DialogueSequenceSnapshot(currentLine, currentIndex, activeLines.Count);
        }
    }
}
