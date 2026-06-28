using System;
using UnityEngine;

namespace ReplicaProjects.Arrows
{
    public class Selection : MonoBehaviour
    {
        [SerializeField] private float _nodeHitDiameter = 0.9f;
        [SerializeField] private float _dragThreshold = 10f; // pixels

        private Camera _camera;
        private Vector3 _pointerDownPosition;

        public event Action<Vector2Int> OnNodeSelected;

        public void Initialize(Camera camera)
        {
            _camera = camera;
        }

        private void Update()
        {
            if (_camera == null)
                return;

            if (Input.GetMouseButtonDown(0))
                _pointerDownPosition = Input.mousePosition;

            if (!Input.GetMouseButtonUp(0))
                return;

            // A drag (pan) moved the pointer beyond the threshold — don't select.
            if ((Input.mousePosition - _pointerDownPosition).sqrMagnitude > _dragThreshold * _dragThreshold)
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