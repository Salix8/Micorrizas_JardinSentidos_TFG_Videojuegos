using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace SmartCampus.Coop.Minigames.GardenImageVoting
{
    public static class GardenImageVotingExternalContentService
    {
        public static IEnumerator LoadTextAsync(string configuredPath, Action<string, string> onCompleted)
        {
            if (string.IsNullOrWhiteSpace(configuredPath))
            {
                onCompleted?.Invoke(null, "No se ha configurado la ruta del CSV.");
                yield break;
            }

            var resolvedUri = ResolveUri(configuredPath);
            if (resolvedUri == null)
            {
                onCompleted?.Invoke(null, $"No se pudo resolver la ruta '{configuredPath}'.");
                yield break;
            }

            if (resolvedUri.IsFile && File.Exists(resolvedUri.LocalPath))
            {
                string fileContent;
                try
                {
                    fileContent = File.ReadAllText(resolvedUri.LocalPath);
                }
                catch (Exception exception)
                {
                    onCompleted?.Invoke(null, exception.Message);
                    yield break;
                }

                onCompleted?.Invoke(fileContent, string.Empty);
                yield break;
            }

            var request = UnityWebRequest.Get(resolvedUri);
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                onCompleted?.Invoke(null, request.error);
                request.Dispose();
                yield break;
            }

            onCompleted?.Invoke(request.downloadHandler.text, string.Empty);
            request.Dispose();
        }

        public static IEnumerator LoadSpriteAsync(string configuredPath, Action<Sprite, string> onCompleted)
        {
            if (string.IsNullOrWhiteSpace(configuredPath))
            {
                onCompleted?.Invoke(null, string.Empty);
                yield break;
            }

            var resolvedUri = ResolveUri(configuredPath);
            if (resolvedUri == null)
            {
                onCompleted?.Invoke(null, $"No se pudo resolver la ruta '{configuredPath}'.");
                yield break;
            }

            var request = UnityWebRequestTexture.GetTexture(resolvedUri);
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                onCompleted?.Invoke(null, request.error);
                request.Dispose();
                yield break;
            }

            var texture = DownloadHandlerTexture.GetContent(request);
            if (texture == null)
            {
                onCompleted?.Invoke(null, "La textura cargada es nula.");
                yield break;
            }

            var sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f);

            onCompleted?.Invoke(sprite, string.Empty);
            request.Dispose();
        }

        private static Uri ResolveUri(string configuredPath)
        {
            if (Uri.TryCreate(configuredPath, UriKind.Absolute, out var absoluteUri))
            {
                return absoluteUri;
            }

            var combinedPath = Path.IsPathRooted(configuredPath)
                ? configuredPath
                : Path.Combine(Application.streamingAssetsPath, configuredPath);

            return Uri.TryCreate(combinedPath, UriKind.Absolute, out absoluteUri) ? absoluteUri : null;
        }
    }
}
