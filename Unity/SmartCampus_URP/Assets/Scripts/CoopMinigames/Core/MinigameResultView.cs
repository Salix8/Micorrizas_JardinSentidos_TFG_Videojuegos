using System;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace SmartCampus.Coop.Minigames
{
    [DisallowMultipleComponent]
    public sealed class MinigameResultView : MonoBehaviour
    {
        [Header("Labels")]
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text scoreLabel;
        [SerializeField] private TMP_Text summaryLabel;

        [Header("Buttons")]
        [SerializeField] private Button returnButton;
        [SerializeField] private TMP_Text returnButtonLabel;
        [SerializeField] private TMP_Text waitingHostLabel;

        [Header("Summary Formatting")]
        [SerializeField] private string successfulActionsLabel = "Aciertos";
        [SerializeField] private string failedActionsLabel = "Errores";

        public void Bind(MinigameResultData result, string returnButtonText, bool canReturnToMap, Action onReturnToMap)
        {
            if (titleLabel != null)
            {
                titleLabel.text = result.Message;
            }

            if (scoreLabel != null)
            {
                scoreLabel.text = $"{result.ScoreOutOfTen:0.0}/10";
            }

            if (summaryLabel != null)
            {
                summaryLabel.text = $"{successfulActionsLabel}: {result.SuccessfulActions}\n{failedActionsLabel}: {result.FailedActions}";
            }

            if (returnButtonLabel != null)
            {
                returnButtonLabel.text = returnButtonText;
            }

            if (returnButton != null)
            {
                returnButton.onClick.RemoveAllListeners();
                returnButton.gameObject.SetActive(canReturnToMap);
                returnButton.interactable = canReturnToMap;

                if (canReturnToMap && onReturnToMap != null)
                {
                    returnButton.onClick.AddListener(() => onReturnToMap());
                }
            }

            if (waitingHostLabel != null)
            {
                waitingHostLabel.gameObject.SetActive(!canReturnToMap);
                waitingHostLabel.text = "Esperando a que el host vuelva al mapa.";
            }
        }
    }
}
