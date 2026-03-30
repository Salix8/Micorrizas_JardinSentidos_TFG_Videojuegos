using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace SmartCampus.Coop.Minigames
{
    [DisallowMultipleComponent]
    public sealed class TutorialPopupController : MonoBehaviour
    {
        [SerializeField] private Button backgroundDismissButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Text titleLabel;
        [SerializeField] private Text subtitleLabel;
        [SerializeField] private Text bodyLabel;
        [SerializeField] private Image illustrationImage;
        [SerializeField] private RawImage videoSurface;
        [SerializeField] private VideoPlayer videoPlayer;
        [SerializeField] private Transform customContentRoot;

        private GameObject customContentInstance;
        private RenderTexture renderTexture;

        public event Action Closed;

        private void Awake()
        {
            if (backgroundDismissButton != null)
            {
                backgroundDismissButton.onClick.AddListener(NotifyClosed);
            }

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(NotifyClosed);
            }
        }

        private void OnDisable()
        {
            ClearDynamicContent();
        }

        public void Bind(MinigameTutorialContentConfig content)
        {
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

        private void NotifyClosed()
        {
            Closed?.Invoke();
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
