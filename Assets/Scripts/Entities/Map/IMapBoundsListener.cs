using EmpireAtWar.Models.SkirmishCamera;

namespace EmpireAtWar.Entities.Map
{
    public interface IMapBoundsListener
    {
        void OnBoundsChanged(Vector2Range bounds);
    }
}
