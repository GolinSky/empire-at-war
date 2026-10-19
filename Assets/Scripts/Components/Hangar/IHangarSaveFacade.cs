using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.Services.Saves;

namespace EmpireAtWar.Components.Hangar
{
    public interface IHangarSaveFacade : IEntityFacade
    {
        HangarSave Capture();

        /// <param name="squadrons">Restored squadrons by saved id; launched ones missing from it are not taken back.</param>
        void Restore(HangarSave save, IReadOnlyDictionary<long, Squadron> squadrons);
    }
}
