using UnityEngine;

namespace SmartCampus.Coop.Minigames
{
    public abstract class CooperativeMinigameConfigBase : ScriptableObject
    {
        [Header("Metadata")]
        [SerializeField] private string displayName = "Cooperative Minigame";

        [Header("Flow")]
        [SerializeField] private MinigameTutorialContentConfig tutorialContent;
        [SerializeField] private string successMessage = "Lo habeis conseguido";
        [SerializeField] private string returnToMapButtonLabel = "Volver al mapa";

        public string DisplayName => displayName;
        public MinigameTutorialContentConfig TutorialContent => tutorialContent;
        public string SuccessMessage => successMessage;
        public string ReturnToMapButtonLabel => returnToMapButtonLabel;
    }
}
