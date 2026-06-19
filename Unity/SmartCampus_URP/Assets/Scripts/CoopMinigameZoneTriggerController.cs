using System.Collections.Generic;
using System;
using Unity.Netcode;
using UnityEngine;
using SmartCampus.Coop.Minigames;
using SmartCampus.Dialogue;
#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
public sealed class CoopMinigameZoneTriggerController : MonoBehaviour
{
    private const string IncompleteZoneSpriteAssetPath = "Assets/Art/MicorrizaMarron.png";
    private const string CompletedZoneSpriteAssetPath = "Assets/Art/MicorrizaVerde.png";
    private const string ZoneVisualObjectName = "ZoneMicorrizaVisual";

    [Header("References")]
    [SerializeField] private CoopGpsMarkerController gpsMarkerController;
    [SerializeField] private CoopGpsStateSync gpsStateSync;
    [SerializeField] private CoopSessionCoordinator coopSessionCoordinator;
    [SerializeField] private CoopSessionProgressSync coopSessionProgressSync;
    [SerializeField] private DialogueFlowSync dialogueFlowSync;
    [SerializeField] private CoopMinigameZoneCountdownUIController countdownUiController;
    [SerializeField] private CoopMinigameZoneDefinition[] zoneDefinitions;

    [Header("Trigger Rules")]
    [SerializeField] [Min(0.5f)] private float countdownDurationSeconds = 2.5f;
    [SerializeField] [Min(0.1f)] private float evaluationIntervalSeconds = 0.1f;
    [SerializeField] private bool logZoneTransitionsToConsole = true;

    [Header("Zone Visuals")]
    [SerializeField] private Sprite incompleteZoneSprite;
    [SerializeField] private Sprite completedZoneSprite;
    [SerializeField] private Vector3 zoneVisualLocalOffset = new(0f, 8f, 0f);
    [SerializeField] private Vector3 zoneVisualLocalEulerAngles = new(90f, 0f, 0f);
    [SerializeField] [Min(0.01f)] private float zoneVisualScaleMultiplier = 0.035f;
    [SerializeField] private int zoneVisualSortingOrder = 25;

    private readonly CoopMinigameZoneCountdownTracker countdownTracker = new();
    private readonly List<ulong> activePlayerIds = new();
    private readonly Dictionary<int, SpriteRenderer> zoneVisualRenderers = new();
    private float nextEvaluationTime;
    private int previousCandidateZoneId = -1;
    private int lastTriggeredZoneId = -1;

    private void Awake()
    {
        ResolveReferences();
        RefreshZoneDefinitionsFromChildren();
        EnsureZoneVisuals();
        RefreshZoneVisualStates();
    }

    private void OnEnable()
    {
        ResolveReferences();
        RefreshZoneDefinitionsFromChildren();
        SubscribeToProgressSync();
        EnsureZoneVisuals();
        RefreshZoneVisualStates();
        ResetCountdown();
    }

    private void OnDisable()
    {
        if (coopSessionProgressSync != null)
        {
            coopSessionProgressSync.ProgressChanged -= HandleProgressChanged;
        }
    }

    private void OnValidate()
    {
        TryResolveEditorSpriteReferences();
        RefreshZoneDefinitionsFromChildren();
        EnsureZoneVisuals();
        RefreshZoneVisualStates();
    }

    private void OnTransformChildrenChanged()
    {
        RefreshZoneDefinitionsFromChildren();
        EnsureZoneVisuals();
        RefreshZoneVisualStates();
    }

    private void Update()
    {
        ResolveReferences();

        if (Time.unscaledTime < nextEvaluationTime)
        {
            return;
        }

        nextEvaluationTime = Time.unscaledTime + evaluationIntervalSeconds;
        EvaluateZones();
    }

    private void EvaluateZones()
    {
        RefreshZoneVisualStates();

        if (!IsWorldMapPhaseReady())
        {
            ResetCountdown();
            return;
        }

        var candidateZone = FindCandidateZone();
        var snapshot = countdownTracker.Update(
            candidateZone == null ? -1 : candidateZone.GetInstanceID(),
            candidateZone != null,
            Time.unscaledTime,
            countdownDurationSeconds);

        if (candidateZone == null)
        {
            if (previousCandidateZoneId != -1 && logZoneTransitionsToConsole)
            {
                Debug.Log("Minigame zone countdown cancelled because not all players remain inside the same valid zone.", this);
            }

            previousCandidateZoneId = -1;
            lastTriggeredZoneId = -1;
            countdownUiController?.Hide();
            return;
        }

        if (snapshot.ZoneId != previousCandidateZoneId && logZoneTransitionsToConsole)
        {
            Debug.Log($"Minigame zone candidate detected: {candidateZone.DisplayName}.", this);
        }

        previousCandidateZoneId = snapshot.ZoneId;
        countdownUiController?.Show(candidateZone.DisplayName, snapshot.Progress01, snapshot.RemainingSeconds);

        if (!snapshot.IsCompleted || lastTriggeredZoneId == snapshot.ZoneId)
        {
            return;
        }

        lastTriggeredZoneId = snapshot.ZoneId;

        if (coopSessionCoordinator != null && coopSessionCoordinator.IsServer)
        {
            if (!coopSessionCoordinator.CanLaunchMiniGame(candidateZone.MiniGameIndex))
            {
                Debug.LogWarning(
                    $"Zone '{candidateZone.DisplayName}' cannot launch minigame. MiniGameNumber={candidateZone.MiniGameNumber} MiniGameIndex={candidateZone.MiniGameIndex} is unavailable in the current cooperative session.",
                    this);
                return;
            }

            coopSessionCoordinator.TryGetMiniGameSceneName(candidateZone.MiniGameIndex, out var sceneName);
            Debug.Log(
                $"Launching zone minigame '{candidateZone.DisplayName}'. MiniGameNumber={candidateZone.MiniGameNumber} MiniGameIndex={candidateZone.MiniGameIndex} Scene='{sceneName}'.",
                this);
            if (dialogueFlowSync != null)
            {
                dialogueFlowSync.RequestStartMinigame(candidateZone.MiniGameIndex);
            }
            else
            {
                coopSessionCoordinator.StartMiniGame(candidateZone.MiniGameIndex);
            }
        }
    }

    private CoopMinigameZoneDefinition FindCandidateZone()
    {
        if (zoneDefinitions == null || zoneDefinitions.Length == 0 || gpsStateSync == null || coopSessionCoordinator == null)
        {
            return null;
        }

        BuildActivePlayerIds();
        if (activePlayerIds.Count == 0)
        {
            return null;
        }

        for (var index = 0; index < zoneDefinitions.Length; index++)
        {
            var zone = zoneDefinitions[index];
            if (zone == null)
            {
                continue;
            }

            if (!coopSessionCoordinator.CanLaunchMiniGame(zone.MiniGameIndex))
            {
                continue;
            }

            var allPlayersInside = true;
            for (var playerIndex = 0; playerIndex < activePlayerIds.Count; playerIndex++)
            {
                var clientId = activePlayerIds[playerIndex];
                if (!gpsStateSync.TryGetState(clientId, out var state) ||
                    !state.HasFix ||
                    state.HorizontalAccuracy > zone.MaxAcceptedAccuracyMeters ||
                    !gpsMarkerController.TryGetMarkerWorldPosition(clientId, out var markerWorldPosition) ||
                    !zone.Contains(markerWorldPosition))
                {
                    allPlayersInside = false;
                    break;
                }
            }

            if (allPlayersInside)
            {
                return zone;
            }
        }

        return null;
    }

    private void BuildActivePlayerIds()
    {
        activePlayerIds.Clear();
        if (coopSessionCoordinator == null)
        {
            return;
        }

        for (var slotIndex = 0; slotIndex < coopSessionCoordinator.RegisteredPlayerCount; slotIndex++)
        {
            if (coopSessionCoordinator.TryGetPlayerClientIdAtSlot(slotIndex, out var clientId))
            {
                activePlayerIds.Add(clientId);
            }
        }
    }

    private bool IsWorldMapPhaseReady()
    {
        return gpsMarkerController != null &&
               gpsStateSync != null &&
               coopSessionCoordinator != null &&
               (dialogueFlowSync == null || !dialogueFlowSync.IsFlowBusy) &&
               coopSessionCoordinator.CurrentPhase == CoopGamePhase.WorldMap;
    }

    private void ResetCountdown()
    {
        countdownTracker.Reset();
        previousCandidateZoneId = -1;
        lastTriggeredZoneId = -1;
        countdownUiController?.Hide();
        RefreshZoneVisualStates();
    }

    private void ResolveReferences()
    {
        gpsMarkerController ??= FindFirstObjectByType<CoopGpsMarkerController>(FindObjectsInactive.Include);
        gpsStateSync ??= FindFirstObjectByType<CoopGpsStateSync>(FindObjectsInactive.Include);
        coopSessionCoordinator ??= FindFirstObjectByType<CoopSessionCoordinator>(FindObjectsInactive.Include);
        coopSessionProgressSync ??= coopSessionCoordinator != null
            ? coopSessionCoordinator.SessionProgressSync
            : FindFirstObjectByType<CoopSessionProgressSync>(FindObjectsInactive.Include);
        dialogueFlowSync ??= coopSessionCoordinator != null
            ? coopSessionCoordinator.GetComponent<DialogueFlowSync>()
            : FindFirstObjectByType<DialogueFlowSync>(FindObjectsInactive.Include);
        countdownUiController ??= FindFirstObjectByType<CoopMinigameZoneCountdownUIController>(FindObjectsInactive.Include);

        if (zoneDefinitions == null || zoneDefinitions.Length == 0)
        {
            RefreshZoneDefinitionsFromChildren();
        }
    }

    private void RefreshZoneDefinitionsFromChildren()
    {
        var resolvedDefinitions = GetComponentsInChildren<CoopMinigameZoneDefinition>(true);
        if (resolvedDefinitions == null || resolvedDefinitions.Length == 0)
        {
            zoneDefinitions = Array.Empty<CoopMinigameZoneDefinition>();
            return;
        }

        Array.Sort(resolvedDefinitions, CompareZoneDefinitions);
        zoneDefinitions = resolvedDefinitions;
    }

    private void SubscribeToProgressSync()
    {
        if (coopSessionProgressSync == null)
        {
            return;
        }

        coopSessionProgressSync.ProgressChanged -= HandleProgressChanged;
        coopSessionProgressSync.ProgressChanged += HandleProgressChanged;
    }

    private void HandleProgressChanged()
    {
        RefreshZoneVisualStates();
    }

    private void EnsureZoneVisuals()
    {
        zoneVisualRenderers.Clear();
        if (zoneDefinitions == null || zoneDefinitions.Length == 0)
        {
            return;
        }

        for (var index = 0; index < zoneDefinitions.Length; index++)
        {
            var zone = zoneDefinitions[index];
            if (zone == null)
            {
                continue;
            }

            var renderer = EnsureZoneVisualRenderer(zone);
            if (renderer != null)
            {
                zoneVisualRenderers[zone.GetInstanceID()] = renderer;
            }
        }
    }

    private SpriteRenderer EnsureZoneVisualRenderer(CoopMinigameZoneDefinition zone)
    {
        var visualTransform = zone.transform.Find(ZoneVisualObjectName);
        GameObject visualObject;
        SpriteRenderer renderer;

        if (visualTransform == null)
        {
            visualObject = new GameObject(ZoneVisualObjectName, typeof(SpriteRenderer));
            visualObject.transform.SetParent(zone.transform, false);
            renderer = visualObject.GetComponent<SpriteRenderer>();
        }
        else
        {
            visualObject = visualTransform.gameObject;
            renderer = visualObject.GetComponent<SpriteRenderer>();
            if (renderer == null)
            {
                renderer = visualObject.AddComponent<SpriteRenderer>();
            }
        }

        ConfigureZoneVisualTransform(zone, visualObject.transform);
        renderer.sortingOrder = zoneVisualSortingOrder;
        renderer.sprite = ResolveZoneSprite(zone);
        renderer.enabled = renderer.sprite != null;
        return renderer;
    }

    private void ConfigureZoneVisualTransform(CoopMinigameZoneDefinition zone, Transform visualTransform)
    {
        visualTransform.localPosition = zoneVisualLocalOffset;
        visualTransform.localRotation = Quaternion.Euler(zoneVisualLocalEulerAngles);
        visualTransform.localScale = Vector3.one * ResolveZoneVisualScale(zone);
    }

    private float ResolveZoneVisualScale(CoopMinigameZoneDefinition zone)
    {
        if (zone != null && zone.ZoneCollider is BoxCollider boxCollider)
        {
            var size = boxCollider.size;
            var largestSide = Mathf.Max(size.x, size.z);
            return Mathf.Max(0.01f, largestSide * zoneVisualScaleMultiplier);
        }

        return Mathf.Max(0.01f, zoneVisualScaleMultiplier);
    }

    private void RefreshZoneVisualStates()
    {
        if (zoneDefinitions == null || zoneDefinitions.Length == 0)
        {
            return;
        }

        for (var index = 0; index < zoneDefinitions.Length; index++)
        {
            var zone = zoneDefinitions[index];
            if (zone == null)
            {
                continue;
            }

            if (!zoneVisualRenderers.TryGetValue(zone.GetInstanceID(), out var renderer) || renderer == null)
            {
                renderer = EnsureZoneVisualRenderer(zone);
                if (renderer == null)
                {
                    continue;
                }

                zoneVisualRenderers[zone.GetInstanceID()] = renderer;
            }

            renderer.sprite = ResolveZoneSprite(zone);
            renderer.enabled = renderer.sprite != null;
            ConfigureZoneVisualTransform(zone, renderer.transform);
        }
    }

    private Sprite ResolveZoneSprite(CoopMinigameZoneDefinition zone)
    {
        if (zone == null)
        {
            return incompleteZoneSprite;
        }

        var isCompleted = coopSessionProgressSync != null && coopSessionProgressSync.IsMinigameCompleted(zone.MiniGameIndex);
        return isCompleted ? completedZoneSprite : incompleteZoneSprite;
    }

    private void TryResolveEditorSpriteReferences()
    {
#if UNITY_EDITOR
        if (incompleteZoneSprite == null)
        {
            incompleteZoneSprite = AssetDatabase.LoadAssetAtPath<Sprite>(IncompleteZoneSpriteAssetPath);
        }

        if (completedZoneSprite == null)
        {
            completedZoneSprite = AssetDatabase.LoadAssetAtPath<Sprite>(CompletedZoneSpriteAssetPath);
        }
#endif
    }

    private static int CompareZoneDefinitions(CoopMinigameZoneDefinition left, CoopMinigameZoneDefinition right)
    {
        if (ReferenceEquals(left, right))
        {
            return 0;
        }

        if (left == null)
        {
            return 1;
        }

        if (right == null)
        {
            return -1;
        }

        var miniGameComparison = left.MiniGameNumber.CompareTo(right.MiniGameNumber);
        if (miniGameComparison != 0)
        {
            return miniGameComparison;
        }

        return string.Compare(left.DisplayName, right.DisplayName, StringComparison.Ordinal);
    }
}
