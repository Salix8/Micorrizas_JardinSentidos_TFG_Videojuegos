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

            Assert.That(minigameTitle.text, Is.EqualTo("MINIJUEGO: MEMORIA DE FRUTOS"));
            Assert.That(progressPercent.text, Is.EqualTo("45%"));
            Assert.That(teamName.text, Is.EqualTo("SALA ABCD"));
            Assert.That(slider.value, Is.EqualTo(0.45f).Within(0.001f));

            Object.DestroyImmediate(root);
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
    }
}
