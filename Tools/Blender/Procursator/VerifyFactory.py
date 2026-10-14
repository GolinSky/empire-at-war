from pathlib import Path
s=Path('Assets/Scripts/Tests/Editor/RealEntityCompositionTests.cs').read_text(encoding='utf-8-sig')
s=s.replace('RealEntityCompositionTests','ProcursatorCompositionCheck').replace('ShipType.Acclamator','ShipType.Procursator').replace('ShipType.Arquitens','ShipType.Procursator')
s+='''
public static class ProcursatorCompositionRunner
{
 public static string Main()
 {
  var test=new EmpireAtWar.Tests.Editor.ProcursatorCompositionCheck();
  try
  {
   test.SetUp();
   test.ActualScopes_AllSevenUnitFactoriesBuildForLocalAndAiOwnersAndPoolWithoutRebuilding(EmpireAtWar.Entities.Game.BattleFeatures.All, EmpireAtWar.Entities.Game.PlayerRole.AiOpponent);
  }
  finally { test.TearDown(); }
  System.IO.File.WriteAllText("Tools/Blender/Procursator/Evidence/Composition.json","{\\"success\\":true,\\"scenario\\":\\"Actual project/battle/local and AI scopes spawn Procursator; health, voice, independent state and teardown verified\\"}");
  return "Procursator composition passed for actual local and AI player scopes.";
 }
}
'''
Path('Temp/ProcursatorCompositionCheck.cs').write_text(s,encoding='utf-8')
