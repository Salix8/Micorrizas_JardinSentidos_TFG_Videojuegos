using NUnit.Framework;
using SmartCampus.Coop.Minigames.DistributedPairs;
using UnityEditor;
using UnityEngine;

namespace SmartCampus.Testing.Editor.Core
{
    public sealed class DistributedPairsScoreServiceTests
    {
        [Test]
        public void PerfectRun_ReturnsMaximumScore()
        {
            var config = CreateConfig(pairCount: 10);

            var result = DistributedPairsScoreService.CreateResult(config, matchedPairs: 10, failedAttempts: 0);

            Assert.That(result.ScoreOutOfTen, Is.EqualTo(10f));
        }

        [Test]
        public void AdditionalErrors_ReduceTheFinalScore()
        {
            var config = CreateConfig(pairCount: 10);

            var cleanResult = DistributedPairsScoreService.CreateResult(config, matchedPairs: 10, failedAttempts: 0);
            var noisyResult = DistributedPairsScoreService.CreateResult(config, matchedPairs: 10, failedAttempts: 8);

            Assert.That(noisyResult.ScoreOutOfTen, Is.LessThan(cleanResult.ScoreOutOfTen));
            Assert.That(noisyResult.ScoreOutOfTen, Is.GreaterThanOrEqualTo(0f));
        }

        private static DistributedPairsMinigameConfig CreateConfig(int pairCount)
        {
            var config = ScriptableObject.CreateInstance<DistributedPairsMinigameConfig>();
            var serializedObject = new SerializedObject(config);

            serializedObject.FindProperty("pairsToUse").intValue = pairCount;
            serializedObject.FindProperty("pairDefinitions").arraySize = pairCount;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            return config;
        }
    }
}
