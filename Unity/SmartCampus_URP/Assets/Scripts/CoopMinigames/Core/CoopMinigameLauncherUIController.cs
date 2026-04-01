using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace SmartCampus.Coop.Minigames
{
    [DisallowMultipleComponent]
    public sealed class CoopMinigameLauncherUIController : MonoBehaviour
    {
        [SerializeField] private CoopSessionCoordinator coopSessionCoordinator;
        [SerializeField] private CoopMinigameCatalogConfig minigameCatalogConfig;
        [SerializeField] private Transform entryRoot;
        [SerializeField] private CoopMinigameLauncherEntryView entryTemplate;
        [SerializeField] private Text helperLabel;

        private readonly List<CoopMinigameLauncherEntryView> instantiatedEntries = new();

        private void Awake()
        {
            ResolveCoordinator();
        }

        private void OnEnable()
        {
            ResolveCoordinator();
            BuildEntries();

            if (coopSessionCoordinator != null)
            {
                coopSessionCoordinator.PhaseChanged += HandleCoordinatorPhaseChanged;
                coopSessionCoordinator.SlotsChanged += HandleSlotsChanged;
            }

            RefreshEntries();
        }

        private void OnDisable()
        {
            if (coopSessionCoordinator != null)
            {
                coopSessionCoordinator.PhaseChanged -= HandleCoordinatorPhaseChanged;
                coopSessionCoordinator.SlotsChanged -= HandleSlotsChanged;
            }
        }

        private void HandleCoordinatorPhaseChanged(CoopGamePhase _)
        {
            RefreshEntries();
        }

        private void HandleSlotsChanged()
        {
            RefreshEntries();
        }

        private void ResolveCoordinator()
        {
            coopSessionCoordinator ??= FindFirstObjectByType<CoopSessionCoordinator>(FindObjectsInactive.Include);
        }

        private void BuildEntries()
        {
            if (entryRoot == null || entryTemplate == null || minigameCatalogConfig == null)
            {
                return;
            }

            for (var index = 0; index < instantiatedEntries.Count; index++)
            {
                if (instantiatedEntries[index] != null)
                {
                    Destroy(instantiatedEntries[index].gameObject);
                }
            }

            instantiatedEntries.Clear();
            entryTemplate.gameObject.SetActive(false);

            foreach (var entry in minigameCatalogConfig.Entries)
            {
                if (entry == null)
                {
                    continue;
                }

                var entryInstance = Instantiate(entryTemplate, entryRoot, false);
                entryInstance.gameObject.SetActive(true);
                instantiatedEntries.Add(entryInstance);
            }
        }

        private void RefreshEntries()
        {
            if (minigameCatalogConfig == null)
            {
                return;
            }

            var isNetworkHost = coopSessionCoordinator != null && coopSessionCoordinator.IsSpawned && coopSessionCoordinator.IsServer;
            var isLocalDebugLauncher = coopSessionCoordinator != null && !coopSessionCoordinator.IsSpawned;
            var canLaunchFromCurrentContext = isNetworkHost || isLocalDebugLauncher;
            if (helperLabel != null)
            {
                helperLabel.text = canLaunchFromCurrentContext
                    ? (isLocalDebugLauncher
                        ? "Modo local de prueba: puedes abrir cualquier minijuego configurado."
                        : "Elige el siguiente minijuego para todo el grupo.")
                    : "Esperando a que el host lance el siguiente minijuego.";
            }

            for (var index = 0; index < instantiatedEntries.Count && index < minigameCatalogConfig.Entries.Count; index++)
            {
                var entry = minigameCatalogConfig.Entries[index];
                var view = instantiatedEntries[index];
                var minigameIndex = entry.MinigameIndex;

                var canLaunch = canLaunchFromCurrentContext &&
                                (coopSessionCoordinator == null || coopSessionCoordinator.IsMiniGameConfigured(minigameIndex));
                view.Bind(
                    entry.DisplayName,
                    entry.Description,
                    canLaunch,
                    () => LaunchMinigame(minigameIndex));
            }
        }

        private void LaunchMinigame(int minigameIndex)
        {
            if (coopSessionCoordinator == null)
            {
                return;
            }

            if (coopSessionCoordinator.IsSpawned && coopSessionCoordinator.IsServer)
            {
                coopSessionCoordinator.StartMiniGame(minigameIndex);
                return;
            }

            if (!coopSessionCoordinator.IsSpawned &&
                coopSessionCoordinator.TryGetMiniGameSceneName(minigameIndex, out var sceneName))
            {
                SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
            }
        }
    }
}
