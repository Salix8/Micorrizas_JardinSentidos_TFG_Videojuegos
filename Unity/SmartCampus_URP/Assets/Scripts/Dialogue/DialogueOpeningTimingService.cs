using UnityEngine;

namespace SmartCampus.Dialogue
{
    public static class DialogueOpeningTimingService
    {
        public static float ResolveGpsTimeout(
            bool isEditor,
            float deviceTimeoutSeconds,
            float editorFallbackSeconds)
        {
            var configuredTimeout = isEditor ? editorFallbackSeconds : deviceTimeoutSeconds;
            return Mathf.Max(0f, configuredTimeout);
        }
    }
}
