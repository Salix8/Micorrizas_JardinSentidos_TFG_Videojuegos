using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class LobbyAdventurerPassUIController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private LocalPlayerMarkerProfileService profileService;
    [SerializeField] private PlayerMarkerAppearanceCatalogConfig appearanceCatalog;
    [SerializeField] private TMP_InputField playerNameInput;
    [SerializeField] private Image previewImage;
    [SerializeField] private Image previewFrameImage;
    [SerializeField] private LobbyMarkerAvatarOptionView[] avatarOptions = new LobbyMarkerAvatarOptionView[0];

    private bool suppressInputCallback;

    private void Awake()
    {
        ResolveReferences();
        ConfigureOptionViews();
    }

    private void OnEnable()
    {
        ResolveReferences();
        ConfigureOptionViews();
        profileService?.EnsureInitialized();

        if (playerNameInput != null)
        {
            playerNameInput.onValueChanged.AddListener(HandlePlayerNameChanged);
        }

        if (profileService != null)
        {
            profileService.ProfileChanged -= HandleProfileChanged;
            profileService.ProfileChanged += HandleProfileChanged;
        }

        SyncUiFromProfile();
        RebuildPreview();
    }

    private void OnDisable()
    {
        if (playerNameInput != null)
        {
            playerNameInput.onValueChanged.RemoveListener(HandlePlayerNameChanged);
        }

        if (profileService != null)
        {
            profileService.ProfileChanged -= HandleProfileChanged;
        }
    }

    private void HandlePlayerNameChanged(string playerName)
    {
        if (suppressInputCallback || profileService == null)
        {
            return;
        }

        profileService.SetDisplayName(playerName);
    }

    private void HandleProfileChanged()
    {
        SyncUiFromProfile();
        RebuildPreview();
    }

    private void ConfigureOptionViews()
    {
        if (profileService != null && appearanceCatalog != null)
        {
            var resolvedAvatarId = appearanceCatalog.ResolveAvatarIdOrDefault(profileService.CurrentAvatarId);
            if (!string.Equals(profileService.CurrentAvatarId, resolvedAvatarId))
            {
                profileService.SetAvatarId(resolvedAvatarId);
            }
        }

        for (var index = 0; index < avatarOptions.Length; index++)
        {
            var option = avatarOptions[index];
            if (option == null || option.Button == null)
            {
                continue;
            }

            option.Button.onClick.RemoveAllListeners();
            var capturedAvatarId = option.AvatarId;
            option.Button.onClick.AddListener(() => SelectAvatar(capturedAvatarId));

            if (appearanceCatalog != null &&
                appearanceCatalog.TryGetAvatar(capturedAvatarId, out var avatar) &&
                avatar != null &&
                avatar.AvatarSprite != null)
            {
                option.Configure(avatar.AvatarId, avatar.AvatarSprite);
                option.gameObject.SetActive(true);
            }
            else
            {
                option.gameObject.SetActive(false);
            }
        }

        RefreshAvatarOptionsLayout();
    }

    private void SyncUiFromProfile()
    {
        if (profileService == null)
        {
            return;
        }

        if (playerNameInput != null)
        {
            suppressInputCallback = true;
            if (!string.Equals(playerNameInput.text, profileService.CurrentDisplayName))
            {
                playerNameInput.SetTextWithoutNotify(profileService.CurrentDisplayName);
            }

            suppressInputCallback = false;
        }

        for (var index = 0; index < avatarOptions.Length; index++)
        {
            if (avatarOptions[index] != null)
            {
                avatarOptions[index].SetSelected(string.Equals(avatarOptions[index].AvatarId, profileService.CurrentAvatarId));
            }
        }
    }

    private void SelectAvatar(string avatarId)
    {
        profileService?.SetAvatarId(avatarId);
    }

    private void RebuildPreview()
    {
        if (previewImage == null || profileService == null)
        {
            return;
        }

        if (!profileService.TryGetSelectedAvatar(out var avatar) || avatar == null || avatar.AvatarSprite == null)
        {
            previewImage.enabled = false;
            return;
        }

        previewImage.enabled = true;
        previewImage.sprite = avatar.AvatarSprite;
        previewImage.color = Color.white;
        previewImage.preserveAspect = true;

        var previewRect = previewImage.rectTransform;
        previewRect.sizeDelta = avatar.PreviewSize;

        if (previewFrameImage != null)
        {
            previewFrameImage.color = new Color(0.25f, 0.34f, 0.18f, 0.96f);
        }
    }

    private void RefreshAvatarOptionsLayout()
    {
        if (avatarOptions == null || avatarOptions.Length == 0)
        {
            return;
        }

        var firstOption = avatarOptions[0];
        if (firstOption == null || firstOption.transform.parent == null)
        {
            return;
        }

        var container = firstOption.transform.parent;
        var grid = container.GetComponent<GridLayoutGroup>();
        var layoutElement = container.GetComponent<LayoutElement>();
        if (grid == null || layoutElement == null)
        {
            return;
        }

        var activeOptionCount = 0;
        for (var index = 0; index < avatarOptions.Length; index++)
        {
            if (avatarOptions[index] != null && avatarOptions[index].gameObject.activeSelf)
            {
                activeOptionCount++;
            }
        }

        var columns = Mathf.Max(1, grid.constraintCount);
        var rows = Mathf.Max(1, Mathf.CeilToInt(activeOptionCount / (float)columns));
        var preferredHeight = (rows * grid.cellSize.y) + ((rows - 1) * grid.spacing.y);
        layoutElement.minHeight = grid.cellSize.y;
        layoutElement.preferredHeight = preferredHeight;
        LayoutRebuilder.MarkLayoutForRebuild(container as RectTransform);
    }

    private void ResolveReferences()
    {
        if (profileService == null)
        {
            profileService = FindFirstObjectByType<LocalPlayerMarkerProfileService>(FindObjectsInactive.Include);
        }

        if (appearanceCatalog == null && profileService != null)
        {
            appearanceCatalog = profileService.AppearanceCatalog;
        }

        if (previewFrameImage == null && previewImage != null && previewImage.transform.parent != null)
        {
            previewFrameImage = previewImage.transform.parent.GetComponent<Image>();
        }
    }
}
