using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SmartCampus.Coop.Minigames
{
    [DisallowMultipleComponent]
    public sealed class CoopMinigameTopPanelView : MonoBehaviour
    {
        [Header("Theme")]
        [SerializeField] private CoopMinigameThemeConfig themeConfig;

        [Header("Backgrounds")]
        [SerializeField] private RoundedPanelGraphic backgroundGraphic;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image titleBandImage;

        [Header("Brand")]
        [SerializeField] private RoundedPanelGraphic logoFrameGraphic;
        [SerializeField] private Image logoImage;
        [SerializeField] private Sprite logoOverrideSprite;
        [SerializeField] private bool hideLogo;
        [SerializeField] private TMP_Text projectTitleLabel;
        [SerializeField] private TMP_Text projectSubtitleLabel;

        [Header("Progress")]
        [SerializeField] private TMP_Text progressTitleLabel;
        [SerializeField] private TMP_Text progressPercentLabel;
        [SerializeField] private Slider progressSlider;
        [SerializeField] private Image progressFillImage;
        [SerializeField] private Image progressBackgroundImage;

        [Header("Team")]
        [SerializeField] private TMP_Text teamTitleLabel;
        [SerializeField] private TMP_Text teamNameLabel;
        [SerializeField] private GridLayoutGroup avatarGridLayout;
        [SerializeField] private List<Image> avatarImages = new();
        [SerializeField] private CoopPlayerProfileSync playerProfileSync;
        [SerializeField] private CoopSessionCoordinator sessionCoordinator;
        [SerializeField] private PlayerMarkerAppearanceCatalogConfig avatarCatalog;

        [Header("Minigame Title")]
        [SerializeField] private TMP_Text minigameTitleLabel;
        [SerializeField] private string titlePrefixOverride;
        [SerializeField] private Image leftLeafImage;
        [SerializeField] private Image rightLeafImage;

        public CoopMinigameThemeConfig ThemeConfig => themeConfig;
        private readonly List<Sprite> roomAvatarSprites = new();
        private string authoredTitlePrefix;
        private bool hasAuthoredTitlePrefix;

        private void Awake()
        {
            CaptureAuthoredTitlePrefix();
            ResolveRuntimeReferences();
            ApplyTheme();
        }

        private void OnEnable()
        {
            ResolveRuntimeReferences();
            SubscribeRuntimeEvents();
            RefreshRoomAvatars();
        }

        private void OnDisable()
        {
            UnsubscribeRuntimeEvents();
        }

        private void OnValidate()
        {
            if (!Application.isPlaying)
            {
                CaptureAuthoredTitlePrefix(forceRefresh: true);
            }

            ApplyTheme();
        }

        public void Bind(string minigameTitle, float globalProgress01, string teamName, string roomCode)
        {
            ApplyTheme();
            SetMinigameTitle(minigameTitle);
            SetProgress(globalProgress01);
            SetTeam(teamName, roomCode);
            RefreshRoomAvatars();
        }

        public void SetTheme(CoopMinigameThemeConfig theme)
        {
            themeConfig = theme;
            ApplyTheme();
        }

        public void SetMinigameTitle(string minigameTitle)
        {
            if (minigameTitleLabel == null)
            {
                return;
            }

            CaptureAuthoredTitlePrefix(forceRefresh: false);
            var normalizedTitle = string.IsNullOrWhiteSpace(minigameTitle) ? string.Empty : minigameTitle.Trim();
            var resolvedPrefix = ResolveTitlePrefix();
            minigameTitleLabel.text = string.IsNullOrWhiteSpace(normalizedTitle)
                ? resolvedPrefix
                : $"{resolvedPrefix} {normalizedTitle}";
        }

        public void SetProgress(float progress01)
        {
            var clampedProgress = Mathf.Clamp01(progress01);
            if (progressSlider != null)
            {
                progressSlider.minValue = 0f;
                progressSlider.maxValue = 1f;
                progressSlider.value = clampedProgress;
            }

            if (progressPercentLabel != null && themeConfig != null)
            {
                progressPercentLabel.text = string.Format(themeConfig.GlobalProgressPercentFormat, clampedProgress * 100f);
            }
        }

        public void SetTeam(string teamName, string roomCode)
        {
            if (themeConfig == null || teamNameLabel == null)
            {
                return;
            }

            teamNameLabel.text = themeConfig.ResolveTeamDisplayName(teamName, roomCode);
        }

        public void SetAvatarSprites(IReadOnlyList<Sprite> avatarSprites)
        {
            if (avatarImages == null || avatarImages.Count == 0)
            {
                return;
            }

            var activeAvatarCount = CountValidSprites(avatarSprites);
            ConfigureAvatarGrid(activeAvatarCount);

            for (var index = 0; index < avatarImages.Count; index++)
            {
                var image = avatarImages[index];
                if (image == null)
                {
                    continue;
                }

                var hasSprite = avatarSprites != null && index < avatarSprites.Count && avatarSprites[index] != null;
                image.sprite = hasSprite ? avatarSprites[index] : null;
                image.enabled = hasSprite;
                var avatarFrame = image.transform.parent != null ? image.transform.parent.gameObject : image.gameObject;
                avatarFrame.SetActive(hasSprite);
            }
        }

        public void ApplyTheme()
        {
            if (themeConfig == null)
            {
                return;
            }

            ResolveSerializedReferencesFromHierarchy();

            var palette = themeConfig.Palette;
            var panelStyle = themeConfig.CardPanelStyle;
            SetRoundedPanel(backgroundGraphic, palette.PanelBackground, palette.PanelBorder, panelStyle.CornerRadius + 18f, panelStyle.BorderWidth);
            SetRoundedPanel(logoFrameGraphic, palette.PrimaryGreen, palette.MutedGreen, themeConfig.TopPanelStyle.LogoSize * 0.5f, 2f);
            EnsureLogoLayout();

            SetImageColor(backgroundImage, palette.PanelBackground);
            SetImageColor(titleBandImage, palette.PrimaryGreen);
            SetImageColor(progressFillImage, palette.ProgressGreen);
            SetImageColor(progressBackgroundImage, palette.Cream);

            if (logoImage != null)
            {
                var logoSprite = ResolveLogoSprite();
                logoImage.sprite = logoSprite;
                logoImage.enabled = !hideLogo && logoSprite != null;
                logoImage.preserveAspect = true;
            }

            SetImageSprite(leftLeafImage, themeConfig.Icons.LeafIcon);
            SetImageSprite(rightLeafImage, themeConfig.Icons.LeafIcon);

            ApplyText(projectTitleLabel, themeConfig.ProjectTitle, themeConfig.Typography.ProjectTitleSize, palette.PrimaryGreen, FontStyles.Bold);
            ApplyText(projectSubtitleLabel, themeConfig.ProjectSubtitle, themeConfig.Typography.BodySize, palette.TextPrimary, FontStyles.Normal);
            ApplyText(progressTitleLabel, themeConfig.GlobalProgressLabel, themeConfig.Typography.CaptionSize, palette.PrimaryGreen, FontStyles.Bold);
            ApplyText(teamTitleLabel, themeConfig.TeamTitle, themeConfig.Typography.CaptionSize, palette.SecondaryGreen, FontStyles.Bold);
            ApplyText(teamNameLabel, themeConfig.ResolveTeamDisplayName(null, null), themeConfig.Typography.CaptionSize, palette.PrimaryGreen, FontStyles.Bold);
            ApplyTextStyle(minigameTitleLabel, themeConfig.Typography.MinigameTitleSize, Color.white, FontStyles.Bold);

            if (progressPercentLabel != null)
            {
                ApplyText(progressPercentLabel, progressPercentLabel.text, themeConfig.Typography.BodyLargeSize, palette.SecondaryGreen, FontStyles.Bold);
            }

            if (progressSlider != null)
            {
                progressSlider.wholeNumbers = false;
                progressSlider.interactable = false;
            }

            if (playerProfileSync == null || playerProfileSync.PlayerProfiles.Count == 0)
            {
                SetAvatarSprites(themeConfig.DefaultAvatarSprites);
            }
        }

        private void RefreshRoomAvatars()
        {
            ResolveRuntimeReferences();
            if (playerProfileSync == null || avatarCatalog == null)
            {
                return;
            }

            roomAvatarSprites.Clear();
            AppendRoomAvatarsInSlotOrder();
            if (roomAvatarSprites.Count == 0)
            {
                SetAvatarSprites(null);
                return;
            }

            SetAvatarSprites(roomAvatarSprites);
        }

        private void AppendRoomAvatarsInSlotOrder()
        {
            if (sessionCoordinator != null && sessionCoordinator.RegisteredPlayerCount > 0)
            {
                for (var slotIndex = 0; slotIndex < sessionCoordinator.RegisteredPlayerCount; slotIndex++)
                {
                    if (sessionCoordinator.TryGetPlayerClientIdAtSlot(slotIndex, out var clientId))
                    {
                        TryAppendAvatarForClient(clientId);
                    }
                }

                return;
            }

            for (var index = 0; index < playerProfileSync.PlayerProfiles.Count; index++)
            {
                TryAppendAvatar(playerProfileSync.PlayerProfiles[index].AvatarId.ToString());
            }
        }

        private void TryAppendAvatarForClient(ulong clientId)
        {
            if (playerProfileSync.TryGetProfile(clientId, out var profile))
            {
                TryAppendAvatar(profile.AvatarId.ToString());
            }
        }

        private void TryAppendAvatar(string avatarId)
        {
            if (avatarCatalog.TryGetAvatar(avatarId, out var avatar) && avatar != null && avatar.AvatarSprite != null)
            {
                roomAvatarSprites.Add(avatar.AvatarSprite);
            }
        }

        private void ResolveRuntimeReferences()
        {
            playerProfileSync ??= FindFirstObjectByType<CoopPlayerProfileSync>(FindObjectsInactive.Include);
            sessionCoordinator ??= FindFirstObjectByType<CoopSessionCoordinator>(FindObjectsInactive.Include);
            avatarCatalog ??= playerProfileSync != null ? playerProfileSync.AppearanceCatalog : null;
        }

        private void ResolveSerializedReferencesFromHierarchy()
        {
            logoFrameGraphic ??= FindChildComponent<RoundedPanelGraphic>("LogoFrame");
            logoImage ??= FindChildComponent<Image>("Logo");
        }

        private void EnsureLogoLayout()
        {
            if (themeConfig == null)
            {
                return;
            }

            var logoSize = themeConfig.TopPanelStyle.LogoSize;
            if (logoFrameGraphic != null)
            {
                var frameMask = logoFrameGraphic.GetComponent<Mask>() ?? logoFrameGraphic.gameObject.AddComponent<Mask>();
                frameMask.showMaskGraphic = true;

                var frameRect = logoFrameGraphic.GetComponent<RectTransform>();
                if (frameRect != null)
                {
                    frameRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, logoSize);
                    frameRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, logoSize);
                }

                var frameLayout = logoFrameGraphic.GetComponent<LayoutElement>();
                if (frameLayout != null)
                {
                    frameLayout.minWidth = logoSize;
                    frameLayout.minHeight = logoSize;
                    frameLayout.preferredWidth = logoSize;
                    frameLayout.preferredHeight = logoSize;
                }
            }

            if (logoImage != null)
            {
                var logoRect = logoImage.rectTransform;
                logoRect.anchorMin = new Vector2(0.5f, 0.5f);
                logoRect.anchorMax = new Vector2(0.5f, 0.5f);
                logoRect.pivot = new Vector2(0.5f, 0.5f);
                logoRect.anchoredPosition = Vector2.zero;
                logoRect.sizeDelta = Vector2.one * Mathf.Max(8f, logoSize - themeConfig.TopPanelStyle.LogoInnerPadding);
                logoImage.maskable = true;
            }
        }

        private void SubscribeRuntimeEvents()
        {
            if (playerProfileSync != null)
            {
                playerProfileSync.ProfilesChanged -= HandleProfilesChanged;
                playerProfileSync.ProfilesChanged += HandleProfilesChanged;
            }

            if (sessionCoordinator != null)
            {
                sessionCoordinator.SlotsChanged -= HandleSlotsChanged;
                sessionCoordinator.SlotsChanged += HandleSlotsChanged;
            }
        }

        private void UnsubscribeRuntimeEvents()
        {
            if (playerProfileSync != null)
            {
                playerProfileSync.ProfilesChanged -= HandleProfilesChanged;
            }

            if (sessionCoordinator != null)
            {
                sessionCoordinator.SlotsChanged -= HandleSlotsChanged;
            }
        }

        private void HandleProfilesChanged()
        {
            RefreshRoomAvatars();
        }

        private void HandleSlotsChanged()
        {
            RefreshRoomAvatars();
        }

        private void ConfigureAvatarGrid(int activeAvatarCount)
        {
            if (avatarGridLayout == null || themeConfig == null)
            {
                return;
            }

            var visibleCount = Mathf.Max(1, activeAvatarCount);
            var columns = visibleCount <= 2 ? visibleCount : 3;
            var rows = Mathf.CeilToInt(visibleCount / (float)columns);
            var avatarSize = themeConfig.TopPanelStyle.AvatarSize;

            avatarGridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            avatarGridLayout.constraintCount = columns;
            avatarGridLayout.cellSize = Vector2.one * avatarSize;
            avatarGridLayout.spacing = Vector2.one * 6f;

            var layoutElement = avatarGridLayout.GetComponent<LayoutElement>();
            if (layoutElement != null)
            {
                layoutElement.preferredHeight = rows * avatarSize + Mathf.Max(0, rows - 1) * avatarGridLayout.spacing.y;
            }
        }

        private void CaptureAuthoredTitlePrefix(bool forceRefresh = false)
        {
            if ((!forceRefresh && hasAuthoredTitlePrefix) || minigameTitleLabel == null)
            {
                return;
            }

            var labelText = minigameTitleLabel.text;
            if (string.IsNullOrWhiteSpace(labelText))
            {
                return;
            }

            authoredTitlePrefix = labelText.Trim();
            hasAuthoredTitlePrefix = true;
        }

        private string ResolveTitlePrefix()
        {
            if (!string.IsNullOrWhiteSpace(titlePrefixOverride))
            {
                return titlePrefixOverride.Trim();
            }

            if (hasAuthoredTitlePrefix && !string.IsNullOrWhiteSpace(authoredTitlePrefix))
            {
                return authoredTitlePrefix.Trim();
            }

            if (themeConfig != null && !string.IsNullOrWhiteSpace(themeConfig.MinigameTitlePrefix))
            {
                return themeConfig.MinigameTitlePrefix.Trim();
            }

            return "MINIJUEGO:";
        }

        private Sprite ResolveLogoSprite()
        {
            if (logoOverrideSprite != null)
            {
                return logoOverrideSprite;
            }

            return themeConfig != null ? themeConfig.LogoSprite : null;
        }

        private static int CountValidSprites(IReadOnlyList<Sprite> avatarSprites)
        {
            if (avatarSprites == null)
            {
                return 0;
            }

            var count = 0;
            for (var index = 0; index < avatarSprites.Count; index++)
            {
                if (avatarSprites[index] != null)
                {
                    count++;
                }
            }

            return count;
        }

        private void ApplyText(TMP_Text label, string value, int fontSize, Color color, FontStyles style)
        {
            if (label == null)
            {
                return;
            }

            ApplyTextStyle(label, fontSize, color, style);
            label.text = value;
        }

        private void ApplyTextStyle(TMP_Text label, int fontSize, Color color, FontStyles style)
        {
            if (label == null)
            {
                return;
            }

            if (themeConfig.PrimaryFont != null)
            {
                label.font = themeConfig.PrimaryFont;
            }

            label.fontSize = fontSize;
            label.color = color;
            label.fontStyle = style;
        }

        private static void SetImageColor(Image image, Color color)
        {
            if (image != null)
            {
                image.color = color;
            }
        }

        private static void SetRoundedPanel(RoundedPanelGraphic graphic, Color fill, Color border, float radius, float width)
        {
            if (graphic != null)
            {
                graphic.Configure(fill, border, radius, width);
            }
        }

        private static void SetImageSprite(Image image, Sprite sprite)
        {
            if (image == null)
            {
                return;
            }

            image.sprite = sprite;
            image.enabled = sprite != null;
            image.preserveAspect = true;
        }

        private T FindChildComponent<T>(string childName) where T : Component
        {
            var child = FindChildRecursive(transform, childName);
            return child == null ? null : child.GetComponent<T>();
        }

        private static Transform FindChildRecursive(Transform root, string childName)
        {
            if (root == null)
            {
                return null;
            }

            for (var index = 0; index < root.childCount; index++)
            {
                var child = root.GetChild(index);
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

}
