using EmpireAtWar.Controllers.Factions;
using MPUIKIT;
using UnityEngine;

namespace EmpireAtWar.Components.Ui
{
    internal static class HeroCardPalette
    {
        public static void ApplyBackground(MPImage background, UnitRequest request)
        {
            if (!request.FactionData.IsHero) return;

            switch (request)
            {
                case SquadronUnitRequest:
                    background.color = new Color32(36, 77, 78, 255);
                    break;
                case ShipUnitRequest:
                    background.color = new Color32(99, 57, 46, 255);
                    break;
                default:
                    return;
            }

            GradientEffect gradient = background.GradientEffect;
            gradient.Enabled = false;
            background.GradientEffect = gradient;
        }
    }
}
