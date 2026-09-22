using EmpireAtWar.Models.Factions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Views.ReinforcementZones
{
    public interface IReinforcementZoneView
    {
        Vector3 Center { get; }
        float Radius { get; }
        PlayerType StartingOwner { get; }
        bool IsCapturable { get; }
        float CaptureDuration { get; }

        void SetCenter(Vector3 center);
        void Render(PlayerType owner, PlayerType capturingPlayer, float captureProgress, bool isContested);
    }

    public sealed class ReinforcementZoneView : MonoBehaviour, IReinforcementZoneView
    {
        [SerializeField] private PlayerType _startingOwner = PlayerType.None;
        [SerializeField] private bool _isCapturable = true;
        [SerializeField, Min(1f)] private float _captureDuration = 10f;
        [SerializeField, Min(1f)] private float _radius = 45f;
        [SerializeField] private MeshRenderer _sphereRenderer;
        [SerializeField] private Canvas _captureCanvas;
        [SerializeField] private Image _captureProgress;
        [SerializeField] private TMP_Text _statusText;
        [SerializeField] private Color _neutralColor = new Color(0.58f, 0.64f, 0.72f, 0.08f);
        [SerializeField] private Color _playerColor = new Color(0.22f, 0.74f, 0.97f, 0.1f);
        [SerializeField] private Color _opponentColor = new Color(0.94f, 0.27f, 0.27f, 0.1f);
        [SerializeField] private Color _contestedColor = new Color(1f, 0.75f, 0.1f, 0.14f);

        private MaterialPropertyBlock _propertyBlock;

        public Vector3 Center => transform.position;
        public float Radius => _radius;
        public PlayerType StartingOwner => _startingOwner;
        public bool IsCapturable => _isCapturable;
        public float CaptureDuration => _captureDuration;

        public void SetCenter(Vector3 center)
        {
            transform.position = center;
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

        public void Render(PlayerType owner, PlayerType capturingPlayer, float captureProgress, bool isContested)
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
                float displayedProgress = capturingPlayer == PlayerType.None && owner != PlayerType.None
                    ? 1f
                    : Mathf.Clamp01(captureProgress);
                // Sprite-free Images display progress through their rect width.
                _captureProgress.rectTransform.anchorMax = new Vector2(displayedProgress, 1f);
                Color progressColor = GetColor(
                    capturingPlayer == PlayerType.None ? owner : capturingPlayer, isContested);
                progressColor.a = 0.95f;
                _captureProgress.color = progressColor;
            }

            if (_statusText != null)
            {
                _statusText.text = GetStatus(owner, capturingPlayer, captureProgress, isContested);
            }

            if (_captureCanvas != null)
            {
                _captureCanvas.enabled = _isCapturable;
            }
        }

        private void LateUpdate()
        {
            if (_captureCanvas == null || Camera.main == null)
            {
                return;
            }

            _captureCanvas.transform.rotation = Camera.main.transform.rotation;
        }

        private Color GetColor(PlayerType owner, bool isContested)
        {
            if (isContested)
            {
                return _contestedColor;
            }

            return owner switch
            {
                PlayerType.Player => _playerColor,
                PlayerType.Opponent => _opponentColor,
                _ => _neutralColor
            };
        }

        private static string GetStatus(
            PlayerType owner,
            PlayerType capturingPlayer,
            float captureProgress,
            bool isContested)
        {
            if (isContested)
            {
                return "CONTESTED";
            }

            if (capturingPlayer != PlayerType.None)
            {
                int percent = Mathf.RoundToInt(Mathf.Clamp01(captureProgress) * 100f);
                return capturingPlayer == PlayerType.Player
                    ? $"CAPTURING {percent}%"
                    : $"ENEMY CAPTURE {percent}%";
            }

            return owner switch
            {
                PlayerType.Player => "ALLIED CONTROL",
                PlayerType.Opponent => "ENEMY CONTROL",
                _ => "AWAITING CAPTURE"
            };
        }
    }
}
