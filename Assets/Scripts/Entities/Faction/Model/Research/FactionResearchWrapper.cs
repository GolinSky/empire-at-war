using System;
using Utilities.ScriptUtils.EditorSerialization;

namespace EmpireAtWar.Models.Factions
{
    [Serializable]
    public class FactionResearchWrapper : DictionaryWrapper<ResearchType, ResearchLineData> {}
}
