using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using SmartCampus.Coop.Minigames.CollaborativePlantGuess;
using UnityEngine;

namespace SmartCampus.Testing.Editor.Core
{
    public sealed class CollaborativePlantGuessMinigameConfigTests
    {
        private Texture2D texture;
        private Sprite sprite;

        [SetUp]
        public void SetUp()
        {
            texture = new Texture2D(8, 8);
            sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f));
        }

        [TearDown]
        public void TearDown()
        {
            if (sprite != null)
            {
                Object.DestroyImmediate(sprite);
            }

            if (texture != null)
            {
                Object.DestroyImmediate(texture);
            }
        }

        [Test]
        public void ApplyInspectorImages_AssignsSpriteByPlantId()
        {
            var config = ScriptableObject.CreateInstance<CollaborativePlantGuessMinigameConfig>();
            var definitions = new List<CollaborativePlantGuessPlantDefinition>
            {
                new(
                    "encina",
                    "Encina",
                    "Quercus ilex",
                    new string[0],
                    string.Empty,
                    "Arbol",
                    "Rugosa",
                    "Perenne",
                    "Coriacea",
                    "Seco",
                    "Bellota")
            };

            SetPrivateField(
                config,
                "plantImages",
                new List<CollaborativePlantGuessImageEntry>
                {
                    CreateImageEntry("encina", sprite)
                });

            config.ApplyInspectorImages(definitions);

            Assert.That(definitions[0].InspectorSprite, Is.EqualTo(sprite));

            Object.DestroyImmediate(config);
        }

        private static CollaborativePlantGuessImageEntry CreateImageEntry(string plantId, Sprite assignedSprite)
        {
            var entry = new CollaborativePlantGuessImageEntry();
            SetPrivateField(entry, "plantId", plantId);
            SetPrivateField(entry, "image", assignedSprite);
            return entry;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing field '{fieldName}' on {target.GetType().Name}.");
            field.SetValue(target, value);
        }
    }
}
