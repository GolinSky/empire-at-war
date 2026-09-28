using EmpireAtWar.Models.Players;
using TMPro;
using UnityEngine;
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
        [SerializeField] private bool _isCapturable = true;
        [SerializeField, Min(1f)] private float _captureDuration = 10f;
        [SerializeField, Min(1f)] private float _radius = 45f;
        [SerializeField] private MeshRenderer _sphereRenderer;
        [SerializeField] private Canvas _captureCanvas;
        [SerializeField] private Image _captureProgress;
        [SerializeField] private TMP_Text _statusText;
        [SerializeField] private Color _neutralColor = new Color(0.48f, 0.55f, 0.62f, 0.08f);
        [SerializeField] private Color _playerColor = new Color(0.18f, 0.53f, 0.68f, 0.1f);
        [SerializeField] private Color _allyColor = new Color(0.27f, 0.62f, 0.43f, 0.1f);
        [SerializeField] private Color _opponentColor = new Color(0.68f, 0.27f, 0.29f, 0.1f);
        [SerializeField] private Color _contestedColor = new Color(0.73f, 0.56f, 0.23f, 0.1f);

        private MaterialPropertyBlock _propertyBlock;
        // Assigned by the map builder at runtime; the prefab itself has no owner.
        private PlayerId _startingOwner = PlayerId.None;

        public Vector3 Center => transform.position;
        public float Radius => _radius;
        public PlayerId StartingOwner => _startingOwner;
        public bool IsCapturable => _isCapturable;
        public float CaptureDuration => _captureDuration;

        public void Configure(PlayerId startingOwner, bool isCapturable)
        {
            _startingOwner = startingOwner;
            _isCapturable = isCapturable;
        }

        private void Awake()
        {
            _propertyBlock = new MaterialPropertyBlock();
            if (_captureCanvas != null)
            {
                _captureCanvas.overrideSorting = true;
                _captureCanvas.sortingOrder = 100;
                CanvasScaler scaler = _captureCanvas.GetComponent<CanvasScaler>();
                if (scaler != null)
                {
                    scaler.dynamicPixelsPerUnit = 10f;
                }
            }
        }

        public void Render(OwnerRelation owner, OwnerRelation capturer, float captureProgress, bool isContested)
        {
            if (_sphereRenderer != null)
            {
                _propertyBlock ??= new MaterialPropertyBlock();
                _sphereRenderer.GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetColor("_BaseColor", GetColor(owner, isContested));
                _propertyBlock.SetColor("_Color", GetColor(owner, isContested));
                _sphereRenderer.SetPropertyBlock(_propertyBlock);
            }

            if (_captureProgress != null)
            {
                float displayedProgress = capturer == OwnerRelation.Neutral && owner != OwnerRelation.Neutral
                    ? 1f
                    : Mathf.Clamp01(captureProgress);
                // Sprite-free Images display progress through their rect width.
                _captureProgress.rectTransform.anchorMax = new Vector2(displayedProgress, 1f);
                Color progressColor = GetColor(
                    capturer == OwnerRelation.Neutral ? owner : capturer, isContested);
                progressColor.a = 0.95f;
                _captureProgress.color = progressColor;
            }

            if (_statusText != null)
            {
                _statusText.text = GetStatus(owner, capturer, captureProgress, isContested);
            }
        }

        public void SetVisibility(bool isVisible, bool showCaptureUi)
        {
            _sphereRenderer.enabled = isVisible;
            _captureCanvas.gameObject.SetActive(showCaptureUi);
        }

        private void LateUpdate()
        {
            if (_captureCanvas == null || Camera.main == null)
            {
                return;
            }

            _captureCanvas.transform.rotation = Camera.main.transform.rotation;
        }

        private Color GetColor(OwnerRelation owner, bool isContested)
        {
            if (isContested)
            {
                return _contestedColor;
            }

            return owner switch
            {
                OwnerRelation.Own => _playerColor,
                OwnerRelation.Ally => _allyColor,
                OwnerRelation.Enemy => _opponentColor,
                _ => _neutralColor
            };
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
