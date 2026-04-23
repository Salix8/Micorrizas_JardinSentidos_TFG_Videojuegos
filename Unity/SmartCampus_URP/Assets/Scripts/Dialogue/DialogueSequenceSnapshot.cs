namespace SmartCampus.Dialogue
{
    public readonly struct DialogueSequenceSnapshot
    {
        public DialogueSequenceSnapshot(DialogueLine currentLine, int currentIndex, int lineCount)
        {
            CurrentLine = currentLine;
            CurrentIndex = currentIndex;
            LineCount = lineCount;
        }

        public DialogueLine CurrentLine { get; }
        public int CurrentIndex { get; }
        public int LineCount { get; }
        public bool HasLine => CurrentLine != null;
        public bool CanMovePrevious => CurrentIndex > 0;
        public bool CanMoveNext => HasLine && CurrentIndex < LineCount - 1;
        public int DisplayIndex => HasLine ? CurrentIndex + 1 : 0;
    }
}
