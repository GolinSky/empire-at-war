using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Editor.Rendering;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
    /// <summary>
    /// Imported models keep their collision/shadow/shield/LOD helpers in the FBX; unit prefabs must not.
    /// Fix failures with Tools/Rendering/Strip Helper Meshes From Unit Prefabs.
    /// </summary>
    public sealed class UnitHelperMeshTests
    {
        [Test]
        public void UnitPrefabs_ContainNoStrippableHelperMeshes()
        {
            List<UnitHelperMeshStripper.Helper> strippable = UnitHelperMeshStripper
                .FindHelpers(UnitHelperMeshStripper.FindUnitPrefabPaths(), new string[0])
                .Where(helper => helper.BlockedBy == null)
                .ToList();

            Assert.That(strippable, Is.Empty, UnitHelperMeshStripper.Report(strippable));
        }
    }
}
