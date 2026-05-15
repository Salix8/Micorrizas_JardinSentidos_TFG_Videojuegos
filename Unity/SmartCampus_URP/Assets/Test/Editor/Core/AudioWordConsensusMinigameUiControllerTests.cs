using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SmartCampus.Coop.Minigames.AudioWordConsensus;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SmartCampus.Testing.Editor.Core
{
    public sealed class AudioWordConsensusMinigameUiControllerTests
    {
        private GameObject uiRoot;
        private GameObject sessionRoot;
        private AudioWordConsensusMinigameUIController controller;
        private RectTransform wordOptionsContainer;
        private Button playSoundButton;
        private TMP_Text playSoundButtonLabel;
        private AudioSource localAudioSource;

        [SetUp]
        public void SetUp()
        {
            uiRoot = new GameObject("AudioWordConsensusUiRoot", typeof(RectTransform));
            controller = uiRoot.AddComponent<AudioWordConsensusMinigameUIController>();
            controller.enabled = false;

            sessionRoot = new GameObject("AudioWordConsensusSession");
            var session = sessionRoot.AddComponent<AudioWordConsensusMinigameSession>();
            localAudioSource = sessionRoot.AddComponent<AudioSource>();

            wordOptionsContainer = new GameObject("WordOptionsContainer", typeof(RectTransform)).GetComponent<RectTransform>();
            wordOptionsContainer.SetParent(uiRoot.transform, false);

            var templateButton = CreateButton("TemplateButton", wordOptionsContainer);
            playSoundButton = CreateButton("PlaySoundButton", uiRoot.transform);
            playSoundButtonLabel = GetButtonLabel(playSoundButton);

            SetPrivateField(controller, "audioWordConsensusMinigameSession", session);
            SetPrivateField(controller, "localAudioSource", localAudioSource);
            SetPrivateField(controller, "playSoundButton", playSoundButton);
            SetPrivateField(controller, "playSoundButtonLabel", playSoundButtonLabel);
            SetPrivateField(controller, "wordOptionsContainer", wordOptionsContainer);
            SetPrivateField(controller, "wordOptionButtonTemplate", templateButton);
            SetPrivateField(controller, "submitWordButton", templateButton);
        }

        [TearDown]
        public void TearDown()
        {
            if (uiRoot != null)
            {
                Object.DestroyImmediate(uiRoot);
            }

            if (sessionRoot != null)
            {
                Object.DestroyImmediate(sessionRoot);
            }
        }

        [Test]
        public void RefreshWordOptionButtons_ReusesButtonPool_WhenButtonsHideAndReturn()
        {
            InvokeRefreshWordOptionButtons(isEmitter: false, new List<string> { "Mirlo", "Garza", "Gorrion" });

            AssertActiveButtonLabels("Mirlo", "Garza", "Gorrion");
            Assert.That(wordOptionsContainer.childCount, Is.EqualTo(4));
            AssertTemplateButtonIsHidden();

            InvokeRefreshWordOptionButtons(isEmitter: true, assignedWords: null);

            Assert.That(GetActiveButtons(), Is.Empty);
            Assert.That(wordOptionsContainer.childCount, Is.EqualTo(4));
            AssertTemplateButtonIsHidden();

            InvokeRefreshWordOptionButtons(isEmitter: false, new List<string> { "Rana", "Petirrojo" });

            AssertActiveButtonLabels("Rana", "Petirrojo");
            Assert.That(wordOptionsContainer.childCount, Is.EqualTo(4));
            AssertTemplateButtonIsHidden();
        }

        [Test]
        public void RefreshWordOptionButtons_UpdatesExistingButtonsWithNewRoundLabels()
        {
            InvokeRefreshWordOptionButtons(isEmitter: false, new List<string> { "A", "B" });
            InvokeRefreshWordOptionButtons(isEmitter: false, new List<string> { "C", "D" });

            AssertActiveButtonLabels("C", "D");
            AssertTemplateButtonIsHidden();
        }

        [Test]
        public void RefreshWordOptionButtons_AppliesVisibleLayoutContractToButtonsAndLabels()
        {
            InvokeRefreshWordOptionButtons(isEmitter: false, new List<string> { "Mirlo" });

            var activeButton = GetActiveButtons().Single();
            var layoutElement = activeButton.GetComponent<LayoutElement>();
            var label = GetButtonLabel(activeButton);
            var labelRectTransform = label.rectTransform;

            Assert.That(layoutElement, Is.Not.Null);
            Assert.That(layoutElement.preferredHeight, Is.EqualTo(76f));
            Assert.That(layoutElement.minHeight, Is.EqualTo(76f));
            Assert.That(labelRectTransform.offsetMin, Is.EqualTo(new Vector2(18f, 10f)));
            Assert.That(labelRectTransform.offsetMax, Is.EqualTo(new Vector2(-18f, -10f)));
        }

        [Test]
        public void RefreshWordOptionButtons_EnsuresListContainerContract_WhenSceneContainerIsBare()
        {
            InvokeRefreshWordOptionButtons(isEmitter: false, new List<string> { "Mirlo", "Garza" });

            var layoutElement = wordOptionsContainer.GetComponent<LayoutElement>();
            var layoutGroup = wordOptionsContainer.GetComponent<VerticalLayoutGroup>();
            var fitter = wordOptionsContainer.GetComponent<ContentSizeFitter>();

            Assert.That(layoutElement, Is.Not.Null);
            Assert.That(layoutElement.preferredHeight, Is.EqualTo(200f));
            Assert.That(layoutGroup, Is.Not.Null);
            Assert.That(layoutGroup.spacing, Is.EqualTo(12f));
            Assert.That(fitter, Is.Not.Null);
            Assert.That(fitter.verticalFit, Is.EqualTo(ContentSizeFitter.FitMode.PreferredSize));
        }

        [Test]
        public void EnsureRestartSoundButton_CreatesRuntimeButton_WhenSceneReferenceIsMissing()
        {
            InvokePrivateMethod("EnsureRestartSoundButton");

            var restartButton = GetPrivateField<Button>(controller, "restartSoundButton");
            var restartLabel = GetPrivateField<TMP_Text>(controller, "restartSoundButtonLabel");

            Assert.That(restartButton, Is.Not.Null);
            Assert.That(restartButton.name, Is.EqualTo("RestartSoundButton"));
            Assert.That(restartButton.transform.parent, Is.EqualTo(playSoundButton.transform.parent));
            Assert.That(restartLabel, Is.Not.Null);
        }

        [Test]
        public void BuildPlayPauseButtonLabel_ReturnsContinue_WhenPlaybackWasPaused()
        {
            SetPrivateField(controller, "localPlaybackPaused", true);
            var roundDefinition = CreateRoundDefinition("Mirlo");
            var config = ScriptableObject.CreateInstance<AudioWordConsensusMinigameConfig>();
            SetPrivateField(config, "missingAudioClipLabel", "Falta sonido");

            var label = (string)InvokePrivateMethod("BuildPlayPauseButtonLabel", roundDefinition, config);

            Assert.That(label, Is.EqualTo("Continuar sonido"));
        }

        private void InvokeRefreshWordOptionButtons(bool isEmitter, IReadOnlyList<string> assignedWords)
        {
            InvokePrivateMethod("RefreshWordOptionButtons", isEmitter, assignedWords);
        }

        private void AssertActiveButtonLabels(params string[] expectedLabels)
        {
            var activeLabels = GetActiveButtons()
                .Select(GetButtonLabel)
                .Select(label => label == null ? string.Empty : label.text)
                .ToArray();

            Assert.That(activeLabels, Is.EqualTo(expectedLabels));
        }

        private List<Button> GetActiveButtons()
        {
            var buttons = new List<Button>();
            for (var index = 0; index < wordOptionsContainer.childCount; index++)
            {
                var button = wordOptionsContainer.GetChild(index).GetComponent<Button>();
                if (button != null && button.gameObject.activeSelf)
                {
                    buttons.Add(button);
                }
            }

            return buttons;
        }

        private void AssertTemplateButtonIsHidden()
        {
            var templateButton = GetPrivateField<Button>(controller, "wordOptionButtonTemplate");
            Assert.That(templateButton, Is.Not.Null);
            Assert.That(templateButton.gameObject.activeSelf, Is.False);
        }

        private static Button CreateButton(string name, Transform parent)
        {
            var buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            buttonObject.transform.SetParent(parent, false);

            var button = buttonObject.GetComponent<Button>();
            button.targetGraphic = buttonObject.GetComponent<Image>();

            var labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(buttonObject.transform, false);

            return button;
        }

        private static TMP_Text GetButtonLabel(Button button)
        {
            return button == null ? null : button.GetComponentInChildren<TMP_Text>(true);
        }

        private static void SetPrivateField(object instance, string fieldName, object value)
        {
            var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Field '{fieldName}' was not found.");
            field.SetValue(instance, value);
        }

        private static T GetPrivateField<T>(object instance, string fieldName) where T : class
        {
            var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Field '{fieldName}' was not found.");
            return field.GetValue(instance) as T;
        }

        private object InvokePrivateMethod(string methodName, params object[] arguments)
        {
            var method = typeof(AudioWordConsensusMinigameUIController).GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(method, Is.Not.Null, $"Method '{methodName}' was not found.");
            return method.Invoke(controller, arguments);
        }

        private static AudioWordConsensusRoundDefinition CreateRoundDefinition(string correctWord)
        {
            var definition = (AudioWordConsensusRoundDefinition)System.Activator.CreateInstance(typeof(AudioWordConsensusRoundDefinition), true);
            SetPrivateField(definition, "promptLabel", "Audio");
            SetPrivateField(definition, "soundClip", AudioClip.Create("Audio", 1, 1, 44100, false));
            SetPrivateField(definition, "correctWord", correctWord);
            SetPrivateField(definition, "distractorWords", new List<string> { "Hoja" });
            return definition;
        }
    }
}
