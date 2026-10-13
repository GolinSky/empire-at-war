using EmpireAtWar.Components.Ui.Tooltip;
using System;
using System.Collections.Generic;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Ui.Base;
using UnityEngine;

namespace EmpireAtWar.Entities.Faction.Ui
{
    public interface IShipBuildPresenter
    {
        void CancelBuilding(string id);
    }
}
