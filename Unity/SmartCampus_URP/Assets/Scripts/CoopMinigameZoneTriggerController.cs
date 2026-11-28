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
    private const string IncompleteZoneSpriteAssetPath = "Assets/Art/icono-sobremapa-Marron.png";
    private const string CompletedZoneSpriteAssetPath = "Assets/Art/icono-sobremapa.png";
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

    [Header("Debug")]
    [Tooltip("Muestra en pantalla por que una zona no lanza su mision. Desactivar para la version final.")]
    [SerializeField] private bool showDebugStatusOnScreen = true;

    [Header("Zone Visuals")]
    [SerializeField] private Sprite incompleteZoneSprite;
    [SerializeField] private Sprite completedZoneSprite;
    [SerializeField] private Vector3 zoneVisualLocalOffset = new(0f, 8f, 0f);
    [SerializeField] private Vector3 zoneVisualLocalEulerAngles = new(90f, 0f, 0f);
    [SerializeField] private int zoneVisualSortingOrder = 25;

    private readonly CoopMinigameZoneCountdownTracker countdownTracker = new();
    private readonly List<ulong> activePlayerIds = new();
    private readonly Dictionary<int, SpriteRenderer> zoneVisualRenderers = new();
    private float nextEvaluationTime;
    private int previousCandidateZoneId = -1;
    private int lastTriggeredZoneId = -1;
    private string previousBlocker;
    private string debugBlocker;
    private string debugState = string.Empty;
    private CoopZoneDebugOverlay debugOverlay;
    private readonly System.Text.StringBuilder debugBuilder = new();

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
        RefreshDebugOverlay();
    }

    private void EvaluateZones()
    {
        RefreshZoneVisualStates();

        if (!IsWorldMapPhaseReady())
        {
            if (coopSessionCoordinator != null && coopSessionCoordinator.IsServer &&
                coopSessionCoordinator.CurrentPhase == CoopGamePhase.WorldMap)
            {
                LogBlocker(dialogueFlowSync != null && dialogueFlowSync.IsFlowBusy
                    ? "El flujo de dialogo esta ocupado."
                    : "Faltan referencias de posicion o sesion.");
            }

            debugState = coopSessionCoordinator == null
                ? "Sin coordinador de sesion."
                : coopSessionCoordinator.CurrentPhase != CoopGamePhase.WorldMap
                    ? $"Fase {coopSessionCoordinator.CurrentPhase} (no es el mapa)."
                    : dialogueFlowSync != null && dialogueFlowSync.IsFlowBusy
                        ? "Dialogo en curso: todos deben terminarlo."
                        : "Faltan referencias de GPS o marcadores.";
            ResetCountdown();
            return;
        }

        var candidateZone = FindCandidateZone();
        var retryRemaining = candidateZone == null || coopSessionProgressSync == null
            ? 0d
            : coopSessionProgressSync.GetRetryRemainingSeconds(candidateZone.MiniGameIndex);
        if (candidateZone != null && retryRemaining > 0d)
        {
            countdownTracker.Reset();
            previousCandidateZoneId = -1;
            lastTriggeredZoneId = -1;
            debugState = $"Espera de reintento en {candidateZone.DisplayName}: {Mathf.CeilToInt((float)retryRemaining)} s.";
            countdownUiController?.ShowRetryWait(candidateZone.DisplayName, (float)retryRemaining, coopSessionProgressSync.RetryDelaySeconds);
            return;
        }

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
            debugState = "Sin zona lista: " + (debugBlocker ?? "motivo desconocido.");
            countdownUiController?.Hide();
            return;
        }

        previousBlocker = null;
        debugBlocker = null;

        if (snapshot.ZoneId != previousCandidateZoneId && logZoneTransitionsToConsole)
        {
            Debug.Log($"Minigame zone candidate detected: {candidateZone.DisplayName}.", this);
        }

        previousCandidateZoneId = snapshot.ZoneId;
        countdownUiController?.Show(candidateZone.DisplayName, snapshot.Progress01, snapshot.RemainingSeconds);
        debugState = $"Cuenta atras en {candidateZone.DisplayName}: {snapshot.RemainingSeconds:0.0} s.";

        if (!snapshot.IsCompleted || lastTriggeredZoneId == snapshot.ZoneId)
        {
            if (snapshot.IsCompleted)
            {
                debugState = $"Mision de {candidateZone.DisplayName} solicitada; esperando dialogo o carga.";
            }

            return;
        }

        if (coopSessionCoordinator == null || !coopSessionCoordinator.IsServer)
        {
            debugState = $"Cuenta atras completa en {candidateZone.DisplayName}; esperando a que el host lance la mision.";
        }

        if (coopSessionCoordinator != null && coopSessionCoordinator.IsServer)
        {
            if (!coopSessionCoordinator.CanLaunchMiniGame(candidateZone.MiniGameIndex))
            {
                debugState = $"No se puede lanzar {candidateZone.DisplayName} (completada, en espera o sin escena).";
                Debug.LogWarning(
                    $"Zone '{candidateZone.DisplayName}' cannot launch minigame. MiniGameNumber={candidateZone.MiniGameNumber} MiniGameIndex={candidateZone.MiniGameIndex} is unavailable in the current cooperative session.",
                    this);
                ResetCountdown();
                return;
            }

            coopSessionCoordinator.TryGetMiniGameSceneName(candidateZone.MiniGameIndex, out var sceneName);
            Debug.Log(
                $"Launching zone minigame '{candidateZone.DisplayName}'. MiniGameNumber={candidateZone.MiniGameNumber} MiniGameIndex={candidateZone.MiniGameIndex} Scene='{sceneName}'.",
                this);
            if (dialogueFlowSync != null)
            {
                if (!dialogueFlowSync.RequestStartMinigame(candidateZone.MiniGameIndex))
                {
                    Debug.LogWarning("Minigame start request was rejected; the countdown will restart.", this);
                    debugState = "Inicio rechazado por el flujo de dialogo; se repite la cuenta atras.";
                    ResetCountdown();
                    return;
                }
            }
            else
            {
                coopSessionCoordinator.StartMiniGame(candidateZone.MiniGameIndex);
            }

            lastTriggeredZoneId = snapshot.ZoneId;
            debugState = $"Lanzando la mision de {candidateZone.DisplayName}.";
        }
    }

    private CoopMinigameZoneDefinition FindCandidateZone()
    {
        if (zoneDefinitions == null || zoneDefinitions.Length == 0 || gpsStateSync == null || coopSessionCoordinator == null)
        {
            LogBlocker("Faltan zonas o referencias de GPS y sesion.");
            return null;
        }

        BuildActivePlayerIds();
        if (activePlayerIds.Count == 0)
        {
            LogBlocker("No hay jugadores registrados en la sesion.");
            return null;
        }

        foreach (var clientId in activePlayerIds)
        {
            if (!gpsStateSync.TryGetState(clientId, out var state) || !state.HasFix)
            {
                LogBlocker($"Jugador {clientId}: sin posicion GPS valida.");
                return null;
            }
        }

        string blocker = null;
        for (var index = 0; index < zoneDefinitions.Length; index++)
        {
            var zone = zoneDefinitions[index];
            if (zone == null)
            {
                continue;
            }

            if (!coopSessionCoordinator.IsMiniGameConfigured(zone.MiniGameIndex) ||
                (coopSessionProgressSync != null && coopSessionProgressSync.IsMinigameCompleted(zone.MiniGameIndex)))
            {
                continue;
            }

            var allPlayersInside = true;
            for (var playerIndex = 0; playerIndex < activePlayerIds.Count; playerIndex++)
            {
                var clientId = activePlayerIds[playerIndex];
                if (!gpsStateSync.TryGetState(clientId, out var state) || !state.HasFix)
                {
                    blocker ??= $"Jugador {clientId}: sin posicion GPS valida.";
                    allPlayersInside = false;
                    break;
                }

                if (state.HorizontalAccuracy > zone.MaxAcceptedAccuracyMeters)
                {
                    blocker ??= $"Jugador {clientId}: precision GPS insuficiente para {zone.DisplayName}.";
                    allPlayersInside = false;
                    break;
                }

                if (!gpsMarkerController.TryGetMarkerWorldPosition(clientId, out var markerWorldPosition))
                {
                    blocker ??= $"Jugador {clientId}: posicion del marcador no disponible.";
                    allPlayersInside = false;
                    break;
                }

                if (!zone.Contains(markerWorldPosition))
                {
                    blocker ??= "Ninguna zona pendiente contiene a todos los jugadores.";
                    allPlayersInside = false;
                    break;
                }
            }

            if (allPlayersInside)
            {
                return zone;
            }
        }

        LogBlocker(blocker ?? "No hay misiones configuradas y pendientes en las zonas.");
        return null;
    }

    private void RefreshDebugOverlay()
    {
        if (!showDebugStatusOnScreen)
        {
            debugOverlay?.SetVisible(false);
            return;
        }

        debugOverlay ??= CoopZoneDebugOverlay.Create(transform);
        debugOverlay.SetVisible(true);

        debugBuilder.Clear();
        var isServer = coopSessionCoordinator != null && coopSessionCoordinator.IsServer;
        debugBuilder.Append("<b>Zonas</b> (").Append(isServer ? "host" : "cliente").Append(")\n");
        debugBuilder.Append(debugState).Append('\n');

        BuildActivePlayerIds();
        if (activePlayerIds.Count == 0)
        {
            debugBuilder.Append("Sin jugadores registrados.");
            debugOverlay.SetText(debugBuilder.ToString());
            return;
        }

        for (var index = 0; index < activePlayerIds.Count; index++)
        {
            var clientId = activePlayerIds[index];
            debugBuilder.Append("J").Append(index + 1).Append(" (id ").Append(clientId).Append("): ");

            if (gpsStateSync == null || !gpsStateSync.TryGetState(clientId, out var state))
            {
                debugBuilder.Append("sin datos GPS\n");
                continue;
            }

            if (!state.HasFix)
            {
                debugBuilder.Append("GPS sin señal\n");
                continue;
            }

            debugBuilder.Append("precision ").Append(state.HorizontalAccuracy.ToString("0")).Append(" m, ");
            if (gpsMarkerController == null || !gpsMarkerController.TryGetMarkerWorldPosition(clientId, out var markerWorldPosition))
            {
                debugBuilder.Append("marcador no disponible\n");
                continue;
            }

            var insideZone = FindZoneContaining(markerWorldPosition);
            if (insideZone == null)
            {
                debugBuilder.Append("fuera de zonas\n");
                continue;
            }

            debugBuilder.Append("dentro de ").Append(insideZone.DisplayName);
            if (state.HorizontalAccuracy > insideZone.MaxAcceptedAccuracyMeters)
            {
                debugBuilder.Append(" <color=#FFB347>(precision > ").Append(insideZone.MaxAcceptedAccuracyMeters.ToString("0")).Append(" m)</color>");
            }

            if (coopSessionProgressSync != null && coopSessionProgressSync.IsMinigameCompleted(insideZone.MiniGameIndex))
            {
                debugBuilder.Append(" [completada]");
            }

            debugBuilder.Append('\n');
        }

        debugOverlay.SetText(debugBuilder.ToString());
    }

    private CoopMinigameZoneDefinition FindZoneContaining(Vector3 worldPosition)
    {
        if (zoneDefinitions == null)
        {
            return null;
        }

        for (var index = 0; index < zoneDefinitions.Length; index++)
        {
            var zone = zoneDefinitions[index];
            if (zone != null && zone.Contains(worldPosition))
            {
                return zone;
            }
        }

        return null;
    }

    private void LogBlocker(string blocker)
    {
        debugBlocker = blocker;

        if (!logZoneTransitionsToConsole || coopSessionCoordinator == null ||
            !coopSessionCoordinator.IsServer || blocker == previousBlocker)
        {
            return;
        }

        previousBlocker = blocker;
        Debug.Log($"Minigame zone waiting: {blocker}", this);
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
        visualTransform.localScale = Vector3.one;
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

        if (coopSessionProgressSync != null &&
            coopSessionProgressSync.TryGetProgressState(zone.MiniGameIndex, out var progressState) &&
            progressState.IsCompleted)
        {
            return completedZoneSprite;
        }

        return incompleteZoneSprite;
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
