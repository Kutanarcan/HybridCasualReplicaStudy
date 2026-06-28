using UnityEngine;

namespace ReplicaProjects.Arrows
{
    public class CameraController : MonoBehaviour
    {
        [SerializeField] private float _minOrthographicSize = 2f;
        [SerializeField] private float _zoomSpeed = 0.5f;
        [SerializeField] private float _boundsPaddingX = 0.5f;
        [SerializeField] private float _boundsPaddingY = 2.5f;

        private Camera _camera;
        private Bounds _boardBounds;
        private float _maxOrthographicSize; // = fit size = max zoom-out
        private Vector3 _lastMousePosition;

        public void Initialize(Camera camera, int width, int height)
        {
            _camera = camera;

            _boardBounds = new Bounds(
                new Vector3((width - 1) * 0.5f, (height - 1) * 0.5f, 0f),
                new Vector3(width, height, 0f));

            // Let the view drift a little past the edges so corner/edge cells are easier to reach.
            _boardBounds.Expand(new Vector3(_boundsPaddingX * 2f, _boundsPaddingY * 2f, 0f));

            // Preserve the existing fit formula as the max zoom-out.
            _maxOrthographicSize = width <= height ? (width + height) * 0.5f : width;

            ResetView();
        }

        public void DeInitialize()
        {
            _camera = null;
        }

        // Frame the whole board (fully zoomed out, centered).
        private void ResetView()
        {
            _camera.orthographicSize = _maxOrthographicSize;

            var z = _camera.transform.position.z;
            _camera.transform.position = new Vector3(_boardBounds.center.x, _boardBounds.center.y, z);
        }

        private void Update()
        {
            if (_camera == null)
                return;

            HandleZoom();
            HandlePan();
            ClampToBounds();
        }

        private void HandleZoom()
        {
            var scroll = Input.mouseScrollDelta.y;
            if (Mathf.Approximately(scroll, 0f))
                return;

            _camera.orthographicSize = Mathf.Clamp(
                _camera.orthographicSize - scroll * _zoomSpeed,
                _minOrthographicSize,
                _maxOrthographicSize);
        }

        private void HandlePan()
        {
            if (Input.GetMouseButtonDown(0))
            {
                _lastMousePosition = Input.mousePosition;
                return;
            }

            if (!Input.GetMouseButton(0))
                return;

            var pixelDelta = Input.mousePosition - _lastMousePosition;
            _lastMousePosition = Input.mousePosition;

            // Drag the content under the pointer: camera moves opposite to the pointer.
            var worldPerPixel = (2f * _camera.orthographicSize) / Screen.height;
            var worldDelta = (Vector3)pixelDelta * worldPerPixel;

            var position = _camera.transform.position;
            position.x -= worldDelta.x;
            position.y -= worldDelta.y;
            _camera.transform.position = position;
        }

        private void ClampToBounds()
        {
            var halfH = _camera.orthographicSize;
            var halfW = halfH * _camera.aspect;

            var position = _camera.transform.position;

            position.x = ClampAxis(position.x, _boardBounds.center.x, _boardBounds.min.x, _boardBounds.max.x, halfW);
            position.y = ClampAxis(position.y, _boardBounds.center.y, _boardBounds.min.y, _boardBounds.max.y, halfH);

            _camera.transform.position = position;
        }

        // Locks to the board center when the view is wider than the board on this axis,
        // otherwise keeps the view edges inside the board.
        private static float ClampAxis(float value, float center, float min, float max, float half)
        {
            if (2f * half >= max - min)
                return center;

            return Mathf.Clamp(value, min + half, max - half);
        }
    }
}
