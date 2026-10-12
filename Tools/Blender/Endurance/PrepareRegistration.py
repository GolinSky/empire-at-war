"""Adapt the existing complete roster/Addressables/icon registration workflow."""
from pathlib import Path

source = Path('Tools/Blender/MC75Profundity/Register.cs').read_text()
source = source.replace('MC75Profundity', 'Endurance').replace('MC75', 'Endurance')
source = source.replace('SHIP_ID=304', 'SHIP_ID=322').replace('ABILITY_ID=19', 'ABILITY_ID=52')
source = source.replace('"Capital ships","Assets/Art/Textures/Ui/Icons/ShipIcon/VictoryIcon.png"', '"Squadrons","Assets/Art/Textures/Ui/Icons/SquadronIcon/TIEBomberIcon.png"')
source = source.replace('"Frigates","Assets/Art/Textures/Ui/Icons/ShipIcon/ArquitensIcon.png"', '"Corvettes","Assets/Art/Textures/Ui/Icons/ShipIcon/CorellianCorvetteIcon.png"')
source = source.replace('"Bomber strikes","Assets/Art/Textures/Ui/Icons/SquadronIcon/TIEBomberIcon.png"', '"Heavy capital ships","Assets/Art/Textures/Ui/Icons/ShipIcon/VictoryIcon.png"')
start = source.index('        value.FindPropertyRelative("description").stringValue=')
end = source.index('        value.FindPropertyRelative("iconKey")', start)
source = source[:start] + '''        value.FindPropertyRelative("description").stringValue="Fleet carrier with six active and six replacement X-Wing squadrons, four medium turbolasers, two medium ion cannons and six point-defense mounts. Fighter Coordination boosts nearby allied squadron accuracy and speed.";
        value.FindPropertyRelative("role").stringValue="Fleet carrier / Squadron support";
''' + source[end:]
source = source.replace('"Endurance Profundity"', '"Endurance Fleet Carrier"')
source = source.replace('"<AvailableLevel>k__BackingField").intValue=3', '"<AvailableLevel>k__BackingField").intValue=4')
source = source.replace('intValue=13000', 'intValue=6000').replace('intValue=260', 'intValue=65')
source = source.replace('new FullSalvoSettings()', 'new FighterCoordinationSettings()')
source = source.replace('"Full Salvo"', '"Fighter Coordination"')
source = source.replace('"For 20 seconds, missiles, rockets and torpedoes fire about three times faster, while other weapons fire at one-third their normal rate. Recharge: 60 seconds after the salvo ends."', '"Nearby allied squadrons gain +20% accuracy and +15% movement speed for 10 seconds. Command radius: 750. Recovery: 45 seconds."')
source = source.replace('floatValue=20;value.FindPropertyRelative("recoveryDelay").floatValue=60', 'floatValue=10;value.FindPropertyRelative("recoveryDelay").floatValue=45')
start = source.index('        var weapons=Load(')
end = source.index('        var abilitySounds=audio.FindProperty', start)
source = source[:start] + '        var audio=Load("Assets/Settings/Data/Models/Audio/ShipSfxData.asset");\n' + source[end:]
start = source.index('        Save(audio);AssetDatabase.SaveAssets();return ')
end = source.index('\n    static SerializedObject Load', start)
source = source[:start] + '        Save(audio);AssetDatabase.SaveAssets();return "Endurance=322 registered: Rebellion roster, ship data/view Addressables, preview, three icon consumers, matchups, Fighter Coordination=52 and audio.";\n    }\n' + source[end:]
Path('Tools/Blender/Endurance/Register.cs').write_text(source)
