using System;
using System.Collections.Generic;
using EmpireAtWar.Models.Factions;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Utilities.ScriptUtils.EditorSerialization;

namespace EmpireAtWar.Models.Audio
{
    [Serializable]
    public class MusicData:DictionaryWrapper<FactionType, List<AssetReferenceT<AudioClip>>> {}
}