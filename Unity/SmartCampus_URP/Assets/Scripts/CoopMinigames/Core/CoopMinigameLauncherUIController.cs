using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using SmartCampus.Dialogue;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SmartCampus.Coop.Minigames
{
    [DisallowMultipleComponent]
    public sealed class CoopMinigameLauncherUIController : MonoBehaviour
    {
        private const string CatalogAssetPath = "Assets/CoopMinigames/Configs/CoopMinigameCatalog.asset";

        [SerializeField] private CoopSessionCoordinator coopSessionCoordinator;
        [SerializeField] private CoopSessionProgressSync coopSessionProgressSync;
        [SerializeField] private DialogueFlowSync dialogueFlowSync;
        [SerializeField] private CoopMinigameCatalogConfig minigameCatalogConfig;
        [SerializeField] private Transform entryRoot;
        [SerializeField] private CoopMinigameLauncherEntryView entryTemplate;
        [SerializeField] private TMP_Text helperLabel;
        [Header("Scene Authored Entries")]
        [SerializeField] private bool useSceneAuthoredEntries = true;
        [SerializeField] private bool preserveSceneAuthoredEntryText = true;
        [SerializeField] private bool instantiateMissingCatalogEntries;

        private readonly List<CoopMinigameLauncherEntryView> instantiatedEntries = new();
        private bool hasLoggedCatalogValidation;

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            ResolveReferences();
            BuildEntries();
            ValidateCatalogMappings();

            if (coopSessionCoordinator != null)
            {
                coopSessionCoordinator.PhaseChanged += HandleCoordinatorPhaseChanged;
                coopSessionCoordinator.SlotsChanged += HandleSlotsChanged;
            }

            if (coopSessionProgressSync != null)
            {
                coopSessionProgressSync.ProgressChanged += HandleProgressChanged;
            }

            RefreshEntries();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                return;
            }

            if (minigameCatalogConfig != null)
            {
                return;
            }

            minigameCatalogConfig = AssetDatabase.LoadAssetAtPath<CoopMinigameCatalogConfig>(CatalogAssetPath);
            if (minigameCatalogConfig != null)
            {
                EditorUtility.SetDirty(this);
            }
        }
#endif

        private void OnDisable()
        {
            if (coopSessionCoordinator != null)
            {
                coopSessionCoordinator.PhaseChanged -= HandleCoordinatorPhaseChanged;
                coopSessionCoordinator.SlotsChanged -= HandleSlotsChanged;
            }

            if (coopSessionProgressSync != null)
            {
                coopSessionProgressSync.ProgressChanged -= HandleProgressChanged;
            }
        }

        private void HandleCoordinatorPhaseChanged(CoopGamePhase _)
        {
            ResolveReferences();
            RefreshEntries();
        }

        private void HandleSlotsChanged()
        {
            ResolveReferences();
            RefreshEntries();
        }

        private void HandleProgressChanged()
        {
            ResolveReferences();
            RefreshEntries();
        }

        private void ResolveReferences()
        {
            coopSessionCoordinator ??= FindFirstObjectByType<CoopSessionCoordinator>(FindObjectsInactive.Include);
            coopSessionProgressSync ??= coopSessionCoordinator != null
                ? coopSessionCoordinator.SessionProgressSync
                : FindFirstObjectByType<CoopSessionProgressSync>(FindObjectsInactive.Include);
            dialogueFlowSync ??= coopSessionCoordinator != null
                ? coopSessionCoordinator.GetComponent<DialogueFlowSync>()
                : FindFirstObjectByType<DialogueFlowSync>(FindObjectsInactive.Include);
            helperLabel ??= transform.Find("HelperLabel")?.GetComponent<TMP_Text>();

            if (entryRoot == null)
            {
                entryRoot = transform.Find("EntriesScrollView/Viewport/EntriesContent");
            }

            if (entryTemplate == null && entryRoot != null)
            {
                entryTemplate = entryRoot.GetComponentInChildren<CoopMinigameLauncherEntryView>(true);
            }
        }

        private void BuildEntries()
        {
            ResolveReferences();
            if (entryRoot == null || minigameCatalogConfig == null)
            {
                return;
            }

            if ((!useSceneAuthoredEntries || instantiateMissingCatalogEntries) && entryTemplate == null)
            {
                return;
            }

            instantiatedEntries.Clear();

            if (!useSceneAuthoredEntries && entryTemplate != null)
            {
                entryTemplate.gameObject.SetActive(false);
            }

            var sceneEntries = new List<CoopMinigameLauncherEntryView>();
            for (var childIndex = 0; childIndex < entryRoot.childCount; childIndex++)
            {
                var child = entryRoot.GetChild(childIndex);
                var isTemplateChild = entryTemplate != null && child == entryTemplate.transform;
                var shouldSkipTemplate = isTemplateChild &&
                                         (!useSceneAuthoredEntries || !entryTemplate.gameObject.activeSelf);
                if (shouldSkipTemplate)
                {
                    continue;
                }

                var childEntry = child.GetComponent<CoopMinigameLauncherEntryView>();
                if (childEntry != null)
                {
                    sceneEntries.Add(childEntry);
                }
            }

            if (!useSceneAuthoredEntries || instantiateMissingCatalogEntries)
            {
                for (var index = sceneEntries.Count; index < minigameCatalogConfig.Entries.Count; index++)
                {
                    var entryInstance = Instantiate(entryTemplate, entryRoot, false);
                    sceneEntries.Add(entryInstance);
                }
            }

            for (var index = 0; index < sceneEntries.Count; index++)
            {
                var hasCatalogEntry = index < minigameCatalogConfig.Entries.Count && minigameCatalogConfig.Entries[index] != null;
                if (!useSceneAuthoredEntries)
                {
                    sceneEntries[index].gameObject.SetActive(hasCatalogEntry);
                }

                if (hasCatalogEntry && sceneEntries[index].gameObject.activeSelf)
                {
                    instantiatedEntries.Add(sceneEntries[index]);
                }
            }
        }

        private void RefreshEntries()
        {
            ResolveReferences();

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

                if (IsCatalogIndexInvalid(entry))
                {
                    Debug.LogWarning(
                        $"Launcher entry '{entry.DisplayName}' uses invalid MinigameIndex={minigameIndex}. Expected runtime indices in range 0..{Mathf.Max(0, minigameCatalogConfig.Entries.Count - 1)}.",
                        this);
                }

                var isCompleted = coopSessionProgressSync != null && coopSessionProgressSync.IsMinigameCompleted(minigameIndex);
                var canLaunch = canLaunchFromCurrentContext &&
                                (isLocalDebugLauncher || (coopSessionCoordinator != null && coopSessionCoordinator.CanLaunchMiniGame(minigameIndex)));
                var buttonText = "Abrir";
                if (isCompleted && coopSessionProgressSync.TryGetResult(minigameIndex, out var completedResult))
                {
                    buttonText = $"Completado {completedResult.ScoreOutOfTen:0.0}/10";
                }
                else if (!canLaunch)
                {
                    buttonText = "Bloqueado";
                }

                view.Bind(
                    entry.DisplayName,
                    entry.Description,
                    canLaunch && !isCompleted,
                    () => LaunchMinigame(entry),
                    buttonText,
                    preserveSceneAuthoredEntryText);
            }
        }

        private void LaunchMinigame(CoopMinigameCatalogEntry entry)
        {
            ResolveReferences();

            if (coopSessionCoordinator != null && !coopSessionCoordinator.CanLaunchMiniGame(entry.MinigameIndex))
            {
                return;
            }

            if (coopSessionCoordinator != null && coopSessionCoordinator.IsSpawned && coopSessionCoordinator.IsServer)
            {
                coopSessionCoordinator.TryGetMiniGameSceneName(entry.MinigameIndex, out var sceneName);
                Debug.Log(
                    $"Launcher requested minigame '{entry.DisplayName}'. MinigameIndex={entry.MinigameIndex} Scene='{sceneName}'.",
                    this);
                if (dialogueFlowSync != null)
                {
                    dialogueFlowSync.RequestStartMinigame(entry.MinigameIndex);
                }
                else
                {
                    coopSessionCoordinator.StartMiniGame(entry.MinigameIndex);
                }

                return;
            }

            if (!HasActiveNetworkSession() && TryResolveLocalSceneName(entry, out var localSceneName))
            {
                Debug.Log(
                    $"Launcher requested local minigame '{entry.DisplayName}'. MinigameIndex={entry.MinigameIndex} Scene='{localSceneName}'.",
                    this);
                SceneManager.LoadScene(localSceneName, LoadSceneMode.Single);
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

        private void ValidateCatalogMappings()
        {
            if (hasLoggedCatalogValidation || minigameCatalogConfig == null)
            {
                return;
            }

            hasLoggedCatalogValidation = true;
            var usedIndices = new HashSet<int>();
            var maxExpectedIndex = Mathf.Max(0, minigameCatalogConfig.Entries.Count - 1);

            for (var index = 0; index < minigameCatalogConfig.Entries.Count; index++)
            {
                var entry = minigameCatalogConfig.Entries[index];
                if (entry == null)
                {
                    continue;
                }

                if (IsCatalogIndexInvalid(entry))
                {
                    Debug.LogWarning(
                        $"Launcher catalog entry '{entry.DisplayName}' is misconfigured. MinigameIndex={entry.MinigameIndex}, expected range 0..{maxExpectedIndex}.",
                        this);
                    continue;
                }

                if (!usedIndices.Add(entry.MinigameIndex))
                {
                    Debug.LogWarning(
                        $"Launcher catalog contains duplicated MinigameIndex={entry.MinigameIndex} for entry '{entry.DisplayName}'.",
                        this);
                }

                if (coopSessionCoordinator != null &&
                    coopSessionCoordinator.TryGetMiniGameSceneName(entry.MinigameIndex, out var coordinatorSceneName) &&
                    !string.IsNullOrWhiteSpace(entry.SceneName) &&
                    !string.Equals(entry.SceneName, coordinatorSceneName, System.StringComparison.Ordinal))
                {
                    Debug.LogWarning(
                        $"Launcher catalog scene mismatch for '{entry.DisplayName}'. Catalog='{entry.SceneName}' Coordinator='{coordinatorSceneName}' Index={entry.MinigameIndex}.",
                        this);
                }
            }
        }

        private bool IsCatalogIndexInvalid(CoopMinigameCatalogEntry entry)
        {
            return entry == null ||
                   minigameCatalogConfig == null ||
                   entry.MinigameIndex < 0 ||
                   entry.MinigameIndex >= minigameCatalogConfig.Entries.Count;
        }
    }
}
