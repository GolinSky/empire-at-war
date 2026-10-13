using EmpireAtWar.Entities.Reinforcement;
using EmpireAtWar.Components.Ui.Tooltip;
using EmpireAtWar.Controllers.Factions;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EmpireAtWar
{
    public interface ISpawnShipUi
    {
        UnitRequest Request { get; }

        void DecreaseUnitCount();

        void AddUnit();

        void Activate(bool isActive);

        void Init(IReinforcementVisitor reinforcementVisitor, UnitRequest request, ScrollRect scrollRect);
    }
}
