using System;
using System.IO;
using SmartCampus.Coop.Minigames;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class TutorialPopupPrefabUtility
{
    private const string TutorialPopupPrefabPath = "Assets/Prefabs/TutorialPopup.prefab";

    private static readonly string[] MinigameScenePaths =
    {
        "Assets/Scenes/GardenImageVotingMinigame.unity",
        "Assets/Scenes/AudioWordConsensusMinigame.unity",
        "Assets/Scenes/CollaborativePlantGuessMinigame.unity",
        "Assets/Scenes/DistributedPairsMinigame.unity",
        "Assets/Scenes/GardenSmellTaxonomyMinigame.unity",
        "Assets/Scenes/PlantPhotoRelayMinigame.unity"
    };

    public static TutorialPopupController InstantiateTutorialPopup(Transform parent)
    {
        if (parent == null)
        {
            throw new ArgumentNullException(nameof(parent));
        }

        var popupPrefab = LoadTutorialPopupPrefab();
        var popupInstance = PrefabUtility.InstantiatePrefab(popupPrefab, parent) as GameObject;
        if (popupInstance == null)
        {
            throw new InvalidOperationException("No se ha podido instanciar el prefab TutorialPopup.");
        }

        popupInstance.name = "TutorialPopup";

        var popupRect = popupInstance.GetComponent<RectTransform>();
        if (popupRect != null)
        {
            StretchToParent(popupRect);
        }

        NormalizeNestedRectTransformOverrides(popupInstance.transform);

        var controller = popupInstance.GetComponent<TutorialPopupController>();
        if (controller == null)
        {
            throw new InvalidOperationException("El prefab TutorialPopup no contiene TutorialPopupController.");
        }

        return controller;
    }

    [MenuItem("Tools/Coop/Replace TutorialPopup With Prefab In Minigame Scenes")]
    public static void ReplaceTutorialPopupWithPrefabInMinigameScenes()
    {
        var popupPrefab = LoadTutorialPopupPrefab();
        var previousScenePath = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;

        try
        {
            foreach (var scenePath in MinigameScenePaths)
            {
                ReplaceTutorialPopupInScene(scenePath, popupPrefab);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        finally
        {
            if (!string.IsNullOrWhiteSpace(previousScenePath) &&
                File.Exists(Path.Combine(Directory.GetCurrentDirectory(), previousScenePath)))
            {
                EditorSceneManager.OpenScene(previousScenePath, OpenSceneMode.Single);
            }
        }
    }

    private static void ReplaceTutorialPopupInScene(string scenePath, GameObject popupPrefab)
    {
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        var uiController = UnityEngine.Object.FindFirstObjectByType<MinigameUIControllerBase>(FindObjectsInactive.Include);
        if (uiController == null)
        {
            throw new InvalidOperationException($"No se ha encontrado MinigameUIControllerBase en {scenePath}.");
        }

        var serializedUiController = new SerializedObject(uiController);
        var tutorialPopupProperty = serializedUiController.FindProperty("tutorialPopupController");
        var currentPopup = tutorialPopupProperty.objectReferenceValue as TutorialPopupController;
        currentPopup ??= UnityEngine.Object.FindFirstObjectByType<TutorialPopupController>(FindObjectsInactive.Include);

        var popupParent = currentPopup != null ? currentPopup.transform.parent : uiController.transform;
        var popupSiblingIndex = currentPopup != null ? currentPopup.transform.GetSiblingIndex() : popupParent.childCount;
        var popupWasActive = currentPopup == null || currentPopup.gameObject.activeSelf;
        var currentRect = currentPopup != null ? currentPopup.GetComponent<RectTransform>() : null;

        var popupInstance = PrefabUtility.InstantiatePrefab(popupPrefab, popupParent) as GameObject;
        if (popupInstance == null)
        {
            throw new InvalidOperationException($"No se ha podido instanciar TutorialPopup en {scenePath}.");
        }

        popupInstance.name = "TutorialPopup";
        popupInstance.SetActive(popupWasActive);

        var popupRect = popupInstance.GetComponent<RectTransform>();
        if (popupRect != null)
        {
            if (currentRect != null)
            {
                CopyRectTransform(currentRect, popupRect);
            }
            else
            {
                StretchToParent(popupRect);
            }

            popupRect.SetSiblingIndex(Mathf.Clamp(popupSiblingIndex, 0, popupParent.childCount - 1));
        }

        NormalizeNestedRectTransformOverrides(popupInstance.transform);

        var popupController = popupInstance.GetComponent<TutorialPopupController>();
        if (popupController == null)
        {
            throw new InvalidOperationException($"La instancia TutorialPopup no contiene TutorialPopupController en {scenePath}.");
        }

        tutorialPopupProperty.objectReferenceValue = popupController;
        serializedUiController.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(uiController);

        if (currentPopup != null && currentPopup.gameObject != popupInstance)
        {
            UnityEngine.Object.DestroyImmediate(currentPopup.gameObject);
        }

        RemoveDuplicateTutorialPopups(scene, popupController);

        EditorSceneManager.SaveScene(scene);
    }

    private static void RemoveDuplicateTutorialPopups(UnityEngine.SceneManagement.Scene scene, TutorialPopupController expectedPopup)
    {
        var tutorialPopups = UnityEngine.Object.FindObjectsByType<TutorialPopupController>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (var popup in tutorialPopups)
        {
            if (popup == null || popup == expectedPopup || popup.gameObject.scene != scene)
            {
                continue;
            }

            UnityEngine.Object.DestroyImmediate(popup.gameObject);
        }
    }

    private static GameObject LoadTutorialPopupPrefab()
    {
        var popupPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TutorialPopupPrefabPath);
        if (popupPrefab == null)
        {
            throw new InvalidOperationException($"No se ha encontrado el prefab en {TutorialPopupPrefabPath}.");
        }

        return popupPrefab;
    }

    private static void StretchToParent(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = Vector2.zero;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        rectTransform.localScale = Vector3.one;
        rectTransform.localRotation = Quaternion.identity;
        rectTransform.localPosition = Vector3.zero;
    }

    private static void CopyRectTransform(RectTransform source, RectTransform target)
    {
        target.anchorMin = source.anchorMin;
        target.anchorMax = source.anchorMax;
        target.pivot = source.pivot;
        target.anchoredPosition = source.anchoredPosition;
        target.sizeDelta = source.sizeDelta;
        target.offsetMin = source.offsetMin;
        target.offsetMax = source.offsetMax;
        target.localScale = source.localScale;
        target.localRotation = source.localRotation;
        target.localPosition = source.localPosition;
    }

    private static void NormalizeNestedRectTransformOverrides(Transform popupRoot)
    {
        if (popupRoot == null)
        {
            return;
        }

        var rectTransforms = popupRoot.GetComponentsInChildren<RectTransform>(true);
        foreach (var rectTransform in rectTransforms)
        {
            if (rectTransform == null || rectTransform.transform == popupRoot)
            {
                continue;
            }

            if (PrefabUtility.IsPartOfPrefabInstance(rectTransform))
            {
                PrefabUtility.RevertObjectOverride(rectTransform, InteractionMode.AutomatedAction);
            }
        }
    }
}
