using System;
using UnityEngine;

namespace ReplicaProjects.Arrows
{
    public class Selection : MonoBehaviour
    {
        [SerializeField] private float _nodeHitDiameter = 0.9f;

        private Camera _camera;

        public event Action<Vector2Int> OnNodeSelected;

        public void Initialize(Camera camera)
        {
            _camera = camera;
        }

        private void Update()
        {
            if (_camera == null)
                return;

            if (!Input.GetMouseButtonDown(0))
                return;

            if (TryGetCoordinateUnderMouse(out var coordinate))
                OnNodeSelected?.Invoke(coordinate);
        }

        private bool TryGetCoordinateUnderMouse(out Vector2Int coordinate)
        {
            var world = _camera.ScreenToWorldPoint(Input.mousePosition);
            var x = Mathf.RoundToInt(world.x);
            var y = Mathf.RoundToInt(world.y);
            coordinate = new Vector2Int(x, y);

            // Circular dead-zone: accept only clicks within radius of the cell center.
            var dx = world.x - x;
            var dy = world.y - y;
            var radius = _nodeHitDiameter * 0.5f;
            return dx * dx + dy * dy <= radius * radius;
        }
    }
}