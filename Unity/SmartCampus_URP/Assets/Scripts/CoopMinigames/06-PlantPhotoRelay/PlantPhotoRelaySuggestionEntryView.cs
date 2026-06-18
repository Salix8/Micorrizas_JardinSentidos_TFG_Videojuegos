using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SmartCampus.Coop.Minigames.PlantPhotoRelay
{
    [DisallowMultipleComponent]
    public sealed class PlantPhotoRelaySuggestionEntryView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text label;

        public void Bind(string text, Action onPressed)
        {
            button ??= GetComponent<Button>();
            label ??= GetComponentInChildren<TMP_Text>(true);

            if (label != null)
            {
                label.text = text;
            }

            if (button == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            if (onPressed != null)
            {
                button.onClick.AddListener(() => onPressed.Invoke());
            }
        }
    }
}
