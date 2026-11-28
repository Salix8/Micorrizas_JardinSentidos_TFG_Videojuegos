using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Texto de depuracion en pantalla para ver en el dispositivo por que una zona no lanza su mision.
/// No tiene GraphicRaycaster, asi que no bloquea toques, arrastre ni zoom del mapa.
/// </summary>
[DisallowMultipleComponent]
public sealed class CoopZoneDebugOverlay : MonoBehaviour
{
    private Transform owner;
    private Canvas overlayCanvas;
    private TextMeshProUGUI label;

    public static CoopZoneDebugOverlay Create(Transform owner)
    {
        var root = new GameObject("CoopZoneDebugOverlay", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(CoopZoneDebugOverlay));
        if (owner != null && owner.gameObject.scene.IsValid())
        {
            SceneManager.MoveGameObjectToScene(root, owner.gameObject.scene);
        }

        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 32001;

        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;

        var panelObject = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        panelObject.transform.SetParent(root.transform, false);
        var panel = panelObject.GetComponent<RectTransform>();
        panel.anchorMin = new Vector2(0f, 0f);
        panel.anchorMax = new Vector2(1f, 0f);
        panel.pivot = new Vector2(0.5f, 0f);
        panel.offsetMin = new Vector2(16f, 40f);
        panel.offsetMax = new Vector2(-16f, 40f);
        panel.sizeDelta = new Vector2(panel.sizeDelta.x, 330f);
        var background = panelObject.GetComponent<Image>();
        background.color = new Color(0f, 0f, 0f, 0.6f);
        background.raycastTarget = false;

        var labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(panel, false);
        var labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(14f, 10f);
        labelRect.offsetMax = new Vector2(-14f, -10f);

        var view = root.GetComponent<CoopZoneDebugOverlay>();
        view.owner = owner;
        view.overlayCanvas = canvas;
        view.label = labelObject.GetComponent<TextMeshProUGUI>();
        view.label.fontSize = 24f;
        view.label.color = Color.white;
        view.label.alignment = TextAlignmentOptions.TopLeft;
        view.label.raycastTarget = false;
        view.label.richText = true;
        view.label.text = string.Empty;
        return view;
    }

    public void SetText(string text)
    {
        if (label != null && label.text != text)
        {
            label.text = text;
        }
    }

    public void SetVisible(bool visible)
    {
        if (overlayCanvas != null && overlayCanvas.enabled != visible)
        {
            overlayCanvas.enabled = visible;
        }
    }

    private void Update()
    {
        if (owner == null)
        {
            Destroy(gameObject);
        }
    }
}
