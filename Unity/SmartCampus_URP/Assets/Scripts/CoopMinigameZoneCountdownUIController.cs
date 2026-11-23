using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class CoopMinigameZoneCountdownUIController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject contentRoot;
    [SerializeField] private Image progressFillImage;
    [SerializeField] private TMP_Text titleLabel;
    [SerializeField] private TMP_Text subtitleLabel;
    [SerializeField] private TMP_Text timerLabel;

    [Header("Labels")]
    [SerializeField] private string defaultTitle = "Zona cooperativa";
    [SerializeField] private string subtitleFormat = "Manteneos dentro de {0}";

    [SerializeField] private string retryTitle = "Misión pendiente";
    [SerializeField] [TextArea] private string retryMessage = "Debéis observar mejor la zona antes de aventuraros de nuevo en esta misión.";

    private void Awake()
    {
        ResolveReferences();
        Hide();
    }

    public void Show(string zoneDisplayName, float progress01, float remainingSeconds)
    {
        ResolveReferences();

        if (contentRoot != null && !contentRoot.activeSelf)
        {
            contentRoot.SetActive(true);
        }

        if (progressFillImage != null)
        {
            progressFillImage.fillAmount = Mathf.Clamp01(progress01);
        }

        if (titleLabel != null)
        {
            titleLabel.text = defaultTitle;
        }

        if (subtitleLabel != null)
        {
            subtitleLabel.text = string.Format(subtitleFormat, zoneDisplayName);
        }

        if (timerLabel != null)
        {
            timerLabel.text = Mathf.Max(0f, remainingSeconds).ToString("0.0s");
        }
    }

    public void ShowRetryWait(string zoneDisplayName, float remainingSeconds, float durationSeconds)
    {
        Show(zoneDisplayName, durationSeconds > 0f ? 1f - remainingSeconds / durationSeconds : 1f, remainingSeconds);
        if (titleLabel != null) titleLabel.text = retryTitle;
        if (subtitleLabel != null) subtitleLabel.text = retryMessage;
        if (timerLabel != null) timerLabel.text = Mathf.CeilToInt(Mathf.Max(0f, remainingSeconds)) + " s";
    }

    public void Hide()
    {
        ResolveReferences();

        if (progressFillImage != null)
        {
            progressFillImage.fillAmount = 0f;
        }

        if (contentRoot != null)
        {
            contentRoot.SetActive(false);
        }
    }

    private void ResolveReferences()
    {
        if (contentRoot == null)
        {
            contentRoot = gameObject;
        }

        progressFillImage ??= FindImage("ProgressFill");
        titleLabel ??= FindText("TitleLabel");
        subtitleLabel ??= FindText("SubtitleLabel");
        timerLabel ??= FindText("TimerLabel");
    }

    private Image FindImage(string childName)
    {
        var child = FindChildRecursive(transform, childName);
        return child != null ? child.GetComponent<Image>() : null;
    }

    private TMP_Text FindText(string childName)
    {
        var child = FindChildRecursive(transform, childName);
        return child != null ? child.GetComponent<TMP_Text>() : null;
    }

    private static Transform FindChildRecursive(Transform parent, string childName)
    {
        if (parent == null)
        {
            return null;
        }

        for (var index = 0; index < parent.childCount; index++)
        {
            var child = parent.GetChild(index);
            if (child.name == childName)
            {
                return child;
            }

            var nested = FindChildRecursive(child, childName);
            if (nested != null)
            {
                return nested;
            }
        }

        return null;
    }
}
