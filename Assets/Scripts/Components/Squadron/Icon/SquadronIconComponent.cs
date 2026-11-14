using EmpireAtWar.Components.Combat;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Models.Selection;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.Player;
using EmpireAtWar.Services.Vision;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace EmpireAtWar.Components.Squadrons.Icon
{
    public sealed class SquadronIconComponent : MonoComponent<SelectionModel>, IInitializable, IRespawnable
    {
        [SerializeField] private RectTransform iconRoot;
        [SerializeField] private Image frameImage;
        [SerializeField] private Image iconImage;
        [SerializeField, Min(1f)] private float screenSize = 72f;
        [SerializeField, Min(1f)] private float clickSize = 132f;
        [SerializeField] private float screenOffset = 60f;
        [SerializeField, Min(0.01f)] private float followSharpness = 12f;

        private readonly SquadronMarkerAnchor _anchor = new SquadronMarkerAnchor();
        private BattleMarker _marker;
        private CombatModifiers _modifiers;
        private IFogVisibilityFacade _fog;
        private ILocalPlayer _localPlayer;
        private IVisionService _vision;
        private IPlayerColors _colors;
        private PlayerId _owner;
        private long _entityId;
        private bool _isReleased = true;

        [Inject]
        private void Construct(IVisionService vision, ILocalPlayer localPlayer, SelectionModel model,
            PlayerId owner, IPlayerColors colors, IBattleMarkerLayer layer, long entityId, CombatModifiers modifiers,
            IFogVisibilityFacade fog)
        {
            SetModel(model);
            _vision = vision;
            _localPlayer = localPlayer;
            _owner = owner;
            _colors = colors;
            _entityId = entityId;
            _modifiers = modifiers;
            _fog = fog;
            _marker = new BattleMarker(layer, iconRoot, frameImage, iconImage, screenSize, clickSize, screenOffset, true);
            _marker.SampleVisibility = RefreshVisibility;
        }

        public void Initialize()
        {
            _isReleased = false;
            _anchor.Reset(transform.position);
            _marker.ResetPosition(transform.position);
            _marker.Register(_entityId, _owner);
            _modifiers.CloakChanged += RefreshVisibility;
            _fog.VisibilityChanged += HandleFogVisibility;
            Model.OnSelected += UpdateColor;
            RefreshVisibility();
            UpdateColor(Model.IsSelected);
        }

        public void Respawn() => Initialize();

        public void AdvanceAnchor(Vector3 centroid, Vector3 intent, float cruiseSpeed, float loiterRadius,
            bool travelling, bool fighting, float deltaTime)
        {
            if (_isReleased || deltaTime <= 0f) return;

            if (_anchor.Advance(centroid, intent, cruiseSpeed, loiterRadius, travelling, fighting, followSharpness, deltaTime))
                _marker.ResetPosition(centroid);
            _marker.SetPosition(_anchor.Position);
            _marker.Velocity = _anchor.Velocity;
        }

        public override void Release()
        {
            if (_isReleased) return;
            _isReleased = true;
            _modifiers.CloakChanged -= RefreshVisibility;
            _fog.VisibilityChanged -= HandleFogVisibility;
            Model.OnSelected -= UpdateColor;
            _marker.Unregister();
        }

        private void RefreshVisibility()
        {
            _marker.SetVisible(_localPlayer.IsFriendly(_owner) ||
                !_modifiers.IsCloaked && !_fog.IsHiddenByFog && _vision.IsVisible(_localPlayer.Id, transform.position));
        }

        private void HandleFogVisibility(bool visible)
        {
            if (!visible) _marker.SetVisible(false);
        }

        private void UpdateColor(bool isSelected) => _marker.SetColor(_colors.GetColor(_owner), isSelected);

        private void OnDestroy()
        {
            if (_marker == null) return;
            Release();
            _marker.Dispose();
        }
    }
}
