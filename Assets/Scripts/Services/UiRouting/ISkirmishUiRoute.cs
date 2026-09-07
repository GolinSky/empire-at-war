using UnityEngine;

namespace EmpireAtWar.Services.UiRouting
{
    public enum SkirmishUiRoutePosition
    {
        MiniMap,
        Content,
        BuildPipeline,
        Economy,
        Reinforcement
    }

    public interface ISkirmishUiRoute
    {
        void Activate(bool isActive, Transform parentTransform);
    }

    public interface ISkirmishRouteNavigation
    {
        void RegisterRoute(
            SkirmishUiRoutePosition position,
            ISkirmishUiRoute route);

        void UnregisterRoute(
            SkirmishUiRoutePosition position,
            ISkirmishUiRoute route);

        void SetRouteActive(
            SkirmishUiRoutePosition position,
            bool isActive);
    }
}
