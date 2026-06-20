using NUnit.Framework;
using SmartCampus.Coop.Minigames;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SmartCampus.Testing.Editor.Core
{
    public sealed class CoopMinigameSharedPanelViewTests
    {
        [Test]
        public void TopPanelBind_FormatsProgressTitleAndTeamFallback()
        {
            var theme = ScriptableObject.CreateInstance<CoopMinigameThemeConfig>();
            var root = new GameObject("TopPanel", typeof(RectTransform), typeof(CoopMinigameTopPanelView));
            var view = root.GetComponent<CoopMinigameTopPanelView>();
            var minigameTitle = CreateLabel(root.transform, "MinigameTitle");
            var progressPercent = CreateLabel(root.transform, "ProgressPercent");
            var teamName = CreateLabel(root.transform, "TeamName");
            var slider = root.AddComponent<Slider>();

            var serializedView = new SerializedObject(view);
            Assign(serializedView, "themeConfig", theme);
            Assign(serializedView, "minigameTitleLabel", minigameTitle);
            Assign(serializedView, "progressPercentLabel", progressPercent);
            Assign(serializedView, "teamNameLabel", teamName);
            Assign(serializedView, "progressSlider", slider);
            serializedView.ApplyModifiedPropertiesWithoutUndo();

            view.Bind("MEMORIA DE FRUTOS", 0.45f, string.Empty, "ABCD");

            Assert.That(minigameTitle.text, Is.EqualTo("MISIÓN: MEMORIA DE FRUTOS"));
            Assert.That(progressPercent.text, Is.EqualTo("45%"));
            Assert.That(teamName.text, Is.EqualTo("SALA ABCD"));
            Assert.That(slider.value, Is.EqualTo(0.45f).Within(0.001f));

            Object.DestroyImmediate(root);
            Object.DestroyImmediate(theme);
        }

        [Test]
        public void TopPanelBind_UsesSceneAuthoredTitlePrefixBeforeThemeFallback()
        {
            var theme = ScriptableObject.CreateInstance<CoopMinigameThemeConfig>();
            var root = new GameObject("TopPanel", typeof(RectTransform), typeof(CoopMinigameTopPanelView));
            var view = root.GetComponent<CoopMinigameTopPanelView>();
            var minigameTitle = CreateLabel(root.transform, "MinigameTitle");
            minigameTitle.text = "QUEST:";

            var serializedView = new SerializedObject(view);
            Assign(serializedView, "themeConfig", theme);
            Assign(serializedView, "minigameTitleLabel", minigameTitle);
            serializedView.ApplyModifiedPropertiesWithoutUndo();

            view.Bind("MEMORIA DE FRUTOS", 0.45f, string.Empty, "ABCD");

            Assert.That(minigameTitle.text, Is.EqualTo("QUEST: MEMORIA DE FRUTOS"));

            Object.DestroyImmediate(root);
            Object.DestroyImmediate(theme);
        }

        [Test]
        public void TopPanelBind_UsesThemePrefixWhenNoSceneTitlePrefixExists()
        {
            var theme = ScriptableObject.CreateInstance<CoopMinigameThemeConfig>();
            var root = new GameObject("TopPanel", typeof(RectTransform), typeof(CoopMinigameTopPanelView));
            var view = root.GetComponent<CoopMinigameTopPanelView>();
            var minigameTitle = CreateLabel(root.transform, "MinigameTitle");

            var serializedView = new SerializedObject(view);
            Assign(serializedView, "themeConfig", theme);
            Assign(serializedView, "minigameTitleLabel", minigameTitle);
            serializedView.ApplyModifiedPropertiesWithoutUndo();

            view.Bind("MEMORIA DE FRUTOS", 0.45f, string.Empty, "ABCD");

            Assert.That(minigameTitle.text, Is.EqualTo("MISIÓN: MEMORIA DE FRUTOS"));

            Object.DestroyImmediate(root);
            Object.DestroyImmediate(theme);
        }

        [Test]
        public void TopPanelBind_UsesUpdatedAuthoredPrefixWhenLabelChangesInEditor()
        {
            var theme = ScriptableObject.CreateInstance<CoopMinigameThemeConfig>();
            var root = new GameObject("TopPanel", typeof(RectTransform), typeof(CoopMinigameTopPanelView));
            var view = root.GetComponent<CoopMinigameTopPanelView>();
            var minigameTitle = CreateLabel(root.transform, "MinigameTitle");
            minigameTitle.text = "MI";

            var serializedView = new SerializedObject(view);
            Assign(serializedView, "themeConfig", theme);
            Assign(serializedView, "minigameTitleLabel", minigameTitle);
            serializedView.ApplyModifiedPropertiesWithoutUndo();

            view.ApplyTheme();
            minigameTitle.text = "MISIÓN:";
            typeof(CoopMinigameTopPanelView)
                .GetMethod("OnValidate", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.Invoke(view, null);
            view.Bind("MEMORIA DE FRUTOS", 0.45f, string.Empty, "ABCD");

            Assert.That(minigameTitle.text, Is.EqualTo("MISIÓN: MEMORIA DE FRUTOS"));

            Object.DestroyImmediate(root);
            Object.DestroyImmediate(theme);
        }

        [Test]
        public void TopPanelApplyTheme_LogoOverrideWinsOverThemeLogo()
        {
            var theme = ScriptableObject.CreateInstance<CoopMinigameThemeConfig>();
            var themeSprite = CreateSprite("ThemeLogo");
            var overrideSprite = CreateSprite("OverrideLogo");
            var root = new GameObject("TopPanel", typeof(RectTransform), typeof(CoopMinigameTopPanelView));
            var view = root.GetComponent<CoopMinigameTopPanelView>();
            var logoObject = new GameObject("Logo", typeof(RectTransform), typeof(Image));
            logoObject.transform.SetParent(root.transform, false);
            var logoImage = logoObject.GetComponent<Image>();

            var serializedTheme = new SerializedObject(theme);
            Assign(serializedTheme, "logoSprite", themeSprite);
            serializedTheme.ApplyModifiedPropertiesWithoutUndo();

            var serializedView = new SerializedObject(view);
            Assign(serializedView, "themeConfig", theme);
            Assign(serializedView, "logoImage", logoImage);
            Assign(serializedView, "logoOverrideSprite", overrideSprite);
            serializedView.ApplyModifiedPropertiesWithoutUndo();

            view.ApplyTheme();

            Assert.That(logoImage.sprite, Is.EqualTo(overrideSprite));
            Assert.That(logoImage.enabled, Is.True);

            Object.DestroyImmediate(root);
            Object.DestroyImmediate(themeSprite.texture);
            Object.DestroyImmediate(themeSprite);
            Object.DestroyImmediate(overrideSprite.texture);
            Object.DestroyImmediate(overrideSprite);
            Object.DestroyImmediate(theme);
        }

        [Test]
        public void TopPanelApplyTheme_HideLogoDisablesLogoImage()
        {
            var theme = ScriptableObject.CreateInstance<CoopMinigameThemeConfig>();
            var themeSprite = CreateSprite("ThemeLogo");
            var root = new GameObject("TopPanel", typeof(RectTransform), typeof(CoopMinigameTopPanelView));
            var view = root.GetComponent<CoopMinigameTopPanelView>();
            var logoObject = new GameObject("Logo", typeof(RectTransform), typeof(Image));
            logoObject.transform.SetParent(root.transform, false);
            var logoImage = logoObject.GetComponent<Image>();

            var serializedTheme = new SerializedObject(theme);
            Assign(serializedTheme, "logoSprite", themeSprite);
            serializedTheme.ApplyModifiedPropertiesWithoutUndo();

            var serializedView = new SerializedObject(view);
            Assign(serializedView, "themeConfig", theme);
            Assign(serializedView, "logoImage", logoImage);
            serializedView.FindProperty("hideLogo").boolValue = true;
            serializedView.ApplyModifiedPropertiesWithoutUndo();

            view.ApplyTheme();

            Assert.That(logoImage.sprite, Is.EqualTo(themeSprite));
            Assert.That(logoImage.enabled, Is.False);

            Object.DestroyImmediate(root);
            Object.DestroyImmediate(themeSprite.texture);
            Object.DestroyImmediate(themeSprite);
            Object.DestroyImmediate(theme);
        }

        [Test]
        public void BottomPanelBind_FormatsTimerPenaltyAndDefaultInstruction()
        {
            var theme = ScriptableObject.CreateInstance<CoopMinigameThemeConfig>();
            var root = new GameObject("BottomPanel", typeof(RectTransform), typeof(CoopMinigameBottomPanelView));
            var view = root.GetComponent<CoopMinigameBottomPanelView>();
            var instructionTitle = CreateLabel(root.transform, "InstructionTitle");
            var instructionBody = CreateLabel(root.transform, "InstructionBody");
            var timeValue = CreateLabel(root.transform, "TimeValue");
            var penaltyValue = CreateLabel(root.transform, "PenaltyValue");
            var slider = root.AddComponent<Slider>();

            var serializedView = new SerializedObject(view);
            Assign(serializedView, "themeConfig", theme);
            Assign(serializedView, "instructionTitleLabel", instructionTitle);
            Assign(serializedView, "instructionBodyLabel", instructionBody);
            Assign(serializedView, "timeValueLabel", timeValue);
            Assign(serializedView, "penaltyValueLabel", penaltyValue);
            Assign(serializedView, "timeSlider", slider);
            serializedView.ApplyModifiedPropertiesWithoutUndo();

            view.Bind(string.Empty, string.Empty, 80f, 100f, 10f);

            Assert.That(instructionTitle.text, Is.EqualTo("DE ACUERDO EN EQUIPO"));
            Assert.That(instructionBody.text, Is.EqualTo("Cuando todos hayais terminado, se contara el resultado."));
            Assert.That(timeValue.text, Is.EqualTo("01:20"));
            Assert.That(penaltyValue.text, Is.EqualTo("-10s"));
            Assert.That(slider.value, Is.EqualTo(0.8f).Within(0.001f));

            Object.DestroyImmediate(root);
            Object.DestroyImmediate(theme);
        }

        private static TMP_Text CreateLabel(Transform parent, string name)
        {
            var labelObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(parent, false);
            return labelObject.GetComponent<TMP_Text>();
        }

        private static void Assign(SerializedObject serializedObject, string propertyName, Object value)
        {
            serializedObject.FindProperty(propertyName).objectReferenceValue = value;
        }

        private static Sprite CreateSprite(string name)
        {
            var texture = new Texture2D(2, 2)
            {
                name = $"{name}Texture"
            };

            texture.SetPixel(0, 0, Color.white);
            texture.SetPixel(1, 0, Color.white);
            texture.SetPixel(0, 1, Color.white);
            texture.SetPixel(1, 1, Color.white);
            texture.Apply();

            return Sprite.Create(texture, new Rect(0f, 0f, 2f, 2f), new Vector2(0.5f, 0.5f));
        }
    }
}
