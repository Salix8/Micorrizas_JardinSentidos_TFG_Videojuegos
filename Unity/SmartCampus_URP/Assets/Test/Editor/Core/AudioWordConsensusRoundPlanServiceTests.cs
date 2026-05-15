using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SmartCampus.Coop.Minigames.AudioWordConsensus;
using UnityEngine;

namespace SmartCampus.Testing.Editor.Core
{
    public sealed class AudioWordConsensusRoundPlanServiceTests
    {
        [Test]
        public void TryBuildRoundPlan_ThreePlayersWithSixSounds_PlansSixUniqueRounds()
        {
            var participants = new ulong[] { 11UL, 22UL, 33UL };
            var roundDefinitions = BuildRoundDefinitions(6);

            var success = AudioWordConsensusRoundPlanService.TryBuildRoundPlan(
                participants,
                roundDefinitions,
                maxRoundCount: 6,
                randomSeed: 7,
                out var plannedRounds,
                out var errorMessage);

            Assert.That(success, Is.True, errorMessage);
            Assert.That(plannedRounds.Count, Is.EqualTo(6));
            Assert.That(plannedRounds.Select(round => round.RoundDefinitionIndex).Distinct().Count(), Is.EqualTo(6));
            for (var index = 1; index < plannedRounds.Count; index++)
            {
                Assert.That(plannedRounds[index].EmitterClientId, Is.Not.EqualTo(plannedRounds[index - 1].EmitterClientId));
            }
        }

        [Test]
        public void TryBuildRoundPlan_SevenPlayersWithSixSounds_PlansAtMostSixRounds()
        {
            var participants = new ulong[] { 1UL, 2UL, 3UL, 4UL, 5UL, 6UL, 7UL };
            var roundDefinitions = BuildRoundDefinitions(6);

            var success = AudioWordConsensusRoundPlanService.TryBuildRoundPlan(
                participants,
                roundDefinitions,
                maxRoundCount: 6,
                randomSeed: 3,
                out var plannedRounds,
                out var errorMessage);

            Assert.That(success, Is.True, errorMessage);
            Assert.That(plannedRounds.Count, Is.EqualTo(6));
            Assert.That(plannedRounds.Select(round => round.RoundDefinitionIndex).Distinct().Count(), Is.EqualTo(6));
            for (var index = 1; index < plannedRounds.Count; index++)
            {
                Assert.That(plannedRounds[index].EmitterClientId, Is.Not.EqualTo(plannedRounds[index - 1].EmitterClientId));
            }
        }

        [Test]
        public void TryBuildRoundPlan_TwoPlayers_AlternatesEmitter()
        {
            var participants = new ulong[] { 10UL, 20UL };
            var roundDefinitions = BuildRoundDefinitions(4);

            var success = AudioWordConsensusRoundPlanService.TryBuildRoundPlan(
                participants,
                roundDefinitions,
                maxRoundCount: 6,
                randomSeed: 17,
                out var plannedRounds,
                out var errorMessage);

            Assert.That(success, Is.True, errorMessage);
            Assert.That(plannedRounds.Count, Is.EqualTo(4));
            for (var index = 1; index < plannedRounds.Count; index++)
            {
                Assert.That(plannedRounds[index].EmitterClientId, Is.Not.EqualTo(plannedRounds[index - 1].EmitterClientId));
            }
        }

        [Test]
        public void TryBuildRoundPlan_IgnoresInvalidRoundsAndFailsWhenNoneRemain()
        {
            var participants = new ulong[] { 1UL, 2UL, 3UL };
            var roundsWithOneValid = new List<AudioWordConsensusRoundDefinition>
            {
                CreateRoundDefinition("Audio 1", hasClip: true, correctWord: "Campana", distractorWords: new []{ "Hoja" }),
                CreateRoundDefinition("Audio 2", hasClip: false, correctWord: "Agua", distractorWords: new []{ "Piedra" })
            };

            var success = AudioWordConsensusRoundPlanService.TryBuildRoundPlan(
                participants,
                roundsWithOneValid,
                maxRoundCount: 6,
                randomSeed: 1,
                out var plannedRounds,
                out var errorMessage);

            Assert.That(success, Is.True, errorMessage);
            Assert.That(plannedRounds.Count, Is.EqualTo(1));

            var invalidOnlyRounds = new List<AudioWordConsensusRoundDefinition>
            {
                CreateRoundDefinition("Audio invalido", hasClip: false, correctWord: "Campana", distractorWords: new []{ "Hoja" })
            };

            success = AudioWordConsensusRoundPlanService.TryBuildRoundPlan(
                participants,
                invalidOnlyRounds,
                maxRoundCount: 6,
                randomSeed: 1,
                out plannedRounds,
                out errorMessage);

            Assert.That(success, Is.False);
            Assert.That(errorMessage, Does.Contain("No hay sonidos configurados"));
        }

        private static List<AudioWordConsensusRoundDefinition> BuildRoundDefinitions(int count)
        {
            var results = new List<AudioWordConsensusRoundDefinition>(count);
            for (var index = 0; index < count; index++)
            {
                results.Add(CreateRoundDefinition($"Audio {index + 1}", hasClip: true, correctWord: $"Correcta {index + 1}", distractorWords: new[] { $"Distractor {index + 1}" }));
            }

            return results;
        }

        private static AudioWordConsensusRoundDefinition CreateRoundDefinition(string promptLabel, bool hasClip, string correctWord, string[] distractorWords)
        {
            var definition = (AudioWordConsensusRoundDefinition)System.Activator.CreateInstance(typeof(AudioWordConsensusRoundDefinition), true);
            var clip = hasClip ? AudioClip.Create(promptLabel, 1, 1, 44100, false) : null;

            SetPrivateField(definition, "promptLabel", promptLabel);
            SetPrivateField(definition, "soundClip", clip);
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
