using EmpireAtWar.Models.Players;
using UnityEngine;

namespace EmpireAtWar.Entities.Capture
{
    public sealed class CaptureRingView : MonoBehaviour
    {
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
        private bool _blink;
        private bool _hidden;
        private bool _visible;

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

        public void Render(OwnerRelation owner, bool isContested, bool blink, bool hidden)
        {
            _color = GetColor(owner, isContested);
            _blink = blink;
            _hidden = hidden;
            Apply();
        }

        public void SetVisibility(bool visible)
        {
            _visible = visible;
            ringRenderer.enabled = _visible && !_hidden;
        }

        private void LateUpdate()
        {
            if (_blink && _visible && !_hidden) Apply();
        }

        private void Apply()
        {
            _propertyBlock ??= new MaterialPropertyBlock();
            Color color = _color;
            float opacity = _blink ? Mathf.PingPong(Time.unscaledTime * 2f, 1f) : 1f;
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
