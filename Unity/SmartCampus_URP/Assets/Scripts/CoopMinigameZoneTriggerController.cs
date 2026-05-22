using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class CoopMinigameZoneTriggerController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CoopGpsMarkerController gpsMarkerController;
    [SerializeField] private CoopGpsStateSync gpsStateSync;
    [SerializeField] private CoopSessionCoordinator coopSessionCoordinator;
    [SerializeField] private CoopMinigameZoneCountdownUIController countdownUiController;
    [SerializeField] private CoopMinigameZoneDefinition[] zoneDefinitions;

    [Header("Trigger Rules")]
    [SerializeField] [Min(0.5f)] private float countdownDurationSeconds = 2.5f;
    [SerializeField] [Min(0.1f)] private float evaluationIntervalSeconds = 0.1f;
    [SerializeField] private bool logZoneTransitionsToConsole = true;

    private readonly CoopMinigameZoneCountdownTracker countdownTracker = new();
    private readonly List<ulong> activePlayerIds = new();
    private float nextEvaluationTime;
    private int previousCandidateZoneId = -1;
    private int lastTriggeredZoneId = -1;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();
        ResetCountdown();
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
            coopSessionCoordinator.StartMiniGame(candidateZone.MiniGameIndex);
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
               coopSessionCoordinator.CurrentPhase == CoopGamePhase.WorldMap;
    }

    private void ResetCountdown()
    {
        countdownTracker.Reset();
        previousCandidateZoneId = -1;
        lastTriggeredZoneId = -1;
        countdownUiController?.Hide();
    }

    private void ResolveReferences()
    {
        gpsMarkerController ??= FindFirstObjectByType<CoopGpsMarkerController>(FindObjectsInactive.Include);
        gpsStateSync ??= FindFirstObjectByType<CoopGpsStateSync>(FindObjectsInactive.Include);
        coopSessionCoordinator ??= FindFirstObjectByType<CoopSessionCoordinator>(FindObjectsInactive.Include);
        countdownUiController ??= FindFirstObjectByType<CoopMinigameZoneCountdownUIController>(FindObjectsInactive.Include);

        if (zoneDefinitions == null || zoneDefinitions.Length == 0)
        {
            zoneDefinitions = GetComponentsInChildren<CoopMinigameZoneDefinition>(true);
        }
    }
}
