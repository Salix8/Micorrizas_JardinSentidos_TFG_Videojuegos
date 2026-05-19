using NUnit.Framework;
using SmartCampus.Dialogue;
using UnityEngine;

namespace SmartCampus.Testing.Editor.Core
{
    public sealed class DialogueAssetFallbackTests
    {
        [Test]
        public void CharacterPortraitDatabase_UnknownCharacter_UsesFallbackPortrait()
        {
            var database = ScriptableObject.CreateInstance<CharacterPortraitDatabase>();
            var fallbackTexture = new Texture2D(4, 4);
            var fallbackSprite = Sprite.Create(fallbackTexture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f));

            var serializedDatabase = new UnityEditor.SerializedObject(database);
            serializedDatabase.FindProperty("fallbackPortrait").objectReferenceValue = fallbackSprite;
            serializedDatabase.ApplyModifiedPropertiesWithoutUndo();

            Assert.That(database.GetPortraitOrFallback("UnknownCharacter"), Is.SameAs(fallbackSprite));
            Assert.That(database.GetDisplayNameOrFallback("UnknownCharacter"), Is.EqualTo("UnknownCharacter"));
            Assert.That(database.GetPortraitVisualPrefabOrNull("UnknownCharacter"), Is.Null);

            Object.DestroyImmediate(fallbackSprite);
            Object.DestroyImmediate(fallbackTexture);
            Object.DestroyImmediate(database);
        }

        [Test]
        public void CharacterPortraitDatabase_KnownCharacter_ReturnsConfiguredVisualPrefab()
        {
            var database = ScriptableObject.CreateInstance<CharacterPortraitDatabase>();
            var portraitVisualPrefab = new GameObject("PortraitVisualPrefab");
            var serializedDatabase = new UnityEditor.SerializedObject(database);
            var entries = serializedDatabase.FindProperty("entries");
            entries.arraySize = 1;
            var entry = entries.GetArrayElementAtIndex(0);
            entry.FindPropertyRelative("characterId").stringValue = "Deeproot";
            entry.FindPropertyRelative("displayName").stringValue = "Deeproot";
            entry.FindPropertyRelative("portraitVisualPrefab").objectReferenceValue = portraitVisualPrefab;
            serializedDatabase.ApplyModifiedPropertiesWithoutUndo();

            Assert.That(database.GetPortraitVisualPrefabOrNull("deeproot"), Is.SameAs(portraitVisualPrefab));

            Object.DestroyImmediate(portraitVisualPrefab);
            Object.DestroyImmediate(database);
        }

        [Test]
        public void DialogueAudioDatabase_UnknownLine_ReturnsNullClip()
        {
            var database = ScriptableObject.CreateInstance<DialogueAudioDatabase>();

            Assert.That(database.TryGetClip("DL_UNKNOWN", out var clip), Is.False);
            Assert.That(clip, Is.Null);
            Assert.That(database.GetClipOrNull("DL_UNKNOWN"), Is.Null);

            Object.DestroyImmediate(database);
        }
    }
}
