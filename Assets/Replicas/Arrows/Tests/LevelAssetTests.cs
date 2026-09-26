using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;

namespace ReplicaProjects.Arrows.Tests
{
    /// <summary>
    /// Guards the shipped LevelData assets: they must still deserialize after type changes
    /// (Vector2Int -> GridCoord keeps the {x, y} layout) and must be valid, solvable boards.
    /// </summary>
    public class LevelAssetTests
    {
        private static IEnumerable<string> LevelPaths()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:LevelData", new[] { "Assets/Replicas/Arrows/Levels" }))
                yield return AssetDatabase.GUIDToAssetPath(guid);
        }

        [Test]
        public void ShippedLevels_Exist()
        {
            CollectionAssert.IsNotEmpty(LevelPaths());
        }

        [TestCaseSource(nameof(LevelPaths))]
        public void ShippedLevel_DeserializesAndIsValid(string path)
        {
            var level = AssetDatabase.LoadAssetAtPath<LevelData>(path);
            Assert.IsNotNull(level, path);
            CollectionAssert.IsNotEmpty(level.heads, "heads lost on deserialization");

            var result = new BoardValidator().ValidateBoard(new BoardValidateInput
            {
                width = level.width,
                height = level.height,
                heads = level.heads
            });

            Assert.IsTrue(result.isValid,
                string.Join(", ", result.violations.ConvertAll(v => $"{v.coordinates} {v.reason}")));
        }
    }
}
