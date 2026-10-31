using EmpireAtWar.Components.Radar;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.BaseEntity.Orders;

namespace EmpireAtWar.Entities.Ship.EntityFacades
{
    public sealed class ShipCombatObserverFacade : IUnitCombatObserverFacade
    {
        private readonly IWeaponComponent _weapon;
        private readonly IRadarModelObserver _radar;
        private readonly UnitOrderModel _orders;

        public float WeaponRange => _weapon.AttackDistance;
        public float RadarRange => _radar.Range;
        public IEntity Target
        {
            get
            {
                if (_orders.Target != null) return _orders.Target;
                for (int i = 0; i < _radar.Enemies.Count; i++)
                {
                    IEntity contact = _radar.Enemies[i];
                    if (!contact.HealthModel.IsDestroyed && contact.HealthModel.HasUnits && IsAttacking(contact))
                        return contact;
                }
                return null;
            }
        }

        public bool IsAttacking(IEntity target) =>
            ReferenceEquals(_orders.Target, target) || _weapon.HasTarget(target.GetFacade<IHealthFacade>());

        public ShipCombatObserverFacade(IWeaponComponent weapon, IRadarModelObserver radar, UnitOrderModel orders)
        {
            _weapon = weapon;
            _radar = radar;
            _orders = orders;
        }
    }
}
