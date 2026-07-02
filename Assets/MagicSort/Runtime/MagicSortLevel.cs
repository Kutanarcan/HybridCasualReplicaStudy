using System.Collections.Generic;
using UnityEngine;

namespace BallSortDesigner
{
    /// <summary>
    /// One bar's contents as stored on disk: color indices bottom → top.
    /// An empty bar is an empty (or null) list. Wrapped in a struct because Unity cannot
    /// serialize a nested <c>List&lt;List&lt;int&gt;&gt;</c> directly.
    /// </summary>
    [System.Serializable]
    public struct BarState
    {
        public List<int> balls;
    }

    /// <summary>
    /// A single saved Magic Sort level (one asset = one level, Arrows style).
    /// Stores a serializable mirror of the board; use <see cref="ToBars"/> /
    /// <see cref="SetFromBars"/> to convert to and from the runtime <see cref="Bar"/> type.
    /// </summary>
    [CreateAssetMenu(menuName = "MagicSort/Level", fileName = "MagicSortLevel")]
    public class MagicSortLevel : ScriptableObject
    {
        [Min(1)] public int barHeight = 4;
        public List<BarState> bars = new List<BarState>();

        /// <summary>Rebuild the level as a list of runtime <see cref="Bar"/>s.</summary>
        public List<Bar> ToBars()
        {
            var result = new List<Bar>(bars.Count);
            foreach (var state in bars)
            {
                var bar = new Bar(barHeight);
                if (state.balls != null)
                    foreach (int color in state.balls)
                        bar.Push(color);
                result.Add(bar);
            }
            return result;
        }

        /// <summary>Overwrite this asset's data from a board of runtime <see cref="Bar"/>s.</summary>
        public void SetFromBars(IReadOnlyList<Bar> source)
        {
            barHeight = source.Count > 0 ? source[0].Capacity : barHeight;
            bars = new List<BarState>(source.Count);
            foreach (var bar in source)
            {
                var balls = new List<int>(bar.Count);
                for (int i = 0; i < bar.Count; i++)
                    balls.Add(bar[i]);
                bars.Add(new BarState { balls = balls });
            }
        }
    }
}
