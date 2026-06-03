using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class LobbyMarkerAvatarOptionView : MonoBehaviour
{
    [SerializeField] private string avatarId = string.Empty;
    [SerializeField] private Button button;
    [SerializeField] private Image avatarImage;
    [SerializeField] private Image selectionOutline;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Color normalBackgroundColor = new(0.86f, 0.8f, 0.68f, 0.94f);
    [SerializeField] private Color selectedBackgroundColor = new(0.25f, 0.34f, 0.18f, 1f);
    [SerializeField] private Color normalAvatarColor = Color.white;
    [SerializeField] private Color selectedAvatarColor = Color.white;

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

        if (selectionOutline != null)
        {
            selectionOutline.enabled = selected;
        }

        if (backgroundImage != null)
        {
            backgroundImage.color = selected ? selectedBackgroundColor : normalBackgroundColor;
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
