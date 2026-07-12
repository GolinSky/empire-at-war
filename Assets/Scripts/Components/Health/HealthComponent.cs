using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Mvc;
using EmpireAtWar.ViewComponents.Health;
using UnityEngine;
using UnityEngine.UI;
using Utilities.ScriptUtils.Dotween;
using Utilities.ScriptUtils.EditorSerialization;
using Utilities.ScriptUtils.Time;
using Zenject;

namespace EmpireAtWar.Components.Ship.Health
{
    public interface IHealthComponent : IComponent
    {
        void ApplyDamage(float damage, WeaponType weaponType, int shipUnitId);
        bool Equal(IHealthModelObserver modelObserver);
        void SetMovementState(bool isMoving);
        bool Destroyed { get; }
        IHealthModelObserver HealthModelObserver { get; }
    }

    public class HealthComponent : MonoComponent<HealthModel>, IInitializable, ILateDisposable,
        IHealthComponent, ITickable
    {
        private static readonly Vector3 DefaultRotation = new(0, 180, 0);
        private const float TWEEN_DURATION = 0.1f;

        [field: SerializeField] public List<HardPointView> ShipUnits { get; set; }
        [SerializeField] private Canvas healthCanvas;
        [SerializeField] private Image shieldsFillImage;
        [SerializeField] private Image armorFillImage;
        [SerializeField] private ShieldView shieldView;
        [SerializeField] private DictionaryWrapper<PlayerType, Color> shieldColors;
        [SerializeField] private DictionaryWrapper<PlayerType, Color> hullColors;

        private ITimer _refreshShieldsTimer;
        private bool _isMoving;
        private float _originShieldValue;
        private Coroutine _shieldsAnimatedCoroutine;
        private Sequence _sequence;
        private float _baseShieldsValue;
        private float _baseArmorValue;
        private bool _isReleased;

        [Inject] private PlayerType PlayerType { get; }
        [InjectOptional] private IEntityLifecycle EntityLifecycle { get; }

        public bool Destroyed => Model.IsDestroyed;
        public IHealthModelObserver HealthModelObserver => Model;

        [Inject]
        private void Construct(HealthModel model)
        {
            SetModel(model);
        }

        public void Initialize()
        {
            Model.InjectDependency(ShipUnits);
            _originShieldValue = Model.Shields;
            _refreshShieldsTimer = TimerFactory.ConstructTimer(Model.ShieldRegenerateDelay);
            _baseShieldsValue = Model.Shields;
            _baseArmorValue = Model.Armor;

            Model.OnValueChanged += UpdateData;
            Model.OnDestroy += HandleDestroy;

            shieldsFillImage.color = shieldColors.Dictionary[PlayerType];
            armorFillImage.color = hullColors.Dictionary[PlayerType];

            if (shieldView != null)
            {
                _shieldsAnimatedCoroutine = StartCoroutine(AnimateShields());
            }
        }

        public void LateDispose()
        {
            Release();
        }

        public override void Release()
        {
            if (_isReleased)
            {
                return;
            }

            _isReleased = true;
            Model.OnValueChanged -= UpdateData;
            Model.OnDestroy -= HandleDestroy;
            healthCanvas.enabled = false;

            if (_shieldsAnimatedCoroutine != null)
            {
                StopCoroutine(_shieldsAnimatedCoroutine);
            }
        }

        public void ApplyDamage(float damage, WeaponType weaponType, int shipUnitId)
        {
            Model.ApplyDamage(damage, weaponType, _isMoving, shipUnitId);
        }

        public void SetMovementState(bool isMoving)
        {
            _isMoving = isMoving;
        }

        public bool Equal(IHealthModelObserver modelObserver)
        {
            return Model == modelObserver;
        }

        public void Tick()
        {
            healthCanvas.transform.rotation = Quaternion.Euler(DefaultRotation);

            if (!Model.IsLostShieldGenerator && Model.Shields < _originShieldValue &&
                _refreshShieldsTimer.IsComplete)
            {
                _refreshShieldsTimer.StartTimer();
                //Model.RegenerateShields(Model.ShieldRegenerateValue);
            }
        }

        public IHardPointView[] GetShipUnits(HardPointType hardPointType)
        {
            List<HardPointView> currentShipUnits = ShipUnits.Where(x => !x.IsDestroyed).ToList();

            if (currentShipUnits.Count == 0)
            {
                return null;
            }

            if (hardPointType == HardPointType.Any ||
                currentShipUnits.All(x => x.HardPointType != hardPointType))
            {
                return currentShipUnits.ToArray();
            }

            return currentShipUnits.Where(x => x.HardPointType == hardPointType).ToArray();
        }

        private void HandleDestroy()
        {
            EntityLifecycle?.Release();
            Release();
        }

        private IEnumerator AnimateShields()
        {
            while (!Model.IsDestroyed && !Model.IsLostShieldGenerator)
            {
                if (shieldView.IsVisibleToCamera && Model.Shields > 0)
                {
                    shieldView.AnimateTextureOffset();
                }

                yield return new WaitForEndOfFrame();
            }
        }

        private void UpdateData()
        {
            if (shieldView != null)
            {
                shieldView.SetActive(Model.Shields > 0f);
            }

            _sequence.KillIfExist();
            _sequence = DOTween.Sequence();
            _sequence.Append(shieldsFillImage.DOFillAmount(Model.Shields / _baseShieldsValue, TWEEN_DURATION));
            _sequence.Append(armorFillImage.DOFillAmount(Model.Armor / _baseArmorValue, TWEEN_DURATION));
        }
    }
}
