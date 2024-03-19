using EmpireAtWar.Models.Movement;
using LightWeightFramework.Components.ViewComponents;

namespace EmpireAtWar.ViewComponents.Move
{
    public class SimpleMoveViewComponent:ViewComponent<ISimpleMoveModelObserver>
    {
        protected override void OnInit()
        {
            base.OnInit();
            transform.position = Model.Position;
        }
    }
}