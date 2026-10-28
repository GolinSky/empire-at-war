using EmpireAtWar.Components.Ui.Tooltip;
using EmpireAtWar.Entities.Capture;
using System;
using EmpireAtWar.Models.Players;
using System.Collections.Generic;
using EmpireAtWar.Components.Obstacles;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Utilities.ScriptUtils.EditorSerialization;

namespace EmpireAtWar.Entities.CaptureSites
{
    public sealed class CaptureSiteView : MonoBehaviour, ICaptureSiteView
    {
        private const float MINIMUM_FRAMEWORK_HEIGHT = 0.05f;

        [SerializeField] private CaptureRingView captureRing;
        [SerializeField] private Transform asteroidRocks;
        [SerializeField] private MapObstacle[] rockObstacles = Array.Empty<MapObstacle>();
        [SerializeField] private DictionaryWrapper<SiteFacilityType, Transform> constructionFrameworks;
        [SerializeField] private Canvas statusCanvas;
        [SerializeField] private Image progressFill;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private TooltipHoverView tooltipHover;
        private Camera _camera;
        private RectTransform _canvasTransform;

        [SerializeField, Min(1f)] private float radius = 60f;
        [SerializeField, Min(1f)] private float captureDuration = 12f;
        [SerializeField, FormerlySerializedAs("miningFacilityHeightOffset"),
         Tooltip("World-space height of a built facility relative to the site center; keeps it on the site rocks.")]
        private float facilityHeightOffset;
        [SerializeField, Range(0.05f, 0.6f), Tooltip("Panel height as a fraction of the screen height.")]
        private float screenHeightFraction = 0.24f;

        public CaptureSiteKind Kind { get; private set; }
        public PlayerId StartingOwner { get; private set; }
        public int PointId { get; private set; } = -1;

        public void Configure(CaptureSiteKind kind, PlayerId owner, int pointId = -1)
        {
            Kind = kind;
            StartingOwner = owner;
            PointId = pointId;
        }

        public TooltipHoverView TooltipHover => tooltipHover;

        public Vector3 Center => transform.position;
        public float Radius => radius * Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.z));
        public float CaptureDuration => captureDuration;
        public Vector3 FacilityPosition => Center + Vector3.up * facilityHeightOffset;
        public IReadOnlyList<MapObstacle> RockObstacles => rockObstacles;

        private void Awake()
        {
            captureRing.SetVisibility(false);
            _camera = Camera.main;
            _canvasTransform = (RectTransform)statusCanvas.transform;
            statusCanvas.worldCamera = _camera;
        }

        private void LateUpdate()
        {
            if (!statusCanvas.gameObject.activeSelf)
            {
                return;
            }

            // Billboard at a constant screen size so the panel stays readable at every zoom level.
            Transform cameraTransform = _camera.transform;
            _canvasTransform.rotation = cameraTransform.rotation;
            float distance = Vector3.Distance(cameraTransform.position, _canvasTransform.position);
            float viewHeight = 2f * distance * Mathf.Tan(_camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float worldScale = viewHeight * screenHeightFraction / _canvasTransform.rect.height;
            _canvasTransform.localScale = Vector3.one * (worldScale / transform.lossyScale.y);
        }

        public void Render(
            OwnerRelation owner,
            CaptureSiteState state,
            SiteFacilityType facilityType,
            string facilityName,
            OwnerRelation capturer,
            float captureProgress,
            float constructionProgress,
            bool isContested)
        {
            bool hidden = state is CaptureSiteState.Constructing or CaptureSiteState.Operational;
            bool blink = state == CaptureSiteState.Owned && owner == OwnerRelation.Own &&
                capturer == OwnerRelation.Neutral && !isContested;
            captureRing.Render(capturer == OwnerRelation.Neutral ? owner : capturer, isContested, blink, hidden);

            bool isConstructing = state == CaptureSiteState.Constructing;
            asteroidRocks.gameObject.SetActive(state != CaptureSiteState.Operational);
            foreach (KeyValuePair<SiteFacilityType, Transform> framework in constructionFrameworks.Dictionary)
            {
                framework.Value.gameObject.SetActive(isConstructing && framework.Key == facilityType);
            }

            if (isConstructing)
            {
                Transform framework = constructionFrameworks.Dictionary[facilityType];
                Vector3 scale = framework.localScale;
                scale.y = Mathf.Lerp(MINIMUM_FRAMEWORK_HEIGHT, 1f, constructionProgress);
                framework.localScale = scale;
            }

            bool isCapturing = capturer != OwnerRelation.Neutral;
            float displayedProgress = isCapturing ? captureProgress
                : isConstructing ? constructionProgress
                : owner == OwnerRelation.Neutral ? 0f
                : 1f;
            // Sprite-free Images display progress through their rect width.
            progressFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(displayedProgress), 1f);
            Color progressColor = captureRing.GetColor(isCapturing ? capturer : owner, isContested);
            progressColor.a = 0.95f;
            progressFill.color = progressColor;

            statusText.text = GetStatus(owner, state, facilityName, capturer,
                captureProgress, constructionProgress, isContested);
        }

        public void SetVisibility(bool isVisible, bool showStatus)
        {
            captureRing.SetVisibility(isVisible);
            statusCanvas.gameObject.SetActive(isVisible && showStatus);
        }

        private static string GetStatus(
            OwnerRelation owner,
            CaptureSiteState state,
            string facilityName,
            OwnerRelation capturer,
            float captureProgress,
            float constructionProgress,
            bool isContested)
        {
            if (isContested)
            {
                return "CONTESTED";
            }

            if (capturer != OwnerRelation.Neutral)
            {
                int capturePercent = Mathf.RoundToInt(Mathf.Clamp01(captureProgress) * 100f);
                return capturer switch
                {
                    OwnerRelation.Own => $"CAPTURING {capturePercent}%",
                    OwnerRelation.Ally => $"ALLY CAPTURE {capturePercent}%",
                    _ => $"ENEMY CAPTURE {capturePercent}%"
                };
            }

            string ownerLabel = owner switch
            {
                OwnerRelation.Own => "ALLIED",
                OwnerRelation.Ally => "ALLY",
                _ => "ENEMY"
            };
            return state switch
            {
                CaptureSiteState.Constructing =>
                    $"{(owner == OwnerRelation.Own ? "BUILDING" : ownerLabel)} {facilityName} " +
                    $"{Mathf.RoundToInt(Mathf.Clamp01(constructionProgress) * 100f)}%",
                CaptureSiteState.Operational => $"{ownerLabel} {facilityName}",
                CaptureSiteState.Owned => owner == OwnerRelation.Own || owner == OwnerRelation.Ally
                    ? $"{ownerLabel} SITE - SELECT TO BUILD" : $"{ownerLabel} SITE",
                _ => "NEUTRAL SITE"
            };
        }
    }
}
