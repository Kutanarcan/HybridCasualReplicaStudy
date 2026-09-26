using System.Collections.Generic;
using ReplicaProjects.Common;
using ReplicaProjects.Common.EditorTools;
using UnityEditor;
using UnityEngine;

namespace ReplicaProjects.MagicSort.EditorTools
{
    /// <summary>
    /// Magic Sort level editor: designer free-moves on a working board, scramble / solvability tools,
    /// and a save that refuses unsolvable layouts. Rules live in Core (MagicSortLevelEditorLogic, solver).
    /// </summary>
    public class MagicSortEditorWindow : EditorWindow
    {
        private const int BarHeight = 4;

        private readonly MagicSortSettingsView _settings = new();
        private readonly MagicSortBoardView _boardView = new();
        private readonly MagicSortGenerator _generator = new(new SeededRandomSource());

        private LevelAssetBrowser<MagicSortLevel> _browser;
        private MagicSortLevelEditorLogic _logic;
        private SolveResult? _solveResult;
        private bool _dirty;
        private Vector2 _scroll;

        [MenuItem("Tools/Level Design/Magic Sort Level Editor")]
        public static void ShowWindow()
        {
            var w = GetWindow<MagicSortEditorWindow>("Magic Sort Replica Level Editor");
            w.minSize = new Vector2(500, 520);
        }

        private void OnEnable()
        {
            _settings.Reconfigured += Reconfigure;
            _boardView.Moved += OnBoardMoved;
            _boardView.RepaintRequested += Repaint;

            _browser = new LevelAssetBrowser<MagicSortLevel>("Assets/MagicSort/Levels", "MagicSortLevel",
                () => _dirty, LoadWorkingCopy, WriteLevel);
            _browser.Refresh();
        }

        private void OnDisable()
        {
            _settings.Reconfigured -= Reconfigure;
            _boardView.Moved -= OnBoardMoved;
            _boardView.RepaintRequested -= Repaint;
        }

        private void OnGUI()
        {
            if (_logic == null)
                RebuildLogic();

            _browser.DrawToolbar();
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            _settings.Draw();
            EditorGUILayout.Space(6);
            DrawControls();
            EditorGUILayout.Space(4);
            DrawSaveRow();
            EditorGUILayout.Space(4);
            MagicSortStatusView.DrawSolveResult(_logic, _solveResult);
            EditorGUILayout.Space(4);
            MagicSortStatusView.DrawMetrics(_logic);
            EditorGUILayout.Space(8);
            _boardView.Draw(_logic, _settings.ColorCount, BarHeight);

            EditorGUILayout.Space(12);
            EditorGUILayout.LabelField(
                "Designer free-move: move balls however you like. \"Can it be solved?\" / Save check against the actual game rules.",
                EditorStyles.centeredGreyMiniLabel);

            EditorGUILayout.EndScrollView();
        }

        // ---- Asset <-> working copy ----------------------------------------

        private void LoadWorkingCopy(MagicSortLevel level)
        {
            LoadBoard(MagicSortFlat.ToBars(level.slots, level.barHeight));

            // Restore the config controls straight from the asset — colorCount is authored, not recomputed.
            _settings.LoadFrom(level);
            _dirty = false;
        }

        private void WriteLevel(MagicSortLevel level) =>
            MagicSortLevelSaver.Write(level, _logic, _settings.ColorCount, BarHeight);

        private void Save()
        {
            if (_browser.Current == null)
                return;

            _solveResult = MagicSortLevelSaver.TrySave(_browser.Current, _logic, _settings.ColorCount, BarHeight, out bool saved);
            if (saved)
                _dirty = false;
        }

        // ---- Controls ------------------------------------------------------

        private void DrawControls()
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField($"Move Count: {_logic.MoveCount}", GUILayout.Width(80));

            GUI.enabled = _logic.CanUndo;
            if (GUILayout.Button("Undo Move", GUILayout.Height(24)))
            {
                _logic.Undo();
                ResetTransientState();
                MarkDirty();
            }
            GUI.enabled = true;

            if (GUILayout.Button("Reset", GUILayout.Height(24)))
                Reconfigure();

            GUI.enabled = _settings.EmptyBars > 0;
            if (GUILayout.Button("Scramble", GUILayout.Height(24)))
                Scramble();

            GUI.enabled = !_logic.IsPuzzleSolved();
            if (GUILayout.Button("Can it be solved?", GUILayout.Height(24)))
            {
                _boardView.ClearSelection();
                _solveResult = _logic.CheckCurrentState();
            }
            GUI.enabled = true;

            EditorGUILayout.EndHorizontal();
        }

        private void DrawSaveRow()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            using (new EditorGUI.DisabledScope(_browser.Current == null))
                if (GUILayout.Button("Save Level", GUILayout.Width(120), GUILayout.Height(24)))
                    Save();
            EditorGUILayout.EndHorizontal();
        }

        private void Scramble()
        {
            var bars = _generator.Generate(_settings.ColorCount, BarHeight, _settings.EmptyBars,
                _settings.DepthLevel, _settings.FragLevel, out var result);
            _logic.LoadLevel(bars);
            _boardView.ClearSelection();
            _solveResult = result;
            MarkDirty();
        }

        // Reloads the working board with a fresh solved level for the current config.
        private void Reconfigure()
        {
            RebuildLogic();
            ResetTransientState();
            MarkDirty();
        }

        private void RebuildLogic() =>
            LoadBoard(MagicSortGenerator.CreateSolvedLevel(_settings.ColorCount, BarHeight, _settings.EmptyBars));

        private void LoadBoard(IReadOnlyList<Bar> bars)
        {
            if (_logic == null)
                _logic = new MagicSortLevelEditorLogic(bars);
            else
                _logic.LoadLevel(bars);

            ResetTransientState();
        }

        private void OnBoardMoved()
        {
            _solveResult = null;
            MarkDirty();
        }

        private void ResetTransientState()
        {
            _boardView.ClearSelection();
            _solveResult = null;
        }

        private void MarkDirty()
        {
            _dirty = true;
            Repaint();
        }
    }
}
