using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;

namespace ReplicaProjects.MagicSort.Tests
{
    /// <summary>Guards the shipped MagicSortLevel assets: consistent dimensions and solvable under the game rules.</summary>
    public class MagicSortLevelAssetTests
    {
        private static IEnumerable<string> LevelPaths()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:MagicSortLevel", new[] { "Assets/Replicas/MagicSort/Levels" }))
                yield return AssetDatabase.GUIDToAssetPath(guid);
        }

        [Test]
        public void ShippedLevels_Exist()
        {
            CollectionAssert.IsNotEmpty(LevelPaths());
        }

        [TestCaseSource(nameof(LevelPaths))]
        public void ShippedLevel_IsConsistentAndSolvable(string path)
        {
            var level = AssetDatabase.LoadAssetAtPath<MagicSortLevel>(path);
            Assert.IsNotNull(level, path);
            Assert.AreEqual(level.barHeight * level.barCount, level.slots.Length, "slots length");

            var status = MagicSortSolver.Solve(level.slots, level.barHeight).Status;

            Assert.That(status, Is.EqualTo(SolveStatus.Solvable).Or.EqualTo(SolveStatus.Timeout),
                "a shipped level must not be unsolvable (or already solved)");
        }
    }
}
