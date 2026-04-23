using UnityEngine;

namespace SmartCampus.Dialogue
{
    [CreateAssetMenu(menuName = "SmartCampus/Dialogue/Dialogue Settings", fileName = DefaultResourceName)]
    public sealed class DialogueSettingsConfig : ScriptableObject
    {
        public const string DefaultResourceName = "DialogueSettingsConfig";

        [SerializeField] private bool storyDialoguesEnabled = true;

        public bool StoryDialoguesEnabled => storyDialoguesEnabled;

        public static bool AreStoryDialoguesEnabled(DialogueSettingsConfig overrideSettings = null)
        {
            if (overrideSettings != null)
            {
                return overrideSettings.StoryDialoguesEnabled;
            }

            var defaultSettings = Resources.Load<DialogueSettingsConfig>(DefaultResourceName);
            return defaultSettings == null || defaultSettings.StoryDialoguesEnabled;
        }
    }
}
