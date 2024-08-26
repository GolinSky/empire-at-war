using EmpireAtWar.Editor.EditorWindows.ShipModelEditor;
using EmpireAtWar.Models.Ship;
using UnityEditor;

namespace EmpireAtWar.Editor.EditorWindows
{
    public class AssetHandler
    {
        public static bool OpenEditor(int instanceId, int line)
        {
            ShipModel model = EditorUtility.InstanceIDToObject(instanceId) as ShipModel;
            if (model != null)
            {
                ShipModelEditorWindow.Open(model);
                return true;
            }
            return false;
        }
    }
    
}