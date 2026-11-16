using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.ShipUi;
using UnityEngine;

namespace EmpireAtWar.Entities.ControlGroups
{
    public sealed class ControlGroupIconSelector
    {
        private readonly FactionCatalog _factions;
        private readonly IShipIconProvider _icons;
        private readonly Dictionary<UnitTypeId, FactionData> _data = new Dictionary<UnitTypeId, FactionData>();

        public ControlGroupIconSelector(FactionCatalog factions, IShipIconProvider icons)
        {
            _factions = factions;
            _icons = icons;
        }

        public void Select(IReadOnlyList<IEntity> members, out Sprite first, out Sprite second)
        {
            IEntity firstMember = null;
            IEntity secondMember = null;
            for (int index = 0; index < members.Count; index++)
            {
                IEntity member = members[index];
                UnitTypeId type = member.GetFacade<IUnitTypeFacade>().UnitTypeId;
                if (firstMember != null && type == Type(firstMember) ||
                    secondMember != null && type == Type(secondMember)) continue;
                if (firstMember == null || Compare(member, firstMember) < 0)
                {
                    secondMember = firstMember;
                    firstMember = member;
                }
                else if (secondMember == null || Compare(member, secondMember) < 0)
                    secondMember = member;
            }
            first = firstMember == null ? null : Icon(Type(firstMember));
            second = secondMember == null ? null : Icon(Type(secondMember));
        }

        private int Compare(IEntity left, IEntity right)
        {
            UnitTypeId leftType = Type(left);
            UnitTypeId rightType = Type(right);
            return ControlGroupIconPriority.Compare(leftType, left.HealthModel.ShipClass, Data(leftType).IsHero,
                rightType, right.HealthModel.ShipClass, Data(rightType).IsHero);
        }

        private FactionData Data(UnitTypeId type)
        {
            if (_data.TryGetValue(type, out FactionData data)) return data;
            data = type.IsShip ? _factions.GetShipFactionData(type.ShipType) :
                _factions.GetSquadronFactionData(type.SquadronType);
            _data.Add(type, data);
            return data;
        }

        private Sprite Icon(UnitTypeId type) => type.IsShip
            ? _icons.GetShipIcon(type.ShipType) : _icons.GetSquadronIcon(type.SquadronType);

        private static UnitTypeId Type(IEntity entity) => entity.GetFacade<IUnitTypeFacade>().UnitTypeId;
    }
}
