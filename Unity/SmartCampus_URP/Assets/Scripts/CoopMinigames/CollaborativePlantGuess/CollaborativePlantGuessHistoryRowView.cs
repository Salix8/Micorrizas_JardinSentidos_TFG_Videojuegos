using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using SmartCampus.Coop.Minigames;

namespace SmartCampus.Coop.Minigames.CollaborativePlantGuess
{
    [DisallowMultipleComponent]
    public sealed class CollaborativePlantGuessHistoryRowView : MonoBehaviour
    {
        [SerializeField] private Text guessedByLabel;
        [SerializeField] private Text plantNameLabel;
        [SerializeField] private Image plantImage;
        [SerializeField] private GameObject plantImagePlaceholder;
        [SerializeField] private Image leafPersistenceCell;
        [SerializeField] private Text leafPersistenceLabel;
        [SerializeField] private Image leafSizeCell;
        [SerializeField] private Text leafSizeLabel;
        [SerializeField] private Image leafTextureCell;
        [SerializeField] private Text leafTextureLabel;
        [SerializeField] private Image fruitTypeCell;
        [SerializeField] private Text fruitTypeLabel;

        private Coroutine imageLoadingCoroutine;
        private string boundImagePath = string.Empty;

        public void Bind(
            MonoBehaviour coroutineHost,
            CollaborativePlantGuessPlantDefinition plantDefinition,
            int guessingPlayerSlot,
            CollaborativePlantGuessHistoryEntryNetworkState historyEntry,
            CollaborativePlantGuessVisualSettings visualSettings)
        {
            if (guessedByLabel != null)
            {
                guessedByLabel.text = guessingPlayerSlot > 0
                    ? $"Intento del dispositivo {guessingPlayerSlot}"
                    : "Intento compartido";
            }

            if (plantNameLabel != null)
            {
                plantNameLabel.text = plantDefinition == null ? "Planta desconocida" : plantDefinition.DisplayName;
            }

            BindCell(leafPersistenceCell, leafPersistenceLabel, plantDefinition == null ? "?" : plantDefinition.LeafPersistence, historyEntry.LeafPersistenceOutcome, visualSettings);
            BindCell(leafSizeCell, leafSizeLabel, plantDefinition == null ? "?" : plantDefinition.LeafSize, historyEntry.LeafSizeOutcome, visualSettings);
            BindCell(leafTextureCell, leafTextureLabel, plantDefinition == null ? "?" : plantDefinition.LeafTexture, historyEntry.LeafTextureOutcome, visualSettings);
            BindCell(fruitTypeCell, fruitTypeLabel, plantDefinition == null ? "?" : plantDefinition.FruitType, historyEntry.FruitTypeOutcome, visualSettings);

            LoadImage(coroutineHost, plantDefinition == null ? string.Empty : plantDefinition.ImagePath);
        }

        private void BindCell(Image cellImage, Text label, string value, CollaborativePlantGuessComparisonOutcome outcome, CollaborativePlantGuessVisualSettings visualSettings)
        {
            if (label != null)
            {
                label.text = value;
            }

            if (cellImage != null)
            {
                cellImage.color = outcome switch
                {
                    CollaborativePlantGuessComparisonOutcome.Exact => visualSettings.ExactMatchColor,
                    CollaborativePlantGuessComparisonOutcome.Close => visualSettings.CloseMatchColor,
                    _ => visualSettings.IncorrectMatchColor
                };
            }
        }

        private void LoadImage(MonoBehaviour coroutineHost, string imagePath)
        {
            if (boundImagePath == imagePath)
            {
                return;
            }

            boundImagePath = imagePath ?? string.Empty;

            if (imageLoadingCoroutine != null && coroutineHost != null)
            {
                coroutineHost.StopCoroutine(imageLoadingCoroutine);
                imageLoadingCoroutine = null;
            }

            if (plantImage != null)
            {
                plantImage.sprite = null;
                plantImage.enabled = false;
            }

            if (plantImagePlaceholder != null)
            {
                plantImagePlaceholder.SetActive(true);
            }

            if (coroutineHost == null || string.IsNullOrWhiteSpace(boundImagePath))
            {
                return;
            }

            imageLoadingCoroutine = coroutineHost.StartCoroutine(LoadImageCoroutine(boundImagePath));
        }

        private IEnumerator LoadImageCoroutine(string imagePath)
        {
            yield return CoopMinigameExternalContentService.LoadSpriteAsync(
                imagePath,
                (sprite, _) =>
                {
                    if (imagePath != boundImagePath)
                    {
                        return;
                    }

                    if (plantImage != null)
                    {
                        plantImage.sprite = sprite;
                        plantImage.enabled = sprite != null;
                    }

                    if (plantImagePlaceholder != null)
                    {
                        plantImagePlaceholder.SetActive(sprite == null);
                    }
                });

            imageLoadingCoroutine = null;
        }
    }
}
