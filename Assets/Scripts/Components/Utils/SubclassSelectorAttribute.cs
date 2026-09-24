using UnityEngine;

namespace EmpireAtWar.Utils
{
    /// <summary>
    /// Draws a type dropdown for a [SerializeReference] field so a concrete subclass can be picked in the Inspector.
    /// </summary>
    public sealed class SubclassSelectorAttribute : PropertyAttribute
    {
    }
}
