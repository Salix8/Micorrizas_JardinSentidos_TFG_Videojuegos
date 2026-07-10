using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SmartCampus.Coop.Minigames;

namespace SmartCampus.Coop.Minigames.CollaborativePlantGuess
{
    [DisallowMultipleComponent]
    public sealed class CollaborativePlantGuessVictoryRevealPopupView : MonoBehaviour
    {
        [SerializeField] private GameObject popupRoot;
        [SerializeField] private Image backdropImage;
        [SerializeField] private Image panelImage;
        [SerializeField] private Image plantImage;
        [SerializeField] private GameObject plantImagePlaceholder;
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text plantNameLabel;
        [SerializeField] private TMP_Text scientificNameLabel;
        [SerializeField] private TMP_Text characteristicsLabel;
        [SerializeField] private TMP_Text instructionLabel;
        [SerializeField] private Button acceptButton;
        [SerializeField] private TMP_Text acceptButtonLabel;
        [Header("Image Layout")]
        [SerializeField, Min(1f)] private float plantImagePreferredWidth = 700f;
        [SerializeField, Min(1f)] private float plantImagePreferredHeight = 430f;
        [SerializeField, Min(1f)] private float plantImageMinHeight = 360f;

        private Coroutine imageLoadingCoroutine;
        private Sprite runtimeSprite;
        private Action accepted;

        private void Awake()
        {
            EnsureRuntimeHierarchy();
            SetVisible(false);
        }

        private void OnDisable()
        {
            ReleaseRuntimeSprite();
            if (acceptButton != null)
            {
                acceptButton.onClick.RemoveListener(HandleAccepted);
            }
        }

        public void Bind(CollaborativePlantGuessPlantDefinition plantDefinition, bool canAccept, Action onAccepted)
        {
            EnsureRuntimeHierarchy();
            accepted = onAccepted;
            SetVisible(plantDefinition != null);

            if (plantDefinition == null)
            {
                return;
            }

            if (titleLabel != null)
            {
                titleLabel.text = "Planta acertada";
            }

            if (plantNameLabel != null)
            {
                plantNameLabel.text = plantDefinition.CommonName;
            }

            if (scientificNameLabel != null)
            {
                scientificNameLabel.text = plantDefinition.ScientificName;
            }

            if (characteristicsLabel != null)
            {
                characteristicsLabel.text = BuildCharacteristicsText(plantDefinition);
            }

            if (instructionLabel != null)
            {
                instructionLabel.text = canAccept
                    ? "Revisad la imagen y las caracteristicas. Pulsa aceptar para continuar."
                    : "Esperando a que el host confirme para continuar.";
            }

            if (acceptButton != null)
            {
                acceptButton.onClick.RemoveListener(HandleAccepted);
                acceptButton.onClick.AddListener(HandleAccepted);
                acceptButton.gameObject.SetActive(canAccept);
                acceptButton.interactable = canAccept;
            }

            if (acceptButtonLabel != null)
            {
                acceptButtonLabel.text = "Aceptar";
            }

            ApplyPlantImageLayout();
            BindPlantImage(plantDefinition);
        }

        public void Hide()
        {
            accepted = null;
            SetVisible(false);
            ReleaseRuntimeSprite();
        }

        public static CollaborativePlantGuessVictoryRevealPopupView CreateRuntime(Transform parent)
        {
            var popupObject = new GameObject(
                "VictoryRevealPopup",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(CollaborativePlantGuessVictoryRevealPopupView));
            popupObject.transform.SetParent(parent, false);
            Stretch(popupObject.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
            var view = popupObject.GetComponent<CollaborativePlantGuessVictoryRevealPopupView>();
            view.EnsureRuntimeHierarchy();
            return view;
        }

        private void EnsureRuntimeHierarchy()
        {
            if (popupRoot != null)
            {
                return;
            }

            popupRoot = gameObject;
            var rootRect = popupRoot.transform as RectTransform;
            if (rootRect != null)
            {
                Stretch(rootRect, Vector2.zero, Vector2.zero);
            }

            backdropImage = GetOrAddComponent<Image>(popupRoot);
            backdropImage.color = new Color(0f, 0f, 0f, 0.55f);
            backdropImage.raycastTarget = true;

            var panelObject = CreateChild("Panel", popupRoot.transform, typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter), typeof(LayoutElement));
            var panelRect = panelObject.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.05f, 0.06f);
            panelRect.anchorMax = new Vector2(0.95f, 0.94f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            panelImage = panelObject.GetComponent<Image>();
            panelImage.color = new Color(0.98f, 0.96f, 0.89f, 1f);

            var panelLayout = panelObject.GetComponent<VerticalLayoutGroup>();
            panelLayout.padding = new RectOffset(36, 36, 32, 32);
            panelLayout.spacing = 12f;
            panelLayout.childControlWidth = true;
            panelLayout.childControlHeight = true;
            panelLayout.childForceExpandWidth = true;
            panelLayout.childForceExpandHeight = false;
            panelLayout.childAlignment = TextAnchor.UpperCenter;

            var panelFitter = panelObject.GetComponent<ContentSizeFitter>();
            panelFitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

            titleLabel = CreateText("TitleLabel", panelObject.transform, 44f, FontStyles.Bold, TextAlignmentOptions.Center);
            titleLabel.color = new Color(0.06f, 0.29f, 0.08f, 1f);

            plantImage = CreateImage("PlantImage", panelObject.transform, plantImagePreferredWidth, plantImagePreferredHeight);
            plantImage.color = Color.white;
            ApplyPlantImageLayout();

            plantImagePlaceholder = CreateText("PlantImagePlaceholder", plantImage.transform, 22f, FontStyles.Italic, TextAlignmentOptions.Center).gameObject;
            var placeholderLabel = plantImagePlaceholder.GetComponent<TMP_Text>();
            placeholderLabel.text = "Imagen de la planta";
            placeholderLabel.color = new Color(0.33f, 0.38f, 0.34f, 1f);
            Stretch(placeholderLabel.rectTransform, Vector2.zero, Vector2.zero);

            plantNameLabel = CreateText("PlantNameLabel", panelObject.transform, 38f, FontStyles.Bold, TextAlignmentOptions.Center);
            plantNameLabel.color = new Color(0.06f, 0.29f, 0.08f, 1f);

            scientificNameLabel = CreateText("ScientificNameLabel", panelObject.transform, 28f, FontStyles.Italic, TextAlignmentOptions.Center);
            scientificNameLabel.color = new Color(0.24f, 0.28f, 0.24f, 1f);

            characteristicsLabel = CreateText("CharacteristicsLabel", panelObject.transform, 24f, FontStyles.Normal, TextAlignmentOptions.Left);
            characteristicsLabel.color = new Color(0.12f, 0.16f, 0.13f, 1f);

            instructionLabel = CreateText("InstructionLabel", panelObject.transform, 24f, FontStyles.Bold, TextAlignmentOptions.Center);
            instructionLabel.color = new Color(0.16f, 0.36f, 0.17f, 1f);

            var buttonObject = CreateChild("AcceptButton", panelObject.transform, typeof(Image), typeof(Button), typeof(LayoutElement));
            var buttonImage = buttonObject.GetComponent<Image>();
            buttonImage.color = new Color(0.21f, 0.42f, 0.19f, 1f);
            acceptButton = buttonObject.GetComponent<Button>();
            var buttonLayout = buttonObject.GetComponent<LayoutElement>();
            buttonLayout.minHeight = 74f;
            buttonLayout.preferredWidth = 260f;
            buttonLayout.flexibleWidth = 0f;

            acceptButtonLabel = CreateText("AcceptButtonLabel", buttonObject.transform, 28f, FontStyles.Bold, TextAlignmentOptions.Center);
            acceptButtonLabel.color = Color.white;
            Stretch(acceptButtonLabel.rectTransform, Vector2.zero, Vector2.zero);
        }

        private void BindPlantImage(CollaborativePlantGuessPlantDefinition plantDefinition)
        {
            ReleaseRuntimeSprite();

            if (plantImage == null)
            {
                return;
            }

            plantImage.sprite = null;
            plantImage.enabled = false;
            if (plantImagePlaceholder != null)
            {
                plantImagePlaceholder.SetActive(true);
            }

            if (plantDefinition == null)
            {
                return;
            }

            if (plantDefinition.InspectorSprite != null)
            {
                plantImage.sprite = plantDefinition.InspectorSprite;
                plantImage.enabled = true;
                if (plantImagePlaceholder != null)
                {
                    plantImagePlaceholder.SetActive(false);
                }

                return;
            }

            if (string.IsNullOrWhiteSpace(plantDefinition.ImagePath))
            {
                return;
            }

            imageLoadingCoroutine = StartCoroutine(CoopMinigameExternalContentService.LoadSpriteAsync(
                plantDefinition.ImagePath,
                (sprite, error) =>
                {
                    imageLoadingCoroutine = null;
                    if (sprite == null)
                    {
                        Debug.LogWarning($"[CollaborativePlantGuess] No se ha podido cargar la imagen del popup para '{plantDefinition.FullDisplayName}'. Error: {error}", this);
                        return;
                    }

                    runtimeSprite = sprite;
                    plantImage.sprite = runtimeSprite;
                    plantImage.enabled = true;
                    if (plantImagePlaceholder != null)
                    {
                        plantImagePlaceholder.SetActive(false);
                    }
                }));
        }

        private void ApplyPlantImageLayout()
        {
            if (plantImage == null)
            {
                return;
            }

            plantImage.preserveAspect = true;
            plantImage.type = Image.Type.Simple;
            plantImage.rectTransform.localScale = Vector3.one;

            var layoutElement = plantImage.GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = plantImage.gameObject.AddComponent<LayoutElement>();
            }

            layoutElement.minHeight = plantImageMinHeight;
            layoutElement.preferredWidth = plantImagePreferredWidth;
            layoutElement.preferredHeight = plantImagePreferredHeight;
            layoutElement.flexibleWidth = 1f;
            layoutElement.flexibleHeight = 0f;
        }

        private static string BuildCharacteristicsText(CollaborativePlantGuessPlantDefinition plantDefinition)
        {
            return
                $"Tipo de planta: {FormatValue(plantDefinition.PlantType)}\n" +
                $"Rugosidad: {FormatValue(plantDefinition.SurfaceRoughness)}\n" +
                $"Tipo de hoja: {FormatValue(plantDefinition.LeafType)}\n" +
                $"Categoria del fruto: {FormatValue(plantDefinition.FruitCategory)}\n" +
                $"Tipo de fruto: {FormatValue(plantDefinition.FruitType)}";
        }

        private static string FormatValue(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "Sin dato" : value;
        }

        private void HandleAccepted()
        {
            accepted?.Invoke();
        }

        private void SetVisible(bool visible)
        {
            if (popupRoot != null)
            {
                popupRoot.SetActive(visible);
            }
        }

        private void ReleaseRuntimeSprite()
        {
            if (imageLoadingCoroutine != null)
            {
                StopCoroutine(imageLoadingCoroutine);
                imageLoadingCoroutine = null;
            }

            if (plantImage != null)
            {
                plantImage.sprite = null;
            }

            if (runtimeSprite != null)
            {
                var texture = runtimeSprite.texture;
                Destroy(runtimeSprite);
                runtimeSprite = null;
                if (texture != null)
                {
                    Destroy(texture);
                }
            }
        }

        private static GameObject CreateChild(string objectName, Transform parent, params Type[] componentTypes)
        {
            var child = new GameObject(objectName, typeof(RectTransform));
            for (var index = 0; index < componentTypes.Length; index++)
            {
                if (componentTypes[index] != typeof(RectTransform))
                {
                    child.AddComponent(componentTypes[index]);
                }
            }

            child.transform.SetParent(parent, false);
            return child;
        }

        private static TMP_Text CreateText(string objectName, Transform parent, float fontSize, FontStyles fontStyle, TextAlignmentOptions alignment)
        {
            var textObject = CreateChild(objectName, parent, typeof(TextMeshProUGUI), typeof(LayoutElement));
            var label = textObject.GetComponent<TMP_Text>();
            label.fontSize = fontSize;
            label.fontStyle = fontStyle;
            label.alignment = alignment;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.overflowMode = TextOverflowModes.Overflow;
            var layoutElement = textObject.GetComponent<LayoutElement>();
            layoutElement.minHeight = fontSize * 1.35f;
            layoutElement.preferredHeight = -1f;
            return label;
        }

        private static Image CreateImage(string objectName, Transform parent, float preferredWidth, float preferredHeight)
        {
            var imageObject = CreateChild(objectName, parent, typeof(Image), typeof(LayoutElement));
            var layoutElement = imageObject.GetComponent<LayoutElement>();
            layoutElement.preferredWidth = preferredWidth;
            layoutElement.preferredHeight = preferredHeight;
            layoutElement.flexibleWidth = 0f;
            return imageObject.GetComponent<Image>();
        }

        private static T GetOrAddComponent<T>(GameObject target) where T : Component
        {
            return target.TryGetComponent<T>(out var component) ? component : target.AddComponent<T>();
        }

        private static void Stretch(RectTransform rectTransform, Vector2 offsetMin, Vector2 offsetMax)
        {
            if (rectTransform == null)
            {
                return;
            }

            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.offsetMin = offsetMin;
            rectTransform.offsetMax = offsetMax;
            rectTransform.localScale = Vector3.one;
        }
    }
}
