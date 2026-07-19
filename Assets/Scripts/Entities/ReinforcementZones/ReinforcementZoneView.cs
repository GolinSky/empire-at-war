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
        [SerializeField] private Color _neutralColor = new Color(0.7f, 0.7f, 0.7f, 0.25f);
        [SerializeField] private Color _playerColor = new Color(0.15f, 0.65f, 1f, 0.3f);
        [SerializeField] private Color _opponentColor = new Color(1f, 0.2f, 0.15f, 0.3f);
        [SerializeField] private Color _contestedColor = new Color(1f, 0.75f, 0.1f, 0.4f);

        private MaterialPropertyBlock _propertyBlock;

        public Vector3 Center => transform.position;
        public float Radius => _radius;
        public PlayerType StartingOwner => _startingOwner;
        public bool IsCapturable => _isCapturable;
        public float CaptureDuration => _captureDuration;

        private void Awake()
        {
            _propertyBlock = new MaterialPropertyBlock();
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
                _captureProgress.fillAmount = captureProgress;
                _captureProgress.color = GetColor(capturingPlayer, isContested);
            }

            if (_statusText != null)
            {
                _statusText.text = GetStatus(owner, capturingPlayer, isContested);
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

        private static string GetStatus(PlayerType owner, PlayerType capturingPlayer, bool isContested)
        {
            if (isContested)
            {
                return "CONTESTED";
            }

            if (capturingPlayer != PlayerType.None)
            {
                return capturingPlayer == PlayerType.Player ? "PLAYER CAPTURING" : "ENEMY CAPTURING";
            }

            return owner switch
            {
                PlayerType.Player => "PLAYER ZONE",
                PlayerType.Opponent => "ENEMY ZONE",
                _ => "NEUTRAL ZONE"
            };
        }
    }
}
