using System;
using System.Collections.Generic;
using EmpireAtWar.Models.Factions;
using UnityEngine;
using Utilities.ScriptUtils.EditorSerialization;

namespace EmpireAtWar.Models.Audio
{
    [Serializable]
    public class MusicData:DictionaryWrapper<FactionType, List<AudioClip>> {}
}