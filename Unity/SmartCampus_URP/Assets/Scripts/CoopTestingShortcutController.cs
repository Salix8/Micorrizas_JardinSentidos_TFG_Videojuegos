using SmartCampus.Coop.Minigames;
using Unity.Netcode;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DisallowMultipleComponent]
public sealed class CoopTestingShortcutController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CoopSessionCoordinator coopSessionCoordinator;

    [Header("Availability")]
    [SerializeField] private bool enableInEditor = true;
    [SerializeField] private bool enableInDevelopmentBuild = true;

    [Header("Forced Win")]
    [SerializeField] [Range(0f, 10f)] private float forcedWinScoreOutOfTen = CoopTestingShortcutService.DefaultForcedScore;
    [SerializeField] private string forcedWinMessage = CoopTestingShortcutService.DefaultForcedWinMessage;

    [Header("Debug")]
    [SerializeField] private bool logShortcutUsage = true;

    private void Awake()
    {
        ResolveReferences();
    }

    private void Update()
    {
        if (!CoopTestingShortcutService.AreShortcutsEnabled(
                enableInEditor,
                enableInDevelopmentBuild,
                Application.isEditor,
                Debug.isDebugBuild))
        {
            return;
        }

        ResolveReferences();

        if (WasRestartShortcutPressed())
        {
            TryRestartSession();
            return;
        }

        if (WasForcedWinShortcutPressed())
        {
            TryForceWinCurrentMinigame();
        }
    }

    private void TryRestartSession()
    {
        if (coopSessionCoordinator == null)
        {
            return;
        }

        if (!CanUseHostTestingShortcut())
        {
            Debug.LogWarning("Ctrl+R solo esta disponible para el host durante una sesion cooperativa activa.", this);
            return;
        }

        coopSessionCoordinator.RestartSessionToMainMap();
        LogShortcut("Ctrl+R -> reinicio cooperativo hacia UJI.");
    }

    private void TryForceWinCurrentMinigame()
    {
        if (!CanUseHostTestingShortcut() || coopSessionCoordinator == null || coopSessionCoordinator.CurrentPhase != CoopGamePhase.MiniGame)
        {
            return;
        }

        var minigameSession = FindFirstObjectByType<CooperativeMinigameBase>(FindObjectsInactive.Include);
        if (minigameSession == null)
        {
            return;
        }

        var forcedResult = CoopTestingShortcutService.CreateForcedWinResult(forcedWinScoreOutOfTen, forcedWinMessage);
        if (!minigameSession.TryForceCompleteForTesting(forcedResult))
        {
            return;
        }

        LogShortcut($"Ctrl+W -> victoria forzada del minijuego con nota {forcedResult.ScoreOutOfTen:0.0}/10.");
    }

    private bool CanUseHostTestingShortcut()
    {
        return coopSessionCoordinator != null &&
               coopSessionCoordinator.IsSpawned &&
               coopSessionCoordinator.IsServer &&
               NetworkManager.Singleton != null &&
               NetworkManager.Singleton.IsListening;
    }

    private void ResolveReferences()
    {
        coopSessionCoordinator ??= FindFirstObjectByType<CoopSessionCoordinator>(FindObjectsInactive.Include);
    }

    private void LogShortcut(string message)
    {
        if (logShortcutUsage)
        {
            Debug.Log(message, this);
        }
    }

    private static bool WasForcedWinShortcutPressed()
    {
        return IsCtrlPressed() && WasKeyPressedThisFrame(KeyCode.W);
    }

    private static bool WasRestartShortcutPressed()
    {
        return IsCtrlPressed() && WasKeyPressedThisFrame(KeyCode.R);
    }

    private static bool IsCtrlPressed()
    {
#if ENABLE_INPUT_SYSTEM
        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            return keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed;
        }
#endif
        return Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
    }

    private static bool WasKeyPressedThisFrame(KeyCode keyCode)
    {
#if ENABLE_INPUT_SYSTEM
        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            return keyCode switch
            {
                KeyCode.W => keyboard.wKey.wasPressedThisFrame,
                KeyCode.R => keyboard.rKey.wasPressedThisFrame,
                _ => false
            };
        }
#endif
        return Input.GetKeyDown(keyCode);
    }
}
