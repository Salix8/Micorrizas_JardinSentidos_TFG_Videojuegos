using UnityEngine;
using UnityEngine.EventSystems;

namespace SmartCampus.Coop.Minigames
{
    public static class LocalZoomPointerService
    {
        public static Vector2 GetLocalDelta(RectTransform target, PointerEventData eventData)
        {
            var parent = target == null ? null : target.parent as RectTransform;
            if (parent != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, eventData.position, eventData.pressEventCamera, out var current)
                && RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, eventData.position - eventData.delta, eventData.pressEventCamera, out var previous))
            {
                return current - previous;
            }
            return Vector2.zero;
        }
    }
}
