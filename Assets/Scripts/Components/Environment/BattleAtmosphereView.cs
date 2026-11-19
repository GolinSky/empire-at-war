using EmpireAtWar.Entities.Map;
using UnityEngine;
using UnityEngine.Rendering;

namespace EmpireAtWar.Components.Environment
{
    public sealed class BattleAtmosphereView : MonoBehaviour
    {
        [SerializeField] private Light sun;
        [SerializeField] private Volume gradeVolume;

        public void Apply(PlanetAtmosphere atmosphere)
        {
            sun.color = atmosphere.SunColor;
            sun.intensity = atmosphere.SunIntensity;
            RenderSettings.ambientSkyColor = atmosphere.AmbientSky;
            RenderSettings.ambientEquatorColor = atmosphere.AmbientEquator;
            RenderSettings.ambientGroundColor = atmosphere.AmbientGround;
            gradeVolume.sharedProfile = atmosphere.Grade;
        }
    }
}
