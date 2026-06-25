using System.Collections.Generic;
using UnityEngine;

namespace ProjectManagement
{
    /// <summary>
    /// How long a task is estimated to take. Stored as the raw hour count so the
    /// values stay meaningful even if entries are added or reordered later.
    /// </summary>
    public enum TaskEstimation
    {
        [InspectorName("NoEst")] H0 = 0,
        [InspectorName("1h")] H1 = 1,
        [InspectorName("2h")] H2 = 2,
        [InspectorName("4h")] H4 = 4,
        [InspectorName("6h")] H6 = 6,
        [InspectorName("8h")] H8 = 8,
        [InspectorName("10h")] H10 = 10,
        [InspectorName("12h")] H12 = 12,
        [InspectorName("16h")] H16 = 16,
        [InspectorName("20h")] H20 = 20,
    }

    /// <summary>Current state of a task on the board.</summary>
    public enum TaskProgress
    {
        [InspectorName("To Do")] ToDo = 0,
        [InspectorName("In Progress")] InProgress = 1,
        [InspectorName("Done")] Done = 2,
        [InspectorName("Cancelled")] Cancelled = 3,
        [InspectorName("Solved")] Solved = 4,
    }

    /// <summary>The kind of work a card represents. Each type has its own color.</summary>
    public enum CardType
    {
        Bug = 0,
        Dev = 1,
        Analyze = 2,
    }

    /// <summary>Shared color lookups so the window and any other tooling stay consistent.</summary>
    public static class CardColors
    {
        public static Color For(CardType type)
        {
            switch (type)
            {
                case CardType.Bug:     return new Color(0.85f, 0.25f, 0.25f); // red
                case CardType.Dev:     return new Color(0.25f, 0.50f, 0.90f); // blue
                case CardType.Analyze: return new Color(0.90f, 0.78f, 0.20f); // yellow
                default:               return Color.gray;
            }
        }

        public static Color For(TaskProgress progress)
        {
            switch (progress)
            {
                case TaskProgress.ToDo:       return new Color(0.55f, 0.55f, 0.55f); // gray
                case TaskProgress.InProgress: return new Color(0.95f, 0.65f, 0.20f); // orange
                case TaskProgress.Done:       return new Color(0.30f, 0.75f, 0.35f); // green
                case TaskProgress.Cancelled:  return new Color(0.75f, 0.30f, 0.35f); // red
                case TaskProgress.Solved:     return new Color(0.30f, 0.30f, 0.75f); // blue

                default:                      return Color.gray;
            }
        }
    }

    [System.Serializable]
    public class Subtask
    {
        // Unique within the owning task. Survives reordering; freed on delete.
        public int Id;
        public string Description = "";
        public CardType Type = CardType.Dev;
        public TaskEstimation Estimation = TaskEstimation.H2;
        public TaskProgress Progress = TaskProgress.ToDo;
    }

    [System.Serializable]
    public class Task
    {
        // Unique across the board. Survives reordering; freed for reuse on delete.
        public int Id;
        public string Description = "";
        public CardType Type = CardType.Dev;
        public TaskEstimation Estimation = TaskEstimation.H2;
        public TaskProgress Progress = TaskProgress.ToDo;
        public List<Subtask> Subtasks = new List<Subtask>();
    }

    /// <summary>
    /// Persistent project-management board. Lives as a committed .asset under
    /// Assets/_ProjectManagement so it syncs to every machine through git.
    /// </summary>
    public class ProjectTaskBoard : ScriptableObject
    {
        public List<Task> Tasks = new List<Task>();

        /// <summary>Lowest positive integer not currently used by a top-level task.</summary>
        public int GetNextTaskId()
        {
            var used = new HashSet<int>();
            foreach (var t in Tasks)
                used.Add(t.Id);
            return LowestFree(used);
        }

        /// <summary>Lowest positive integer not currently used within a given task.</summary>
        public static int GetNextSubtaskId(Task owner)
        {
            var used = new HashSet<int>();
            foreach (var s in owner.Subtasks)
                used.Add(s.Id);
            return LowestFree(used);
        }

        private static int LowestFree(HashSet<int> used)
        {
            int id = 1;
            while (used.Contains(id))
                id++;
            return id;
        }
    }
}
