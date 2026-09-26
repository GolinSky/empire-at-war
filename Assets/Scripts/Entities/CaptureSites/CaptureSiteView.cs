using System;
using EmpireAtWar.Models.Factions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Entities.CaptureSites
{
    public sealed class CaptureSiteView : MonoBehaviour, ICaptureSiteView
    {
        private const float MINIMUM_FRAMEWORK_HEIGHT = 0.05f;
        private static readonly int BASE_COLOR_ID = Shader.PropertyToID("_BaseColor");
        private static readonly int COLOR_ID = Shader.PropertyToID("_Color");

        [SerializeField] private SiteFacilityType facilityType = SiteFacilityType.Mining;
        [SerializeField, Min(1f)] private float radius = 40f;
        [SerializeField, Min(1f)] private float captureDuration = 12f;
        [SerializeField] private MeshRenderer ringRenderer;
        [SerializeField] private Transform constructionFramework;
        [SerializeField] private Canvas statusCanvas;
        [SerializeField] private Image progressFill;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private Button buildButton;
        [SerializeField] private TMP_Text buildLabel;
        [SerializeField] private Color neutralColor = new Color(0.58f, 0.64f, 0.72f, 0.08f);
        [SerializeField] private Color playerColor = new Color(0.22f, 0.74f, 0.97f, 0.1f);
        [SerializeField] private Color opponentColor = new Color(0.94f, 0.27f, 0.27f, 0.1f);
        [SerializeField] private Color contestedColor = new Color(1f, 0.75f, 0.1f, 0.14f);

        private MaterialPropertyBlock _propertyBlock;
        private Camera _camera;

        public event Action BuildPressed;

        public Vector3 Center => transform.position;
        public float Radius => radius;
        public float CaptureDuration => captureDuration;
        public SiteFacilityType FacilityType => facilityType;

        private void Awake()
        {
            _propertyBlock = new MaterialPropertyBlock();
            _camera = Camera.main;
            statusCanvas.worldCamera = _camera;
            buildButton.onClick.AddListener(HandleBuildClicked);
        }

        private void OnDestroy()
        {
            buildButton.onClick.RemoveListener(HandleBuildClicked);
        }

        private void LateUpdate()
        {
            if (statusCanvas.gameObject.activeSelf)
            {
                statusCanvas.transform.rotation = _camera.transform.rotation;
            }
        }

        public void Render(
            PlayerType owner,
            CaptureSiteState state,
            PlayerType capturingPlayer,
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
            constructionFramework.gameObject.SetActive(isConstructing);
            if (isConstructing)
            {
                Vector3 scale = constructionFramework.localScale;
                scale.y = Mathf.Lerp(MINIMUM_FRAMEWORK_HEIGHT, 1f, constructionProgress);
                constructionFramework.localScale = scale;
            }

            bool isCapturing = capturingPlayer != PlayerType.None;
            float displayedProgress = isCapturing ? captureProgress
                : isConstructing ? constructionProgress
                : owner == PlayerType.None ? 0f
                : 1f;
            // Sprite-free Images display progress through their rect width.
            progressFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(displayedProgress), 1f);
            Color progressColor = GetColor(isCapturing ? capturingPlayer : owner, isContested);
            progressColor.a = 0.95f;
            progressFill.color = progressColor;

            statusText.text = GetStatus(owner, state, capturingPlayer, captureProgress,
                constructionProgress, isContested);
        }

        public void SetVisibility(bool isVisible, bool showStatus)
        {
            ringRenderer.enabled = isVisible;
            statusCanvas.gameObject.SetActive(isVisible && showStatus);
        }

        public void SetBuildOption(bool isVisible, bool isInteractable, string label)
        {
            buildButton.gameObject.SetActive(isVisible);
            buildButton.interactable = isInteractable;
            buildLabel.text = label;
        }

        private void HandleBuildClicked()
        {
            BuildPressed?.Invoke();
        }

        private Color GetColor(PlayerType owner, bool isContested)
        {
            if (isContested)
            {
                return contestedColor;
            }

            return owner switch
            {
                PlayerType.Player => playerColor,
                PlayerType.Opponent => opponentColor,
                _ => neutralColor
            };
        }

        private static string GetStatus(
            PlayerType owner,
            CaptureSiteState state,
            PlayerType capturingPlayer,
            float captureProgress,
            float constructionProgress,
            bool isContested)
        {
            if (isContested)
            {
                return "CONTESTED";
            }

            if (capturingPlayer != PlayerType.None)
            {
                int capturePercent = Mathf.RoundToInt(Mathf.Clamp01(captureProgress) * 100f);
                return capturingPlayer == PlayerType.Player
                    ? $"CAPTURING {capturePercent}%"
                    : $"ENEMY CAPTURE {capturePercent}%";
            }

            bool isAllied = owner == PlayerType.Player;
            return state switch
            {
                CaptureSiteState.Constructing =>
                    $"{(isAllied ? "CONSTRUCTING" : "ENEMY CONSTRUCTION")} " +
                    $"{Mathf.RoundToInt(Mathf.Clamp01(constructionProgress) * 100f)}%",
                CaptureSiteState.Operational => isAllied ? "ALLIED FACILITY" : "ENEMY FACILITY",
                CaptureSiteState.Owned => isAllied ? "ALLIED SITE" : "ENEMY SITE",
                _ => "NEUTRAL SITE"
            };
        }
    }
}
