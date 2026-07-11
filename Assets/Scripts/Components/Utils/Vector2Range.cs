using System;
using UnityEngine;
using Utilities.ScriptUtils.Math;

namespace EmpireAtWar.Models.SkirmishCamera
{
    [Serializable]
    public class Vector2Range:Range<Vector2>
    {
        public override bool IsInRange(Vector2 value)
        {
            if (value.x < Max.x && value.x > Min.x  && value.y < Max.y && value.y > Min.y )
            {
                return true;
            }

            return false;
        }

        public override Vector2 Clamp(Vector2 value)
        {
            value.x = Mathf.Clamp(value.x, Min.x, Max.x);
            value.y = Mathf.Clamp(value.y, Min.y, Max.y);
            return value;
        }
    }
}