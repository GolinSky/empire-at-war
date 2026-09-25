using System;
using EmpireAtWar.Entities.Squadrons;
using Utilities.ScriptUtils.EditorSerialization;

namespace EmpireAtWar.Models.Factions
{
    [Serializable]
    public class SquadronFactionDataWrapper : DictionaryWrapper<SquadronType, FactionData> {}
}
