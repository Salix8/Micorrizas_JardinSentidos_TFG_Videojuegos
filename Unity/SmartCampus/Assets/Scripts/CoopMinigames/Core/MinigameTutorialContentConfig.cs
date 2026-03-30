using UnityEngine;
using UnityEngine.Video;

namespace SmartCampus.Coop.Minigames
{
    [CreateAssetMenu(menuName = "SmartCampus/Coop/Minigames/Tutorial Content", fileName = "TutorialContentConfig")]
    public sealed class MinigameTutorialContentConfig : ScriptableObject
    {
        [SerializeField] private string title = "Como jugar";
        [SerializeField] private string subtitle = "Tutorial";
        [SerializeField] [TextArea(4, 10)] private string bodyText = "Define aqui el contenido del tutorial.";
        [SerializeField] private Sprite illustration;
        [SerializeField] private VideoClip videoClip;
        [SerializeField] private GameObject customContentPrefab;

        public string Title => title;
        public string Subtitle => subtitle;
        public string BodyText => bodyText;
        public Sprite Illustration => illustration;
        public VideoClip VideoClip => videoClip;
        public GameObject CustomContentPrefab => customContentPrefab;
    }
}
