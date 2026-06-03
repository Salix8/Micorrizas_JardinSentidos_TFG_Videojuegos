using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace SmartCampus.Coop.Minigames.PlantPhotoRelay
{
    public interface IPlantPhotoCaptureService
    {
        bool IsCaptureSupported { get; }
        IEnumerator CapturePhotoAsync(PlantPhotoRelayPhotoCaptureRequest request, Action<PlantPhotoRelayPhotoCaptureResult> onCompleted);
    }

    public readonly struct PlantPhotoRelayPhotoCaptureRequest
    {
        public PlantPhotoRelayPhotoCaptureRequest(int maxDimension, int jpegQuality)
        {
            MaxDimension = maxDimension;
            JpegQuality = jpegQuality;
        }

        public int MaxDimension { get; }
        public int JpegQuality { get; }
    }

    public readonly struct PlantPhotoRelayPhotoCaptureResult
    {
        public PlantPhotoRelayPhotoCaptureResult(bool success, byte[] imageBytes, int width, int height, string errorMessage)
        {
            Success = success;
            ImageBytes = imageBytes;
            Width = width;
            Height = height;
            ErrorMessage = errorMessage ?? string.Empty;
        }

        public bool Success { get; }
        public byte[] ImageBytes { get; }
        public int Width { get; }
        public int Height { get; }
        public string ErrorMessage { get; }
    }

    public static class PlantPhotoRelayPhotoCaptureServiceFactory
    {
        public static IPlantPhotoCaptureService CreateDefault()
        {
            return Application.isEditor
                ? (IPlantPhotoCaptureService)new PlantPhotoRelayEditorPhotoCaptureService()
                : new PlantPhotoRelayNativeCameraCaptureService();
        }
    }

    public sealed class PlantPhotoRelayEditorPhotoCaptureService : IPlantPhotoCaptureService
    {
        public bool IsCaptureSupported => true;

        public IEnumerator CapturePhotoAsync(PlantPhotoRelayPhotoCaptureRequest request, Action<PlantPhotoRelayPhotoCaptureResult> onCompleted)
        {
            var size = Mathf.Clamp(request.MaxDimension, 128, 1024);
            var texture = new Texture2D(size, size, TextureFormat.RGB24, false);
            var pixels = texture.GetPixels32();
            for (var index = 0; index < pixels.Length; index++)
            {
                var x = index % size;
                var y = index / size;
                var isBand = ((x / 32) + (y / 32)) % 2 == 0;
                pixels[index] = isBand ? new Color32(84, 122, 73, 255) : new Color32(172, 194, 141, 255);
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            var bytes = texture.EncodeToJPG(request.JpegQuality);
            UnityEngine.Object.Destroy(texture);
            onCompleted?.Invoke(new PlantPhotoRelayPhotoCaptureResult(true, bytes, size, size, string.Empty));
            yield break;
        }
    }

    public sealed class PlantPhotoRelayNativeCameraCaptureService : IPlantPhotoCaptureService
    {
        public bool IsCaptureSupported => ResolveNativeCameraType() != null;

        public IEnumerator CapturePhotoAsync(PlantPhotoRelayPhotoCaptureRequest request, Action<PlantPhotoRelayPhotoCaptureResult> onCompleted)
        {
            var nativeCameraType = ResolveNativeCameraType();
            if (nativeCameraType == null)
            {
                onCompleted?.Invoke(new PlantPhotoRelayPhotoCaptureResult(false, null, 0, 0, "No se ha encontrado una integracion NativeCamera en el proyecto."));
                yield break;
            }

            var callbackInvoked = false;
            string capturedPath = null;
            var permissionCallbackType = nativeCameraType.GetNestedType("CameraCallback", BindingFlags.Public | BindingFlags.NonPublic);
            var takePictureMethod = nativeCameraType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(method => method.Name == "TakePicture" && method.GetParameters().Length >= 2);
            if (permissionCallbackType == null || takePictureMethod == null)
            {
                onCompleted?.Invoke(new PlantPhotoRelayPhotoCaptureResult(false, null, 0, 0, "La API NativeCamera detectada no tiene la firma esperada."));
                yield break;
            }

            Action<string> callback = path =>
            {
                capturedPath = path;
                callbackInvoked = true;
            };

            var callbackDelegate = Delegate.CreateDelegate(permissionCallbackType, callback.Target, callback.Method);
            var arguments = new object[takePictureMethod.GetParameters().Length];
            arguments[0] = callbackDelegate;
            arguments[1] = request.MaxDimension;
            for (var index = 2; index < arguments.Length; index++)
            {
                arguments[index] = Type.Missing;
            }

            takePictureMethod.Invoke(null, arguments);

            while (!callbackInvoked)
            {
                yield return null;
            }

            if (string.IsNullOrWhiteSpace(capturedPath) || !System.IO.File.Exists(capturedPath))
            {
                onCompleted?.Invoke(new PlantPhotoRelayPhotoCaptureResult(false, null, 0, 0, "La captura de camara fue cancelada o no devolvio un archivo valido."));
                yield break;
            }

            var fileBytes = System.IO.File.ReadAllBytes(capturedPath);
            var texture = new Texture2D(2, 2, TextureFormat.RGB24, false);
            if (!texture.LoadImage(fileBytes, true))
            {
                UnityEngine.Object.Destroy(texture);
                onCompleted?.Invoke(new PlantPhotoRelayPhotoCaptureResult(false, null, 0, 0, "No se pudo decodificar la imagen capturada."));
                yield break;
            }

            var scaledTexture = Downscale(texture, request.MaxDimension);
            UnityEngine.Object.Destroy(texture);
            var jpgBytes = scaledTexture.EncodeToJPG(request.JpegQuality);
            var result = new PlantPhotoRelayPhotoCaptureResult(true, jpgBytes, scaledTexture.width, scaledTexture.height, string.Empty);
            UnityEngine.Object.Destroy(scaledTexture);
            onCompleted?.Invoke(result);
        }

        private static Type ResolveNativeCameraType()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (var index = 0; index < assemblies.Length; index++)
            {
                var candidate = assemblies[index].GetType("NativeCamera");
                if (candidate != null)
                {
                    return candidate;
                }
            }

            return null;
        }

        private static Texture2D Downscale(Texture2D source, int maxDimension)
        {
            var maxSourceDimension = Mathf.Max(source.width, source.height);
            if (maxSourceDimension <= maxDimension)
            {
                return source;
            }

            var scale = maxDimension / (float)maxSourceDimension;
            var width = Mathf.Max(1, Mathf.RoundToInt(source.width * scale));
            var height = Mathf.Max(1, Mathf.RoundToInt(source.height * scale));
            var renderTexture = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
            Graphics.Blit(source, renderTexture);
            var previous = RenderTexture.active;
            RenderTexture.active = renderTexture;
            var result = new Texture2D(width, height, TextureFormat.RGB24, false);
            result.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
            result.Apply(false, true);
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(renderTexture);
            return result;
        }
    }
}
