using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SmartCampus.Coop.Minigames.DistributedPairs;
using UnityEngine;

namespace SmartCampus.Testing.Editor.Core
{
    public sealed class DistributedPairsMinigameSessionTests
    {
        [Test]
        public void CreateInitialDealPlan_WithTwoDevices_LeavesResolvableVisiblePair()
        {
            var config = CreateConfig(cardsPerDevice: 4, pairsToUse: 10, guaranteedVisiblePairsOffset: 1);
            var participantIds = new ulong[] { 1UL, 2UL };

            var method = typeof(DistributedPairsMinigameSession).GetMethod(
                "CreateInitialDealPlan",
                BindingFlags.NonPublic | BindingFlags.Static);

            Assert.That(method, Is.Not.Null);

            var plan = method.Invoke(null, new object[] { participantIds, config, 42 });
            var deckCards = ReadProperty<IReadOnlyList<DistributedPairsCardModel>>(plan, "DeckCards");
            var assignments = ReadProperty<IReadOnlyDictionary<int, ulong>>(plan, "Assignments");

            Assert.That(assignments.Count, Is.EqualTo(8));
            Assert.That(CountVisiblePairs(assignments, deckCards), Is.GreaterThanOrEqualTo(1));

            UnityEngine.Object.DestroyImmediate(config);
        }

        private static T ReadProperty<T>(object target, string propertyName)
        {
            var property = target.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            Assert.That(property, Is.Not.Null);
            return (T)property.GetValue(target);
        }

        private static DistributedPairsMinigameConfig CreateConfig(
            int cardsPerDevice,
            int pairsToUse,
            int guaranteedVisiblePairsOffset)
        {
            var config = ScriptableObject.CreateInstance<DistributedPairsMinigameConfig>();
            SetPrivateField(config, "cardsPerDevice", cardsPerDevice);
            SetPrivateField(config, "pairsToUse", pairsToUse);
            SetPrivateField(config, "guaranteedVisiblePairsOffset", guaranteedVisiblePairsOffset);
            SetPrivateField(
                config,
                "pairDefinitions",
                Enumerable.Range(0, pairsToUse)
                    .Select(_ => new DistributedPairDefinition())
                    .ToList());

            return config;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(field, Is.Not.Null, $"Missing field '{fieldName}' on {target.GetType().Name}.");
            field.SetValue(target, value);
        }

        private static int CountVisiblePairs(
            IReadOnlyDictionary<int, ulong> assignments,
            IReadOnlyList<DistributedPairsCardModel> cards)
        {
            var cardsById = cards.ToDictionary(card => card.CardInstanceId, card => card.PairId);
            return assignments
                .GroupBy(entry => cardsById[entry.Key])
                .Count(group => group.Select(entry => entry.Value).Distinct().Count() >= 2);
        }
    }
}
