using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class LobbyMarkerShapeOptionView : MonoBehaviour
{
    [SerializeField] private string shapeId = string.Empty;
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text label;
    [SerializeField] private Image selectionOutline;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Color normalBackgroundColor = new(0.67f, 0.58f, 0.46f, 0.84f);
    [SerializeField] private Color selectedBackgroundColor = new(0.94f, 0.86f, 0.65f, 1f);
    [SerializeField] private Color normalLabelColor = new(0.96f, 0.87f, 0.74f, 1f);
    [SerializeField] private Color selectedLabelColor = new(0.27f, 0.18f, 0.09f, 1f);

    public string ShapeId => shapeId;
    public Button Button => button;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnValidate()
    {
        ResolveReferences();
    }

    public void Configure(string displayName)
    {
        ResolveReferences();

        if (label != null)
        {
            label.text = displayName;
            label.color = normalLabelColor;
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
        label ??= GetComponentInChildren<TMP_Text>(true);

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
