using UnityEngine;

namespace SmartCampus.Dialogue
{
    [CreateAssetMenu(menuName = "SmartCampus/Dialogue/System Config", fileName = "DialogueSystemConfig")]
    public sealed class DialogueSystemConfig : ScriptableObject
    {
        [Header("Content")]
        [SerializeField] private TextAsset dialogueCsvAsset;
        [SerializeField] private CharacterPortraitDatabase characterPortraitDatabase;
        [SerializeField] private DialogueAudioDatabase dialogueAudioDatabase;

        [Header("Localization")]
        [SerializeField] private DialogueLanguage defaultLanguage = DialogueLanguage.Spanish;

        [Header("Presentation")]
        [SerializeField] [Min(0f)] private float typewriterCharactersPerSecond = 60f;
        [SerializeField] private bool useTypewriterEffect = true;
        [SerializeField] private bool revealFullLineOnAdvanceDuringTypewriter = true;
        [SerializeField] private bool closePanelWhenSequenceCompletes = true;
        [SerializeField] private bool playAudioOnLineChanged = true;

        public TextAsset DialogueCsvAsset => dialogueCsvAsset;
        public CharacterPortraitDatabase CharacterPortraitDatabase => characterPortraitDatabase;
        public DialogueAudioDatabase DialogueAudioDatabase => dialogueAudioDatabase;
        public DialogueLanguage DefaultLanguage => defaultLanguage;
        public float TypewriterCharactersPerSecond => typewriterCharactersPerSecond;
        public bool UseTypewriterEffect => useTypewriterEffect;
        public bool RevealFullLineOnAdvanceDuringTypewriter => revealFullLineOnAdvanceDuringTypewriter;
        public bool ClosePanelWhenSequenceCompletes => closePanelWhenSequenceCompletes;
        public bool PlayAudioOnLineChanged => playAudioOnLineChanged;
    }
}
