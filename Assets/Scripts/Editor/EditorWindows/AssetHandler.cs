using EmpireAtWar.Editor.EditorWindows.ShipModelEditor;
using EmpireAtWar.Ship;
using UnityEditor;

namespace EmpireAtWar.Editor.EditorWindows
{
    public class AssetHandler
    {
        public static bool OpenEditor(int instanceId, int line)
        {
            ShipComponentsData model = EditorUtility.InstanceIDToObject(instanceId) as ShipComponentsData;
            if (model != null)
            {
                ShipModelEditorWindow.Open(model);
                return true;
            }
            return false;
        }
    }
    
}