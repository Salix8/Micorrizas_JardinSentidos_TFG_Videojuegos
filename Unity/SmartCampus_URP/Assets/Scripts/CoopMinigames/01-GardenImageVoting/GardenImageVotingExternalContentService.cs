using System;
using System.Collections;
using UnityEngine;
using SmartCampus.Coop.Minigames;

namespace SmartCampus.Coop.Minigames.GardenImageVoting
{
    public static class GardenImageVotingExternalContentService
    {
        public static IEnumerator LoadTextAsync(string configuredPath, Action<string, string> onCompleted)
        {
            return CoopMinigameExternalContentService.LoadTextAsync(configuredPath, onCompleted);
        }

        public static IEnumerator LoadSpriteAsync(string configuredPath, Action<Sprite, string> onCompleted)
        {
            return LoadSpriteAsync(configuredPath, null, onCompleted);
        }

        public static IEnumerator LoadSpriteAsync(string configuredPath, string relativeToConfiguredPath, Action<Sprite, string> onCompleted)
        {
            return CoopMinigameExternalContentService.LoadSpriteAsync(configuredPath, relativeToConfiguredPath, onCompleted);
        }

        public static string ResolveConfiguredPath(string configuredPath, string relativeToConfiguredPath = null)
        {
            return CoopMinigameExternalContentService.ResolveConfiguredPath(configuredPath, relativeToConfiguredPath);
        }

        public static string ResolveConfiguredPath(string configuredPath, string relativeToConfiguredPath, string streamingAssetsPathOverride)
        {
            return CoopMinigameExternalContentService.ResolveConfiguredPath(configuredPath, relativeToConfiguredPath, streamingAssetsPathOverride);
        }
    }
}
