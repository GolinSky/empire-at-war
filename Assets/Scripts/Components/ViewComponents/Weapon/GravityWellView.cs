using EmpireAtWar.Models.Selection;
using UnityEngine;

namespace EmpireAtWar.ViewComponents.Weapon
{
    public sealed class GravityWellView : MonoBehaviour
    {
        private const int SEGMENTS = 96;
        private const float PLANE_Y = -2f;
        private static readonly int COLOR_ID = Shader.PropertyToID("_Color");
        private static readonly int BRIGHTNESS_ID = Shader.PropertyToID("_Brightness");
        private static readonly int ROUNDNESS_ID = Shader.PropertyToID("_Roundness");

        [SerializeField] private LineRenderer ring;
        [SerializeField] private Color friendlyColor = new Color(0.26f, 0.82f, 1f, 0.85f);
        [SerializeField] private Color hostileColor = new Color(1f, 0.28f, 0.32f, 0.85f);
        [SerializeField] private float brightness = 2.5f;

        private MaterialPropertyBlock _properties;
        private Transform _caster;
        private ISelectionModelObserver _selection;
        private float _radius = -1f;

        public void Show(Transform caster, float radius, bool hostile, ISelectionModelObserver selection)
        {
            _caster = caster;
            _selection = selection;
            _selection.OnSelected += HandleSelected;
            ring.enabled = _selection.IsSelected;
            if (_radius != radius)
            {
                _radius = radius;
                ring.positionCount = SEGMENTS;
                for (int i = 0; i < SEGMENTS; i++)
                {
                    float angle = i * Mathf.PI * 2f / SEGMENTS;
                    ring.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius));
                }
            }

            _properties ??= new MaterialPropertyBlock();
            _properties.Clear();
            _properties.SetColor(COLOR_ID, hostile ? hostileColor : friendlyColor);
            _properties.SetFloat(BRIGHTNESS_ID, brightness);
            _properties.SetFloat(ROUNDNESS_ID, 0f);
            ring.SetPropertyBlock(_properties);
            SyncPosition();
            gameObject.SetActive(true);
        }

        private void HandleSelected(bool isSelected) => ring.enabled = isSelected;

        private void LateUpdate() => SyncPosition();

        private void SyncPosition()
        {
            if (_caster == null) return;
            Vector3 pos = _caster.position;
            transform.position = new Vector3(pos.x, PLANE_Y, pos.z);
        }

        public void Hide()
        {
            _caster = null;
            if (_selection != null)
            {
                _selection.OnSelected -= HandleSelected;
                _selection = null;
            }
            if (this != null) gameObject.SetActive(false);
        }
    }
}
