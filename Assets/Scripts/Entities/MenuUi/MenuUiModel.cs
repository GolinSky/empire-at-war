using EmpireAtWar.Mvc;

namespace EmpireAtWar.Entities.MenuUi
{
    public interface IMenuUiModel : IModelObserver
    {
    }

    public class MenuUiModel : PureModel, IMenuUiModel
    {
        
    }
}