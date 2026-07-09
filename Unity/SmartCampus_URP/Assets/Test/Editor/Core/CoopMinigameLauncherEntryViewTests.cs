using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using SmartCampus.Coop.Minigames;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SmartCampus.Testing.Editor.Core
{
    public sealed class CoopMinigameLauncherEntryViewTests
    {
        private GameObject root;

        [TearDown]
        public void TearDown()
        {
            if (root != null)
            {
                Object.DestroyImmediate(root);
                root = null;
            }
        }

        [Test]
        public void Bind_WithPreserveAuthoredText_DoesNotOverwriteSceneText()
        {
            var view = CreateEntryView(
                "Texto de escena",
                "Descripcion de escena",
                "Boton de escena",
                out var titleLabel,
                out var descriptionLabel,
                out var buttonLabel,
                out _);

            view.Bind(
                "Texto de catalogo",
                "Descripcion de catalogo",
                true,
                null,
                "Boton de catalogo",
                preserveAuthoredText: true);

            Assert.That(titleLabel.text, Is.EqualTo("Texto de escena"));
            Assert.That(descriptionLabel.text, Is.EqualTo("Descripcion de escena"));
            Assert.That(buttonLabel.text, Is.EqualTo("Boton de escena"));
        }

        [Test]
        public void Bind_WithPreserveAuthoredText_StillConfiguresButtonInteraction()
        {
            var view = CreateEntryView(
                "Texto de escena",
                "Descripcion de escena",
                "Boton de escena",
                out _,
                out _,
                out _,
                out var launchButton);
            var clickCount = 0;

            view.Bind(
                "Texto de catalogo",
                "Descripcion de catalogo",
                true,
                () => clickCount++,
                "Boton de catalogo",
                preserveAuthoredText: true);

            launchButton.onClick.Invoke();

            Assert.That(launchButton.interactable, Is.True);
            Assert.That(clickCount, Is.EqualTo(1));
        }

        [Test]
        public void LauncherBuildEntries_WithSceneAuthoredActiveTemplate_DoesNotDisableEntry()
        {
            var controllerObject = new GameObject("Launcher", typeof(CoopMinigameLauncherUIController));
            var entryRoot = new GameObject("EntriesContent", typeof(RectTransform));
            entryRoot.transform.SetParent(controllerObject.transform, false);

            var entryView = CreateEntryView(
                "Vista",
                "Texto de escena",
                "Abrir",
                out _,
                out _,
                out _,
                out _);
            entryView.transform.SetParent(entryRoot.transform, false);
            entryView.gameObject.SetActive(true);
            root = controllerObject;

            var catalog = CreateCatalogWithSingleEntry();
            var controller = controllerObject.GetComponent<CoopMinigameLauncherUIController>();
            SetSerializedField(controller, "entryRoot", entryRoot.transform);
            SetSerializedField(controller, "entryTemplate", entryView);
            SetSerializedField(controller, "minigameCatalogConfig", catalog);
            SetSerializedField(controller, "useSceneAuthoredEntries", true);
            SetSerializedField(controller, "preserveSceneAuthoredEntryText", true);
            SetSerializedField(controller, "instantiateMissingCatalogEntries", false);

            InvokePrivateMethod(controller, "BuildEntries");

            Assert.That(entryView.gameObject.activeSelf, Is.True);
        }

        private CoopMinigameLauncherEntryView CreateEntryView(
            string title,
            string description,
            string buttonText,
            out TMP_Text titleLabel,
            out TMP_Text descriptionLabel,
            out TMP_Text buttonLabel,
            out Button launchButton)
        {
            root = new GameObject("Entry", typeof(RectTransform), typeof(CoopMinigameLauncherEntryView));
            var titleObject = new GameObject("TitleLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
            var descriptionObject = new GameObject("DescriptionLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
            var buttonObject = new GameObject("LaunchButton", typeof(RectTransform), typeof(Button));
            var buttonLabelObject = new GameObject("ButtonLabel", typeof(RectTransform), typeof(TextMeshProUGUI));

            titleObject.transform.SetParent(root.transform, false);
            descriptionObject.transform.SetParent(root.transform, false);
            buttonObject.transform.SetParent(root.transform, false);
            buttonLabelObject.transform.SetParent(buttonObject.transform, false);

            titleLabel = titleObject.GetComponent<TMP_Text>();
            descriptionLabel = descriptionObject.GetComponent<TMP_Text>();
            launchButton = buttonObject.GetComponent<Button>();
            buttonLabel = buttonLabelObject.GetComponent<TMP_Text>();

            titleLabel.text = title;
            descriptionLabel.text = description;
            buttonLabel.text = buttonText;

            var view = root.GetComponent<CoopMinigameLauncherEntryView>();
            SetSerializedField(view, "titleLabel", titleLabel);
            SetSerializedField(view, "descriptionLabel", descriptionLabel);
            SetSerializedField(view, "launchButton", launchButton);
            SetSerializedField(view, "buttonLabel", buttonLabel);

            return view;
        }

        private static void SetSerializedField<TValue>(object target, string fieldName, TValue value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"No existe el campo serializado '{fieldName}'.");
            field.SetValue(target, value);
        }

        private static CoopMinigameCatalogConfig CreateCatalogWithSingleEntry()
        {
            var catalog = ScriptableObject.CreateInstance<CoopMinigameCatalogConfig>();
            var entry = new CoopMinigameCatalogEntry();
            SetSerializedField(entry, "minigameIndex", 0);
            SetSerializedField(entry, "displayName", "Catalogo");
            SetSerializedField(entry, "description", "Descripcion");
            SetSerializedField(entry, "sceneName", "Scene");
            SetSerializedField(catalog, "entries", new List<CoopMinigameCatalogEntry> { entry });
            return catalog;
        }

        private static void InvokePrivateMethod(object target, string methodName)
        {
            var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, $"No existe el metodo privado '{methodName}'.");
            method.Invoke(target, null);
        }
    }
}
