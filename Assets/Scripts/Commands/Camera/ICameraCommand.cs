using LightWeightFramework.Command;

namespace EmpireAtWar.Commands.Camera
{
    public interface ICameraCommand:ICommand
    {
        void ZoomIn();
        void ZoomOut();
        void MaxZoomIn();
        void MaxZoomOut();
    }
}