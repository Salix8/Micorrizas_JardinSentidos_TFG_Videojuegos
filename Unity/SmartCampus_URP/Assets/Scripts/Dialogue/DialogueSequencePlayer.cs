using System;

namespace SmartCampus.Dialogue
{
    public sealed class DialogueSequencePlayer
    {
        private readonly DialogueCatalogService dialogueCatalog;
        private DialogueSequenceDefinition currentSequence;
        private int currentLineIndex = -1;
        private bool isCompleted;

        public DialogueSequencePlayer(DialogueCatalogService dialogueCatalog)
        {
            this.dialogueCatalog = dialogueCatalog;
        }

        public DialogueSequenceDefinition CurrentSequence => currentSequence;
        public DialogueLineDefinition CurrentLine => IsPlaying ? currentSequence.Lines[currentLineIndex] : null;
        public bool IsPlaying => currentSequence != null && currentLineIndex >= 0 && currentLineIndex < currentSequence.Lines.Count;
        public bool IsCompleted => isCompleted;

        public event Action<DialogueSequenceDefinition> SequenceStarted;
        public event Action<DialogueLineDefinition> LineChanged;
        public event Action<DialogueSequenceDefinition> SequenceCompleted;
        public event Action Closed;

        public bool PlaySequence(string sequenceKey)
        {
            if (dialogueCatalog == null || !dialogueCatalog.TryGetSequence(sequenceKey, out var sequence) || sequence.Lines.Count == 0)
            {
                return false;
            }

            SetCurrentSequence(sequence);
            return true;
        }

        public bool PlayLine(string stringId)
        {
            if (dialogueCatalog == null || !dialogueCatalog.TryBuildSingleLineSequence(stringId, out var sequence))
            {
                return false;
            }

            SetCurrentSequence(sequence);
            return true;
        }

        public bool Advance()
        {
            if (!IsPlaying || isCompleted)
            {
                return false;
            }

            if (currentLineIndex + 1 < currentSequence.Lines.Count)
            {
                currentLineIndex++;
                LineChanged?.Invoke(CurrentLine);
                return true;
            }

            isCompleted = true;
            SequenceCompleted?.Invoke(currentSequence);
            return false;
        }

        public void Close()
        {
            if (!IsPlaying && currentSequence == null)
            {
                return;
            }

            currentSequence = null;
            currentLineIndex = -1;
            isCompleted = false;
            Closed?.Invoke();
        }

        private void SetCurrentSequence(DialogueSequenceDefinition sequence)
        {
            if (currentSequence != null)
            {
                Close();
            }

            currentSequence = sequence;
            currentLineIndex = 0;
            isCompleted = false;
            SequenceStarted?.Invoke(currentSequence);
            LineChanged?.Invoke(CurrentLine);
        }
    }
}
