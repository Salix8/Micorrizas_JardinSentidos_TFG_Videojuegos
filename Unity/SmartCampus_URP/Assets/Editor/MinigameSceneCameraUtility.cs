using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MinigameSceneCameraUtility
{
    public static Camera EnsureFixedCamera(Scene scene, Color backgroundColor)
    {
        var camera = Object.FindFirstObjectByType<Camera>(FindObjectsInactive.Include);

        if (camera == null)
        {
            var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            camera = cameraObject.GetComponent<Camera>();
        }

        var cameraTransform = camera.transform;
        camera.gameObject.name = "Main Camera";
        camera.gameObject.tag = "MainCamera";
        cameraTransform.position = new Vector3(0f, 0f, -10f);
        cameraTransform.rotation = Quaternion.identity;

        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = backgroundColor;
        camera.orthographic = true;
        camera.orthographicSize = 10f;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 100f;
        camera.depth = -1f;

        if (!camera.TryGetComponent<AudioListener>(out _))
        {
            camera.gameObject.AddComponent<AudioListener>();
        }

        EditorSceneManager.MarkSceneDirty(scene);
        return camera;
    }
}
