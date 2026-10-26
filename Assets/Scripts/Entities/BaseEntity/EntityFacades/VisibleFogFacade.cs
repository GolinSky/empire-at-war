using System;

namespace EmpireAtWar.Entities.BaseEntity.EntityFacades
{
    public sealed class VisibleFogFacade : IFogVisibilityFacade
    {
        public event Action<bool> VisibilityChanged { add { } remove { } }
        public bool IsHiddenByFog => false;
    }
}
