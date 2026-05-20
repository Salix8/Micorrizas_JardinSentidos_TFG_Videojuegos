using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class SceneLoadButtonUIController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Button targetButton;

    [Header("Scene Routing")]
    [SerializeField] private string targetSceneName = "Dialogue";
    [SerializeField] private LoadSceneMode loadSceneMode = LoadSceneMode.Single;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();

        if (targetButton != null)
        {
            targetButton.onClick.RemoveListener(LoadTargetScene);
            targetButton.onClick.AddListener(LoadTargetScene);
        }
    }

    private void OnDisable()
    {
        if (targetButton != null)
        {
            targetButton.onClick.RemoveListener(LoadTargetScene);
        }
    }

    public void LoadTargetScene()
    {
        if (string.IsNullOrWhiteSpace(targetSceneName))
        {
            Debug.LogWarning($"{nameof(SceneLoadButtonUIController)} has no target scene configured.", this);
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(targetSceneName))
        {
            Debug.LogWarning($"Scene '{targetSceneName}' is not available in build settings.", this);
            return;
        }

        SceneManager.LoadScene(targetSceneName, loadSceneMode);
    }

    private void ResolveReferences()
    {
        targetButton ??= GetComponent<Button>();
    }
}
