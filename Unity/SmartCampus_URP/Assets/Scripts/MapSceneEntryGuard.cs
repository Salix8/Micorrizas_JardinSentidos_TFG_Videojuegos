using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class MapSceneEntryGuard : MonoBehaviour
{
    [Header("Scene Routing")]
    [SerializeField] private string worldMapSceneName = "UJI";
    [SerializeField] private string lobbySceneName = "Lobby";
    [SerializeField] private bool allowDirectScenePlayInEditor = true;

    private void Start()
    {
        if (!Application.isPlaying)
        {
            return;
        }

#if UNITY_EDITOR
        if (allowDirectScenePlayInEditor)
        {
            return;
        }
#endif

        var activeScene = SceneManager.GetActiveScene();
        if (!activeScene.IsValid() ||
            !string.Equals(activeScene.name, worldMapSceneName, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var sessionCoordinator = FindFirstObjectByType<CoopSessionCoordinator>(FindObjectsInactive.Include);
        if (sessionCoordinator != null &&
            sessionCoordinator.IsSpawned &&
            sessionCoordinator.CurrentPhase == CoopGamePhase.WorldMap)
        {
            return;
        }

        var networkManager = NetworkManager.Singleton ?? FindFirstObjectByType<NetworkManager>(FindObjectsInactive.Include);
        if (networkManager != null && networkManager.IsListening)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(lobbySceneName))
        {
            Debug.LogWarning($"{nameof(MapSceneEntryGuard)} cannot redirect because the lobby scene name is empty.", this);
            return;
        }

        Debug.Log($"Redirecting direct map startup to '{lobbySceneName}'.", this);
        SceneManager.LoadScene(lobbySceneName, LoadSceneMode.Single);
    }
}
