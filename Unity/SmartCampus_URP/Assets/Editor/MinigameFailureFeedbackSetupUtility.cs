using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using SmartCampus.Coop.Minigames;

public static class MinigameFailureFeedbackSetupUtility
{
    private const string SharedConfigFolder = "Assets/CoopMinigames/Configs";
    private const string SharedConfigPath = SharedConfigFolder + "/MinigameFailureFeedbackConfig.asset";

    public static MinigameFailureFeedbackController CreateOrUpdateFailureFeedback(GameObject uiRoot, GameObject gameplayPanel)
    {
        if (uiRoot == null)
        {
            return null;
        }

        var feedbackRoot = FindOrCreateChild(uiRoot.transform, "FailureFeedback");
        var feedbackRect = EnsureComponent<RectTransform>(feedbackRoot);
        feedbackRect.anchorMin = Vector2.zero;
        feedbackRect.anchorMax = Vector2.one;
        feedbackRect.offsetMin = Vector2.zero;
        feedbackRect.offsetMax = Vector2.zero;
        feedbackRect.SetAsLastSibling();

        var audioSource = EnsureComponent<AudioSource>(feedbackRoot);
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;

        var flashOverlay = FindOrCreateUiChild(feedbackRoot.transform, "FailureFlashOverlay");
        var flashRect = flashOverlay.GetComponent<RectTransform>();
        flashRect.anchorMin = Vector2.zero;
        flashRect.anchorMax = Vector2.one;
        flashRect.offsetMin = Vector2.zero;
        flashRect.offsetMax = Vector2.zero;
        flashRect.SetAsLastSibling();

        var flashImage = EnsureComponent<Image>(flashOverlay);
        flashImage.raycastTarget = false;
        var flashColor = flashImage.color;
        flashColor.a = 0f;
        flashImage.color = flashColor;
        flashOverlay.SetActive(false);

        var feedbackController = EnsureComponent<MinigameFailureFeedbackController>(feedbackRoot);
        var sharedConfig = GetOrCreateSharedConfig();
        var serializedFeedback = new SerializedObject(feedbackController);
        serializedFeedback.FindProperty("sharedConfig").objectReferenceValue = sharedConfig;
        serializedFeedback.FindProperty("shakeTarget").objectReferenceValue = gameplayPanel != null
            ? gameplayPanel.GetComponent<RectTransform>()
            : uiRoot.GetComponent<RectTransform>();
        serializedFeedback.FindProperty("flashOverlay").objectReferenceValue = flashImage;
        serializedFeedback.FindProperty("feedbackAudioSource").objectReferenceValue = audioSource;
        serializedFeedback.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(feedbackRoot);
        return feedbackController;
    }

    public static void AssignToUiController(SerializedObject serializedUiController, MinigameFailureFeedbackController feedbackController)
    {
        if (serializedUiController == null)
        {
            return;
        }

        serializedUiController.FindProperty("failureFeedbackController").objectReferenceValue = feedbackController;
    }

    private static GameObject FindOrCreateChild(Transform parent, string name)
    {
        var child = parent.Find(name);
        if (child != null)
        {
            return child.gameObject;
        }

        var childObject = new GameObject(name);
        childObject.layer = parent.gameObject.layer;
        childObject.transform.SetParent(parent, false);
        return childObject;
    }

    private static GameObject FindOrCreateUiChild(Transform parent, string name)
    {
        var childObject = FindOrCreateChild(parent, name);
        EnsureComponent<CanvasRenderer>(childObject);
        EnsureComponent<RectTransform>(childObject);
        return childObject;
    }

    private static T EnsureComponent<T>(GameObject target) where T : Component
    {
        var component = target.GetComponent<T>();
        if (component == null)
        {
            component = target.AddComponent<T>();
        }

        return component;
    }

    private static MinigameFailureFeedbackConfig GetOrCreateSharedConfig()
    {
        EnsureFolder("Assets", "CoopMinigames");
        EnsureFolder("Assets/CoopMinigames", "Configs");

        var config = AssetDatabase.LoadAssetAtPath<MinigameFailureFeedbackConfig>(SharedConfigPath);
        if (config != null)
        {
            return config;
        }

        config = ScriptableObject.CreateInstance<MinigameFailureFeedbackConfig>();
        AssetDatabase.CreateAsset(config, SharedConfigPath);
        EditorUtility.SetDirty(config);
        AssetDatabase.SaveAssets();
        return config;
    }

    private static void EnsureFolder(string parent, string name)
    {
        var fullPath = $"{parent}/{name}";
        if (!AssetDatabase.IsValidFolder(fullPath))
        {
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
