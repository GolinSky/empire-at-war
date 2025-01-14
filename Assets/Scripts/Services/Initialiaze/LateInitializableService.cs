using System.Collections.Generic;
using Zenject;

namespace EmpireAtWar.Services.Initialiaze
{
    public class LateInitializableService: IInitializable
    {
        private IEnumerable<ILateIInitializable> _lateInitializes;

        public LateInitializableService(IEnumerable<ILateIInitializable> lateInitializes)
        {
            _lateInitializes = lateInitializes;
        }
        
        public void Initialize()
        {
            foreach (ILateIInitializable lateInitialize in _lateInitializes)
            {
                lateInitialize.LateInitialize();
            }
        }
    }
}