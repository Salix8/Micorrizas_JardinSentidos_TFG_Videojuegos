using UnityEngine;
using TMPro;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class MinigameLauncherToggleUIController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject targetPanel;
    [SerializeField] private Button toggleButton;
    [SerializeField] private TMP_Text toggleButtonLabel;

    [Header("Labels")]
    [SerializeField] private string visibleStateLabel = "Ocultar\nminijuegos";
    [SerializeField] private string hiddenStateLabel = "Mostrar\nminijuegos";

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();

        if (toggleButton != null)
        {
            toggleButton.onClick.RemoveListener(ToggleTargetPanel);
            toggleButton.onClick.AddListener(ToggleTargetPanel);
        }

        RefreshLabel();
    }

    private void OnDisable()
    {
        if (toggleButton != null)
        {
            toggleButton.onClick.RemoveListener(ToggleTargetPanel);
        }
    }

    public void ToggleTargetPanel()
    {
        ResolveReferences();
        if (targetPanel == null)
        {
            return;
        }

        targetPanel.SetActive(!targetPanel.activeSelf);
        RefreshLabel();
    }

    private void RefreshLabel()
    {
        if (toggleButtonLabel == null)
        {
            return;
        }

        var isVisible = targetPanel == null || targetPanel.activeSelf;
        toggleButtonLabel.text = isVisible ? visibleStateLabel : hiddenStateLabel;
    }

    private void ResolveReferences()
    {
        if (targetPanel == null)
        {
            var panel = GameObject.Find("CoopMinigameLauncherCanvas");
            if (panel != null)
            {
                targetPanel = panel;
            }
        }

        toggleButton ??= GetComponentInChildren<Button>(true);
        if (toggleButtonLabel == null && toggleButton != null)
        {
            toggleButtonLabel = toggleButton.GetComponentInChildren<TMP_Text>(true);
        }
    }
}
