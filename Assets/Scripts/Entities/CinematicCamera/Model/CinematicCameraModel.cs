using EmpireAtWar.Mvc;

namespace EmpireAtWar.Entities.CinematicCamera.Model
{
    public class CinematicCameraModel : PureModel, ICinematicCameraModelObserver
    {
        public bool IsActive { get; private set; }

        public void SetActive(bool isActive)
        {
            IsActive = isActive;
        }
    }
}
