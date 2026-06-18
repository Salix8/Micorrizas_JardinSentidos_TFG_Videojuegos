using System;
using System.Collections.Generic;
using SmartCampus.Coop.Minigames;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class CoopMinigameSharedPanelPrefabUtility
{
    public const string SharedPrefabFolder = "Assets/CoopMinigames/Prefabs";
    public const string TopPanelPrefabPath = SharedPrefabFolder + "/CoopMinigameTopPanel.prefab";
    public const string BottomPanelPrefabPath = SharedPrefabFolder + "/CoopMinigameBottomPanel.prefab";
    private const string PlayerAvatarCatalogPath = "Assets/ScriptableObjects/Lobby/PlayerMarkerAppearanceCatalogConfig.asset";

    [MenuItem("Tools/Coop/Theme/Create Or Update Shared Panel Prefabs")]
    public static void CreateOrUpdateSharedPanelPrefabs()
    {
        EnsureFolder("Assets", "CoopMinigames");
        EnsureFolder("Assets/CoopMinigames", "Prefabs");

        var themeConfig = CoopMinigameThemeSetupUtility.GetOrCreateDefaultTheme();
        CreateOrUpdateTopPanelPrefab(themeConfig);
        CreateOrUpdateBottomPanelPrefab(themeConfig);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void CreateOrUpdateTopPanelPrefab(CoopMinigameThemeConfig themeConfig)
    {
        var root = CreateUiObject("CoopMinigameTopPanel", null, typeof(RoundedPanelGraphic), typeof(Mask), typeof(VerticalLayoutGroup), typeof(CoopMinigameTopPanelView));
        var rootRect = root.GetComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(themeConfig.ScreenLayout.ReferenceResolution.x, themeConfig.ScreenLayout.TopPanelHeight);
        root.GetComponent<Mask>().showMaskGraphic = true;

        var rootLayout = root.GetComponent<VerticalLayoutGroup>();
        rootLayout.padding = new RectOffset(32, 32, 28, 0);
        rootLayout.spacing = 0f;
        rootLayout.childControlWidth = true;
        rootLayout.childControlHeight = true;
        rootLayout.childForceExpandWidth = true;
        rootLayout.childForceExpandHeight = false;

        var upperSection = CreateUiObject("UpperSection", root.transform, typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        upperSection.GetComponent<LayoutElement>().preferredHeight = themeConfig.TopPanelStyle.UpperHeight;
        var upperLayout = upperSection.GetComponent<HorizontalLayoutGroup>();
        upperLayout.spacing = 22f;
        upperLayout.childAlignment = TextAnchor.MiddleCenter;
        upperLayout.childControlWidth = true;
        upperLayout.childControlHeight = true;
        upperLayout.childForceExpandWidth = false;
        upperLayout.childForceExpandHeight = false;

        var progressColumn = CreateTopProgressColumn(upperSection.transform, themeConfig);
        var brandColumn = CreateTopBrandColumn(upperSection.transform, themeConfig);
        var teamColumn = CreateTopTeamColumn(upperSection.transform, themeConfig, out var avatarGridLayout, out var avatarImages);

        var titleBand = CreateUiObject("TitleBand", root.transform, typeof(Image), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        titleBand.GetComponent<LayoutElement>().preferredHeight = themeConfig.TopPanelStyle.TitleBandHeight;
        var titleBandLayout = titleBand.GetComponent<HorizontalLayoutGroup>();
        titleBandLayout.padding = new RectOffset(40, 40, 0, 0);
        titleBandLayout.spacing = 18f;
        titleBandLayout.childAlignment = TextAnchor.MiddleCenter;
        titleBandLayout.childControlWidth = false;
        titleBandLayout.childControlHeight = false;

        var leftLeaf = CreateIconImage("LeftLeaf", titleBand.transform, new Vector2(42f, 42f));
        var titleLabel = CreateText("MinigameTitleLabel", titleBand.transform, themeConfig, themeConfig.MinigameTitlePrefix, themeConfig.Typography.MinigameTitleSize, TextAlignmentOptions.Center, FontStyles.Bold);
        titleLabel.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        var rightLeaf = CreateIconImage("RightLeaf", titleBand.transform, new Vector2(42f, 42f));
        rightLeaf.rectTransform.localScale = new Vector3(-1f, 1f, 1f);

        var view = root.GetComponent<CoopMinigameTopPanelView>();
        var serializedView = new SerializedObject(view);
        Assign(serializedView, "themeConfig", themeConfig);
        Assign(serializedView, "backgroundGraphic", root.GetComponent<RoundedPanelGraphic>());
        Assign(serializedView, "backgroundImage", null);
        Assign(serializedView, "titleBandImage", titleBand.GetComponent<Image>());
        Assign(serializedView, "logoFrameGraphic", progressColumn.LogoFrameGraphic);
        Assign(serializedView, "logoImage", progressColumn.LogoImage);
        Assign(serializedView, "projectTitleLabel", brandColumn.ProjectTitle);
        Assign(serializedView, "projectSubtitleLabel", brandColumn.ProjectSubtitle);
        Assign(serializedView, "progressTitleLabel", progressColumn.ProgressTitle);
        Assign(serializedView, "progressPercentLabel", progressColumn.ProgressPercent);
        Assign(serializedView, "progressSlider", progressColumn.ProgressSlider);
        Assign(serializedView, "progressFillImage", progressColumn.ProgressFill);
        Assign(serializedView, "progressBackgroundImage", progressColumn.ProgressBackground);
        Assign(serializedView, "teamTitleLabel", teamColumn.TeamTitle);
        Assign(serializedView, "teamNameLabel", teamColumn.TeamName);
        Assign(serializedView, "avatarGridLayout", avatarGridLayout);
        Assign(serializedView, "avatarCatalog", AssetDatabase.LoadAssetAtPath<PlayerMarkerAppearanceCatalogConfig>(PlayerAvatarCatalogPath));
        Assign(serializedView, "minigameTitleLabel", titleLabel);
        Assign(serializedView, "leftLeafImage", leftLeaf);
        Assign(serializedView, "rightLeafImage", rightLeaf);
        AssignArray(serializedView, "avatarImages", avatarImages);
        serializedView.ApplyModifiedPropertiesWithoutUndo();
        view.ApplyTheme();
        view.SetProgress(0.45f);

        PrefabUtility.SaveAsPrefabAsset(root, TopPanelPrefabPath);
        UnityEngine.Object.DestroyImmediate(root);
    }

    private static void CreateOrUpdateBottomPanelPrefab(CoopMinigameThemeConfig themeConfig)
    {
        var root = CreateUiObject("CoopMinigameBottomPanel", null, typeof(VerticalLayoutGroup), typeof(CoopMinigameBottomPanelView));
        var rootRect = root.GetComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(themeConfig.ScreenLayout.ReferenceResolution.x, themeConfig.ScreenLayout.BottomPanelHeight);

        var rootLayout = root.GetComponent<VerticalLayoutGroup>();
        rootLayout.padding = new RectOffset(32, 32, 0, 28);
        rootLayout.spacing = 14f;
        rootLayout.childControlWidth = true;
        rootLayout.childControlHeight = true;
        rootLayout.childForceExpandWidth = true;
        rootLayout.childForceExpandHeight = false;

        var instructionPanel = CreateInstructionPanel(
            root.transform,
            themeConfig,
            out var instructionPanelGraphic,
            out var instructionIconCircleGraphic,
            out var instructionIcon,
            out var instructionTitle,
            out var instructionBody);
        var timerPanel = CreateTimerPanel(
            root.transform,
            themeConfig,
            out var timerPanelGraphic,
            out var timerIconCircleGraphic,
            out var timerIcon,
            out var timeLabel,
            out var timeValue,
            out var timeSlider,
            out var timeFill,
            out var timeBackground,
            out var divider,
            out var penaltyIconCircleGraphic,
            out var penaltyIcon,
            out var penaltyLabel,
            out var penaltyValue);

        var view = root.GetComponent<CoopMinigameBottomPanelView>();
        var serializedView = new SerializedObject(view);
        Assign(serializedView, "themeConfig", themeConfig);
        Assign(serializedView, "instructionPanelGraphic", instructionPanelGraphic);
        Assign(serializedView, "instructionPanelImage", null);
        Assign(serializedView, "instructionIconCircleGraphic", instructionIconCircleGraphic);
        Assign(serializedView, "instructionIconImage", instructionIcon);
        Assign(serializedView, "instructionTitleLabel", instructionTitle);
        Assign(serializedView, "instructionBodyLabel", instructionBody);
        Assign(serializedView, "timerPanelGraphic", timerPanelGraphic);
        Assign(serializedView, "timerPanelImage", null);
        Assign(serializedView, "timerIconCircleGraphic", timerIconCircleGraphic);
        Assign(serializedView, "timerIconImage", timerIcon);
        Assign(serializedView, "timeLabel", timeLabel);
        Assign(serializedView, "timeValueLabel", timeValue);
        Assign(serializedView, "timeSlider", timeSlider);
        Assign(serializedView, "timeSliderFillImage", timeFill);
        Assign(serializedView, "timeSliderBackgroundImage", timeBackground);
        Assign(serializedView, "dividerImage", divider);
        Assign(serializedView, "penaltyIconCircleGraphic", penaltyIconCircleGraphic);
        Assign(serializedView, "penaltyIconImage", penaltyIcon);
        Assign(serializedView, "penaltyLabel", penaltyLabel);
        Assign(serializedView, "penaltyValueLabel", penaltyValue);
        serializedView.ApplyModifiedPropertiesWithoutUndo();
        view.ApplyTheme();
        view.SetTimer(80f, 100f);
        view.SetPenaltySeconds(10f);

        PrefabUtility.SaveAsPrefabAsset(root, BottomPanelPrefabPath);
        UnityEngine.Object.DestroyImmediate(root);
    }

    private static TopProgressReferences CreateTopProgressColumn(Transform parent, CoopMinigameThemeConfig themeConfig)
    {
        var root = CreateUiObject("ProgressArea", parent, typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        var rootLayout = root.GetComponent<HorizontalLayoutGroup>();
        rootLayout.spacing = 16f;
        rootLayout.childAlignment = TextAnchor.MiddleLeft;
        rootLayout.childControlHeight = false;
        rootLayout.childControlWidth = false;
        root.GetComponent<LayoutElement>().preferredWidth = 330f;

        var logoFrame = CreateUiObject("LogoFrame", root.transform, typeof(RoundedPanelGraphic), typeof(LayoutElement));
        logoFrame.GetComponent<LayoutElement>().preferredWidth = themeConfig.TopPanelStyle.LogoSize;
        logoFrame.GetComponent<LayoutElement>().preferredHeight = themeConfig.TopPanelStyle.LogoSize;
        var logoFrameGraphic = logoFrame.GetComponent<RoundedPanelGraphic>();
        logoFrameGraphic.Configure(
            themeConfig.Palette.PrimaryGreen,
            themeConfig.Palette.MutedGreen,
            themeConfig.TopPanelStyle.LogoSize * 0.5f,
            2f);

        var logo = CreateIconImage("Logo", logoFrame.transform, Vector2.one * (themeConfig.TopPanelStyle.LogoSize - 22f));
        Stretch(logo.rectTransform, new Vector2(11f, 11f), new Vector2(-11f, -11f));

        var labels = CreateUiObject("ProgressLabels", root.transform, typeof(VerticalLayoutGroup), typeof(LayoutElement));
        labels.GetComponent<LayoutElement>().preferredWidth = 190f;
        var labelsLayout = labels.GetComponent<VerticalLayoutGroup>();
        labelsLayout.spacing = 5f;
        labelsLayout.childAlignment = TextAnchor.MiddleLeft;
        labelsLayout.childControlWidth = true;
        labelsLayout.childControlHeight = false;

        var title = CreateText("ProgressTitleLabel", labels.transform, themeConfig, themeConfig.GlobalProgressLabel, themeConfig.Typography.CaptionSize, TextAlignmentOptions.Left, FontStyles.Bold);
        var percent = CreateText("ProgressPercentLabel", labels.transform, themeConfig, "45%", themeConfig.Typography.BodyLargeSize, TextAlignmentOptions.Left, FontStyles.Bold);
        var slider = CreateSlider("GlobalProgressSlider", labels.transform, themeConfig.TopPanelStyle.ProgressBarWidth, themeConfig.TopPanelStyle.ProgressBarHeight, out var fill, out var background);

        return new TopProgressReferences(logoFrameGraphic, logo, title, percent, slider, fill, background);
    }

    private static TopBrandReferences CreateTopBrandColumn(Transform parent, CoopMinigameThemeConfig themeConfig)
    {
        var root = CreateUiObject("BrandArea", parent, typeof(VerticalLayoutGroup), typeof(LayoutElement));
        root.GetComponent<LayoutElement>().flexibleWidth = 1f;
        var layout = root.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlHeight = false;
        layout.childControlWidth = true;

        var title = CreateText("ProjectTitleLabel", root.transform, themeConfig, themeConfig.ProjectTitle, themeConfig.Typography.ProjectTitleSize, TextAlignmentOptions.Center, FontStyles.Bold);
        var subtitle = CreateText("ProjectSubtitleLabel", root.transform, themeConfig, themeConfig.ProjectSubtitle, themeConfig.Typography.BodySize, TextAlignmentOptions.Center, FontStyles.Normal);
        subtitle.textWrappingMode = TextWrappingModes.Normal;

        return new TopBrandReferences(title, subtitle);
    }

    private static TopTeamReferences CreateTopTeamColumn(
        Transform parent,
        CoopMinigameThemeConfig themeConfig,
        out GridLayoutGroup avatarGridLayout,
        out List<Image> avatarImages)
    {
        var root = CreateUiObject("TeamArea", parent, typeof(VerticalLayoutGroup), typeof(LayoutElement));
        root.GetComponent<LayoutElement>().preferredWidth = 330f;
        var layout = root.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 5f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlHeight = false;
        layout.childControlWidth = true;

        var title = CreateText("TeamTitleLabel", root.transform, themeConfig, themeConfig.TeamTitle, themeConfig.Typography.CaptionSize, TextAlignmentOptions.Center, FontStyles.Bold);
        var name = CreateText("TeamNameLabel", root.transform, themeConfig, themeConfig.TeamTitle, themeConfig.Typography.CaptionSize, TextAlignmentOptions.Center, FontStyles.Bold);

        var avatarRow = CreateUiObject("AvatarRow", root.transform, typeof(GridLayoutGroup), typeof(LayoutElement));
        avatarRow.GetComponent<LayoutElement>().preferredHeight = themeConfig.TopPanelStyle.AvatarSize * 2f + 6f;
        avatarGridLayout = avatarRow.GetComponent<GridLayoutGroup>();
        avatarGridLayout.padding = new RectOffset(0, 0, 0, 0);
        avatarGridLayout.spacing = Vector2.one * 6f;
        avatarGridLayout.cellSize = Vector2.one * themeConfig.TopPanelStyle.AvatarSize;
        avatarGridLayout.childAlignment = TextAnchor.MiddleCenter;
        avatarGridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        avatarGridLayout.constraintCount = 3;

        avatarImages = new List<Image>();
        for (var index = 0; index < 6; index++)
        {
            var avatarFrame = CreateUiObject($"Avatar{index + 1}", avatarRow.transform, typeof(Image), typeof(LayoutElement));
            avatarFrame.GetComponent<LayoutElement>().preferredWidth = themeConfig.TopPanelStyle.AvatarSize;
            avatarFrame.GetComponent<LayoutElement>().preferredHeight = themeConfig.TopPanelStyle.AvatarSize;
            avatarFrame.GetComponent<Image>().color = themeConfig.Palette.PanelBackground;
            var avatar = CreateIconImage("Icon", avatarFrame.transform, Vector2.one * (themeConfig.TopPanelStyle.AvatarSize - 8f));
            Stretch(avatar.rectTransform, new Vector2(4f, 4f), new Vector2(-4f, -4f));
            avatarImages.Add(avatar);
        }

        return new TopTeamReferences(title, name);
    }

    private static GameObject CreateInstructionPanel(
        Transform parent,
        CoopMinigameThemeConfig themeConfig,
        out RoundedPanelGraphic panelGraphic,
        out RoundedPanelGraphic instructionIconCircleGraphic,
        out Image instructionIcon,
        out TMP_Text instructionTitle,
        out TMP_Text instructionBody)
    {
        var panel = CreateUiObject("InstructionPanel", parent, typeof(RoundedPanelGraphic), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        panelGraphic = panel.GetComponent<RoundedPanelGraphic>();
        panelGraphic.Configure(
            themeConfig.Palette.PanelBackground,
            themeConfig.Palette.PanelBorder,
            themeConfig.CardPanelStyle.CornerRadius + 8f,
            themeConfig.CardPanelStyle.BorderWidth);
        panel.GetComponent<LayoutElement>().preferredHeight = themeConfig.BottomPanelStyle.InstructionHeight;
        var layout = panel.GetComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(28, 28, 18, 18);
        layout.spacing = 22f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlHeight = true;
        layout.childControlWidth = false;

        var iconCircle = CreateUiObject("InstructionIconCircle", panel.transform, typeof(RoundedPanelGraphic), typeof(LayoutElement));
        iconCircle.GetComponent<LayoutElement>().preferredWidth = themeConfig.InstructionPanelStyle.IconCircleSize;
        iconCircle.GetComponent<LayoutElement>().preferredHeight = themeConfig.InstructionPanelStyle.IconCircleSize;
        instructionIconCircleGraphic = iconCircle.GetComponent<RoundedPanelGraphic>();
        instructionIconCircleGraphic.Configure(
            themeConfig.Palette.PanelBackground,
            themeConfig.Palette.PanelBorder,
            themeConfig.InstructionPanelStyle.IconCircleSize * 0.5f,
            themeConfig.InstructionPanelStyle.IconCircleBorderWidth);
        instructionIcon = CreateIconImage("Icon", iconCircle.transform, Vector2.one * (themeConfig.InstructionPanelStyle.IconCircleSize * 0.58f));
        Center(instructionIcon.rectTransform);

        var textColumn = CreateUiObject("InstructionText", panel.transform, typeof(VerticalLayoutGroup), typeof(LayoutElement));
        textColumn.GetComponent<LayoutElement>().flexibleWidth = 1f;
        var textLayout = textColumn.GetComponent<VerticalLayoutGroup>();
        textLayout.spacing = 6f;
        textLayout.childAlignment = TextAnchor.MiddleLeft;
        textLayout.childControlWidth = true;
        textLayout.childControlHeight = false;

        instructionTitle = CreateText("InstructionTitleLabel", textColumn.transform, themeConfig, themeConfig.BottomPanelStyle.DefaultInstructionTitle, themeConfig.Typography.SectionTitleSize, TextAlignmentOptions.Left, FontStyles.Bold);
        instructionBody = CreateText("InstructionBodyLabel", textColumn.transform, themeConfig, themeConfig.BottomPanelStyle.DefaultInstructionBody, themeConfig.Typography.BodySize, TextAlignmentOptions.Left, FontStyles.Normal);
        instructionBody.textWrappingMode = TextWrappingModes.Normal;
        return panel;
    }

    private static GameObject CreateTimerPanel(
        Transform parent,
        CoopMinigameThemeConfig themeConfig,
        out RoundedPanelGraphic panelGraphic,
        out RoundedPanelGraphic timerIconCircleGraphic,
        out Image timerIcon,
        out TMP_Text timeLabel,
        out TMP_Text timeValue,
        out Slider timeSlider,
        out Image timeFill,
        out Image timeBackground,
        out Image divider,
        out RoundedPanelGraphic penaltyIconCircleGraphic,
        out Image penaltyIcon,
        out TMP_Text penaltyLabel,
        out TMP_Text penaltyValue)
    {
        var panel = CreateUiObject("TimerPenaltyPanel", parent, typeof(RoundedPanelGraphic), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        panelGraphic = panel.GetComponent<RoundedPanelGraphic>();
        panelGraphic.Configure(
            themeConfig.Palette.PanelBackground,
            themeConfig.Palette.PanelBorder,
            themeConfig.CardPanelStyle.CornerRadius + 8f,
            themeConfig.CardPanelStyle.BorderWidth);
        panel.GetComponent<LayoutElement>().preferredHeight = themeConfig.BottomPanelStyle.TimerPanelHeight;
        var layout = panel.GetComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(30, 30, 18, 18);
        layout.spacing = 26f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlHeight = true;
        layout.childControlWidth = true;

        var timeGroup = CreateUiObject("TimeGroup", panel.transform, typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        timeGroup.GetComponent<LayoutElement>().flexibleWidth = 1f;
        var timeGroupLayout = timeGroup.GetComponent<HorizontalLayoutGroup>();
        timeGroupLayout.spacing = 18f;
        timeGroupLayout.childAlignment = TextAnchor.MiddleLeft;
        timeGroupLayout.childControlWidth = false;
        timeGroupLayout.childControlHeight = true;

        var timerIconCircle = CreateUiObject("TimerIconCircle", timeGroup.transform, typeof(RoundedPanelGraphic), typeof(LayoutElement));
        timerIconCircle.GetComponent<LayoutElement>().preferredWidth = themeConfig.BottomPanelStyle.LargeIconSize;
        timerIconCircle.GetComponent<LayoutElement>().preferredHeight = themeConfig.BottomPanelStyle.LargeIconSize;
        timerIconCircleGraphic = timerIconCircle.GetComponent<RoundedPanelGraphic>();
        timerIconCircleGraphic.Configure(
            themeConfig.Palette.PanelBackground,
            themeConfig.Palette.ProgressGreen,
            themeConfig.BottomPanelStyle.LargeIconSize * 0.5f,
            2f);
        timerIcon = CreateIconImage("Icon", timerIconCircle.transform, Vector2.one * (themeConfig.BottomPanelStyle.LargeIconSize * 0.58f));
        Center(timerIcon.rectTransform);

        var timeTexts = CreateUiObject("TimeTexts", timeGroup.transform, typeof(VerticalLayoutGroup), typeof(LayoutElement));
        timeTexts.GetComponent<LayoutElement>().preferredWidth = 330f;
        var timeTextLayout = timeTexts.GetComponent<VerticalLayoutGroup>();
        timeTextLayout.spacing = 4f;
        timeTextLayout.childAlignment = TextAnchor.MiddleLeft;
        timeTextLayout.childControlWidth = true;
        timeTextLayout.childControlHeight = false;

        timeLabel = CreateText("TimeLabel", timeTexts.transform, themeConfig, themeConfig.BottomPanelStyle.TimeRemainingLabel, themeConfig.Typography.CaptionSize, TextAlignmentOptions.Left, FontStyles.Bold);
        timeValue = CreateText("TimeValueLabel", timeTexts.transform, themeConfig, "01:20", themeConfig.Typography.ProjectTitleSize, TextAlignmentOptions.Left, FontStyles.Bold);
        timeSlider = CreateSlider("TimeSlider", timeTexts.transform, themeConfig.BottomPanelStyle.TimerBarWidth, themeConfig.BottomPanelStyle.TimerBarHeight, out timeFill, out timeBackground);

        var dividerObject = CreateUiObject("Divider", panel.transform, typeof(Image), typeof(LayoutElement));
        divider = dividerObject.GetComponent<Image>();
        divider.raycastTarget = false;
        dividerObject.GetComponent<LayoutElement>().preferredWidth = themeConfig.BottomPanelStyle.DividerWidth;

        var penaltyGroup = CreateUiObject("PenaltyGroup", panel.transform, typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        penaltyGroup.GetComponent<LayoutElement>().flexibleWidth = 1f;
        var penaltyGroupLayout = penaltyGroup.GetComponent<HorizontalLayoutGroup>();
        penaltyGroupLayout.spacing = 18f;
        penaltyGroupLayout.childAlignment = TextAnchor.MiddleCenter;
        penaltyGroupLayout.childControlWidth = false;
        penaltyGroupLayout.childControlHeight = true;

        var penaltyTexts = CreateUiObject("PenaltyTexts", penaltyGroup.transform, typeof(VerticalLayoutGroup), typeof(LayoutElement));
        penaltyTexts.GetComponent<LayoutElement>().preferredWidth = 190f;
        var penaltyTextLayout = penaltyTexts.GetComponent<VerticalLayoutGroup>();
        penaltyTextLayout.spacing = 4f;
        penaltyTextLayout.childAlignment = TextAnchor.MiddleCenter;
        penaltyTextLayout.childControlWidth = true;
        penaltyTextLayout.childControlHeight = false;
        penaltyLabel = CreateText("PenaltyLabel", penaltyTexts.transform, themeConfig, themeConfig.BottomPanelStyle.PenaltyLabel, themeConfig.Typography.CaptionSize, TextAlignmentOptions.Center, FontStyles.Bold);
        penaltyValue = CreateText("PenaltyValueLabel", penaltyTexts.transform, themeConfig, "-10s", themeConfig.Typography.ProjectTitleSize, TextAlignmentOptions.Center, FontStyles.Bold);

        var penaltyIconCircle = CreateUiObject("PenaltyIconCircle", penaltyGroup.transform, typeof(RoundedPanelGraphic), typeof(LayoutElement));
        penaltyIconCircle.GetComponent<LayoutElement>().preferredWidth = themeConfig.BottomPanelStyle.LargeIconSize;
        penaltyIconCircle.GetComponent<LayoutElement>().preferredHeight = themeConfig.BottomPanelStyle.LargeIconSize;
        penaltyIconCircleGraphic = penaltyIconCircle.GetComponent<RoundedPanelGraphic>();
        penaltyIconCircleGraphic.Configure(
            themeConfig.Palette.PanelBackground,
            themeConfig.Palette.DangerSoft,
            themeConfig.BottomPanelStyle.LargeIconSize * 0.5f,
            2f);
        penaltyIcon = CreateIconImage("Icon", penaltyIconCircle.transform, Vector2.one * (themeConfig.BottomPanelStyle.LargeIconSize * 0.58f));
        Center(penaltyIcon.rectTransform);

        return panel;
    }

    private static Slider CreateSlider(string name, Transform parent, float width, float height, out Image fillImage, out Image backgroundImage)
    {
        var sliderObject = CreateUiObject(name, parent, typeof(Slider), typeof(LayoutElement));
        sliderObject.GetComponent<LayoutElement>().preferredWidth = width;
        sliderObject.GetComponent<LayoutElement>().preferredHeight = height;
        var sliderRect = sliderObject.GetComponent<RectTransform>();
        sliderRect.sizeDelta = new Vector2(width, height);

        var background = CreateUiObject("Background", sliderObject.transform, typeof(Image));
        backgroundImage = background.GetComponent<Image>();
        Stretch(background.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);

        var fillArea = CreateUiObject("Fill Area", sliderObject.transform, typeof(RectTransform));
        Stretch(fillArea.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);

        var fill = CreateUiObject("Fill", fillArea.transform, typeof(Image));
        fillImage = fill.GetComponent<Image>();
        Stretch(fill.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);

        var slider = sliderObject.GetComponent<Slider>();
        slider.targetGraphic = fillImage;
        slider.fillRect = fill.GetComponent<RectTransform>();
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;
        slider.interactable = false;
        return slider;
    }

    private static Image CreateIconImage(string name, Transform parent, Vector2 size)
    {
        var imageObject = CreateUiObject(name, parent, typeof(Image), typeof(LayoutElement));
        var layout = imageObject.GetComponent<LayoutElement>();
        layout.preferredWidth = size.x;
        layout.preferredHeight = size.y;
        var rect = imageObject.GetComponent<RectTransform>();
        rect.sizeDelta = size;
        var image = imageObject.GetComponent<Image>();
        image.preserveAspect = true;
        image.raycastTarget = false;
        return image;
    }

    private static TMP_Text CreateText(
        string name,
        Transform parent,
        CoopMinigameThemeConfig themeConfig,
        string value,
        int fontSize,
        TextAlignmentOptions alignment,
        FontStyles style)
    {
        var textObject = CreateUiObject(name, parent, typeof(TextMeshProUGUI));
        var text = textObject.GetComponent<TMP_Text>();
        text.text = value;
        text.font = themeConfig.PrimaryFont;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = themeConfig.Palette.TextPrimary;
        text.raycastTarget = false;
        return text;
    }

    private static GameObject CreateUiObject(string name, Transform parent, params Type[] components)
    {
        var componentList = new List<Type> { typeof(RectTransform), typeof(CanvasRenderer) };
        foreach (var component in components)
        {
            if (component != null && !componentList.Contains(component))
            {
                componentList.Add(component);
            }
        }

        var gameObject = new GameObject(name, componentList.ToArray());
        if (parent != null)
        {
            gameObject.layer = parent.gameObject.layer;
            gameObject.transform.SetParent(parent, false);
        }

        return gameObject;
    }

    private static void Assign(SerializedObject serializedObject, string propertyName, UnityEngine.Object value)
    {
        serializedObject.FindProperty(propertyName).objectReferenceValue = value;
    }

    private static void AssignArray(SerializedObject serializedObject, string propertyName, IReadOnlyList<UnityEngine.Object> values)
    {
        var property = serializedObject.FindProperty(propertyName);
        property.ClearArray();

        if (values == null)
        {
            return;
        }

        for (var index = 0; index < values.Count; index++)
        {
            property.InsertArrayElementAtIndex(property.arraySize);
            property.GetArrayElementAtIndex(property.arraySize - 1).objectReferenceValue = values[index];
        }
    }

    private static void Stretch(RectTransform rectTransform, Vector2 offsetMin, Vector2 offsetMax)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = offsetMin;
        rectTransform.offsetMax = offsetMax;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
    }

    private static void Center(RectTransform rectTransform)
    {
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
    }

    private static void EnsureFolder(string parent, string name)
    {
        var fullPath = $"{parent}/{name}";
        if (!AssetDatabase.IsValidFolder(fullPath))
        {
            AssetDatabase.CreateFolder(parent, name);
        }
    }

    private readonly struct TopProgressReferences
    {
        public TopProgressReferences(RoundedPanelGraphic logoFrameGraphic, Image logoImage, TMP_Text progressTitle, TMP_Text progressPercent, Slider progressSlider, Image progressFill, Image progressBackground)
        {
            LogoFrameGraphic = logoFrameGraphic;
            LogoImage = logoImage;
            ProgressTitle = progressTitle;
            ProgressPercent = progressPercent;
            ProgressSlider = progressSlider;
            ProgressFill = progressFill;
            ProgressBackground = progressBackground;
        }

        public RoundedPanelGraphic LogoFrameGraphic { get; }
        public Image LogoImage { get; }
        public TMP_Text ProgressTitle { get; }
        public TMP_Text ProgressPercent { get; }
        public Slider ProgressSlider { get; }
        public Image ProgressFill { get; }
        public Image ProgressBackground { get; }
    }

    private readonly struct TopBrandReferences
    {
        public TopBrandReferences(TMP_Text projectTitle, TMP_Text projectSubtitle)
        {
            ProjectTitle = projectTitle;
            ProjectSubtitle = projectSubtitle;
        }

        public TMP_Text ProjectTitle { get; }
        public TMP_Text ProjectSubtitle { get; }
    }

    private readonly struct TopTeamReferences
    {
        public TopTeamReferences(TMP_Text teamTitle, TMP_Text teamName)
        {
            TeamTitle = teamTitle;
            TeamName = teamName;
        }

        public TMP_Text TeamTitle { get; }
        public TMP_Text TeamName { get; }
    }
}

internal static class CoopMinigameSharedPanelSetupUtility
{
    public static T InstantiateSharedPanel<T>(string prefabPath, Transform parent, float preferredHeight) where T : Component
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            throw new InvalidOperationException($"No se pudo cargar el prefab compartido requerido: {prefabPath}");
        }

        var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        if (instance == null)
        {
            throw new InvalidOperationException($"No se pudo instanciar el prefab compartido requerido: {prefabPath}");
        }

        instance.transform.SetParent(parent, false);
        SetLayerRecursively(instance, 5);
        ConfigureLayoutElement(instance, preferredHeight);

        var view = instance.GetComponent<T>();
        if (view == null)
        {
            throw new InvalidOperationException($"El prefab '{prefabPath}' no contiene el componente requerido {typeof(T).Name}.");
        }

        return view;
    }

    private static void ConfigureLayoutElement(GameObject instance, float preferredHeight)
    {
        var layoutElement = instance.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = instance.AddComponent<LayoutElement>();
        }

        layoutElement.preferredHeight = preferredHeight;
        layoutElement.minHeight = Mathf.Min(preferredHeight, 120f);
        layoutElement.flexibleHeight = 0f;
    }

    private static void SetLayerRecursively(GameObject root, int layer)
    {
        root.layer = layer;
        foreach (Transform child in root.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }
}
