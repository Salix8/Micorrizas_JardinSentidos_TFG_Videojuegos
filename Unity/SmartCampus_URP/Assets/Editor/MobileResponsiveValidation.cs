using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using SmartCampus.Coop.Minigames;

public static class MobileResponsiveValidation
{
    private const float MinimumVisualMargin = 24f;
    private static readonly string ReportPath = Path.Combine(Directory.GetCurrentDirectory(), "mobile-responsive-validation.txt");
    private static readonly DeviceProfile[] Profiles =
    {
        new("Android Compact", 720f, 1280f),
        new("Android Standard", 1080f, 1920f),
        new("Android Tall", 1080f, 2400f),
        new("Android Large", 1440f, 3040f)
    };

    [MenuItem("Tools/Coop/Validate Mobile Responsive UI")]
    public static void RunValidation()
    {
        var report = new StringBuilder();
        report.AppendLine("Mobile Responsive UI Validation");
        report.AppendLine("================================");
        report.AppendLine();
        report.AppendLine("Profiles:");
        foreach (var profile in Profiles)
        {
            report.AppendLine($"- {profile.Name}: {profile.Width:0}x{profile.Height:0}");
        }

        report.AppendLine();

        foreach (var sceneGuid in AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" }))
        {
            var scenePath = AssetDatabase.GUIDToAssetPath(sceneGuid);
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            report.AppendLine(scene.path);

            var canvases = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Canvas>(true))
                .Distinct()
                .ToArray();

            if (canvases.Length == 0)
            {
                report.AppendLine("  No UI canvas found.");
                report.AppendLine();
                continue;
            }

            foreach (var canvas in canvases)
            {
                ValidateCanvas(canvas, report);
            }

            report.AppendLine();
        }

        File.WriteAllText(ReportPath, report.ToString());
        AssetDatabase.Refresh();
        Debug.Log($"Mobile responsive validation written to {ReportPath}");
    }

    private static void ValidateCanvas(Canvas canvas, StringBuilder report)
    {
        report.AppendLine($"  Canvas: {canvas.gameObject.name}");

        var scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            report.AppendLine("    Warning: missing CanvasScaler.");
        }
        else
        {
            report.AppendLine($"    CanvasScaler: mode={scaler.uiScaleMode} ref={scaler.referenceResolution.x:0}x{scaler.referenceResolution.y:0} match={scaler.matchWidthOrHeight:0.00}");
        }

        if (canvas.GetComponentsInChildren<Graphic>(true).Any() && canvas.GetComponentsInChildren<SafeAreaFitter>(true).Length == 0)
        {
            report.AppendLine("    Warning: canvas has UI graphics but no SafeAreaFitter.");
        }

        var candidatePanels = FindCandidatePanels(canvas).ToArray();
        foreach (var profile in Profiles)
        {
            var profileIssues = new List<string>();
            foreach (var panel in candidatePanels)
            {
                var estimation = TryEstimateRect(panel, new Vector2(profile.Width, profile.Height));
                if (!estimation.IsValid)
                {
                    continue;
                }

                if (estimation.LeftMargin < MinimumVisualMargin ||
                    estimation.RightMargin < MinimumVisualMargin ||
                    estimation.TopMargin < MinimumVisualMargin ||
                    estimation.BottomMargin < MinimumVisualMargin)
                {
                    profileIssues.Add($"{panel.name} margin too tight ({estimation.LeftMargin:0}/{estimation.RightMargin:0}/{estimation.TopMargin:0}/{estimation.BottomMargin:0}).");
                }

                if (panel.TryGetComponent(out VerticalLayoutGroup verticalLayout))
                {
                    var requiredHeight = EstimateRequiredHeight(verticalLayout);
                    if (requiredHeight > estimation.Height + 0.5f)
                    {
                        profileIssues.Add($"{panel.name} vertical content needs ~{requiredHeight:0}px but panel height is ~{estimation.Height:0}px.");
                    }
                }

                if (panel.TryGetComponent(out HorizontalLayoutGroup horizontalLayout))
                {
                    var availableChildWidth = EstimateAvailableChildWidth(horizontalLayout, estimation.Width);
                    foreach (var child in panel.Cast<Transform>())
                    {
                        if (child is not RectTransform childRect || !child.gameObject.activeSelf)
                        {
                            continue;
                        }

                        var layoutElement = child.GetComponent<LayoutElement>();
                        if (layoutElement == null || layoutElement.minWidth <= 0f)
                        {
                            continue;
                        }

                        if (availableChildWidth < layoutElement.minWidth)
                        {
                            profileIssues.Add($"{panel.name} gives ~{availableChildWidth:0}px per child, below min width {layoutElement.minWidth:0}px of {child.name}.");
                        }
                    }
                }
            }

            if (profileIssues.Count == 0)
            {
                report.AppendLine($"    {profile.Name}: ok");
            }
            else
            {
                report.AppendLine($"    {profile.Name}:");
                foreach (var issue in profileIssues)
                {
                    report.AppendLine($"      Warning: {issue}");
                }
            }
        }
    }

    private static IEnumerable<RectTransform> FindCandidatePanels(Canvas canvas)
    {
        var importantNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "SurfacePanel",
            "GameplayPanel",
            "LauncherPanel",
            "WaitingPanel",
            "ContentPanel",
            "CenterArea",
            "CardPanel"
        };

        return canvas.GetComponentsInChildren<RectTransform>(true)
            .Where(rect =>
                rect.GetComponent<ResponsivePanelLayoutController>() != null ||
                importantNames.Contains(rect.name))
            .Distinct();
    }

    private static EstimatedRect TryEstimateRect(RectTransform rectTransform, Vector2 canvasSize)
    {
        if (rectTransform == null)
        {
            return default;
        }

        var responsivePanel = rectTransform.GetComponent<ResponsivePanelLayoutController>();
        var width = rectTransform.rect.width;
        var height = rectTransform.rect.height;

        var stretchesHorizontally = Mathf.Abs(rectTransform.anchorMin.x - rectTransform.anchorMax.x) > 0.0001f;
        var stretchesVertically = Mathf.Abs(rectTransform.anchorMin.y - rectTransform.anchorMax.y) > 0.0001f;

        if (responsivePanel != null)
        {
            var serializedObject = new SerializedObject(responsivePanel);
            var widthRatio = serializedObject.FindProperty("widthRatio").floatValue;
            var heightRatio = serializedObject.FindProperty("heightRatio").floatValue;
            var minSize = serializedObject.FindProperty("minSize").vector2Value;
            var maxSize = serializedObject.FindProperty("maxSize").vector2Value;
            var outerMargin = serializedObject.FindProperty("outerMargin").vector2Value;
            var availableWidth = Mathf.Max(0f, canvasSize.x - outerMargin.x * 2f);
            var availableHeight = Mathf.Max(0f, canvasSize.y - outerMargin.y * 2f);
            width = Mathf.Min(availableWidth, Mathf.Clamp(availableWidth * widthRatio, minSize.x, maxSize.x));
            height = Mathf.Min(availableHeight, Mathf.Clamp(availableHeight * heightRatio, minSize.y, maxSize.y));
        }
        else if (stretchesHorizontally || stretchesVertically)
        {
            var minX = rectTransform.anchorMin.x * canvasSize.x + rectTransform.offsetMin.x;
            var maxX = rectTransform.anchorMax.x * canvasSize.x + rectTransform.offsetMax.x;
            var minY = rectTransform.anchorMin.y * canvasSize.y + rectTransform.offsetMin.y;
            var maxY = rectTransform.anchorMax.y * canvasSize.y + rectTransform.offsetMax.y;
            width = maxX - minX;
            height = maxY - minY;
        }
        else
        {
            width = rectTransform.sizeDelta.x;
            height = rectTransform.sizeDelta.y;
        }

        if (rectTransform.parent is RectTransform parentRect &&
            TryEstimateRect(parentRect, canvasSize) is { IsValid: true } parentEstimate)
        {
            if (parentRect.TryGetComponent(out VerticalLayoutGroup parentVerticalLayout))
            {
                width = Mathf.Max(
                    width,
                    parentEstimate.Width - parentVerticalLayout.padding.left - parentVerticalLayout.padding.right);
            }

            if (parentRect.TryGetComponent(out HorizontalLayoutGroup parentHorizontalLayout))
            {
                height = Mathf.Max(
                    height,
                    parentEstimate.Height - parentHorizontalLayout.padding.top - parentHorizontalLayout.padding.bottom);
            }
        }

        if (width <= 0f || height <= 0f)
        {
            return default;
        }

        var left = EstimateEdge(rectTransform.anchorMin.x, rectTransform.offsetMin.x, rectTransform.anchoredPosition.x, rectTransform.pivot.x, width, canvasSize.x, stretchesHorizontally, isMinEdge: true);
        var right = EstimateEdge(rectTransform.anchorMax.x, rectTransform.offsetMax.x, rectTransform.anchoredPosition.x, rectTransform.pivot.x, width, canvasSize.x, stretchesHorizontally, isMinEdge: false);
        var bottom = EstimateEdge(rectTransform.anchorMin.y, rectTransform.offsetMin.y, rectTransform.anchoredPosition.y, rectTransform.pivot.y, height, canvasSize.y, stretchesVertically, isMinEdge: true);
        var top = EstimateEdge(rectTransform.anchorMax.y, rectTransform.offsetMax.y, rectTransform.anchoredPosition.y, rectTransform.pivot.y, height, canvasSize.y, stretchesVertically, isMinEdge: false);

        return new EstimatedRect(true, width, height, left, right, top, bottom);
    }

    private static float EstimateEdge(float anchor, float offset, float anchoredPosition, float pivot, float size, float totalSize, bool isStretched, bool isMinEdge)
    {
        if (isStretched)
        {
            return isMinEdge
                ? anchor * totalSize + offset
                : totalSize - (anchor * totalSize + offset);
        }

        var pivotPosition = anchor * totalSize + anchoredPosition;
        var min = pivotPosition - pivot * size;
        var max = min + size;
        return isMinEdge ? min : totalSize - max;
    }

    private static float EstimateRequiredHeight(VerticalLayoutGroup layoutGroup)
    {
        float total = layoutGroup.padding.top + layoutGroup.padding.bottom;
        var activeChildren = 0;

        foreach (var child in layoutGroup.transform.Cast<Transform>())
        {
            if (child is not RectTransform childRect || !child.gameObject.activeSelf)
            {
                continue;
            }

            activeChildren++;
            var layoutElement = child.GetComponent<LayoutElement>();
            if (layoutElement != null)
            {
                if (layoutElement.preferredHeight > 0f)
                {
                    total += layoutElement.preferredHeight;
                    continue;
                }

                if (layoutElement.minHeight > 0f)
                {
                    total += layoutElement.minHeight;
                    continue;
                }

                if (layoutElement.flexibleHeight > 0f)
                {
                    continue;
                }
            }

            total += Mathf.Max(0f, childRect.sizeDelta.y);
        }

        if (activeChildren > 1)
        {
            total += layoutGroup.spacing * (activeChildren - 1);
        }

        return total;
    }

    private static float EstimateAvailableChildWidth(HorizontalLayoutGroup layoutGroup, float parentWidth)
    {
        var activeChildren = layoutGroup.transform.Cast<Transform>().Count(child => child.gameObject.activeSelf);
        if (activeChildren <= 0)
        {
            return 0f;
        }

        float innerWidth = parentWidth - layoutGroup.padding.left - layoutGroup.padding.right - layoutGroup.spacing * Mathf.Max(0, activeChildren - 1);
        return innerWidth / activeChildren;
    }

    private readonly struct DeviceProfile
    {
        public DeviceProfile(string name, float width, float height)
        {
            Name = name;
            Width = width;
            Height = height;
        }

        public string Name { get; }
        public float Width { get; }
        public float Height { get; }
    }

    private readonly struct EstimatedRect
    {
        public EstimatedRect(
            bool isValid,
            float width,
            float height,
            float leftMargin,
            float rightMargin,
            float topMargin,
            float bottomMargin)
        {
            IsValid = isValid;
            Width = width;
            Height = height;
            LeftMargin = leftMargin;
            RightMargin = rightMargin;
            TopMargin = topMargin;
            BottomMargin = bottomMargin;
        }

        public bool IsValid { get; }
        public float Width { get; }
        public float Height { get; }
        public float LeftMargin { get; }
        public float RightMargin { get; }
        public float TopMargin { get; }
        public float BottomMargin { get; }
    }
}
