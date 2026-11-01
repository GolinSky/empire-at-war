using DG.Tweening;
using EmpireAtWar.Models.Players;
using UnityEngine;

namespace EmpireAtWar.Entities.Capture
{
    public sealed class CaptureRingView : MonoBehaviour
    {
        private const float MIN_BLINK_OPACITY = 0.6f;
        private const float BLINK_FADE_DURATION = 0.8f;
        private const float CAPTURE_COLOR_FADE_DURATION = 1f;

        [SerializeField] private MeshRenderer ringRenderer;
        [SerializeField] private Color neutralColor;
        [SerializeField] private Color playerColor;
        [SerializeField] private Color allyColor;
        [SerializeField] private Color opponentColor;
        [SerializeField] private Color contestedColor;
        [SerializeField] private bool siteBorder;
        private static readonly int BASE_COLOR_ID = Shader.PropertyToID("_BaseColor");
        private static readonly int COLOR_ID = Shader.PropertyToID("_Color");
        private static readonly int BORDER_WIDTH_ID = Shader.PropertyToID("_BorderWidth");
        private static readonly int OUTLINE_WIDTH_ID = Shader.PropertyToID("_OutlineWidth");
        private static readonly int BORDER_OPACITY_ID = Shader.PropertyToID("_BorderOpacity");
        private static readonly int OUTLINE_OPACITY_ID = Shader.PropertyToID("_OutlineOpacity");
        private MaterialPropertyBlock _propertyBlock;
        private Color _color;
        private Color _targetColor;
        private bool _blink;
        private bool _captureBlink;
        private bool _hidden;
        private bool _visible;
        private float _blend;
        private Tween _blinkTween;

        public Color GetColor(OwnerRelation owner, bool isContested)
        {
            if (isContested) return contestedColor;
            return owner switch
            {
                OwnerRelation.Own => playerColor,
                OwnerRelation.Ally => allyColor,
                OwnerRelation.Enemy => opponentColor,
                _ => neutralColor
            };
        }

        public void Render(OwnerRelation owner, bool isContested, bool blink, bool hidden,
            OwnerRelation capturer = OwnerRelation.Neutral,
            OwnerRelation contestingFirst = OwnerRelation.Neutral,
            OwnerRelation contestingSecond = OwnerRelation.Neutral)
        {
            bool hasOpposingSides = contestingFirst != OwnerRelation.Neutral &&
                contestingSecond != OwnerRelation.Neutral;
            bool captureBlink = hasOpposingSides || capturer != OwnerRelation.Neutral;
            Color color = GetColor(hasOpposingSides ? contestingFirst : owner, false);
            Color targetColor = GetColor(hasOpposingSides ? contestingSecond :
                capturer == OwnerRelation.Neutral ? owner : capturer, false);
            if (_color != color || _targetColor != targetColor || _captureBlink != captureBlink)
                StopBlink();
            _color = color;
            _targetColor = targetColor;
            _captureBlink = captureBlink;
            _blink = blink || captureBlink;
            _hidden = hidden;
            UpdateBlink();
            Apply();
        }

        public void SetVisibility(bool visible)
        {
            _visible = visible;
            UpdateBlink();
            Apply();
        }

        private void OnEnable() => UpdateBlink();

        private void OnDisable() => StopBlink();

        private void UpdateBlink()
        {
            if (!_blink || !_visible || _hidden || !isActiveAndEnabled)
            {
                StopBlink();
                return;
            }

            if (_blinkTween != null) return;
            _blinkTween = DOTween.To(() => _blend, blend =>
                {
                    _blend = blend;
                    Apply();
                }, 1f, _captureBlink ? CAPTURE_COLOR_FADE_DURATION : BLINK_FADE_DURATION)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetUpdate(true);
        }

        private void StopBlink()
        {
            if (_blinkTween != null)
            {
                _blinkTween.Kill();
                _blinkTween = null;
            }
            _blend = 0f;
        }

        private void Apply()
        {
            _propertyBlock ??= new MaterialPropertyBlock();
            float opacity = _captureBlink ? 1f : Mathf.Lerp(1f, MIN_BLINK_OPACITY, _blend);
            Color color = Color.Lerp(_color, _targetColor, _blend);
            color.a *= opacity;
            ringRenderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(BASE_COLOR_ID, color);
            _propertyBlock.SetColor(COLOR_ID, color);
            if (siteBorder)
            {
                _propertyBlock.SetFloat(BORDER_WIDTH_ID, 0.003f);
                _propertyBlock.SetFloat(OUTLINE_WIDTH_ID, 0.006f);
                _propertyBlock.SetFloat(BORDER_OPACITY_ID, 0.24f * opacity);
                _propertyBlock.SetFloat(OUTLINE_OPACITY_ID, 0.1f * opacity);
            }
            ringRenderer.SetPropertyBlock(_propertyBlock);
            ringRenderer.enabled = _visible && !_hidden;
        }
    }
}
