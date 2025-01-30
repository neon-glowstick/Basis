using Basis.Scripts.BasisSdk.Players;
using Basis.Scripts.Device_Management;
using Basis.Scripts.Networking;
using Basis.Scripts.TransformBinders.BoneControl;
using System;
using System.Collections;
using TMPro;
using UnityEngine;
namespace Basis.Scripts.UI.NamePlate
{
    public abstract class BasisNamePlate : MonoBehaviour
    {
        public Vector3 dirToCamera;
        public BasisBoneControl HipTarget;
        public BasisBoneControl MouthTarget;
        public TextMeshPro Text;
        public SpriteRenderer Loadingbar;
        public TextMeshPro Loadingtext;
        public float YHeightMultiplier = 1.25f;
        public BasisRemotePlayer BasisRemotePlayer;
        public SpriteRenderer namePlateImage;
        public Color NormalColor;
        public Color IsTalkingColor;
        public Color OutOfRangeColor;
        [SerializeField]
        public float transitionDuration = 0.3f;
        [SerializeField]
        public float returnDelay = 0.4f;
        public Coroutine colorTransitionCoroutine;
        public Coroutine returnToNormalCoroutine;
        public Vector3 cachedDirection;
        public Quaternion cachedRotation;
        public bool HasRendererCheckWiredUp = false;
        public bool IsVisible = true;
        public void Initalize(BasisBoneControl hipTarget, BasisRemotePlayer basisRemotePlayer)
        {
            BasisRemotePlayer = basisRemotePlayer;
            HipTarget = hipTarget;
            MouthTarget = BasisRemotePlayer.MouthControl;
            Text.text = BasisRemotePlayer.DisplayName;
            BasisRemotePlayer.ProgressReportAvatarLoad.OnProgressReport += ProgressReport;
            BasisRemotePlayer.AudioReceived += OnAudioReceived;
            BasisRemotePlayer.OnAvatarSwitched += RebuildRenderCheck;
            BasisRemotePlayer.OnAvatarSwitchedFallBack += RebuildRenderCheck;
            RemoteNamePlateDriver.Instance.AddNamePlate(this);
        }
        public void RebuildRenderCheck()
        {
            if (HasRendererCheckWiredUp)
            {
                DeInitalizeCallToRender();
            }
            HasRendererCheckWiredUp = false;
            if (BasisRemotePlayer != null && BasisRemotePlayer.FaceRenderer != null)
            {
                BasisDebug.Log("Wired up Renderer Check For Blinking");
                BasisRemotePlayer.FaceRenderer.Check += UpdateFaceVisibility;
                BasisRemotePlayer.FaceRenderer.DestroyCalled += AvatarUnloaded;
                UpdateFaceVisibility(BasisRemotePlayer.FaceisVisible);
                HasRendererCheckWiredUp = true;
            }
        }

        private void AvatarUnloaded()
        {
            UpdateFaceVisibility(true);
        }

        private void UpdateFaceVisibility(bool State)
        {
            IsVisible = State;
            gameObject.SetActive(State);
            if (IsVisible == false)
            {
                if (returnToNormalCoroutine != null)
                {
                    StopCoroutine(returnToNormalCoroutine);
                }
                if (colorTransitionCoroutine != null)
                {
                    StopCoroutine(colorTransitionCoroutine);
                }
            }
        }
        public void OnAudioReceived(bool hasRealAudio)
        {
            if (IsVisible)
            {
                Color targetColor;
                if (BasisRemotePlayer.OutOfRangeFromLocal)
                {
                    targetColor = hasRealAudio ? OutOfRangeColor : NormalColor;
                }
                else
                {
                    targetColor = hasRealAudio ? IsTalkingColor : NormalColor;
                }
                BasisNetworkManagement.MainThreadContext.Post(_ =>
                {
                    if (isActiveAndEnabled)
                    {
                        if (colorTransitionCoroutine != null)
                        {
                            StopCoroutine(colorTransitionCoroutine);
                        }
                        colorTransitionCoroutine = StartCoroutine(TransitionColor(targetColor));
                    }
                }, null);
            }
        }
        private IEnumerator TransitionColor(Color targetColor)
        {
            // Cache the initial values
            Color initialColor = namePlateImage.color;
            float elapsedTime = 0f;

            // Use a simple loop, minimizing redundant computations
            while (elapsedTime < transitionDuration)
            {
                elapsedTime += Time.deltaTime;

                // Calculate the interpolation progress
                float lerpProgress = Mathf.Clamp01(elapsedTime / transitionDuration);

                // Interpolate only when needed
                namePlateImage.color = Color.Lerp(initialColor, targetColor, lerpProgress);

                // Avoid using `yield return null` directly to reduce allocations
                yield return new WaitForEndOfFrame();
            }

            // Set the final color explicitly to avoid rounding issues
            namePlateImage.color = targetColor;

            // Nullify the reference to clean up
            colorTransitionCoroutine = null;

            // Handle the delayed return logic if necessary
            if (targetColor == IsTalkingColor)
            {
                if (returnToNormalCoroutine != null)
                {
                    StopCoroutine(returnToNormalCoroutine);
                }
                returnToNormalCoroutine = StartCoroutine(DelayedReturnToNormal());
            }
        }
        private IEnumerator DelayedReturnToNormal()
        {
            yield return new WaitForSeconds(returnDelay);
            yield return StartCoroutine(TransitionColor(NormalColor));
            returnToNormalCoroutine = null;
        }
        public void OnDestroy()
        {
            BasisRemotePlayer.ProgressReportAvatarLoad.OnProgressReport -= ProgressReport;
            BasisRemotePlayer.AudioReceived -= OnAudioReceived;
            DeInitalizeCallToRender();
            RemoteNamePlateDriver.Instance.RemoveNamePlate(this);
        }
        public void DeInitalizeCallToRender()
        {
            if (HasRendererCheckWiredUp && BasisRemotePlayer != null && BasisRemotePlayer.FaceRenderer != null)
            {
                BasisRemotePlayer.FaceRenderer.Check -= UpdateFaceVisibility;
                BasisRemotePlayer.FaceRenderer.DestroyCalled -= AvatarUnloaded;
            }
        }
        public bool HasProgressBarVisible = false;
        public void ProgressReport(string UniqueID, float progress, string info)
        {
            BasisDeviceManagement.EnqueueOnMainThread(() =>
              {
                  if (progress == 100)
                  {
                      if (HasProgressBarVisible)
                      {
                          Loadingtext.gameObject.SetActive(false);
                          Loadingbar.gameObject.SetActive(false);
                          HasProgressBarVisible = false;
                      }
                  }
                  else
                  {
                      if (HasProgressBarVisible == false)
                      {
                          Loadingbar.gameObject.SetActive(true);
                          Loadingtext.gameObject.SetActive(true);
                          HasProgressBarVisible = true;
                      }

                      Loadingtext.text = info;
                      UpdateProgressBar(UniqueID, progress);
                  }
              });
        }
        public void UpdateProgressBar(string UniqueID,float progress)
        {
            Vector2 scale = Loadingbar.size;
            scale.x = progress/2;
            Loadingbar.size = scale;
        }
    }
}
