using EmpireAtWar.Models.Selection;
using UnityEngine;

namespace EmpireAtWar.ViewComponents.Weapon
{
    public sealed class GravityWellView : MonoBehaviour
    {
        private const float PLANE_Y = -2f;
        private static readonly int COLOR_ID = Shader.PropertyToID("_BaseColor");

        [SerializeField] private MeshRenderer ring;
        [SerializeField] private Color friendlyColor = new Color(0.26f, 0.82f, 1f, 0.85f);
        [SerializeField] private Color hostileColor = new Color(1f, 0.28f, 0.32f, 0.85f);

        private MaterialPropertyBlock _properties;
        private Transform _caster;
        private ISelectionModelObserver _selection;

        public void Show(Transform caster, float radius, bool hostile, ISelectionModelObserver selection)
        {
            _caster = caster;
            _selection = selection;
            _selection.OnSelected += HandleSelected;
            transform.localScale = new Vector3(radius, 1f, radius);

            _properties ??= new MaterialPropertyBlock();
            _properties.SetColor(COLOR_ID, hostile ? hostileColor : friendlyColor);
            ring.SetPropertyBlock(_properties);

            HandleSelected(_selection.IsSelected);
            gameObject.SetActive(true);
        }

        private void HandleSelected(bool isSelected)
        {
            if (isSelected) LateUpdate();
            ring.enabled = isSelected;
            enabled = isSelected;
        }

        private void LateUpdate()
        {
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
