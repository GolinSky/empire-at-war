using System;
using EmpireAtWar.Models.Players;
using System.Collections.Generic;
using EmpireAtWar.Components.Obstacles;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Utilities.ScriptUtils.EditorSerialization;

namespace EmpireAtWar.Entities.CaptureSites
{
    public sealed class CaptureSiteView : MonoBehaviour, ICaptureSiteView
    {
        private const float MINIMUM_FRAMEWORK_HEIGHT = 0.05f;
        private static readonly int BASE_COLOR_ID = Shader.PropertyToID("_BaseColor");
        private static readonly int COLOR_ID = Shader.PropertyToID("_Color");

        [SerializeField, Min(1f)] private float radius = 40f;
        [SerializeField, Min(1f)] private float captureDuration = 12f;
        [SerializeField, Tooltip("World-space height of a built mining facility relative to the site center; keeps it below ship and station hulls.")]
        private float miningFacilityHeightOffset;
        [SerializeField] private MeshRenderer ringRenderer;
        [SerializeField] private MapObstacle[] rockObstacles = Array.Empty<MapObstacle>();
        [SerializeField] private DictionaryWrapper<SiteFacilityType, Transform> constructionFrameworks;
        [SerializeField] private Canvas statusCanvas;
        [SerializeField, Range(0.05f, 0.6f), Tooltip("Panel height as a fraction of the screen height.")]
        private float screenHeightFraction = 0.24f;
        [SerializeField] private Image progressFill;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private GameObject buildOptions;
        [SerializeField] private SiteFacilityOptionView[] facilityOptions = Array.Empty<SiteFacilityOptionView>();
        [SerializeField] private Color neutralColor = new Color(0.58f, 0.64f, 0.72f, 0.08f);
        [SerializeField] private Color playerColor = new Color(0.22f, 0.74f, 0.97f, 0.1f);
        [SerializeField] private Color allyColor = new Color(0.36f, 0.9f, 0.62f, 0.1f);
        [SerializeField] private Color opponentColor = new Color(0.94f, 0.27f, 0.27f, 0.1f);
        [SerializeField] private Color contestedColor = new Color(1f, 0.75f, 0.1f, 0.14f);

        private MaterialPropertyBlock _propertyBlock;
        private Camera _camera;
        private RectTransform _canvasTransform;

        public event Action<SiteFacilityType> BuildPressed;

        public Vector3 Center => transform.position;
        public float Radius => radius * Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.z));
        public float CaptureDuration => captureDuration;
        public IReadOnlyList<MapObstacle> RockObstacles => rockObstacles;

        private void Awake()
        {
            _propertyBlock = new MaterialPropertyBlock();
            _camera = Camera.main;
            _canvasTransform = (RectTransform)statusCanvas.transform;
            statusCanvas.worldCamera = _camera;
            foreach (SiteFacilityOptionView option in facilityOptions)
            {
                option.Pressed += HandleOptionPressed;
            }
        }

        private void OnDestroy()
        {
            foreach (SiteFacilityOptionView option in facilityOptions)
            {
                option.Pressed -= HandleOptionPressed;
            }
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

        public Vector3 GetFacilityPosition(SiteFacilityType facilityType)
        {
            return facilityType == SiteFacilityType.Mining
                ? Center + Vector3.up * miningFacilityHeightOffset
                : Center;
        }

        public void ConfigureOption(SiteFacilityType facilityType, string displayName, string costLabel)
        {
            GetOption(facilityType).Configure(displayName, costLabel);
        }

        public void Render(
            OwnerRelation owner,
            CaptureSiteState state,
            SiteFacilityType facilityType,
            OwnerRelation capturer,
            float captureProgress,
            float constructionProgress,
            bool isContested)
        {
            Color ownerColor = GetColor(owner, isContested);
            ringRenderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(BASE_COLOR_ID, ownerColor);
            _propertyBlock.SetColor(COLOR_ID, ownerColor);
            ringRenderer.SetPropertyBlock(_propertyBlock);

            bool isConstructing = state == CaptureSiteState.Constructing;
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
            Color progressColor = GetColor(isCapturing ? capturer : owner, isContested);
            progressColor.a = 0.95f;
            progressFill.color = progressColor;

            statusText.text = GetStatus(owner, state, GetOption(facilityType).DisplayName, capturer,
                captureProgress, constructionProgress, isContested);
        }

        public void SetVisibility(bool isVisible, bool showStatus)
        {
            ringRenderer.enabled = isVisible;
            statusCanvas.gameObject.SetActive(isVisible && showStatus);
        }

        public void SetBuildOptionsVisible(bool isVisible)
        {
            buildOptions.SetActive(isVisible);
            if (isVisible)
            {
                statusText.text = "CHOOSE FACILITY";
            }
        }

        public void SetOptionInteractable(SiteFacilityType facilityType, bool isInteractable)
        {
            GetOption(facilityType).SetInteractable(isInteractable);
        }

        private SiteFacilityOptionView GetOption(SiteFacilityType facilityType)
        {
            foreach (SiteFacilityOptionView option in facilityOptions)
            {
                if (option.FacilityType == facilityType)
                {
                    return option;
                }
            }

            throw new ArgumentOutOfRangeException(nameof(facilityType), facilityType, "No build option on the site.");
        }

        private void HandleOptionPressed(SiteFacilityType facilityType)
        {
            BuildPressed?.Invoke(facilityType);
        }

        private Color GetColor(OwnerRelation owner, bool isContested)
        {
            if (isContested)
            {
                return contestedColor;
            }

            return owner switch
            {
                OwnerRelation.Own => playerColor,
                OwnerRelation.Ally => allyColor,
                OwnerRelation.Enemy => opponentColor,
                _ => neutralColor
            };
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
                CaptureSiteState.Owned => owner == OwnerRelation.Own ? "ALLIED SITE - SELECT TO BUILD" : $"{ownerLabel} SITE",
                _ => "NEUTRAL SITE"
            };
        }
    }
}
