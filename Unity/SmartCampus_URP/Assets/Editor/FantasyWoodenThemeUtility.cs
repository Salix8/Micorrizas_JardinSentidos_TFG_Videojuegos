using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

internal static class FantasyWoodenThemeUtility
{
    private const string ThemeRoot = "Assets/Art/Fantasy Wooden GUI  Free";
    private const string SceneFolder = "Assets/Scenes";
    private const string LargeBoardPath = ThemeRoot + "/UI board Large  parchment.png";
    private const string LargeStoneBoardPath = ThemeRoot + "/UI board Large stone.png";
    private const string MediumBoardPath = ThemeRoot + "/UI board Medium  parchment.png";
    private const string MediumStoneBoardPath = ThemeRoot + "/UI board Medium stone.png";
    private const string SmallBoardPath = ThemeRoot + "/UI board Small  parchment.png";
    private const string SmallStoneBoardPath = ThemeRoot + "/UI board Small  stone.png";
    private const string PrimaryButtonPath = ThemeRoot + "/TextBTN_Big.png";
    private const string PrimaryButtonPressedPath = ThemeRoot + "/TextBTN_Big_Pressed.png";
    private const string CloseButtonPath = ThemeRoot + "/Close Button.png";
    private const float ButtonSpriteAspectRatio = 338f / 112f;
    private const float MinimumButtonWidth = 220f;
    private const float MaximumButtonWidth = 400f;

    private static readonly HashSet<string> OverlayNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Background",
        "DismissBackground"
    };

    private static readonly HashSet<string> LargePanelNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "SurfacePanel",
        "GameplayPanel",
        "LauncherPanel",
        "CardPanel",
        "ContentPanel",
        "HomePanel",
        "HostPanel",
        "JoinPanel",
        "SessionPanel"
    };

    private static readonly HashSet<string> MediumPanelNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "StatusPanel",
        "InteractionPanel",
        "InputPanel",
        "HistoryPanel",
        "HandPanel",
        "DeckPanel",
        "DiscardPanel",
        "SuggestionPanel",
        "WaitingPanel",
        "CardRoot",
        "PlantCard"
    };

    private static ThemeAssets cachedAssets;

    [MenuItem("Tools/Coop/Apply Fantasy Wooden Theme To All Scenes")]
    public static void ApplyThemeToAllScenes()
    {
        EnsureAssetsLoaded();

        var sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { SceneFolder });
        foreach (var sceneGuid in sceneGuids)
        {
            var scenePath = AssetDatabase.GUIDToAssetPath(sceneGuid);
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            ApplyThemeToOpenScene(scene);
            EditorSceneManager.SaveScene(scene);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    public static void ApplyThemeToOpenScene(Scene scene)
    {
        EnsureAssetsLoaded();

        var canvases = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Canvas>(true))
            .Distinct()
            .ToArray();

        foreach (var canvas in canvases)
        {
            ApplyThemeToCanvas(canvas);
        }

        EditorSceneManager.MarkSceneDirty(scene);
    }

    private static void ApplyThemeToCanvas(Canvas canvas)
    {
        foreach (var image in canvas.GetComponentsInChildren<Image>(true))
        {
            if (image.GetComponent<Button>() != null ||
                image.GetComponent<InputField>() != null ||
                image.GetComponent<TMP_InputField>() != null)
            {
                continue;
            }

            ApplyImageTheme(image);
        }

        foreach (var inputField in canvas.GetComponentsInChildren<InputField>(true))
        {
            ApplyInputTheme(inputField);
        }

        foreach (var inputField in canvas.GetComponentsInChildren<TMP_InputField>(true))
        {
            ApplyInputTheme(inputField);
        }

        foreach (var button in canvas.GetComponentsInChildren<Button>(true))
        {
            ApplyButtonTheme(button);
        }

        foreach (var text in canvas.GetComponentsInChildren<Text>(true))
        {
            ApplyTextTheme(text);
        }

        foreach (var text in canvas.GetComponentsInChildren<TMP_Text>(true))
        {
            ApplyTextTheme(text);
        }
    }

    private static void ApplyImageTheme(Image image)
    {
        if (image == null)
        {
            return;
        }

        var objectName = image.gameObject.name;

        if (string.Equals(objectName, "DismissBackground", StringComparison.OrdinalIgnoreCase))
        {
            image.sprite = null;
            image.type = Image.Type.Simple;
            image.color = new Color(0.11f, 0.08f, 0.05f, 0.78f);
            return;
        }

        if (string.Equals(objectName, "Background", StringComparison.OrdinalIgnoreCase))
        {
            if (IsFullScreenBackground(image.rectTransform))
            {
                image.sprite = null;
                image.type = Image.Type.Simple;
                image.color = new Color(0.26f, 0.18f, 0.11f, 1f);
                return;
            }

            image.sprite = null;
            image.type = Image.Type.Simple;
            image.color = new Color(0.11f, 0.08f, 0.05f, 0.78f);
            return;
        }

        if (IsTransparentLayoutHelper(image))
        {
            image.sprite = null;
            image.type = Image.Type.Simple;
            image.color = new Color(1f, 1f, 1f, Mathf.Min(image.color.a, 0.02f));
            return;
        }

        if (LargePanelNames.Contains(objectName))
        {
            ApplySlicedSprite(image, cachedAssets.LargeBoard, new Color(1f, 1f, 1f, 0.98f));
            return;
        }

        if (MediumPanelNames.Contains(objectName))
        {
            var sprite = string.Equals(objectName, "WaitingPanel", StringComparison.OrdinalIgnoreCase)
                ? cachedAssets.MediumStoneBoard
                : cachedAssets.MediumBoard;
            ApplySlicedSprite(image, sprite, new Color(1f, 1f, 1f, 0.98f));
            return;
        }

        if (objectName.IndexOf("Popup", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            ApplySlicedSprite(image, cachedAssets.LargeStoneBoard, new Color(1f, 1f, 1f, 0.98f));
            return;
        }

        if (objectName.IndexOf("Placeholder", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            ApplySlicedSprite(image, cachedAssets.SmallBoard, new Color(1f, 1f, 1f, 0.9f));
            return;
        }

        if (objectName.IndexOf("Illustration", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            image.color = Color.white;
            return;
        }
    }

    private static void ApplyInputTheme(InputField inputField)
    {
        if (inputField == null)
        {
            return;
        }

        var image = inputField.GetComponent<Image>();
        if (image != null)
        {
            ApplySlicedSprite(image, cachedAssets.SmallBoard, new Color(1f, 1f, 1f, 1f));
        }

        if (inputField.textComponent != null)
        {
            inputField.textComponent.color = new Color(0.23f, 0.14f, 0.08f, 1f);
            inputField.textComponent.alignment = TextAnchor.MiddleCenter;
            ConfigureTextShadow(inputField.textComponent, new Color(0.93f, 0.87f, 0.74f, 0.45f), new Vector2(1f, -1f));
        }

        if (inputField.placeholder is Text placeholder)
        {
            placeholder.color = new Color(0.43f, 0.29f, 0.16f, 0.78f);
            placeholder.alignment = TextAnchor.MiddleCenter;
        }
    }

    private static void ApplyInputTheme(TMP_InputField inputField)
    {
        if (inputField == null)
        {
            return;
        }

        var image = inputField.GetComponent<Image>();
        if (image != null)
        {
            ApplySlicedSprite(image, cachedAssets.SmallBoard, new Color(1f, 1f, 1f, 1f));
        }

        if (inputField.textComponent != null)
        {
            inputField.textComponent.color = new Color(0.23f, 0.14f, 0.08f, 1f);
            inputField.textComponent.alignment = TextAlignmentOptions.Center;
            ConfigureGraphicShadow(inputField.textComponent, new Color(0.93f, 0.87f, 0.74f, 0.45f), new Vector2(1f, -1f));
        }

        if (inputField.placeholder is TMP_Text placeholder)
        {
            placeholder.color = new Color(0.43f, 0.29f, 0.16f, 0.78f);
            placeholder.alignment = TextAlignmentOptions.Center;
        }
    }

    private static void ApplyButtonTheme(Button button)
    {
        if (button == null)
        {
            return;
        }

        var image = button.GetComponent<Image>();
        if (image == null)
        {
            return;
        }

        var style = ResolveButtonStyle(button.gameObject.name);
        ApplySlicedSprite(image, style.NormalSprite, Color.white);
        NormalizeButtonLayout(button);

        button.transition = style.UsesSpriteSwap
            ? Selectable.Transition.SpriteSwap
            : Selectable.Transition.ColorTint;
        button.targetGraphic = image;

        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.98f, 0.9f, 1f);
        colors.pressedColor = new Color(0.9f, 0.88f, 0.82f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(0.7f, 0.7f, 0.7f, 0.7f);
        colors.colorMultiplier = 1f;
        button.colors = colors;

        var spriteState = button.spriteState;
        spriteState.highlightedSprite = style.HighlightedSprite;
        spriteState.pressedSprite = style.PressedSprite;
        spriteState.selectedSprite = style.HighlightedSprite;
        spriteState.disabledSprite = style.NormalSprite;
        button.spriteState = spriteState;
    }

    private static void ApplyTextTheme(Text text)
    {
        if (text == null)
        {
            return;
        }

        var objectName = text.gameObject.name;
        var parentName = text.transform.parent != null ? text.transform.parent.name : string.Empty;

        if (string.Equals(parentName, "DismissBackground", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (string.Equals(objectName, "Label", StringComparison.OrdinalIgnoreCase) &&
            text.GetComponentInParent<Button>(true) != null)
        {
            text.color = new Color(0.27f, 0.13f, 0.03f, 1f);
            text.fontStyle = FontStyle.Bold;
            ConfigureTextShadow(text, new Color(0.95f, 0.84f, 0.62f, 0.85f), new Vector2(1f, -1f));
            return;
        }

        if (IsTitleLike(objectName))
        {
            text.color = new Color(0.33f, 0.18f, 0.07f, 1f);
            text.fontStyle = FontStyle.Bold;
            ConfigureTextShadow(text, new Color(0.98f, 0.92f, 0.78f, 0.9f), new Vector2(1f, -1f));
            return;
        }

        if (IsImportantStatus(objectName))
        {
            text.color = new Color(0.29f, 0.16f, 0.07f, 1f);
            text.fontStyle = FontStyle.Bold;
            ConfigureTextShadow(text, new Color(0.94f, 0.88f, 0.75f, 0.55f), new Vector2(1f, -1f));
            return;
        }

        text.color = new Color(0.25f, 0.15f, 0.08f, 1f);
        ConfigureTextShadow(text, new Color(0.96f, 0.9f, 0.8f, 0.35f), new Vector2(1f, -1f));
    }

    private static void ApplyTextTheme(TMP_Text text)
    {
        if (text == null)
        {
            return;
        }

        var objectName = text.gameObject.name;
        var parentName = text.transform.parent != null ? text.transform.parent.name : string.Empty;

        if (string.Equals(parentName, "DismissBackground", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (string.Equals(objectName, "Label", StringComparison.OrdinalIgnoreCase) &&
            text.GetComponentInParent<Button>(true) != null)
        {
            text.color = new Color(0.27f, 0.13f, 0.03f, 1f);
            text.fontStyle = FontStyles.Bold;
            ConfigureGraphicShadow(text, new Color(0.95f, 0.84f, 0.62f, 0.85f), new Vector2(1f, -1f));
            return;
        }

        if (IsTitleLike(objectName))
        {
            text.color = new Color(0.33f, 0.18f, 0.07f, 1f);
            text.fontStyle = FontStyles.Bold;
            ConfigureGraphicShadow(text, new Color(0.98f, 0.92f, 0.78f, 0.9f), new Vector2(1f, -1f));
            return;
        }

        if (IsImportantStatus(objectName))
        {
            text.color = new Color(0.29f, 0.16f, 0.07f, 1f);
            text.fontStyle = FontStyles.Bold;
            ConfigureGraphicShadow(text, new Color(0.94f, 0.88f, 0.75f, 0.55f), new Vector2(1f, -1f));
            return;
        }

        text.color = new Color(0.25f, 0.15f, 0.08f, 1f);
        ConfigureGraphicShadow(text, new Color(0.96f, 0.9f, 0.8f, 0.35f), new Vector2(1f, -1f));
    }

    private static ButtonStyle ResolveButtonStyle(string objectName)
    {
        if (objectName.IndexOf("Close", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return new ButtonStyle(cachedAssets.CloseButton, cachedAssets.CloseButton, cachedAssets.CloseButton, false);
        }

        return new ButtonStyle(cachedAssets.PrimaryButton, cachedAssets.PrimaryButton, cachedAssets.PrimaryButtonPressed, true);
    }

    private static void NormalizeButtonLayout(Button button)
    {
        if (button == null || button.gameObject.name.IndexOf("Close", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return;
        }

        var layoutElement = button.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = button.gameObject.AddComponent<LayoutElement>();
        }

        var preferredHeight = layoutElement.preferredHeight > 0f
            ? layoutElement.preferredHeight
            : Mathf.Max(60f, button.GetComponent<RectTransform>().rect.height);

        var preferredWidth = Mathf.Clamp(preferredHeight * ButtonSpriteAspectRatio, MinimumButtonWidth, MaximumButtonWidth);
        layoutElement.preferredWidth = preferredWidth;
        layoutElement.minWidth = Mathf.Min(preferredWidth, 240f);
        layoutElement.flexibleWidth = 0f;

        if (button.transform.parent is RectTransform parentRect &&
            parentRect.TryGetComponent(out HorizontalOrVerticalLayoutGroup parentLayout))
        {
            if (ParentLooksLikeButtonGroup(parentRect))
            {
                parentLayout.childForceExpandWidth = false;
                parentLayout.childAlignment = parentLayout is VerticalLayoutGroup
                    ? TextAnchor.UpperCenter
                    : TextAnchor.MiddleCenter;
            }
        }
    }

    private static bool ParentLooksLikeButtonGroup(RectTransform parentRect)
    {
        var directChildren = parentRect.Cast<Transform>()
            .Where(child => child.gameObject.activeSelf)
            .ToArray();

        if (directChildren.Length == 0)
        {
            return false;
        }

        var parentName = parentRect.gameObject.name;
        if (parentName.IndexOf("Action", StringComparison.OrdinalIgnoreCase) >= 0 ||
            parentName.IndexOf("Button", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return true;
        }

        return directChildren.All(child =>
            child.GetComponent<Button>() != null ||
            child.GetComponent<LayoutElement>() != null && child.childCount == 0);
    }

    private static bool IsFullScreenBackground(RectTransform rectTransform)
    {
        return rectTransform != null &&
               rectTransform.anchorMin == Vector2.zero &&
               rectTransform.anchorMax == Vector2.one &&
               rectTransform.offsetMin == Vector2.zero &&
               rectTransform.offsetMax == Vector2.zero;
    }

    private static bool IsTransparentLayoutHelper(Image image)
    {
        var objectName = image.gameObject.name;
        return objectName.IndexOf("ScrollView", StringComparison.OrdinalIgnoreCase) >= 0 ||
               string.Equals(objectName, "Viewport", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(objectName, "Illustration", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsTitleLike(string objectName)
    {
        return objectName.IndexOf("Title", StringComparison.OrdinalIgnoreCase) >= 0 ||
               objectName.IndexOf("Header", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool IsImportantStatus(string objectName)
    {
        return objectName.IndexOf("Score", StringComparison.OrdinalIgnoreCase) >= 0 ||
               objectName.IndexOf("Progress", StringComparison.OrdinalIgnoreCase) >= 0 ||
               objectName.IndexOf("Timer", StringComparison.OrdinalIgnoreCase) >= 0 ||
               objectName.IndexOf("Status", StringComparison.OrdinalIgnoreCase) >= 0 ||
               objectName.IndexOf("Badge", StringComparison.OrdinalIgnoreCase) >= 0 ||
               objectName.IndexOf("Summary", StringComparison.OrdinalIgnoreCase) >= 0 ||
               objectName.IndexOf("Shared", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static void ApplySlicedSprite(Image image, Sprite sprite, Color color)
    {
        if (image == null || sprite == null)
        {
            return;
        }

        image.sprite = sprite;
        image.type = Image.Type.Sliced;
        image.color = color;
        image.preserveAspect = false;
        image.fillCenter = true;
    }

    private static void ConfigureTextShadow(Text text, Color color, Vector2 distance)
    {
        ConfigureGraphicShadow(text, color, distance);
    }

    private static void ConfigureGraphicShadow(Graphic graphic, Color color, Vector2 distance)
    {
        var shadow = graphic.GetComponent<Shadow>();
        if (shadow == null)
        {
            shadow = graphic.gameObject.AddComponent<Shadow>();
        }

        shadow.effectColor = color;
        shadow.effectDistance = distance;
        shadow.useGraphicAlpha = true;
    }

    private static void EnsureAssetsLoaded()
    {
        if (cachedAssets.IsValid)
        {
            return;
        }

        cachedAssets = new ThemeAssets(
            LoadSprite(LargeBoardPath),
            LoadSprite(LargeStoneBoardPath),
            LoadSprite(MediumBoardPath),
            LoadSprite(MediumStoneBoardPath),
            LoadSprite(SmallBoardPath),
            LoadSprite(SmallStoneBoardPath),
            LoadSprite(PrimaryButtonPath),
            LoadSprite(PrimaryButtonPressedPath),
            LoadSprite(CloseButtonPath));
    }

    private static Sprite LoadSprite(string assetPath)
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        if (sprite == null)
        {
            throw new InvalidOperationException($"No se pudo cargar el sprite requerido: {assetPath}");
        }

        return sprite;
    }

    private readonly struct ButtonStyle
    {
        public ButtonStyle(Sprite normalSprite, Sprite highlightedSprite, Sprite pressedSprite, bool usesSpriteSwap)
        {
            NormalSprite = normalSprite;
            HighlightedSprite = highlightedSprite;
            PressedSprite = pressedSprite;
            UsesSpriteSwap = usesSpriteSwap;
        }

        public Sprite NormalSprite { get; }
        public Sprite HighlightedSprite { get; }
        public Sprite PressedSprite { get; }
        public bool UsesSpriteSwap { get; }
    }

    private readonly struct ThemeAssets
    {
        public ThemeAssets(
            Sprite largeBoard,
            Sprite largeStoneBoard,
            Sprite mediumBoard,
            Sprite mediumStoneBoard,
            Sprite smallBoard,
            Sprite smallStoneBoard,
            Sprite primaryButton,
            Sprite primaryButtonPressed,
            Sprite closeButton)
        {
            LargeBoard = largeBoard;
            LargeStoneBoard = largeStoneBoard;
            MediumBoard = mediumBoard;
            MediumStoneBoard = mediumStoneBoard;
            SmallBoard = smallBoard;
            SmallStoneBoard = smallStoneBoard;
            PrimaryButton = primaryButton;
            PrimaryButtonPressed = primaryButtonPressed;
            CloseButton = closeButton;
        }

        public Sprite LargeBoard { get; }
        public Sprite LargeStoneBoard { get; }
        public Sprite MediumBoard { get; }
        public Sprite MediumStoneBoard { get; }
        public Sprite SmallBoard { get; }
        public Sprite SmallStoneBoard { get; }
        public Sprite PrimaryButton { get; }
        public Sprite PrimaryButtonPressed { get; }
        public Sprite CloseButton { get; }

        public bool IsValid => LargeBoard != null && MediumBoard != null && PrimaryButton != null;
    }
}
