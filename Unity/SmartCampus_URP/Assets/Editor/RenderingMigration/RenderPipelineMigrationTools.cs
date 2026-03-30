using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Esri.ArcGISMapsSDK.Components;
using SmartCampus.Rendering;

namespace SmartCampus.Editor.RenderingMigration
{
    public static class RenderPipelineMigrationTools
    {
        private const string ReportsFolderName = "MigrationReports";
        private const string UrpHighAssetPath = "Assets/Settings/URPAndroid/SmartCampus_URP_High.asset";
        private const string UrpBalancedAssetPath = "Assets/Settings/URPAndroid/SmartCampus_URP_Balanced.asset";
        private const string UrpMobileAssetPath = "Assets/Settings/URPAndroid/SmartCampus_URP_Mobile.asset";
        private const string UrpNeutralVolumeProfilePath = "Assets/Settings/URPAndroid/SmartCampus_URP_NeutralVolume.asset";
        private const string ArcGisTerrainShaderPath = "Assets/Shaders/ArcGIS/SmartCampus_ArcGIS_Terrain_URP.shader";
        private const string ArcGisTerrainMaterialPath = "Assets/Settings/URPAndroid/ArcGIS/SmartCampus_ArcGIS_Terrain_URP.mat";
        private const string UjiScenePath = "Assets/UJI.unity";

        private static readonly string[] ProjectAssetRoots = { "Assets" };
        private static readonly string[] PackageRoots = { "Packages/com.esri.arcgis-maps-sdk" };
        private static readonly string[] TextScanExtensions =
        {
            ".asset",
            ".asmdef",
            ".cs",
            ".mat",
            ".prefab",
            ".shader",
            ".shadergraph",
            ".shadersubgraph",
            ".unity",
            ".vfx"
        };

        private static readonly string[] HdrpTokens =
        {
            "Unity.RenderPipelines.HighDefinition",
            "HDAdditionalCameraData",
            "HDAdditionalLightData",
            "HDRenderPipeline",
            "HDRP/",
            "HighDefinition",
            "LocalVolumetricFog",
            "PathTracing",
            "PhysicallyBasedSky",
            "RayTracing",
            "ScreenSpaceLensFlare",
            "VisualEnvironment"
        };

        private static readonly string[] VolumeTypeHints =
        {
            "Exposure",
            "Fog",
            "PathTracing",
            "PhysicallyBasedSky",
            "ScreenSpaceLensFlare",
            "VisualEnvironment",
            "Volumetric"
        };

        [MenuItem("Tools/Rendering Migration/Run HDRP Audit")]
        public static void RunHdrpAuditMenu()
        {
            RunHdrpAuditBatch();
        }

        [MenuItem("Tools/Rendering Migration/Apply URP Android Setup")]
        public static void ApplyUrpAndroidSetupMenu()
        {
            ApplyUrpAndroidSetupBatch();
        }

        [MenuItem("Tools/Rendering Migration/Run URP Validation")]
        public static void RunUrpValidationMenu()
        {
            RunUrpValidationBatch();
        }

        [MenuItem("Tools/Rendering Migration/Repair URP Artifacts")]
        public static void RepairUrpArtifactsMenu()
        {
            RepairUrpArtifactsBatch();
        }

        [MenuItem("Tools/Rendering Migration/Configure ArcGIS URP Map Surface")]
        public static void ConfigureArcGisUrpMapSurfaceMenu()
        {
            ConfigureArcGisUrpMapSurfaceBatch();
        }

        public static void RunHdrpAuditBatch()
        {
            var lines = new List<string>
            {
                "HDRP Audit",
                $"Generated: {DateTime.UtcNow:O}",
                $"Active render pipeline: {DescribeCurrentPipeline()}",
                $"Project root: {ProjectRootPath}"
            };

            lines.Add(string.Empty);
            lines.AddRange(BuildMaterialAuditSection());
            lines.Add(string.Empty);
            lines.AddRange(BuildVolumeProfileAuditSection());
            lines.Add(string.Empty);
            lines.AddRange(BuildScriptAuditSection());
            lines.Add(string.Empty);
            lines.AddRange(BuildSceneAuditSection());
            lines.Add(string.Empty);
            lines.AddRange(BuildPackageAuditSection());
            lines.Add(string.Empty);
            lines.AddRange(BuildAssetInventorySection());

            WriteReport("hdrp-audit.txt", lines);
            AssetDatabase.Refresh();
        }

        public static void ApplyUrpAndroidSetupBatch()
        {
            var lines = new List<string>
            {
                "URP Android Setup",
                $"Generated: {DateTime.UtcNow:O}"
            };

            var highAsset = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(UrpHighAssetPath);
            var balancedAsset = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(UrpBalancedAssetPath);

            if (highAsset == null)
            {
                lines.Add($"Missing render pipeline asset: {UrpHighAssetPath}");
            }
            else
            {
                GraphicsSettings.defaultRenderPipeline = highAsset;
                lines.Add($"GraphicsSettings.defaultRenderPipeline -> {UrpHighAssetPath}");
            }

            var balancedQualityIndex = Array.FindIndex(QualitySettings.names, name => name == "Balanced");
            if (balancedQualityIndex >= 0)
            {
                QualitySettings.SetQualityLevel(balancedQualityIndex, true);
                if (balancedAsset != null)
                {
                    QualitySettings.renderPipeline = balancedAsset;
                }

                lines.Add("QualitySettings -> Balanced");
            }
            else
            {
                lines.Add("Balanced quality level not found.");
            }

            PlayerSettings.colorSpace = ColorSpace.Linear;
#pragma warning disable CS0618
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.Android, ManagedStrippingLevel.Low);
#pragma warning restore CS0618
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

            lines.Add("PlayerSettings.colorSpace -> Linear");
            lines.Add("PlayerSettings Android scripting backend -> IL2CPP");
            lines.Add("PlayerSettings Android architectures -> ARM64");
            lines.Add("PlayerSettings Android managed stripping -> Low");

            try
            {
                var switched = EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
                lines.Add(switched ? "Active build target -> Android" : "Active build target switch to Android was skipped.");
            }
            catch (Exception exception)
            {
                lines.Add($"Active build target switch failed: {exception.Message}");
            }

            WriteReport("urp-android-setup.txt", lines);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public static void RunUrpValidationBatch()
        {
            var lines = new List<string>
            {
                "URP Validation",
                $"Generated: {DateTime.UtcNow:O}",
                $"Active render pipeline: {DescribeCurrentPipeline()}"
            };

            lines.Add(string.Empty);
            lines.AddRange(BuildMaterialValidationSection());
            lines.Add(string.Empty);
            lines.AddRange(BuildSceneValidationSection());
            lines.Add(string.Empty);
            lines.AddRange(BuildTextScanSection("Serialized HDRP references under Assets", ProjectAssetRoots, includeOnlyHdrpMatches: true));
            lines.Add(string.Empty);
            lines.AddRange(BuildTextScanSection("Embedded package references that still mention HDRP", PackageRoots, includeOnlyHdrpMatches: true));

            WriteReport("urp-validation.txt", lines);
            AssetDatabase.Refresh();
        }

        public static void RepairUrpArtifactsBatch()
        {
            var lines = new List<string>
            {
                "URP Repair",
                $"Generated: {DateTime.UtcNow:O}"
            };

            var repairedMaterials = RepairProjectMaterials();
            var cleanedScenes = RemoveMissingScriptsFromScenes().ToList();
            cleanedScenes.AddRange(ReplaceHdrpVolumeProfilesInScenes());

            AppendEntries(lines, "Materials reassigned to URP shaders", repairedMaterials);
            AppendEntries(lines, "Scenes cleaned from missing HDRP scripts", cleanedScenes);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            WriteReport("urp-repair.txt", lines);
        }

        public static void ConfigureArcGisUrpMapSurfaceBatch()
        {
            var lines = new List<string>
            {
                "ArcGIS URP Map Surface Setup",
                $"Generated: {DateTime.UtcNow:O}"
            };

            var terrainMaterial = EnsureArcGisTerrainMaterial(lines);
            if (terrainMaterial != null)
            {
                ConfigureUjiArcGisMapSurface(lines, terrainMaterial);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            WriteReport("arcgis-urp-map-surface.txt", lines);
        }

        private static IEnumerable<string> BuildMaterialAuditSection()
        {
            var lines = new List<string> { "Materials" };
            var materials = AssetDatabase.FindAssets("t:Material", ProjectAssetRoots)
                .Select(AssetDatabase.GUIDToAssetPath)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            lines.Add($"Total materials under Assets: {materials.Length}");

            var hdrpMaterials = new List<string>();
            foreach (var materialPath in materials)
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (material == null)
                {
                    continue;
                }

                var shaderName = material.shader != null ? material.shader.name : "Missing Shader";
                if (shaderName.Contains("HDRP", StringComparison.OrdinalIgnoreCase) ||
                    shaderName.Contains("High Definition", StringComparison.OrdinalIgnoreCase) ||
                    MaterialLooksHdrp(material))
                {
                    hdrpMaterials.Add($"{materialPath} :: {shaderName}");
                }
            }

            AppendEntries(lines, "Materials with HDRP dependency", hdrpMaterials);
            return lines;
        }

        private static IEnumerable<string> BuildVolumeProfileAuditSection()
        {
            var lines = new List<string> { "Volume Profiles" };
            var profiles = AssetDatabase.FindAssets("t:VolumeProfile", ProjectAssetRoots)
                .Select(AssetDatabase.GUIDToAssetPath)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            lines.Add($"Total volume profiles under Assets: {profiles.Length}");

            var hdrpProfiles = new List<string>();
            foreach (var profilePath in profiles)
            {
                var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
                if (profile == null)
                {
                    continue;
                }

                foreach (var component in profile.components)
                {
                    if (component == null)
                    {
                        hdrpProfiles.Add($"{profilePath} :: Missing volume component");
                        continue;
                    }

                    var typeName = component.GetType().FullName ?? component.GetType().Name;
                    if (IsHdrpTypeName(typeName) || VolumeTypeHints.Any(typeName.Contains))
                    {
                        hdrpProfiles.Add($"{profilePath} :: {typeName}");
                    }
                }
            }

            AppendEntries(lines, "Volume components that need URP review", hdrpProfiles);
            return lines;
        }

        private static IEnumerable<string> BuildScriptAuditSection()
        {
            var lines = new List<string> { "Scripts" };
            var matches = FindTextMatches(ProjectAssetRoots, new[] { ".cs", ".asmdef" }, HdrpTokens);
            AppendEntries(lines, "Project scripts or asmdefs that reference HDRP", matches);
            return lines;
        }

        private static IEnumerable<string> BuildSceneAuditSection()
        {
            var lines = new List<string> { "Scenes" };
            var scenePaths = AssetDatabase.FindAssets("t:Scene", ProjectAssetRoots)
                .Select(AssetDatabase.GUIDToAssetPath)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            lines.Add($"Total scenes under Assets: {scenePaths.Length}");

            var hdrpComponents = new List<string>();
            var missingScripts = new List<string>();
            var areaLights = new List<string>();
            var missingShaders = new List<string>();

            foreach (var scenePath in scenePaths)
            {
                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                foreach (var gameObject in EnumerateSceneGameObjects(scene))
                {
                    var transformPath = GetTransformPath(gameObject.transform);
                    foreach (var component in gameObject.GetComponents<Component>())
                    {
                        if (component == null)
                        {
                            missingScripts.Add($"{scenePath} :: {transformPath}");
                            continue;
                        }

                        var typeName = component.GetType().FullName ?? component.GetType().Name;
                        if (IsHdrpTypeName(typeName))
                        {
                            hdrpComponents.Add($"{scenePath} :: {transformPath} :: {typeName}");
                        }

                        if (component is Light light &&
                            (light.type == LightType.Rectangle || light.type == LightType.Disc))
                        {
                            areaLights.Add($"{scenePath} :: {transformPath}");
                        }

                        if (component is Renderer renderer)
                        {
                            foreach (var material in renderer.sharedMaterials)
                            {
                                if (material == null)
                                {
                                    continue;
                                }

                                if (material.shader == null)
                                {
                                    missingShaders.Add($"{scenePath} :: {transformPath} :: {material.name}");
                                }
                            }
                        }
                    }
                }
            }

            AppendEntries(lines, "HDRP-specific scene components", hdrpComponents);
            AppendEntries(lines, "Scene objects with missing scripts", missingScripts);
            AppendEntries(lines, "Area lights that need manual URP replacement", areaLights);
            AppendEntries(lines, "Scene materials already missing shaders", missingShaders);
            return lines;
        }

        private static IEnumerable<string> BuildPackageAuditSection()
        {
            var lines = new List<string> { "Embedded Packages" };
            AppendEntries(lines, "ArcGIS package files that still reference HDRP", FindTextMatches(PackageRoots, TextScanExtensions, HdrpTokens));
            return lines;
        }

        private static IEnumerable<string> BuildAssetInventorySection()
        {
            var lines = new List<string> { "Asset Inventory" };
            lines.Add($"Scenes: {AssetDatabase.FindAssets("t:Scene", ProjectAssetRoots).Length}");
            lines.Add($"Materials: {AssetDatabase.FindAssets("t:Material", ProjectAssetRoots).Length}");
            lines.Add($"Shader Graphs: {DirectoryFileCount(ProjectAssetRoots, ".shadergraph")}");
            lines.Add($"Shaders: {DirectoryFileCount(ProjectAssetRoots, ".shader")}");
            lines.Add($"VFX Graphs: {DirectoryFileCount(ProjectAssetRoots, ".vfx")}");
            return lines;
        }

        private static IEnumerable<string> BuildMaterialValidationSection()
        {
            var lines = new List<string> { "Materials" };
            var invalidMaterials = new List<string>();

            foreach (var materialPath in AssetDatabase.FindAssets("t:Material", ProjectAssetRoots)
                         .Select(AssetDatabase.GUIDToAssetPath)
                         .OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (material == null)
                {
                    invalidMaterials.Add($"{materialPath} :: Material could not be loaded");
                    continue;
                }

                if (material.shader == null)
                {
                    invalidMaterials.Add($"{materialPath} :: Missing shader");
                    continue;
                }

                if (material.shader.name == "Hidden/InternalErrorShader")
                {
                    invalidMaterials.Add($"{materialPath} :: Hidden/InternalErrorShader");
                }
            }

            AppendEntries(lines, "Materials that still need manual repair", invalidMaterials);
            return lines;
        }

        private static IEnumerable<string> BuildSceneValidationSection()
        {
            var lines = new List<string> { "Scenes" };
            var missingScripts = new List<string>();
            var missingShaders = new List<string>();
            var lingeringHdrpComponents = new List<string>();

            foreach (var scenePath in AssetDatabase.FindAssets("t:Scene", ProjectAssetRoots)
                         .Select(AssetDatabase.GUIDToAssetPath)
                         .OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
            {
                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                foreach (var gameObject in EnumerateSceneGameObjects(scene))
                {
                    var transformPath = GetTransformPath(gameObject.transform);
                    foreach (var component in gameObject.GetComponents<Component>())
                    {
                        if (component == null)
                        {
                            missingScripts.Add($"{scenePath} :: {transformPath}");
                            continue;
                        }

                        var typeName = component.GetType().FullName ?? component.GetType().Name;
                        if (IsHdrpTypeName(typeName))
                        {
                            lingeringHdrpComponents.Add($"{scenePath} :: {transformPath} :: {typeName}");
                        }

                        if (component is Renderer renderer)
                        {
                            foreach (var material in renderer.sharedMaterials)
                            {
                                if (material == null)
                                {
                                    continue;
                                }

                                if (material.shader == null || material.shader.name == "Hidden/InternalErrorShader")
                                {
                                    missingShaders.Add($"{scenePath} :: {transformPath} :: {material.name}");
                                }
                            }
                        }
                    }
                }
            }

            AppendEntries(lines, "Scene objects with missing scripts", missingScripts);
            AppendEntries(lines, "Scene objects with broken materials", missingShaders);
            AppendEntries(lines, "Scene objects still using HDRP component types", lingeringHdrpComponents);
            return lines;
        }

        private static IEnumerable<string> BuildTextScanSection(string title, IEnumerable<string> roots, bool includeOnlyHdrpMatches)
        {
            var lines = new List<string> { title };
            var matches = includeOnlyHdrpMatches
                ? FindTextMatches(roots, TextScanExtensions, HdrpTokens)
                : FindTextMatches(roots, TextScanExtensions, Array.Empty<string>());

            AppendEntries(lines, "Matches", matches);
            return lines;
        }

        private static IEnumerable<string> RepairProjectMaterials()
        {
            var repairedMaterials = new List<string>();
            var targetShader = Shader.Find("Universal Render Pipeline/Lit") ??
                               Shader.Find("Universal Render Pipeline/Simple Lit");

            if (targetShader == null)
            {
                repairedMaterials.Add("Target URP shader could not be found.");
                return repairedMaterials;
            }

            foreach (var materialPath in AssetDatabase.FindAssets("t:Material", ProjectAssetRoots)
                         .Select(AssetDatabase.GUIDToAssetPath)
                         .OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (material == null)
                {
                    continue;
                }

                if (material.shader != null && material.shader.name != "Hidden/InternalErrorShader")
                {
                    continue;
                }

                material.shader = targetShader;
                EditorUtility.SetDirty(material);
                repairedMaterials.Add(materialPath);
            }

            return repairedMaterials;
        }

        private static IEnumerable<string> RemoveMissingScriptsFromScenes()
        {
            var cleanedScenes = new List<string>();

            foreach (var scenePath in AssetDatabase.FindAssets("t:Scene", ProjectAssetRoots)
                         .Select(AssetDatabase.GUIDToAssetPath)
                         .OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
            {
                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                var removedScripts = 0;

                foreach (var gameObject in EnumerateSceneGameObjects(scene))
                {
                    removedScripts += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(gameObject);
                }

                if (removedScripts <= 0)
                {
                    continue;
                }

                EditorSceneManager.SaveScene(scene);
                cleanedScenes.Add($"{scenePath} :: removed {removedScripts} missing scripts");
            }

            return cleanedScenes;
        }

        private static IEnumerable<string> ReplaceHdrpVolumeProfilesInScenes()
        {
            var updatedScenes = new List<string>();
            var neutralProfile = LoadOrCreateNeutralUrpVolumeProfile();
            if (neutralProfile == null)
            {
                updatedScenes.Add("Neutral URP volume profile could not be created.");
                return updatedScenes;
            }

            foreach (var scenePath in AssetDatabase.FindAssets("t:Scene", ProjectAssetRoots)
                         .Select(AssetDatabase.GUIDToAssetPath)
                         .OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
            {
                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                var updatedVolumeCount = 0;

                foreach (var gameObject in EnumerateSceneGameObjects(scene))
                {
                    foreach (var component in gameObject.GetComponents<Component>())
                    {
                        if (component is not Volume volume || volume.sharedProfile == null)
                        {
                            continue;
                        }

                        if (!VolumeProfileNeedsUrpReplacement(volume.sharedProfile))
                        {
                            continue;
                        }

                        volume.sharedProfile = neutralProfile;
                        EditorUtility.SetDirty(volume);
                        updatedVolumeCount++;
                    }
                }

                if (updatedVolumeCount <= 0)
                {
                    continue;
                }

                EditorSceneManager.SaveScene(scene);
                updatedScenes.Add($"{scenePath} :: reassigned {updatedVolumeCount} HDRP volume references");
            }

            return updatedScenes;
        }

        private static VolumeProfile LoadOrCreateNeutralUrpVolumeProfile()
        {
            var existingProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(UrpNeutralVolumeProfilePath);
            if (existingProfile != null)
            {
                return existingProfile;
            }

            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = Path.GetFileNameWithoutExtension(UrpNeutralVolumeProfilePath);
            Directory.CreateDirectory(Path.Combine(ProjectRootPath, "Assets/Settings/URPAndroid"));
            AssetDatabase.CreateAsset(profile, UrpNeutralVolumeProfilePath);
            return profile;
        }

        private static bool VolumeProfileNeedsUrpReplacement(VolumeProfile profile)
        {
            var profilePath = AssetDatabase.GetAssetPath(profile);
            if (string.IsNullOrEmpty(profilePath))
            {
                return false;
            }

            if (profilePath.Contains("HDRP", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (profilePath.Contains("OutdoorsSceneProfile", StringComparison.OrdinalIgnoreCase) ||
                profilePath.Contains("Outdoors RayTracing Profile", StringComparison.OrdinalIgnoreCase) ||
                profilePath.Contains("SkyandFogSettingsProfile", StringComparison.OrdinalIgnoreCase) ||
                profilePath.Contains("Sky and Fog Settings", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return profile.components.Any(component =>
                component != null && IsHdrpTypeName(component.GetType().FullName ?? component.GetType().Name));
        }

        private static Material EnsureArcGisTerrainMaterial(List<string> lines)
        {
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(ArcGisTerrainShaderPath);
            if (shader == null)
            {
                lines.Add($"ArcGIS terrain shader not found: {ArcGisTerrainShaderPath}");
                return null;
            }

            Directory.CreateDirectory(Path.Combine(ProjectRootPath, "Assets/Settings/URPAndroid/ArcGIS"));

            var material = AssetDatabase.LoadAssetAtPath<Material>(ArcGisTerrainMaterialPath);
            if (material == null)
            {
                material = new Material(shader)
                {
                    name = Path.GetFileNameWithoutExtension(ArcGisTerrainMaterialPath)
                };
                AssetDatabase.CreateAsset(material, ArcGisTerrainMaterialPath);
                lines.Add($"Created ArcGIS terrain material: {ArcGisTerrainMaterialPath}");
            }
            else
            {
                lines.Add($"Updated ArcGIS terrain material: {ArcGisTerrainMaterialPath}");
            }

            material.shader = shader;
            material.SetColor("_BaseColor", Color.white);
            EditorUtility.SetDirty(material);

            return material;
        }

        private static void ConfigureUjiArcGisMapSurface(List<string> lines, Material terrainMaterial)
        {
            if (!File.Exists(Path.Combine(ProjectRootPath, UjiScenePath.Replace('/', Path.DirectorySeparatorChar))))
            {
                lines.Add($"Scene not found: {UjiScenePath}");
                return;
            }

            var scene = EditorSceneManager.OpenScene(UjiScenePath, OpenSceneMode.Single);
            var mapComponent = UnityEngine.Object.FindFirstObjectByType<ArcGISMapComponent>();
            if (mapComponent == null)
            {
                lines.Add($"ArcGISMapComponent not found in {UjiScenePath}");
                return;
            }

            var controller = mapComponent.GetComponent<ArcGISUrpMaterialOverrideController>();
            if (controller == null)
            {
                controller = mapComponent.gameObject.AddComponent<ArcGISUrpMaterialOverrideController>();
                lines.Add($"Added {nameof(ArcGISUrpMaterialOverrideController)} to {GetTransformPath(mapComponent.transform)}");
            }
            else
            {
                lines.Add($"Updated {nameof(ArcGISUrpMaterialOverrideController)} on {GetTransformPath(mapComponent.transform)}");
            }

            var serializedController = new SerializedObject(controller);
            serializedController.FindProperty("mapComponent").objectReferenceValue = mapComponent;
            serializedController.FindProperty("elevationMaterial").objectReferenceValue = terrainMaterial;
            serializedController.FindProperty("recreateBasemapOnFirstApply").boolValue = true;
            serializedController.FindProperty("retryLoadAfterApply").boolValue = true;
            serializedController.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(controller);
            EditorSceneManager.SaveScene(scene);
            lines.Add($"Saved ArcGIS surface override in {UjiScenePath}");
        }

        private static IEnumerable<string> FindTextMatches(IEnumerable<string> roots, IEnumerable<string> allowedExtensions, IEnumerable<string> tokens)
        {
            var allowed = new HashSet<string>(allowedExtensions, StringComparer.OrdinalIgnoreCase);
            var tokenList = tokens.ToArray();
            var matches = new List<string>();

            foreach (var filePath in EnumerateProjectFiles(roots, allowed))
            {
                string content;
                try
                {
                    content = File.ReadAllText(filePath);
                }
                catch (Exception exception)
                {
                    matches.Add($"{ToProjectRelativePath(filePath)} :: Read failed :: {exception.Message}");
                    continue;
                }

                if (tokenList.Length == 0)
                {
                    matches.Add(ToProjectRelativePath(filePath));
                    continue;
                }

                var foundTokens = tokenList.Where(content.Contains).Distinct().OrderBy(token => token, StringComparer.OrdinalIgnoreCase).ToArray();
                if (foundTokens.Length > 0)
                {
                    matches.Add($"{ToProjectRelativePath(filePath)} :: {string.Join(", ", foundTokens)}");
                }
            }

            matches.Sort(StringComparer.OrdinalIgnoreCase);
            return matches;
        }

        private static IEnumerable<GameObject> EnumerateSceneGameObjects(Scene scene)
        {
            var queue = new Queue<Transform>();
            foreach (var rootObject in scene.GetRootGameObjects())
            {
                queue.Enqueue(rootObject.transform);
            }

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                yield return current.gameObject;

                for (var i = 0; i < current.childCount; i++)
                {
                    queue.Enqueue(current.GetChild(i));
                }
            }
        }

        private static bool MaterialLooksHdrp(Material material)
        {
            return material.HasProperty("_DiffusionProfile") ||
                   material.HasProperty("_DiffusionProfileHash") ||
                   material.HasProperty("_EmissiveExposureWeight");
        }

        private static bool IsHdrpTypeName(string typeName)
        {
            return HdrpTokens.Any(typeName.Contains);
        }

        private static void AppendEntries(List<string> lines, string label, IEnumerable<string> entries)
        {
            var materializedEntries = entries.ToArray();
            lines.Add($"{label}: {materializedEntries.Length}");

            foreach (var entry in materializedEntries)
            {
                lines.Add($"- {entry}");
            }

            if (materializedEntries.Length == 0)
            {
                lines.Add("- none");
            }
        }

        private static int DirectoryFileCount(IEnumerable<string> roots, string extension)
        {
            return EnumerateProjectFiles(roots, new[] { extension }).Count();
        }

        private static IEnumerable<string> EnumerateProjectFiles(IEnumerable<string> roots, IEnumerable<string> extensions)
        {
            var extensionSet = new HashSet<string>(extensions, StringComparer.OrdinalIgnoreCase);
            foreach (var root in roots)
            {
                var absoluteRoot = Path.Combine(ProjectRootPath, root.Replace('/', Path.DirectorySeparatorChar));
                if (!Directory.Exists(absoluteRoot))
                {
                    continue;
                }

                foreach (var filePath in Directory.EnumerateFiles(absoluteRoot, "*", SearchOption.AllDirectories))
                {
                    if (extensionSet.Contains(Path.GetExtension(filePath)))
                    {
                        yield return filePath;
                    }
                }
            }
        }

        private static void WriteReport(string fileName, IEnumerable<string> lines)
        {
            var reportsDirectory = Path.Combine(ProjectRootPath, ReportsFolderName);
            Directory.CreateDirectory(reportsDirectory);
            var reportPath = Path.Combine(reportsDirectory, fileName);
            File.WriteAllText(reportPath, string.Join(Environment.NewLine, lines), Encoding.UTF8);
            Debug.Log($"Render pipeline migration report written to {reportPath}");
        }

        private static string DescribeCurrentPipeline()
        {
            var pipeline = GraphicsSettings.currentRenderPipeline ?? GraphicsSettings.defaultRenderPipeline;
            return pipeline == null ? "Built-in Render Pipeline" : pipeline.GetType().FullName ?? pipeline.name;
        }

        private static string GetTransformPath(Transform target)
        {
            var path = target.name;
            while (target.parent != null)
            {
                target = target.parent;
                path = $"{target.name}/{path}";
            }

            return path;
        }

        private static string ToProjectRelativePath(string absolutePath)
        {
            if (!absolutePath.StartsWith(ProjectRootPath, StringComparison.OrdinalIgnoreCase))
            {
                return absolutePath;
            }

            return absolutePath.Substring(ProjectRootPath.Length + 1).Replace('\\', '/');
        }

        private static string ProjectRootPath => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
    }
}
