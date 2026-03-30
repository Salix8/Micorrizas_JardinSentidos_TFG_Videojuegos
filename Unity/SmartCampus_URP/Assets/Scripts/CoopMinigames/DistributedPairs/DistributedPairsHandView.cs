using System;
using System.Collections.Generic;
using UnityEngine;
using SmartCampus.Coop.Minigames;

namespace SmartCampus.Coop.Minigames.DistributedPairs
{
    [DisallowMultipleComponent]
    public sealed class DistributedPairsHandView : MonoBehaviour
    {
        [SerializeField] private Transform cardRoot;
        [SerializeField] private DistributedPairsCardView cardPrefab;
        [SerializeField] private ResponsiveGridLayoutController responsiveGridLayoutController;

        private readonly List<DistributedPairsCardView> instantiatedCards = new();

        public void Render(
            IReadOnlyList<DistributedPairsCardNetworkState> handStates,
            DistributedPairsMinigameConfig config,
            bool isInteractable,
            Action<int> onCardSelected)
        {
            if (cardRoot == null || cardPrefab == null || config == null)
            {
                return;
            }

            ClearCards();

            if (responsiveGridLayoutController != null)
            {
                var visuals = config.CardVisualSettings;
                responsiveGridLayoutController.Configure(
                    visuals.MaxColumns,
                    visuals.MinCardSize,
                    visuals.MaxCardSize,
                    visuals.CardAspectRatio);
            }

            foreach (var state in handStates)
            {
                var view = Instantiate(cardPrefab, cardRoot, false);
                view.Bind(
                    state,
                    config.GetPairDefinition(state.PairId),
                    config.CardVisualSettings,
                    isInteractable,
                    onCardSelected);
                instantiatedCards.Add(view);
            }

            if (responsiveGridLayoutController != null)
            {
                responsiveGridLayoutController.RefreshLayout();
            }
        }

        private void ClearCards()
        {
            for (var index = 0; index < instantiatedCards.Count; index++)
            {
                if (instantiatedCards[index] != null)
                {
                    Destroy(instantiatedCards[index].gameObject);
                }
            }

            instantiatedCards.Clear();
        }
    }
}
