using UnityEngine;

namespace SmartCampus.Coop.Minigames
{
    public static class LocalZoomService
    {
        public static float ClampZoom(float value, float maximum) => Mathf.Clamp(value, 1f, Mathf.Max(1f, maximum));

        public static Vector2 ClampPosition(Vector2 position, Vector2 viewportSize, float zoom)
        {
            var excess = viewportSize * Mathf.Max(0f, zoom - 1f) * 0.5f;
            return new Vector2(Mathf.Clamp(position.x, -excess.x, excess.x), Mathf.Clamp(position.y, -excess.y, excess.y));
        }

        /// <summary>
        /// Devuelve la nueva posicion del contenido (relativa al centro del visor) para que el punto
        /// que estaba bajo <paramref name="previousFocus"/> quede bajo <paramref name="currentFocus"/>
        /// tras pasar de <paramref name="previousZoom"/> a <paramref name="newZoom"/>.
        /// Combina el zoom centrado en los dedos con el arrastre de dos dedos.
        /// </summary>
        public static Vector2 ZoomAroundFocus(Vector2 contentPosition, float previousZoom, float newZoom, Vector2 previousFocus, Vector2 currentFocus)
        {
            var safePreviousZoom = Mathf.Max(0.0001f, previousZoom);
            var contentPoint = (previousFocus - contentPosition) / safePreviousZoom;
            return currentFocus - contentPoint * newZoom;
        }
    }
}
