using System.Collections.Generic;
using NUnit.Framework;
using SmartCampus.Coop.Minigames.AudioWordConsensus;
using UnityEditor;
using UnityEngine;

namespace SmartCampus.Testing.Editor.Core
{
    public sealed class AudioWordConsensusMinigameConfigTests
    {
        [Test]
        public void SupportsParticipantCount_AllowsFewerPlayersThanConfiguredRounds()
        {
            var config = ScriptableObject.CreateInstance<AudioWordConsensusMinigameConfig>();
            try
            {
                var serializedObject = new SerializedObject(config);
                serializedObject.FindProperty("maxSupportedDevices").intValue = 6;
                serializedObject.FindProperty("roundDefinitions").arraySize = 6;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();

                Assert.That(config.SupportsParticipantCount(3), Is.True);
                Assert.That(config.SupportsParticipantCount(5), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void SupportsParticipantCount_RequiresAtLeastOneConfiguredRound()
        {
            var config = ScriptableObject.CreateInstance<AudioWordConsensusMinigameConfig>();
            try
            {
                var serializedObject = new SerializedObject(config);
                serializedObject.FindProperty("maxSupportedDevices").intValue = 6;
                serializedObject.FindProperty("roundDefinitions").arraySize = 0;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();

                Assert.That(config.SupportsParticipantCount(3), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }
    }
}
