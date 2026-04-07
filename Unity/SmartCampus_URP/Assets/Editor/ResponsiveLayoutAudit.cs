using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using SmartCampus.Coop.Minigames;

public static class ResponsiveLayoutAudit
{
    private const string ReportPath = "C:/Users/saulp/Documents/UJI/Micorrizas_JardinSentidos_TFG_Videojuegos/responsive-layout-audit.txt";

    [MenuItem("Tools/Coop/Audit Responsive Layout")]
    public static void GenerateReport()
    {
        var report = new StringBuilder();
        report.AppendLine("Responsive Layout Audit");
        report.AppendLine("======================");
        report.AppendLine();

        foreach (var sceneSetting in EditorBuildSettings.scenes)
        {
            if (!sceneSetting.enabled || !File.Exists(sceneSetting.path))
            {
                continue;
            }

            var scene = EditorSceneManager.OpenScene(sceneSetting.path, OpenSceneMode.Single);
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var scalers = Object.FindObjectsByType<CanvasScaler>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var safeAreas = Object.FindObjectsByType<SafeAreaFitter>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var scrollRects = Object.FindObjectsByType<ScrollRect>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var responsivePanels = Object.FindObjectsByType<ResponsivePanelLayoutController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var responsiveAspectLayouts = Object.FindObjectsByType<ResponsiveAspectRatioLayoutController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var responsiveGrids = Object.FindObjectsByType<ResponsiveGridLayoutController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var contentFitters = Object.FindObjectsByType<ContentSizeFitter>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            report.AppendLine(scene.path);
            report.AppendLine($"  Canvases: {canvases.Length}");
            report.AppendLine($"  CanvasScalers: {scalers.Length}");
            report.AppendLine($"  SafeAreaFitters: {safeAreas.Length}");
            report.AppendLine($"  ScrollRects: {scrollRects.Length}");
            report.AppendLine($"  ResponsivePanels: {responsivePanels.Length}");
            report.AppendLine($"  ResponsiveAspectLayouts: {responsiveAspectLayouts.Length}");
            report.AppendLine($"  ResponsiveGrids: {responsiveGrids.Length}");
            report.AppendLine($"  ContentSizeFitters: {contentFitters.Length}");

            foreach (var scaler in scalers)
            {
                report.AppendLine(
                    $"    CanvasScaler[{scaler.gameObject.name}] mode={scaler.uiScaleMode} ref={scaler.referenceResolution.x}x{scaler.referenceResolution.y} match={scaler.matchWidthOrHeight:0.00}");
            }

            if (canvases.Length > 0 && safeAreas.Length == 0)
            {
                report.AppendLine("    Warning: scene has Canvas but no SafeAreaFitter.");
            }

            foreach (var issue in FindMissingComponentWarnings(scene))
            {
                report.AppendLine($"    Warning: {issue}");
            }

            report.AppendLine();
        }

        report.AppendLine("PlayerSettings");
        report.AppendLine($"  defaultInterfaceOrientation: {PlayerSettings.defaultInterfaceOrientation}");
        report.AppendLine($"  allowedPortrait: {PlayerSettings.allowedAutorotateToPortrait}");
        report.AppendLine($"  allowedPortraitUpsideDown: {PlayerSettings.allowedAutorotateToPortraitUpsideDown}");
        report.AppendLine($"  allowedLandscapeLeft: {PlayerSettings.allowedAutorotateToLandscapeLeft}");
        report.AppendLine($"  allowedLandscapeRight: {PlayerSettings.allowedAutorotateToLandscapeRight}");

        File.WriteAllText(ReportPath, report.ToString());
        AssetDatabase.Refresh();
        Debug.Log($"Responsive layout audit written to {ReportPath}");
    }

    private static System.Collections.Generic.IEnumerable<string> FindMissingComponentWarnings(Scene scene)
    {
        foreach (var rootObject in scene.GetRootGameObjects())
        {
            foreach (var transform in rootObject.GetComponentsInChildren<Transform>(true))
            {
                var components = transform.GetComponents<Component>();
                for (var index = 0; index < components.Length; index++)
                {
                    if (components[index] == null)
                    {
                        yield return $"{transform.name} has missing component at index {index}.";
                    }
                }
            }
        }
    }
}
