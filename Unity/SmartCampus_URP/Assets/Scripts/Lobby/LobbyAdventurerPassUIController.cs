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
    [SerializeField] private RawImage previewImage;
    [SerializeField] private Camera previewCamera;
    [SerializeField] private Transform previewRoot;
    [SerializeField] private Image previewFrameImage;
    [SerializeField] private LobbyMarkerShapeOptionView[] shapeOptions = new LobbyMarkerShapeOptionView[0];
    [SerializeField] private LobbyMarkerColorOptionView[] colorOptions = new LobbyMarkerColorOptionView[0];

    private GameObject currentPreviewInstance;
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
        for (var index = 0; index < shapeOptions.Length; index++)
        {
            var option = shapeOptions[index];
            if (option == null || option.Button == null)
            {
                continue;
            }

            option.Button.onClick.RemoveAllListeners();
            var capturedShapeId = option.ShapeId;
            option.Button.onClick.AddListener(() => SelectShape(capturedShapeId));

            if (appearanceCatalog != null && appearanceCatalog.TryGetShape(capturedShapeId, out var shape))
            {
                option.Configure(shape.DisplayName);
                option.gameObject.SetActive(true);
            }
            else
            {
                option.gameObject.SetActive(false);
            }
        }

        RefreshShapeOptionsLayout();

        for (var index = 0; index < colorOptions.Length; index++)
        {
            var option = colorOptions[index];
            if (option == null || option.Button == null)
            {
                continue;
            }

            option.Button.onClick.RemoveAllListeners();
            var capturedColorId = option.ColorId;
            option.Button.onClick.AddListener(() => SelectColor(capturedColorId));

            if (appearanceCatalog != null && appearanceCatalog.TryGetColor(capturedColorId, out var color))
            {
                option.Configure(color.DisplayName, color.Color);
                option.gameObject.SetActive(true);
            }
            else
            {
                option.gameObject.SetActive(false);
            }
        }
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

        for (var index = 0; index < shapeOptions.Length; index++)
        {
            if (shapeOptions[index] != null)
            {
                shapeOptions[index].SetSelected(string.Equals(shapeOptions[index].ShapeId, profileService.CurrentShapeId));
            }
        }

        for (var index = 0; index < colorOptions.Length; index++)
        {
            if (colorOptions[index] != null)
            {
                colorOptions[index].SetSelected(string.Equals(colorOptions[index].ColorId, profileService.CurrentColorId));
            }
        }
    }

    private void SelectShape(string shapeId)
    {
        profileService?.SetShapeId(shapeId);
    }

    private void SelectColor(string colorId)
    {
        profileService?.SetColorId(colorId);
    }

    private void RebuildPreview()
    {
        if (previewRoot == null || profileService == null)
        {
            return;
        }

        ClearPreviewRoot();

        if (!profileService.TryGetSelectedShape(out var shape) || shape == null || shape.VisualPrefab == null)
        {
            return;
        }

        currentPreviewInstance = Instantiate(shape.VisualPrefab, previewRoot);
        currentPreviewInstance.name = "PreviewVisual";
        currentPreviewInstance.transform.localPosition = Vector3.zero;
        currentPreviewInstance.transform.localRotation = Quaternion.Euler(shape.PreviewEulerAngles);
        currentPreviewInstance.transform.localScale = shape.PreviewScale;

        var previewColor = profileService.TryGetSelectedColor(out var colorDefinition) && colorDefinition != null
            ? colorDefinition.Color
            : Color.white;

        ApplyColor(currentPreviewInstance, previewColor);
        ApplyPreviewFrameAccent(previewColor);
        EnsurePreviewCameraEnabled();
    }

    private void EnsurePreviewCameraEnabled()
    {
        if (previewCamera != null)
        {
            previewCamera.enabled = true;
            if (previewRoot != null)
            {
                previewCamera.transform.LookAt(previewRoot.position);
            }

            previewCamera.Render();
        }

        if (previewImage != null && previewCamera != null)
        {
            previewImage.texture = previewCamera.targetTexture;
            previewImage.raycastTarget = false;
        }
    }

    private void DestroyPreviewInstance()
    {
        if (currentPreviewInstance == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(currentPreviewInstance);
        }
        else
        {
            DestroyImmediate(currentPreviewInstance);
        }

        currentPreviewInstance = null;
    }

    private void ClearPreviewRoot()
    {
        DestroyPreviewInstance();

        if (previewRoot == null)
        {
            return;
        }

        for (var index = previewRoot.childCount - 1; index >= 0; index--)
        {
            var child = previewRoot.GetChild(index);
            if (child == null)
            {
                continue;
            }

            if (Application.isPlaying)
            {
                Destroy(child.gameObject);
            }
            else
            {
                DestroyImmediate(child.gameObject);
            }
        }
    }

    private void RefreshShapeOptionsLayout()
    {
        if (shapeOptions == null || shapeOptions.Length == 0)
        {
            return;
        }

        var firstOption = shapeOptions[0];
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
        for (var index = 0; index < shapeOptions.Length; index++)
        {
            if (shapeOptions[index] != null && shapeOptions[index].gameObject.activeSelf)
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

    private void ApplyPreviewFrameAccent(Color markerColor)
    {
        if (previewFrameImage == null)
        {
            return;
        }

        previewFrameImage.color = Color.Lerp(
            new Color(0.26f, 0.44f, 0.62f, 0.9f),
            new Color(markerColor.r, markerColor.g, markerColor.b, 0.92f),
            0.35f);
    }

    private static void ApplyColor(GameObject targetRoot, Color color)
    {
        var renderers = targetRoot.GetComponentsInChildren<Renderer>(true);
        for (var index = 0; index < renderers.Length; index++)
        {
            var renderer = renderers[index];
            if (renderer == null)
            {
                continue;
            }

            var sourceMaterials = renderer.sharedMaterials;
            var runtimeMaterials = new Material[sourceMaterials.Length];
            for (var materialIndex = 0; materialIndex < sourceMaterials.Length; materialIndex++)
            {
                var sourceMaterial = sourceMaterials[materialIndex];
                if (sourceMaterial == null)
                {
                    continue;
                }

                var runtimeMaterial = new Material(sourceMaterial);
                if (runtimeMaterial.HasProperty("_BaseColor"))
                {
                    runtimeMaterial.SetColor("_BaseColor", color);
                }

                if (runtimeMaterial.HasProperty("_Color"))
                {
                    runtimeMaterial.SetColor("_Color", color);
                }

                runtimeMaterial.color = color;
                runtimeMaterials[materialIndex] = runtimeMaterial;
            }

            renderer.materials = runtimeMaterials;
        }
    }
}
