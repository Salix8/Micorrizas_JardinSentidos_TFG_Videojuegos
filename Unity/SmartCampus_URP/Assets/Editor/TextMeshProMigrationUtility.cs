using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class TextMeshProMigrationUtility
{
    [MenuItem("Tools/Migration/Convert Legacy UI To TextMeshPro")]
    public static void ConvertLegacyUiToTextMeshProMenu()
    {
        var report = ConvertLegacyUiToTextMeshPro();
        Debug.Log($"[TMP Migration] Converted {report.ConvertedTextCount} Text and {report.ConvertedInputFieldCount} InputField components across {report.ProcessedAssetPaths.Count} assets.");
    }

    public static MigrationReport ConvertLegacyUiToTextMeshPro()
    {
        var report = new MigrationReport();
        var defaultFontAsset = EnsureDefaultFontAsset();
        var originalScenePath = SceneManager.GetActiveScene().path;

        try
        {
            foreach (var prefabGuid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" }))
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(prefabGuid);
                var prefabRoot = PrefabUtility.LoadPrefabContents(assetPath);
                try
                {
                    if (ConvertHierarchy(new[] { prefabRoot }, defaultFontAsset, report))
                    {
                        PrefabUtility.SaveAsPrefabAsset(prefabRoot, assetPath);
                        report.ProcessedAssetPaths.Add(assetPath);
                    }
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(prefabRoot);
                }
            }

            foreach (var sceneGuid in AssetDatabase.FindAssets("t:Scene", new[] { "Assets" }))
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(sceneGuid);
                var scene = EditorSceneManager.OpenScene(assetPath, OpenSceneMode.Single);
                if (!scene.IsValid())
                {
                    continue;
                }

                if (ConvertHierarchy(scene.GetRootGameObjects(), defaultFontAsset, report))
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                    report.ProcessedAssetPaths.Add(assetPath);
                }
            }
        }
        finally
        {
            if (!string.IsNullOrWhiteSpace(originalScenePath))
            {
                EditorSceneManager.OpenScene(originalScenePath, OpenSceneMode.Single);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        return report;
    }

    private static bool ConvertHierarchy(IReadOnlyList<GameObject> roots, TMP_FontAsset defaultFontAsset, MigrationReport report)
    {
        var legacyTexts = new List<Text>();
        var legacyInputFields = new List<InputField>();
        for (var index = 0; index < roots.Count; index++)
        {
            var root = roots[index];
            if (root == null)
            {
                continue;
            }

            legacyTexts.AddRange(root.GetComponentsInChildren<Text>(true));
            legacyInputFields.AddRange(root.GetComponentsInChildren<InputField>(true));
        }

        if (legacyTexts.Count == 0 && legacyInputFields.Count == 0)
        {
            return false;
        }

        var legacyObjects = new List<UnityEngine.Object>(legacyTexts.Count + legacyInputFields.Count);
        legacyObjects.AddRange(legacyTexts);
        legacyObjects.AddRange(legacyInputFields);
        var referenceSites = CollectReferenceSites(roots, legacyObjects);
        var inputSnapshots = CaptureInputFieldSnapshots(legacyInputFields);
        var replacements = new Dictionary<UnityEngine.Object, UnityEngine.Object>();

        for (var index = 0; index < legacyTexts.Count; index++)
        {
            var legacyText = legacyTexts[index];
            if (legacyText == null)
            {
                continue;
            }

            var textState = CaptureTextState(legacyText);
            var gameObject = legacyText.gameObject;
            UnityEngine.Object.DestroyImmediate(legacyText, true);
            var replacement = gameObject.GetComponent<TextMeshProUGUI>();
            if (replacement == null)
            {
                replacement = gameObject.AddComponent<TextMeshProUGUI>();
            }

            ApplyTextState(textState, replacement, defaultFontAsset);
            replacements[legacyText] = replacement;
        }

        for (var index = 0; index < inputSnapshots.Count; index++)
        {
            var inputSnapshot = inputSnapshots[index];
            if (inputSnapshot.Component == null)
            {
                continue;
            }

            var gameObject = inputSnapshot.Component.gameObject;
            UnityEngine.Object.DestroyImmediate(inputSnapshot.Component, true);
            var replacement = gameObject.GetComponent<TMP_InputField>();
            if (replacement == null)
            {
                replacement = gameObject.AddComponent<TMP_InputField>();
            }

            ApplyInputFieldState(inputSnapshot, replacement, replacements);
            replacements[inputSnapshot.Component] = replacement;
        }

        ApplyReferenceSites(referenceSites, replacements);

        for (var index = 0; index < legacyInputFields.Count; index++)
        {
            if (legacyInputFields[index] != null)
            {
                report.ConvertedInputFieldCount++;
            }
        }

        for (var index = 0; index < legacyTexts.Count; index++)
        {
            if (legacyTexts[index] != null)
            {
                UnityEngine.Object.DestroyImmediate(legacyTexts[index], true);
                report.ConvertedTextCount++;
            }
        }

        return true;
    }

    private static Dictionary<UnityEngine.Object, List<ReferenceSite>> CollectReferenceSites(IReadOnlyList<GameObject> roots, IReadOnlyCollection<UnityEngine.Object> legacyObjects)
    {
        var referenceSites = new Dictionary<UnityEngine.Object, List<ReferenceSite>>();
        var legacySet = new HashSet<UnityEngine.Object>(legacyObjects);
        var components = new List<Component>();
        for (var index = 0; index < roots.Count; index++)
        {
            var root = roots[index];
            if (root == null)
            {
                continue;
            }

            components.AddRange(root.GetComponentsInChildren<Component>(true));
        }

        for (var componentIndex = 0; componentIndex < components.Count; componentIndex++)
        {
            var component = components[componentIndex];
            if (component == null)
            {
                continue;
            }

            var serializedObject = new SerializedObject(component);
            var iterator = serializedObject.GetIterator();
            var changed = false;
            var enterChildren = true;
            while (iterator.Next(enterChildren))
            {
                enterChildren = false;
                if (iterator.propertyType != SerializedPropertyType.ObjectReference || string.Equals(iterator.propertyPath, "m_Script", StringComparison.Ordinal))
                {
                    continue;
                }

                var reference = iterator.objectReferenceValue;
                if (reference == null || !legacySet.Contains(reference))
                {
                    continue;
                }

                if (!referenceSites.TryGetValue(reference, out var sites))
                {
                    sites = new List<ReferenceSite>();
                    referenceSites.Add(reference, sites);
                }

                sites.Add(new ReferenceSite(component, iterator.propertyPath));
            }
        }

        return referenceSites;
    }

    private static void ApplyReferenceSites(
        IReadOnlyDictionary<UnityEngine.Object, List<ReferenceSite>> referenceSites,
        IReadOnlyDictionary<UnityEngine.Object, UnityEngine.Object> replacements)
    {
        foreach (var pair in referenceSites)
        {
            if (!replacements.TryGetValue(pair.Key, out var replacement) || replacement == null)
            {
                continue;
            }

            var sites = pair.Value;
            for (var index = 0; index < sites.Count; index++)
            {
                var site = sites[index];
                if (site.Component == null)
                {
                    continue;
                }

                var serializedObject = new SerializedObject(site.Component);
                var property = serializedObject.FindProperty(site.PropertyPath);
                if (property == null || property.propertyType != SerializedPropertyType.ObjectReference)
                {
                    continue;
                }

                property.objectReferenceValue = replacement;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(site.Component);
            }
        }
    }

    private static TextState CaptureTextState(Text source)
    {
        return new TextState
        {
            Text = source.text,
            FontSize = source.fontSize,
            Color = source.color,
            Alignment = source.alignment,
            RaycastTarget = source.raycastTarget,
            RichText = source.supportRichText,
            AutoSizing = source.resizeTextForBestFit,
            MinFontSize = source.resizeTextMinSize,
            MaxFontSize = source.resizeTextMaxSize,
            EnableWordWrapping = source.horizontalOverflow != HorizontalWrapMode.Overflow,
            OverflowMode = source.verticalOverflow == VerticalWrapMode.Truncate
                ? TextOverflowModes.Truncate
                : TextOverflowModes.Overflow,
            LineSpacing = source.lineSpacing,
            FontStyle = source.fontStyle
        };
    }

    private static void ApplyTextState(TextState source, TextMeshProUGUI target, TMP_FontAsset defaultFontAsset)
    {
        if (target == null)
        {
            return;
        }

        if (defaultFontAsset != null)
        {
            target.font = defaultFontAsset;
        }

        target.text = source.Text;
        target.fontSize = source.FontSize;
        target.color = source.Color;
        target.alignment = ConvertAlignment(source.Alignment);
        target.raycastTarget = source.RaycastTarget;
        target.richText = source.RichText;
        target.enableAutoSizing = source.AutoSizing;
        target.fontSizeMin = source.MinFontSize;
        target.fontSizeMax = source.MaxFontSize;
        target.enableWordWrapping = source.EnableWordWrapping;
        target.overflowMode = source.OverflowMode;
        target.lineSpacing = source.LineSpacing;
        target.fontStyle = ConvertFontStyle(source.FontStyle);
        target.margin = Vector4.zero;
    }

    private static List<InputFieldState> CaptureInputFieldSnapshots(IReadOnlyList<InputField> inputFields)
    {
        var snapshots = new List<InputFieldState>(inputFields.Count);
        for (var index = 0; index < inputFields.Count; index++)
        {
            var source = inputFields[index];
            if (source == null)
            {
                continue;
            }

            snapshots.Add(new InputFieldState
            {
                Component = source,
                LegacyTextComponent = source.textComponent,
                LegacyPlaceholder = source.placeholder,
                Interactable = source.interactable,
                Transition = source.transition,
                Colors = source.colors,
                SpriteState = source.spriteState,
                AnimationTriggers = source.animationTriggers,
                Navigation = source.navigation,
                TargetGraphic = source.targetGraphic,
                CharacterLimit = source.characterLimit,
                LineType = source.lineType.ToString(),
                InputType = source.inputType.ToString(),
                ContentType = source.contentType.ToString(),
                CharacterValidation = source.characterValidation.ToString(),
                KeyboardType = source.keyboardType,
                ReadOnly = source.readOnly,
                CaretBlinkRate = source.caretBlinkRate,
                CaretWidth = source.caretWidth,
                CustomCaretColor = source.customCaretColor,
                CaretColor = source.caretColor,
                SelectionColor = source.selectionColor,
                ShouldHideMobileInput = source.shouldHideMobileInput,
                Text = source.text
            });
        }

        return snapshots;
    }

    private static void ApplyInputFieldState(InputFieldState source, TMP_InputField target, IDictionary<UnityEngine.Object, UnityEngine.Object> replacements)
    {
        if (target == null)
        {
            return;
        }

        target.interactable = source.Interactable;
        target.transition = source.Transition;
        target.colors = source.Colors;
        target.spriteState = source.SpriteState;
        target.animationTriggers = source.AnimationTriggers;
        target.navigation = source.Navigation;
        target.targetGraphic = source.TargetGraphic;
        target.textViewport = source.LegacyTextComponent != null ? source.LegacyTextComponent.rectTransform.parent as RectTransform : null;

        if (source.LegacyTextComponent != null && replacements.TryGetValue(source.LegacyTextComponent, out var replacement))
        {
            target.textComponent = replacement as TMP_Text;
        }
        else
        {
            target.textComponent = target.GetComponentInChildren<TMP_Text>(true);
        }
        if (source.LegacyPlaceholder != null && replacements.TryGetValue(source.LegacyPlaceholder, out var placeholderReplacement))
        {
            target.placeholder = placeholderReplacement as Graphic;
        }
        else
        {
            target.placeholder = source.LegacyPlaceholder;
        }

        target.characterLimit = source.CharacterLimit;
        target.lineType = ParseEnum(source.LineType, TMP_InputField.LineType.SingleLine);
        target.inputType = ParseEnum(source.InputType, TMP_InputField.InputType.Standard);
        target.contentType = ParseEnum(source.ContentType, TMP_InputField.ContentType.Standard);
        target.characterValidation = ParseEnum(source.CharacterValidation, TMP_InputField.CharacterValidation.None);
        target.keyboardType = source.KeyboardType;
        target.readOnly = source.ReadOnly;
        target.caretBlinkRate = source.CaretBlinkRate;
        target.caretWidth = source.CaretWidth;
        target.customCaretColor = source.CustomCaretColor;
        target.caretColor = source.CaretColor;
        target.selectionColor = source.SelectionColor;
        target.shouldHideMobileInput = source.ShouldHideMobileInput;
        target.text = source.Text;
        target.SetTextWithoutNotify(source.Text);
    }

    private static TMP_FontAsset EnsureDefaultFontAsset()
    {
        var fontAsset = FindAnyFontAsset();
        if (fontAsset != null)
        {
            return fontAsset;
        }

        EditorApplication.ExecuteMenuItem("Window/TextMeshPro/Import TMP Essential Resources");
        AssetDatabase.Refresh();

        fontAsset = FindAnyFontAsset();
        if (fontAsset == null)
        {
            throw new InvalidOperationException("TextMesh Pro default font asset is not available after importing essential resources.");
        }

        return fontAsset;
    }

    private static TMP_FontAsset FindAnyFontAsset()
    {
        var fontAssetGuids = AssetDatabase.FindAssets("t:TMP_FontAsset", new[] { "Assets" });
        for (var index = 0; index < fontAssetGuids.Length; index++)
        {
            var assetPath = AssetDatabase.GUIDToAssetPath(fontAssetGuids[index]);
            var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
            if (fontAsset != null)
            {
                return fontAsset;
            }
        }

        return null;
    }

    private static TextAlignmentOptions ConvertAlignment(TextAnchor alignment)
    {
        return alignment switch
        {
            TextAnchor.UpperLeft => TextAlignmentOptions.TopLeft,
            TextAnchor.UpperCenter => TextAlignmentOptions.Top,
            TextAnchor.UpperRight => TextAlignmentOptions.TopRight,
            TextAnchor.MiddleLeft => TextAlignmentOptions.Left,
            TextAnchor.MiddleCenter => TextAlignmentOptions.Center,
            TextAnchor.MiddleRight => TextAlignmentOptions.Right,
            TextAnchor.LowerLeft => TextAlignmentOptions.BottomLeft,
            TextAnchor.LowerCenter => TextAlignmentOptions.Bottom,
            TextAnchor.LowerRight => TextAlignmentOptions.BottomRight,
            _ => TextAlignmentOptions.TopLeft
        };
    }

    private static FontStyles ConvertFontStyle(FontStyle style)
    {
        return style switch
        {
            FontStyle.Bold => FontStyles.Bold,
            FontStyle.Italic => FontStyles.Italic,
            FontStyle.BoldAndItalic => FontStyles.Bold | FontStyles.Italic,
            _ => FontStyles.Normal
        };
    }

    private static TEnum ParseEnum<TEnum>(string value, TEnum fallback) where TEnum : struct
    {
        return Enum.TryParse(value, true, out TEnum parsedValue) ? parsedValue : fallback;
    }

    public sealed class MigrationReport
    {
        public int ConvertedTextCount { get; set; }

        public int ConvertedInputFieldCount { get; set; }

        public List<string> ProcessedAssetPaths { get; } = new List<string>();
    }

    private readonly struct ReferenceSite
    {
        public ReferenceSite(Component component, string propertyPath)
        {
            Component = component;
            PropertyPath = propertyPath;
        }

        public Component Component { get; }

        public string PropertyPath { get; }
    }

    private sealed class TextState
    {
        public string Text { get; set; }

        public float FontSize { get; set; }

        public Color Color { get; set; }

        public TextAnchor Alignment { get; set; }

        public bool RaycastTarget { get; set; }

        public bool RichText { get; set; }

        public bool AutoSizing { get; set; }

        public float MinFontSize { get; set; }

        public float MaxFontSize { get; set; }

        public bool EnableWordWrapping { get; set; }

        public TextOverflowModes OverflowMode { get; set; }

        public float LineSpacing { get; set; }

        public FontStyle FontStyle { get; set; }
    }

    private sealed class InputFieldState
    {
        public InputField Component { get; set; }

        public Text LegacyTextComponent { get; set; }

        public Graphic LegacyPlaceholder { get; set; }

        public bool Interactable { get; set; }

        public Selectable.Transition Transition { get; set; }

        public ColorBlock Colors { get; set; }

        public SpriteState SpriteState { get; set; }

        public AnimationTriggers AnimationTriggers { get; set; }

        public Navigation Navigation { get; set; }

        public Graphic TargetGraphic { get; set; }

        public int CharacterLimit { get; set; }

        public string LineType { get; set; }

        public string InputType { get; set; }

        public string ContentType { get; set; }

        public string CharacterValidation { get; set; }

        public TouchScreenKeyboardType KeyboardType { get; set; }

        public bool ReadOnly { get; set; }

        public float CaretBlinkRate { get; set; }

        public int CaretWidth { get; set; }

        public bool CustomCaretColor { get; set; }

        public Color CaretColor { get; set; }

        public Color SelectionColor { get; set; }

        public bool ShouldHideMobileInput { get; set; }

        public string Text { get; set; }
    }
}
