using UnityEditor;

namespace ReplicaProjects.MagicSort.EditorTools
{
    /// <summary>Writes the working board into a level asset; saving refuses layouts the solver proves unsolvable.</summary>
    public static class MagicSortLevelSaver
    {
        // Flat DOD layout + explicit dimensions.
        public static void Write(MagicSortLevel level, MagicSortLevelEditorLogic logic, int colorCount, int barHeight)
        {
            var snap = logic.Snapshot();
            level.colorCount = colorCount;
            level.barHeight = barHeight;
            level.barCount = snap.Count;
            level.slots = MagicSortFlat.Flatten(snap, barHeight);
        }

        /// <summary>Returns the solver verdict; the asset is written only when solvable (or the designer accepts a timeout).</summary>
        public static SolveResult TrySave(MagicSortLevel level, MagicSortLevelEditorLogic logic, int colorCount,
                                          int barHeight, out bool saved)
        {
            saved = false;
            var result = MagicSortSolver.Solve(MagicSortFlat.Flatten(logic.Snapshot(), barHeight), barHeight);

            if (result.Status == SolveStatus.Unsolvable)
            {
                EditorUtility.DisplayDialog("Cannot Save Level",
                    "This layout is unsolvable under the game rules. Fix it before saving.", "OK");
                return result;
            }

            if (result.Status == SolveStatus.Timeout &&
                !EditorUtility.DisplayDialog("Solvability Unknown",
                    "The solver hit its state limit and could not confirm this level is solvable. Save anyway?",
                    "Save", "Cancel"))
                return result;

            Undo.RecordObject(level, "Save Magic Sort Level");
            Write(level, logic, colorCount, barHeight);
            EditorUtility.SetDirty(level);
            AssetDatabase.SaveAssets();
            saved = true;
            return result;
        }
    }
}
