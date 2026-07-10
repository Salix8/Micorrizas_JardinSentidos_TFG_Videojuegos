using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace SmartCampus.Coop.Minigames
{
    public static class CoopMinigameExternalContentService
    {
        public static IEnumerator LoadTextAsync(string configuredPath, Action<string, string> onCompleted)
        {
            if (string.IsNullOrWhiteSpace(configuredPath))
            {
                onCompleted?.Invoke(null, "No se ha configurado la ruta del contenido.");
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
            return LoadSpriteAsync(configuredPath, null, onCompleted);
        }

        public static IEnumerator LoadSpriteAsync(string configuredPath, string relativeToConfiguredPath, Action<Sprite, string> onCompleted)
        {
            if (string.IsNullOrWhiteSpace(configuredPath))
            {
                onCompleted?.Invoke(null, string.Empty);
                yield break;
            }

            var resolvedUri = ResolveUri(configuredPath, relativeToConfiguredPath);
            if (resolvedUri == null)
            {
                onCompleted?.Invoke(null, $"No se pudo resolver la ruta '{configuredPath}'.");
                yield break;
            }

            if (resolvedUri.IsFile && File.Exists(resolvedUri.LocalPath))
            {
                byte[] fileBytes;
                try
                {
                    fileBytes = File.ReadAllBytes(resolvedUri.LocalPath);
                }
                catch (Exception exception)
                {
                    onCompleted?.Invoke(null, exception.Message);
                    yield break;
                }

                var fileTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!fileTexture.LoadImage(fileBytes))
                {
                    UnityEngine.Object.Destroy(fileTexture);
                    onCompleted?.Invoke(null, $"No se pudo decodificar la imagen '{configuredPath}'.");
                    yield break;
                }

                var fileSprite = Sprite.Create(
                    fileTexture,
                    new Rect(0f, 0f, fileTexture.width, fileTexture.height),
                    new Vector2(0.5f, 0.5f),
                    100f);

                onCompleted?.Invoke(fileSprite, string.Empty);
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
                request.Dispose();
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

        public static string ResolveConfiguredPath(string configuredPath, string relativeToConfiguredPath = null)
        {
            return ResolveConfiguredPath(configuredPath, relativeToConfiguredPath, null);
        }

        public static string ResolveConfiguredPath(string configuredPath, string relativeToConfiguredPath, string streamingAssetsPathOverride)
        {
            var resolvedUri = ResolveUri(configuredPath, relativeToConfiguredPath, streamingAssetsPathOverride);
            if (resolvedUri == null)
            {
                return configuredPath;
            }

            return resolvedUri.IsFile ? resolvedUri.LocalPath : resolvedUri.AbsoluteUri;
        }

        private static Uri ResolveUri(string configuredPath, string relativeToConfiguredPath = null, string streamingAssetsPathOverride = null)
        {
            if (Uri.TryCreate(configuredPath, UriKind.Absolute, out var absoluteUri))
            {
                return absoluteUri;
            }

            configuredPath = NormalizePath(configuredPath);

            if (!string.IsNullOrWhiteSpace(relativeToConfiguredPath))
            {
                var baseUri = ResolveUri(relativeToConfiguredPath, null, streamingAssetsPathOverride);
                if (baseUri != null)
                {
                    if (baseUri.IsFile)
                    {
                        var baseDirectory = Path.GetDirectoryName(baseUri.LocalPath);
                        if (!string.IsNullOrWhiteSpace(baseDirectory))
                        {
                            var combinedBasePath = Path.Combine(baseDirectory, configuredPath);
                            if (Uri.TryCreate(combinedBasePath, UriKind.Absolute, out var resolvedFromBaseDirectory))
                            {
                                return resolvedFromBaseDirectory;
                            }
                        }
                    }
                    else if (Uri.TryCreate(baseUri, configuredPath, out var resolvedRelativeUri))
                    {
                        return resolvedRelativeUri;
                    }
                }
            }

            var combinedPath = Path.IsPathRooted(configuredPath)
                ? configuredPath
                : CombineWithStreamingAssetsPath(configuredPath, streamingAssetsPathOverride);

            return Uri.TryCreate(combinedPath, UriKind.Absolute, out absoluteUri) ? absoluteUri : null;
        }

        private static string CombineWithStreamingAssetsPath(string configuredPath, string streamingAssetsPathOverride)
        {
            var normalizedConfiguredPath = NormalizePath(configuredPath).Replace(Path.DirectorySeparatorChar, '/').TrimStart('/');
            var streamingAssetsPath = string.IsNullOrWhiteSpace(streamingAssetsPathOverride)
                ? Application.streamingAssetsPath
                : streamingAssetsPathOverride;

            if (string.IsNullOrWhiteSpace(streamingAssetsPath))
            {
                return normalizedConfiguredPath;
            }

            var normalizedStreamingAssetsPath = streamingAssetsPath.Replace('\\', '/');
            if (Uri.TryCreate(normalizedStreamingAssetsPath, UriKind.Absolute, out var streamingAssetsUri))
            {
                if (streamingAssetsUri.IsFile)
                {
                    return Path.Combine(streamingAssetsUri.LocalPath, NormalizePath(configuredPath));
                }

                var baseUri = normalizedStreamingAssetsPath.EndsWith("/", StringComparison.Ordinal)
                    ? new Uri(normalizedStreamingAssetsPath, UriKind.Absolute)
                    : new Uri(normalizedStreamingAssetsPath + "/", UriKind.Absolute);
                return new Uri(baseUri, normalizedConfiguredPath).AbsoluteUri;
            }

            return Path.Combine(streamingAssetsPath, NormalizePath(configuredPath));
        }

        private static string NormalizePath(string configuredPath)
        {
            return configuredPath
                .Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar)
                .Replace('\\', Path.DirectorySeparatorChar);
        }
    }
}
