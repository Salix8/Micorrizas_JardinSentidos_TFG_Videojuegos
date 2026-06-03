using TMPro;
using UnityEditor;
using UnityEngine;
using SmartCampus.Coop.Minigames;

public static class CoopMinigameThemeSetupUtility
{
    public const string SharedConfigFolder = "Assets/CoopMinigames/Configs";
    public const string SharedConfigPath = SharedConfigFolder + "/CoopMinigameThemeConfig.asset";
    private const string ExistingLogoPath = "Assets/Art/Logo.jpg";
    private const string GeneratedLogoCandidatePath = "Assets/CoopMinigames/Theme/Generated/MicorrhizalLogoCandidate.png";
    private static readonly string[] GeneratedAvatarPaths =
    {
        "Assets/CoopMinigames/Theme/Generated/Avatars/TeamAvatarMushroom.png",
        "Assets/CoopMinigames/Theme/Generated/Avatars/TeamAvatarBee.png",
        "Assets/CoopMinigames/Theme/Generated/Avatars/TeamAvatarHedgehog.png",
        "Assets/CoopMinigames/Theme/Generated/Avatars/TeamAvatarSprout.png",
        "Assets/CoopMinigames/Theme/Generated/Avatars/TeamAvatarRobin.png",
        "Assets/CoopMinigames/Theme/Generated/Avatars/TeamAvatarWaterDrop.png"
    };

    [MenuItem("Tools/Coop/Theme/Create Or Update Minigame Theme")]
    public static CoopMinigameThemeConfig CreateOrUpdateDefaultTheme()
    {
        EnsureFolder("Assets", "CoopMinigames");
        EnsureFolder("Assets/CoopMinigames", "Configs");

        var themeConfig = AssetDatabase.LoadAssetAtPath<CoopMinigameThemeConfig>(SharedConfigPath);
        if (themeConfig == null)
        {
            themeConfig = ScriptableObject.CreateInstance<CoopMinigameThemeConfig>();
            AssetDatabase.CreateAsset(themeConfig, SharedConfigPath);
        }

        AssignOptionalDefaults(themeConfig);
        EditorUtility.SetDirty(themeConfig);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = themeConfig;
        return themeConfig;
    }

    public static CoopMinigameThemeConfig GetOrCreateDefaultTheme()
    {
        var themeConfig = AssetDatabase.LoadAssetAtPath<CoopMinigameThemeConfig>(SharedConfigPath);
        return themeConfig != null ? themeConfig : CreateOrUpdateDefaultTheme();
    }

    private static void AssignOptionalDefaults(CoopMinigameThemeConfig themeConfig)
    {
        var serializedTheme = new SerializedObject(themeConfig);

        var logoProperty = serializedTheme.FindProperty("logoSprite");
        if (logoProperty.objectReferenceValue == null)
        {
            logoProperty.objectReferenceValue = LoadSpriteForUi(ExistingLogoPath);
        }

        LoadSpriteForUi(GeneratedLogoCandidatePath);
        AssignDefaultAvatarsIfMissing(serializedTheme);

        var fontProperty = serializedTheme.FindProperty("primaryFont");
        if (fontProperty.objectReferenceValue == null)
        {
            fontProperty.objectReferenceValue = FindDefaultTmpFont();
        }

        serializedTheme.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void AssignDefaultAvatarsIfMissing(SerializedObject serializedTheme)
    {
        var avatarsProperty = serializedTheme.FindProperty("defaultAvatarSprites");
        if (avatarsProperty == null || avatarsProperty.arraySize > 0)
        {
            return;
        }

        for (var index = 0; index < GeneratedAvatarPaths.Length; index++)
        {
            var avatarSprite = LoadSpriteForUi(GeneratedAvatarPaths[index]);
            if (avatarSprite == null)
            {
                continue;
            }

            avatarsProperty.InsertArrayElementAtIndex(avatarsProperty.arraySize);
            avatarsProperty.GetArrayElementAtIndex(avatarsProperty.arraySize - 1).objectReferenceValue = avatarSprite;
        }
    }

    private static TMP_FontAsset FindDefaultTmpFont()
    {
        var guids = AssetDatabase.FindAssets("t:TMP_FontAsset", new[] { "Assets/TextMesh Pro", "Assets/Art" });
        for (var index = 0; index < guids.Length; index++)
        {
            var path = AssetDatabase.GUIDToAssetPath(guids[index]);
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (font != null)
            {
                return font;
            }
        }

        return null;
    }

    private static Sprite LoadSpriteForUi(string path)
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite != null)
        {
            return sprite;
        }

        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            return null;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static void EnsureFolder(string parent, string name)
    {
        var fullPath = $"{parent}/{name}";
        if (!AssetDatabase.IsValidFolder(fullPath))
        {
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
