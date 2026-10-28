using EmpireAtWar.Models.Players;
using EmpireAtWar.Entities.Capture;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace EmpireAtWar.Views.ReinforcementZones
{
    public interface IReinforcementZoneView
    {
        Vector3 Center { get; }
        float Radius { get; }
        PlayerId StartingOwner { get; }
        bool IsCapturable { get; }
        float CaptureDuration { get; }

        void SetVisibility(bool isVisible, bool showCaptureUi);

        void Render(OwnerRelation owner, OwnerRelation capturer, float captureProgress, bool isContested);
    }

    public sealed class ReinforcementZoneView : MonoBehaviour, IReinforcementZoneView
    {
        [SerializeField] private CaptureRingView captureRing;
        [SerializeField, FormerlySerializedAs("_captureCanvas")] private Canvas captureCanvas;
        [SerializeField, FormerlySerializedAs("_captureCanvasScaler")] private CanvasScaler captureCanvasScaler;
        [SerializeField, FormerlySerializedAs("_captureProgress")] private Image captureProgress;
        [SerializeField, FormerlySerializedAs("_statusText")] private TMP_Text statusText;

        // Assigned by the map builder at runtime; the prefab itself has no owner.
        private PlayerId _startingOwner = PlayerId.None;

        [SerializeField, FormerlySerializedAs("_captureDuration"), Min(1f)] private float captureDuration = 10f;
        [SerializeField, FormerlySerializedAs("_radius"), Min(1f)] private float radius = 45f;

        [SerializeField, FormerlySerializedAs("_isCapturable")] private bool isCapturable = true;

        public Vector3 Center => transform.position;
        public float Radius => radius;
        public PlayerId StartingOwner => _startingOwner;
        public bool IsCapturable => isCapturable;
        public float CaptureDuration => captureDuration;

        public void Configure(PlayerId startingOwner, bool isCapturable)
        {
            _startingOwner = startingOwner;
            this.isCapturable = isCapturable;
        }

        private void Awake()
        {
            if (captureCanvas != null)
            {
                captureCanvas.overrideSorting = true;
                captureCanvas.sortingOrder = 100;
                captureCanvasScaler.dynamicPixelsPerUnit = 10f;
            }
        }

        public void Render(OwnerRelation owner, OwnerRelation capturer, float captureProgress, bool isContested)
        {
            captureRing.Render(owner, isContested, false, false);

            if (this.captureProgress != null)
            {
                float displayedProgress = capturer == OwnerRelation.Neutral && owner != OwnerRelation.Neutral
                    ? 1f
                    : Mathf.Clamp01(captureProgress);
                // Sprite-free Images display progress through their rect width.
                this.captureProgress.rectTransform.anchorMax = new Vector2(displayedProgress, 1f);
                Color progressColor = captureRing.GetColor(
                    capturer == OwnerRelation.Neutral ? owner : capturer, isContested);
                progressColor.a = 0.95f;
                this.captureProgress.color = progressColor;
            }

            if (statusText != null)
            {
                statusText.text = GetStatus(owner, capturer, captureProgress, isContested);
            }
        }

        public void SetVisibility(bool isVisible, bool showCaptureUi)
        {
            captureRing.SetVisibility(isVisible);
            captureCanvas.gameObject.SetActive(showCaptureUi);
        }

        private void LateUpdate()
        {
            if (captureCanvas == null || Camera.main == null)
            {
                return;
            }

            captureCanvas.transform.rotation = Camera.main.transform.rotation;
        }

        private static string GetStatus(
            OwnerRelation owner,
            OwnerRelation capturer,
            float captureProgress,
            bool isContested)
        {
            if (isContested)
            {
                return "CONTESTED";
            }

            if (capturer != OwnerRelation.Neutral)
            {
                int percent = Mathf.RoundToInt(Mathf.Clamp01(captureProgress) * 100f);
                return capturer switch
                {
                    OwnerRelation.Own => $"CAPTURING {percent}%",
                    OwnerRelation.Ally => $"ALLY CAPTURE {percent}%",
                    _ => $"ENEMY CAPTURE {percent}%"
                };
            }

            return owner switch
            {
                OwnerRelation.Own => "ALLIED CONTROL",
                OwnerRelation.Ally => "ALLY CONTROL",
                OwnerRelation.Enemy => "ENEMY CONTROL",
                _ => "AWAITING CAPTURE"
            };
        }
    }
}
