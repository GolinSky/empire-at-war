using System.Collections.Generic;
using EmpireAtWar.Components.Squadrons.Icon;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Squadrons")]
    public sealed class SquadronClusterLayoutTests
    {
        private const float PITCH = 84f;

        [TestCase(1, 1, 1)]
        [TestCase(2, 2, 1)]
        [TestCase(3, 2, 2)]
        [TestCase(4, 2, 2)]
        [TestCase(5, 3, 2)]
        [TestCase(9, 3, 3)]
        [TestCase(10, 4, 3)]
        [TestCase(16, 4, 4)]
        [TestCase(17, 4, 5)]
        public void DimensionsFollowTheSquareRootCappedAtFourColumns(int count, int columns, int rows)
        {
            Assert.That(SquadronClusterLayout.Columns(count), Is.EqualTo(columns));
            Assert.That(SquadronClusterLayout.Rows(count), Is.EqualTo(rows));
        }

        [Test]
        public void EveryCountFillsDistinctCells()
        {
            for (int count = 1; count <= 20; count++)
            {
                var cells = new HashSet<Vector2>();
                for (int rank = 0; rank < count; rank++)
                    Assert.That(cells.Add(SquadronClusterLayout.CellOffset(rank, count, PITCH)), Is.True,
                        $"count {count} rank {rank}");
            }
        }

        [Test]
        public void OneSquadronSitsAtTheGridCenter()
        {
            Assert.That(SquadronClusterLayout.CellOffset(0, 1, PITCH), Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void FiveSquadronsFormTwoRowsWithTheLastRowCentered()
        {
            Vector2 first = SquadronClusterLayout.CellOffset(0, 5, PITCH);
            Vector2 third = SquadronClusterLayout.CellOffset(2, 5, PITCH);
            Vector2 fourth = SquadronClusterLayout.CellOffset(3, 5, PITCH);
            Vector2 fifth = SquadronClusterLayout.CellOffset(4, 5, PITCH);
            Assert.That(third.x - first.x, Is.EqualTo(2f * PITCH));
            Assert.That(fourth.y, Is.EqualTo(first.y - PITCH), "Row-major: the fourth cell starts row two.");
            Assert.That((fourth.x + fifth.x) * 0.5f, Is.Zero.Within(0.001f), "Last row is centered.");
            Assert.That(fifth.x - fourth.x, Is.EqualTo(PITCH));
        }

        [Test]
        public void GridIsCenteredHorizontally()
        {
            for (int count = 1; count <= 16; count++)
            {
                float sum = 0f;
                for (int rank = 0; rank < count; rank++) sum += SquadronClusterLayout.CellOffset(rank, count, PITCH).x;
                Assert.That(sum, Is.Zero.Within(0.01f), $"count {count}");
            }
        }
    }
}
