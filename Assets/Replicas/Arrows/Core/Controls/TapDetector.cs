using System;
using ReplicaProjects.Common;

namespace ReplicaProjects.Arrows
{
    /// <summary>
    /// Turns press/release pairs into cell taps. A release that moved beyond the drag threshold
    /// is a pan, not a tap.
    /// </summary>
    public sealed class TapDetector : ITickable
    {
        public event Action<GridCoord> CellTapped;

        private readonly IInputSource _input;
        private readonly IViewport _viewport;
        private readonly CellPicker _picker;
        private readonly float _dragThresholdSqr;

        private float _downX;
        private float _downY;

        public TapDetector(IInputSource input, IViewport viewport, CellPicker picker, float dragThresholdPixels)
        {
            _input = input;
            _viewport = viewport;
            _picker = picker;
            _dragThresholdSqr = dragThresholdPixels * dragThresholdPixels;
        }

        public void Tick(float deltaTime)
        {
            var frame = _input.Read();

            if (frame.Pressed)
            {
                _downX = frame.X;
                _downY = frame.Y;
            }

            if (!frame.Released)
                return;

            float dx = frame.X - _downX;
            float dy = frame.Y - _downY;
            if (dx * dx + dy * dy > _dragThresholdSqr)
                return;

            _viewport.ScreenToWorld(frame.X, frame.Y, out float worldX, out float worldY);
            if (_picker.TryPick(worldX, worldY, out var cell))
                CellTapped?.Invoke(cell);
        }
    }
}
