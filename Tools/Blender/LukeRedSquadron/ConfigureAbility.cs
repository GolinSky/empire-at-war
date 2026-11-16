using System.Linq;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Services.ShipAbilities;
using EmpireAtWar.Services.ShipAbilities.Abilities;
using UnityEditor;
using UnityEngine;

public static class LukeAbilityConfiguration
{
    public static object Apply()
    {
        var weapons = AssetDatabase.LoadAssetAtPath<WeaponsData>("Assets/Settings/Data/Models/Weapon/WeaponsData.asset");
        var catalog = new SerializedObject(AssetDatabase.LoadAssetAtPath<ShipAbilityCatalog>("Assets/Settings/Data/Models/ShipAbilities/ShipAbilityCatalog.asset"));
        var definitions = catalog.FindProperty("definitions.keyValue");
        SerializedProperty entry = null;
        for (int i = 0; i < definitions.arraySize; i++)
            if (definitions.GetArrayElementAtIndex(i).FindPropertyRelative("key").intValue == (int)ShipAbilityId.LuckyShot)
                entry = definitions.GetArrayElementAtIndex(i);
        if (entry == null) { int index = definitions.arraySize++; entry = definitions.GetArrayElementAtIndex(index); }
        entry.FindPropertyRelative("key").intValue = (int)ShipAbilityId.LuckyShot;
        var definition = entry.FindPropertyRelative("value");
        definition.FindPropertyRelative("icon").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Textures/Ui/Icons/ShipAbilities/TorpedoSalvoIcon.png");
        definition.FindPropertyRelative("displayName").stringValue = "Lucky Shot";
        definition.FindPropertyRelative("description").stringValue = "Luke makes a 1.25 s attack run, then fires a precision proton strike for 4,000 base hull damage, bypassing shields. Targets capital ships and structures only. Move/Stop cancels preparation. 60 s cooldown.";
        definition.FindPropertyRelative("duration").floatValue = 0f;
        definition.FindPropertyRelative("recoveryDelay").floatValue = 60f;
        definition.FindPropertyRelative("canCancel").boolValue = false;
        definition.FindPropertyRelative("isToggle").boolValue = false;
        definition.FindPropertyRelative("requiresEnemyTarget").boolValue = true;
        definition.FindPropertyRelative("range").floatValue = 350f;
        definition.FindPropertyRelative("aiUse").intValue = 2;
        definition.FindPropertyRelative("autoPower").intValue = 0;
        var settings = definition.FindPropertyRelative("settings");
        settings.managedReferenceValue = new LuckyShotSettings();
        settings.FindPropertyRelative("damage").floatValue = 4000f;
        settings.FindPropertyRelative("windup").floatValue = 1.25f;
        var projectile = settings.FindPropertyRelative("projectile");
        projectile.boxedValue = JsonUtility.FromJson<WeaponProfile>(JsonUtility.ToJson(weapons.GetProfile(WeaponType.XWingProtonTorpedo)));
        projectile.FindPropertyRelative("damageType").intValue = (int)DamageType.PrecisionStrike;
        projectile.FindPropertyRelative("damage").floatValue = 4000f;
        projectile.FindPropertyRelative("shotsPerSalvo").intValue = 1;
        projectile.FindPropertyRelative("shotInterval").floatValue = 0f;
        projectile.FindPropertyRelative("reload").floatValue = 60f;
        projectile.FindPropertyRelative("range").floatValue = 350f;
        projectile.FindPropertyRelative("projectileSpeed").floatValue = 220f;
        projectile.FindPropertyRelative("interceptable").boolValue = false;
        catalog.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(catalog.targetObject);

        var matrix = new SerializedObject(AssetDatabase.LoadAssetAtPath<DamageMatrixData>("Assets/Settings/Data/Models/Weapon/DamageMatrixData.asset"));
        var types = matrix.FindProperty("damageTypes");
        SerializedProperty precision = null;
        for (int i = 0; i < types.arraySize; i++)
            if (types.GetArrayElementAtIndex(i).FindPropertyRelative("damageType").intValue == (int)DamageType.PrecisionStrike)
                precision = types.GetArrayElementAtIndex(i);
        if (precision == null) { int index = types.arraySize++; precision = types.GetArrayElementAtIndex(index); }
        precision.FindPropertyRelative("damageType").intValue = (int)DamageType.PrecisionStrike;
        foreach (string group in new[] { "damage", "accuracy" })
            foreach (string shipClass in new[] { "fighter", "bomber", "interceptor", "corvette", "frigate", "cruiser", "capital", "heavyCapital", "structure", "heroStrikecraft" })
                precision.FindPropertyRelative(group + "." + shipClass).floatValue = new[] { "capital", "heavyCapital", "structure" }.Contains(shipClass) ? 1f : 0f;
        precision.FindPropertyRelative("vsShield").floatValue = 0f;
        precision.FindPropertyRelative("shipShieldMultiplier").floatValue = 1f;
        precision.FindPropertyRelative("squadronShieldMultiplier").floatValue = 1f;
        precision.FindPropertyRelative("shieldPiercing").boolValue = true;
        matrix.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(matrix.targetObject);
        AssetDatabase.SaveAssets();
        return new { ability = "LuckyShot", damage = 4000, windup = 1.25, range = 350, recovery = 60 };
    }
}
