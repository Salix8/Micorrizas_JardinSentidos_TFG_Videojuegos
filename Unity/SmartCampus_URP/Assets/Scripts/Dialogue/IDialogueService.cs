using System;
using System.Collections.Generic;

namespace SmartCampus.Dialogue
{
    public interface IDialogueService
    {
        event Action<DialogueSequenceSnapshot> SnapshotChanged;
        event Action SequenceCompleted;

        bool IsPlaying { get; }
        DialogueSequenceSnapshot CurrentSnapshot { get; }

        void Start(IReadOnlyList<DialogueLine> sequenceLines);
        bool MoveNext();
        bool MovePrevious();
        void Complete();
    }
}
