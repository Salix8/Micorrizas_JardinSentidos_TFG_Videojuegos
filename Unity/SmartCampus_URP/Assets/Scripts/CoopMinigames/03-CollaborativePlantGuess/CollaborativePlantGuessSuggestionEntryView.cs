using System;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace SmartCampus.Coop.Minigames.CollaborativePlantGuess
{
    [DisallowMultipleComponent]
    public sealed class CollaborativePlantGuessSuggestionEntryView : MonoBehaviour
    {
        [SerializeField] private Button selectionButton;
        [SerializeField] private TMP_Text titleLabel;

        public void Bind(string displayName, Action onSelected)
        {
            if (titleLabel != null)
            {
                titleLabel.text = displayName;
            }

            if (selectionButton != null)
            {
                selectionButton.onClick.RemoveAllListeners();
                selectionButton.interactable = onSelected != null;
                if (onSelected != null)
                {
                    selectionButton.onClick.AddListener(() => onSelected());
                }
            }
        }
    }
}
