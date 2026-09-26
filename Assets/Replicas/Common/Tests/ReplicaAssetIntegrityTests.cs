using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ReplicaProjects.Common.Tests
{
    /// <summary>
    /// Scripts are referenced from prefabs, scenes and assets by the GUID in their .meta file.
    /// Moving or renaming a script without its .meta breaks those references silently; this catches it.
    /// </summary>
    public class ReplicaAssetIntegrityTests
    {
        private const string Root = "Assets/Replicas";

        private static IEnumerable<string> Paths(string filter)
        {
            foreach (var guid in AssetDatabase.FindAssets(filter, new[] { Root }))
                yield return AssetDatabase.GUIDToAssetPath(guid);
        }

        private static IEnumerable<string> PrefabPaths() => Paths("t:Prefab");
        private static IEnumerable<string> ScenePaths() => Paths("t:Scene");
        private static IEnumerable<string> ScriptableObjectPaths() => Paths("t:ScriptableObject");

        [TestCaseSource(nameof(PrefabPaths))]
        public void Prefab_HasNoMissingScripts(string path)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            Assert.AreEqual(0, CountMissing(prefab), path);
        }

        [TestCaseSource(nameof(ScenePaths))]
        public void Scene_HasNoMissingScripts(string path)
        {
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                int missing = 0;
                foreach (var root in scene.GetRootGameObjects())
                    missing += CountMissing(root);

                Assert.AreEqual(0, missing, path);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [TestCaseSource(nameof(ScriptableObjectPaths))]
        public void ScriptableObject_ResolvesItsScript(string path)
        {
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<ScriptableObject>(path), path);
        }

        private static int CountMissing(GameObject root)
        {
            int missing = 0;
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                missing += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
            return missing;
        }
    }
}
