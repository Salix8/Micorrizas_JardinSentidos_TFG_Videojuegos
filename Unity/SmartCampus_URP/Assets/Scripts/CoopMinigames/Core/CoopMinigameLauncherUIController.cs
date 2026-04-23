using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using SmartCampus.Dialogue;

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
        [SerializeField] private DialogueSettingsConfig dialogueSettings;
        [SerializeField] private DialogueUIController dialogueController;

        private readonly List<CoopMinigameLauncherEntryView> instantiatedEntries = new();
        private CoopMinigameCatalogEntry pendingEntryAfterStoryDialogue;

        private void Awake()
        {
            ResolveCoordinator();
            ResolveDialogueController();
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

            if (dialogueController != null)
            {
                dialogueController.DialogueCompleted += HandleStoryDialogueCompleted;
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

            if (dialogueController != null)
            {
                dialogueController.DialogueCompleted -= HandleStoryDialogueCompleted;
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

        private void ResolveDialogueController()
        {
            dialogueController ??= FindFirstObjectByType<DialogueUIController>(FindObjectsInactive.Include);
        }

        private void BuildEntries()
        {
            if (entryRoot == null || entryTemplate == null || minigameCatalogConfig == null)
            {
                return;
            }

            instantiatedEntries.Clear();
            entryTemplate.gameObject.SetActive(false);

            var sceneEntries = new List<CoopMinigameLauncherEntryView>();
            for (var childIndex = 0; childIndex < entryRoot.childCount; childIndex++)
            {
                var child = entryRoot.GetChild(childIndex);
                if (child == entryTemplate.transform)
                {
                    continue;
                }

                var childEntry = child.GetComponent<CoopMinigameLauncherEntryView>();
                if (childEntry != null)
                {
                    sceneEntries.Add(childEntry);
                }
            }

            for (var index = sceneEntries.Count; index < minigameCatalogConfig.Entries.Count; index++)
            {
                var entryInstance = Instantiate(entryTemplate, entryRoot, false);
                sceneEntries.Add(entryInstance);
            }

            for (var index = 0; index < sceneEntries.Count; index++)
            {
                var shouldBeActive = index < minigameCatalogConfig.Entries.Count && minigameCatalogConfig.Entries[index] != null;
                sceneEntries[index].gameObject.SetActive(shouldBeActive);
                if (shouldBeActive)
                {
                    instantiatedEntries.Add(sceneEntries[index]);
                }
            }
        }

        private void RefreshEntries()
        {
            ResolveCoordinator();

            if (minigameCatalogConfig == null)
            {
                return;
            }

            var isNetworkHost = coopSessionCoordinator != null && coopSessionCoordinator.IsSpawned && coopSessionCoordinator.IsServer;
            var isLocalDebugLauncher = !HasActiveNetworkSession();
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
                                (isLocalDebugLauncher || (coopSessionCoordinator != null && coopSessionCoordinator.IsMiniGameConfigured(minigameIndex)));
                view.Bind(
                    entry.DisplayName,
                    entry.Description,
                    canLaunch,
                    () => LaunchMinigame(entry));
            }
        }

        private void LaunchMinigame(CoopMinigameCatalogEntry entry)
        {
            if (TryLaunchStoryDialogueBeforeMinigame(entry))
            {
                return;
            }

            LaunchMinigameImmediately(entry);
        }

        private bool TryLaunchStoryDialogueBeforeMinigame(CoopMinigameCatalogEntry entry)
        {
            if (entry == null ||
                !entry.HasStoryDialogue ||
                !DialogueSettingsConfig.AreStoryDialoguesEnabled(dialogueSettings))
            {
                return false;
            }

            ResolveDialogueController();
            if (dialogueController == null)
            {
                Debug.LogWarning("El catalogo tiene un dialogo previo configurado, pero no hay DialogueUIController en la escena.", this);
                return false;
            }

            pendingEntryAfterStoryDialogue = entry;
            var started = entry.HasStoryDialogueLineIds
                ? dialogueController.PlayLineIds(entry.StoryDialogueLineIds)
                : dialogueController.PlayActOrLocation(entry.StoryDialogueActOrLocation);

            if (!started)
            {
                pendingEntryAfterStoryDialogue = null;
            }

            return started;
        }

        private void HandleStoryDialogueCompleted()
        {
            if (pendingEntryAfterStoryDialogue == null)
            {
                return;
            }

            var entry = pendingEntryAfterStoryDialogue;
            pendingEntryAfterStoryDialogue = null;
            LaunchMinigameImmediately(entry);
        }

        private void LaunchMinigameImmediately(CoopMinigameCatalogEntry entry)
        {
            ResolveCoordinator();

            if (coopSessionCoordinator != null && coopSessionCoordinator.IsSpawned && coopSessionCoordinator.IsServer)
            {
                coopSessionCoordinator.StartMiniGame(entry.MinigameIndex);
                return;
            }

            if (!HasActiveNetworkSession() && TryResolveLocalSceneName(entry, out var sceneName))
            {
                SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
            }
        }

        private bool TryResolveLocalSceneName(CoopMinigameCatalogEntry entry, out string sceneName)
        {
            sceneName = string.Empty;
            if (entry == null)
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(entry.SceneName))
            {
                sceneName = entry.SceneName;
                return true;
            }

            if (coopSessionCoordinator != null &&
                !coopSessionCoordinator.IsSpawned &&
                coopSessionCoordinator.TryGetMiniGameSceneName(entry.MinigameIndex, out sceneName))
            {
                return !string.IsNullOrWhiteSpace(sceneName);
            }

            return false;
        }

        private static bool HasActiveNetworkSession()
        {
            return NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
        }
    }
}
