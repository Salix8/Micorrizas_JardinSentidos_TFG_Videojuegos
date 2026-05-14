using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using SmartCampus.Coop.Minigames.AudioWordConsensus;
using UnityEngine;

namespace SmartCampus.Testing.Editor.Core
{
    public sealed class AudioWordConsensusMinigameConfigTests
    {
        [Test]
        public void TryValidateForParticipantCount_ReturnsFalse_WhenEveryRoundIsMissingAudio()
        {
            var config = ScriptableObject.CreateInstance<AudioWordConsensusMinigameConfig>();
            SetPrivateField(config, "maxSupportedDevices", 6);
            SetPrivateField(config, "roundDefinitions", new List<AudioWordConsensusRoundDefinition>
            {
                CreateRoundDefinition(hasClip: false, correctWord: "Mirlo", distractorWords: new[] { "Gorrion" }),
                CreateRoundDefinition(hasClip: false, correctWord: "Rana comun", distractorWords: new[] { "Garza" })
            });

            var success = config.TryValidateForParticipantCount(3, out var errorMessage);

            Assert.That(success, Is.False);
            Assert.That(errorMessage, Does.Contain("No hay rondas utilizables"));
            Assert.That(errorMessage, Does.Contain("Falta asignar el AudioClip"));
        }

        [Test]
        public void TryValidateForParticipantCount_ReturnsTrue_WhenAtLeastOneRoundIsUsable()
        {
            var config = ScriptableObject.CreateInstance<AudioWordConsensusMinigameConfig>();
            SetPrivateField(config, "maxSupportedDevices", 6);
            SetPrivateField(config, "roundDefinitions", new List<AudioWordConsensusRoundDefinition>
            {
                CreateRoundDefinition(hasClip: true, correctWord: "Mirlo", distractorWords: new[] { "Gorrion", "Garza" }),
                CreateRoundDefinition(hasClip: false, correctWord: "Rana comun", distractorWords: new[] { "Mirlo" })
            });

            var success = config.TryValidateForParticipantCount(4, out var errorMessage);

            Assert.That(success, Is.True);
            Assert.That(errorMessage, Is.Empty);
            Assert.That(config.CountUsableRoundDefinitions(4), Is.EqualTo(1));
        }

        private static AudioWordConsensusRoundDefinition CreateRoundDefinition(bool hasClip, string correctWord, string[] distractorWords)
        {
            var definition = (AudioWordConsensusRoundDefinition)System.Activator.CreateInstance(typeof(AudioWordConsensusRoundDefinition), true);
            SetPrivateField(definition, "promptLabel", "Audio");
            SetPrivateField(definition, "soundClip", hasClip ? AudioClip.Create("Audio", 1, 1, 44100, false) : null);
            SetPrivateField(definition, "correctWord", correctWord);
            SetPrivateField(definition, "distractorWords", new List<string>(distractorWords));
            return definition;
        }

        private static void SetPrivateField(object instance, string fieldName, object value)
        {
            var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(instance, value);
        }
    }
}
