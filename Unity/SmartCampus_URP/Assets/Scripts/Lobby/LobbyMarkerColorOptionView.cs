using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class LobbyMarkerColorOptionView : MonoBehaviour
{
    [SerializeField] private string colorId = string.Empty;
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text label;
    [SerializeField] private Image swatchImage;
    [SerializeField] private Image selectionOutline;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Color normalLabelColor = new(0.29f, 0.22f, 0.17f, 1f);
    [SerializeField] private Color selectedLabelColor = new(0.98f, 0.95f, 0.88f, 1f);
    [SerializeField] private Color normalBackgroundColor = new(0.96f, 0.91f, 0.82f, 0.92f);
    [SerializeField] private Color selectedBackgroundColor = new(0.42f, 0.29f, 0.16f, 1f);

    public string ColorId => colorId;
    public Button Button => button;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnValidate()
    {
        ResolveReferences();
    }

    public void Configure(string displayName, Color color)
    {
        ResolveReferences();

        if (label != null)
        {
            label.text = displayName;
            label.color = normalLabelColor;
        }

        if (swatchImage != null)
        {
            swatchImage.color = color;
        }
    }

    public void SetSelected(bool selected)
    {
        ResolveReferences();

        if (selectionOutline != null)
        {
            selectionOutline.enabled = selected;
        }

        if (backgroundImage != null)
        {
            backgroundImage.color = selected ? selectedBackgroundColor : normalBackgroundColor;
        }

        if (label != null)
        {
            label.color = selected ? selectedLabelColor : normalLabelColor;
        }
    }

    private void ResolveReferences()
    {
        button ??= GetComponent<Button>();
        backgroundImage ??= GetComponent<Image>();

        if (label == null)
        {
            label = GetComponentInChildren<TMP_Text>(true);
        }

        if (swatchImage == null)
        {
            var swatchTransform = transform.Find("Swatch");
            if (swatchTransform != null)
            {
                swatchImage = swatchTransform.GetComponent<Image>();
            }
        }

        if (selectionOutline == null)
        {
            var outlineTransform = transform.Find("SelectionOutline");
            if (outlineTransform != null)
            {
                selectionOutline = outlineTransform.GetComponent<Image>();
            }
        }
    }
}
