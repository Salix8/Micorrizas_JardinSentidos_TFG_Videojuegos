using System;
using UnityEngine;
using UnityEngine.UI;

namespace SmartCampus.Coop.Minigames
{
    [DisallowMultipleComponent]
    public sealed class MinigameResultView : MonoBehaviour
    {
        [SerializeField] private Text titleLabel;
        [SerializeField] private Text scoreLabel;
        [SerializeField] private Text summaryLabel;
        [SerializeField] private Button returnButton;
        [SerializeField] private Text returnButtonLabel;
        [SerializeField] private Text waitingHostLabel;

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
                summaryLabel.text = $"Parejas acertadas: {result.SuccessfulActions}\nIntentos incorrectos: {result.FailedActions}";
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
