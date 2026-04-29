using System;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Video;

namespace SmartCampus.Coop.Minigames
{
    [DisallowMultipleComponent]
    public sealed class TutorialPopupController : MonoBehaviour
    {
        [SerializeField] private Button backgroundDismissButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text subtitleLabel;
        [SerializeField] private TMP_Text bodyLabel;
        [SerializeField] private Image illustrationImage;
        [SerializeField] private RawImage videoSurface;
        [SerializeField] private VideoPlayer videoPlayer;
        [SerializeField] private Transform customContentRoot;

        private GameObject customContentInstance;
        private RenderTexture renderTexture;
        private bool dismissListenersRegistered;

        public event Action Closed;

        private void Awake()
        {
            ResolveReferences();
            RegisterDismissListeners();
        }

        private void OnDisable()
        {
            ClearDynamicContent();
        }

        private void OnDestroy()
        {
            UnregisterDismissListeners();
            ReleaseRenderTexture();
        }

        public void Bind(MinigameTutorialContentConfig content)
        {
            ResolveReferences();

            if (content == null)
            {
                return;
            }

            if (titleLabel != null)
            {
                titleLabel.text = content.Title;
            }

            if (subtitleLabel != null)
            {
                subtitleLabel.text = content.Subtitle;
            }

            if (bodyLabel != null)
            {
                bodyLabel.text = content.BodyText;
            }

            if (illustrationImage != null)
            {
                var hasIllustration = content.Illustration != null;
                illustrationImage.gameObject.SetActive(hasIllustration);
                illustrationImage.sprite = content.Illustration;
                illustrationImage.preserveAspect = true;
            }

            SetupVideo(content.VideoClip);
            SetupCustomContent(content.CustomContentPrefab);
        }

        public void SetDismissButtonsInteractable(bool interactable)
        {
            ResolveReferences();

            if (backgroundDismissButton != null)
            {
                backgroundDismissButton.interactable = interactable;
            }

            if (closeButton != null)
            {
                closeButton.interactable = interactable;
            }
        }

        private void NotifyClosed()
        {
            Closed?.Invoke();
        }

        private void ResolveReferences()
        {
            backgroundDismissButton = ResolveButton(
                backgroundDismissButton,
                "DismissBackground");
            closeButton = ResolveButton(
                closeButton,
                "ContentPanel/ContentScrollView/Viewport/Content/CloseButton");
            titleLabel = ResolveComponent(titleLabel, "ContentPanel/ContentScrollView/Viewport/Content/TitleLabel");
            subtitleLabel = ResolveComponent(subtitleLabel, "ContentPanel/ContentScrollView/Viewport/Content/SubtitleLabel");
            bodyLabel = ResolveComponent(bodyLabel, "ContentPanel/ContentScrollView/Viewport/Content/BodyLabel");
            illustrationImage = ResolveComponent(illustrationImage, "ContentPanel/ContentScrollView/Viewport/Content/Illustration");
            videoSurface = ResolveComponent(videoSurface, "ContentPanel/ContentScrollView/Viewport/Content/VideoSurface");
            customContentRoot = ResolveComponent(customContentRoot, "ContentPanel/ContentScrollView/Viewport/Content/CustomContentRoot");
            videoPlayer ??= GetComponent<VideoPlayer>();
        }

        private void RegisterDismissListeners()
        {
            if (dismissListenersRegistered)
            {
                return;
            }

            if (backgroundDismissButton != null)
            {
                backgroundDismissButton.onClick.AddListener(NotifyClosed);
            }

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(NotifyClosed);
            }

            dismissListenersRegistered = true;
        }

        private void UnregisterDismissListeners()
        {
            if (!dismissListenersRegistered)
            {
                return;
            }

            if (backgroundDismissButton != null)
            {
                backgroundDismissButton.onClick.RemoveListener(NotifyClosed);
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(NotifyClosed);
            }

            dismissListenersRegistered = false;
        }

        private T ResolveComponent<T>(T currentReference, string relativePath) where T : Component
        {
            if (currentReference != null)
            {
                return currentReference;
            }

            var child = transform.Find(relativePath);
            return child == null ? null : child.GetComponent<T>();
        }

        private Button ResolveButton(Button currentReference, string relativePath)
        {
            if (currentReference != null)
            {
                return currentReference;
            }

            var child = transform.Find(relativePath);
            if (child == null)
            {
                return null;
            }

            var button = child.GetComponent<Button>();
            if (button != null)
            {
                return button;
            }

            button = child.gameObject.AddComponent<Button>();
            button.targetGraphic = child.GetComponent<Graphic>();
            return button;
        }

        private void SetupVideo(VideoClip videoClip)
        {
            if (videoPlayer == null || videoSurface == null)
            {
                return;
            }

            if (videoClip == null)
            {
                videoPlayer.Stop();
                videoSurface.texture = null;
                videoSurface.gameObject.SetActive(false);
                ReleaseRenderTexture();
                return;
            }

            ReleaseRenderTexture();

            renderTexture = new RenderTexture(1280, 720, 0);
            renderTexture.Create();

            videoPlayer.audioOutputMode = VideoAudioOutputMode.None;
            videoPlayer.isLooping = true;
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            videoPlayer.targetTexture = renderTexture;
            videoPlayer.clip = videoClip;

            videoSurface.gameObject.SetActive(true);
            videoSurface.texture = renderTexture;
            videoPlayer.Play();
        }

        private void SetupCustomContent(GameObject customContentPrefab)
        {
            if (customContentRoot == null)
            {
                return;
            }

            if (customContentInstance != null)
            {
                Destroy(customContentInstance);
                customContentInstance = null;
            }

            if (customContentPrefab != null)
            {
                customContentInstance = Instantiate(customContentPrefab, customContentRoot, false);
            }
        }

        private void ClearDynamicContent()
        {
            if (videoPlayer != null)
            {
                videoPlayer.Stop();
                videoPlayer.targetTexture = null;
            }

            if (videoSurface != null)
            {
                videoSurface.texture = null;
            }

            ReleaseRenderTexture();

            if (customContentInstance != null)
            {
                Destroy(customContentInstance);
                customContentInstance = null;
            }
        }

        private void ReleaseRenderTexture()
        {
            if (renderTexture == null)
            {
                return;
            }

            renderTexture.Release();
            Destroy(renderTexture);
            renderTexture = null;
        }
    }
}
