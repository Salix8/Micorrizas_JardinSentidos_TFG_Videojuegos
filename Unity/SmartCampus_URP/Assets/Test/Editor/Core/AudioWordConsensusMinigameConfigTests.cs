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

        [Test]
        public void TryValidateForParticipantCount_ReturnsFalse_WhenRoundHasNoIncorrectOption()
        {
            var config = ScriptableObject.CreateInstance<AudioWordConsensusMinigameConfig>();
            SetPrivateField(config, "maxSupportedDevices", 6);
            SetPrivateField(config, "roundDefinitions", new List<AudioWordConsensusRoundDefinition>
            {
                CreateRoundDefinition(hasClip: true, correctWord: "Mirlo", distractorWords: new[] { "Mirlo", " mirlo " })
            });

            var success = config.TryValidateForParticipantCount(2, out var errorMessage);

            Assert.That(success, Is.False);
            Assert.That(errorMessage, Does.Contain("Cada sonido necesita al menos una respuesta incorrecta"));
        }

        [Test]
        public void GetRevealStageImage_FallsBackToLastAvailableConfiguredSprite()
        {
            var firstStageTexture = new Texture2D(4, 4);
            var thirdStageTexture = new Texture2D(4, 4);
            var firstStageSprite = Sprite.Create(firstStageTexture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f));
            var thirdStageSprite = Sprite.Create(thirdStageTexture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f));
            var definition = CreateRoundDefinition(
                hasClip: true,
                correctWord: "Mirlo",
                distractorWords: new[] { "Garza" },
                revealStageImages: new List<Sprite> { firstStageSprite, null, thirdStageSprite, null });

            Assert.That(definition.RevealStageImages.Count, Is.EqualTo(4));
            Assert.That(definition.GetRevealStageImage(0), Is.SameAs(firstStageSprite));
            Assert.That(definition.GetRevealStageImage(1), Is.SameAs(firstStageSprite));
            Assert.That(definition.GetRevealStageImage(2), Is.SameAs(thirdStageSprite));
            Assert.That(definition.GetRevealStageImage(3), Is.SameAs(thirdStageSprite));

            Object.DestroyImmediate(firstStageSprite);
            Object.DestroyImmediate(thirdStageSprite);
            Object.DestroyImmediate(firstStageTexture);
            Object.DestroyImmediate(thirdStageTexture);
        }

        private static AudioWordConsensusRoundDefinition CreateRoundDefinition(
            bool hasClip,
            string correctWord,
            string[] distractorWords,
            List<Sprite> revealStageImages = null)
        {
            var definition = (AudioWordConsensusRoundDefinition)System.Activator.CreateInstance(typeof(AudioWordConsensusRoundDefinition), true);
            SetPrivateField(definition, "promptLabel", "Audio");
            SetPrivateField(definition, "soundClip", hasClip ? AudioClip.Create("Audio", 1, 1, 44100, false) : null);
            SetPrivateField(definition, "correctWord", correctWord);
            SetPrivateField(definition, "revealStageImages", revealStageImages ?? new List<Sprite>(AudioWordConsensusMinigameConfig.DefaultRevealStageCount));
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
