using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class LobbyMarkerAvatarOptionView : MonoBehaviour
{
    [SerializeField] private string avatarId = string.Empty;
    [SerializeField] private Button button;
    [SerializeField] private Image avatarImage;
    [SerializeField] private GameObject selectionBorderRoot;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Color normalBackgroundColor = new(0.86f, 0.8f, 0.68f, 0.94f);
    [SerializeField] private Color normalAvatarColor = Color.white;
    [SerializeField] private Color selectedAvatarColor = Color.white;
    [SerializeField] private Color selectionBorderColor = new(0.13f, 0.31f, 0.14f, 1f);
    [SerializeField] [Min(1f)] private float selectionBorderThickness = 5f;

    public string AvatarId => avatarId;
    public Button Button => button;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnValidate()
    {
        ResolveReferences();
    }

    public void Configure(string id, Sprite avatarSprite)
    {
        avatarId = id;
        ResolveReferences();

        if (avatarImage != null)
        {
            avatarImage.sprite = avatarSprite;
            avatarImage.color = normalAvatarColor;
            avatarImage.preserveAspect = true;
        }
    }

    public void SetSelected(bool selected)
    {
        ResolveReferences();

        if (selectionBorderRoot != null)
        {
            selectionBorderRoot.SetActive(selected);
        }

        if (backgroundImage != null)
        {
            backgroundImage.color = normalBackgroundColor;
        }

        if (avatarImage != null)
        {
            avatarImage.color = selected ? selectedAvatarColor : normalAvatarColor;
        }
    }

    private void ResolveReferences()
    {
        button ??= GetComponent<Button>();
        backgroundImage ??= GetComponent<Image>();

        if (avatarImage == null)
        {
            var avatarTransform = transform.Find("AvatarImage");
            if (avatarTransform != null)
            {
                avatarImage = avatarTransform.GetComponent<Image>();
            }
        }

        if (selectionBorderRoot == null)
        {
            var borderTransform = transform.Find("SelectionBorder") ?? transform.Find("SelectionOutline");
            if (borderTransform != null)
            {
                selectionBorderRoot = borderTransform.gameObject;
            }
        }

        EnsureSelectionBorder();
    }

    private void EnsureSelectionBorder()
    {
        if (selectionBorderRoot == null)
        {
            selectionBorderRoot = new GameObject("SelectionBorder", typeof(RectTransform));
            selectionBorderRoot.transform.SetParent(transform, false);
            var rect = selectionBorderRoot.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(3f, 3f);
            rect.offsetMax = new Vector2(-3f, -3f);
        }
        else if (selectionBorderRoot.name == "SelectionOutline")
        {
            selectionBorderRoot.name = "SelectionBorder";
        }

        var legacyFill = selectionBorderRoot.GetComponent<Image>();
        if (legacyFill != null)
        {
            legacyFill.enabled = false;
            legacyFill.raycastTarget = false;
        }

        EnsureBorderSegment("BorderTop", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -selectionBorderThickness), Vector2.zero);
        EnsureBorderSegment("BorderBottom", Vector2.zero, new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, selectionBorderThickness));
        EnsureBorderSegment("BorderLeft", Vector2.zero, new Vector2(0f, 1f), new Vector2(0f, selectionBorderThickness), new Vector2(selectionBorderThickness, -selectionBorderThickness));
        EnsureBorderSegment("BorderRight", new Vector2(1f, 0f), Vector2.one, new Vector2(-selectionBorderThickness, selectionBorderThickness), new Vector2(0f, -selectionBorderThickness));
    }

    private void EnsureBorderSegment(string segmentName, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        var segmentTransform = selectionBorderRoot.transform.Find(segmentName);
        if (segmentTransform == null)
        {
            var segment = new GameObject(segmentName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            segment.transform.SetParent(selectionBorderRoot.transform, false);
            segmentTransform = segment.transform;
        }

        var rect = segmentTransform.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;

        var image = segmentTransform.GetComponent<Image>();
        image.color = selectionBorderColor;
        image.raycastTarget = false;
    }
}
