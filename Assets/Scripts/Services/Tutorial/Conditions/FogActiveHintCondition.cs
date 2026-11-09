using EmpireAtWar.Services.Vision;

namespace EmpireAtWar.Services.Tutorial
{
    public sealed class FogActiveHintCondition : IHintCondition
    {
        private readonly IVisionService _vision;

        public bool IsMet => _vision.FogEnabled;

        public FogActiveHintCondition(IVisionService vision)
        {
            _vision = vision;
        }

        public void Dispose()
        {
        }
    }
}
