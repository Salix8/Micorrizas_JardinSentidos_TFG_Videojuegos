using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SmartCampus.Testing.Editor
{
    public static class SmartCampusProjectQaUtility
    {
        public const string ToolName = "Smart Campus QA Panel";
        public const string LobbyScenePath = "Assets/Lobby.unity";
        public const string LobbySceneName = "Lobby";
        public const string MainMapScenePath = "Assets/UJI.unity";
        public const string MainMapSceneName = "UJI";

        public static bool SceneExists(string scenePath)
        {
            return AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) != null;
        }

        public static bool IsSceneEnabled(string scenePath)
        {
            return EditorBuildSettings.scenes.Any(scene => PathsEqual(scene.path, scenePath) && scene.enabled);
        }

        public static TResult InspectScene<TResult>(string scenePath, Func<Scene, TResult> inspector)
        {
            var sceneSetup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                return inspector(scene);
            }
            finally
            {
                EditorSceneManager.RestoreSceneManagerSetup(sceneSetup);
            }
        }

        public static List<T> FindComponents<T>(Scene scene) where T : Component
        {
            var components = new List<T>();
            foreach (var root in scene.GetRootGameObjects())
            {
                components.AddRange(root.GetComponentsInChildren<T>(true));
            }

            return components;
        }

        public static bool HasAssignedReference(UnityEngine.Object target, string propertyName)
        {
            var property = new SerializedObject(target).FindProperty(propertyName);
            return property != null && property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue != null;
        }

        public static bool ReadBool(UnityEngine.Object target, string propertyName)
        {
            var property = new SerializedObject(target).FindProperty(propertyName);
            return property != null && property.boolValue;
        }

        public static int ReadInt(UnityEngine.Object target, string propertyName)
        {
            var property = new SerializedObject(target).FindProperty(propertyName);
            return property?.intValue ?? 0;
        }

        public static string ReadString(UnityEngine.Object target, string propertyName)
        {
            var property = new SerializedObject(target).FindProperty(propertyName);
            return property?.stringValue ?? string.Empty;
        }

        private static bool PathsEqual(string left, string right)
        {
            return string.Equals(NormalizePath(left), NormalizePath(right), StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizePath(string value)
        {
            return (value ?? string.Empty).Replace('\\', '/');
        }
    }
}
