using UnityEngine;
using DG.Tweening;

namespace ReplicaProjects.Arrows
{
    public class CameraController : MonoBehaviour
    {
        [SerializeField] private float _minOrthographicSize = 2f;
        [SerializeField] private float _zoomSpeed = 0.5f;
        [SerializeField] private float _pinchZoomSpeed = 0.01f;
        [SerializeField] private float _boundsPaddingX = 0.5f;
        [SerializeField] private float _boundsPaddingY = 2.5f;
        [SerializeField] private float _shakeStrength = 0.35f;
        [SerializeField] private float _shakeDuration = 0.25f;

        private Camera _camera;
        private Bounds _boardBounds;
        private float _maxOrthographicSize; // = fit size = max zoom-out
        private Vector3 _lastMousePosition;
        private float _lastPinchDistance;

        // Shake is layered on top of the clamped position each frame, so ClampToBounds (which can snap
        // back to centre) never cancels it.
        private Vector3 _shakeOffset;
        private Vector3 _shakeOffsetApplied;
        private Tween _shakeTween;

        public void Initialize(Camera camera, int width, int height)
        {
            _camera = camera;

            _boardBounds = new Bounds(
                new Vector3((width - 1) * 0.5f, (height - 1) * 0.5f, 0f),
                new Vector3(width, height, 0f));

            // Let the view drift a little past the edges so corner/edge cells are easier to reach.
            _boardBounds.Expand(new Vector3(_boundsPaddingX * 2f, _boundsPaddingY * 2f, 0f));

            _maxOrthographicSize = ComputeFitSize();

            ResetView();
        }

        public void DeInitialize()
        {
            _shakeTween?.Kill();
            _shakeOffset = Vector3.zero;
            _shakeOffsetApplied = Vector3.zero;
            _camera = null;
        }

        // Quick decaying random shake, e.g. on a wrong answer.
        public void Shake()
        {
            if (_camera == null)
                return;

            _shakeTween?.Kill();

            float strength = _shakeStrength;
            _shakeTween = DOTween.To(() => strength, x => strength = x, 0f, _shakeDuration)
                .SetEase(Ease.OutQuad)
                .OnUpdate(() =>
                {
                    var rnd = Random.insideUnitCircle * strength;
                    _shakeOffset = new Vector3(rnd.x, rnd.y, 0f);
                })
                .OnComplete(() => _shakeOffset = Vector3.zero)
                .SetLink(gameObject);
        }

        // Smallest orthographic size that keeps the whole (padded) board on screen for the
        // current aspect. orthographicSize is a half-height, so the width constraint has to be
        // divided by aspect — on a tall phone (aspect < 1) that's what drives the zoom-out.
        private float ComputeFitSize()
        {
            var aspect = _camera.aspect;
            if (aspect <= 0f)
                aspect = (float)Screen.width / Screen.height;

            var halfForHeight = _boardBounds.extents.y;
            var halfForWidth = _boardBounds.extents.x / aspect;

            return Mathf.Max(halfForHeight, halfForWidth);
        }

        // Frame the whole board (fully zoomed out, centered).
        private void ResetView()
        {
            // Recompute in case the aspect changed since Initialize (e.g. orientation change).
            _maxOrthographicSize = ComputeFitSize();
            _camera.orthographicSize = _maxOrthographicSize;

            var z = _camera.transform.position.z;
            _camera.transform.position = new Vector3(_boardBounds.center.x, _boardBounds.center.y, z);
        }

        private void Update()
        {
            if (_camera == null)
                return;

            // Strip last frame's shake so pan/zoom/clamp operate on the clean base position.
            _camera.transform.position -= _shakeOffsetApplied;

            // Two fingers down => pinch zoom; suppress pan so the camera doesn't jump
            // as the mouse-emulated touch tracks the average finger position.
            if (Input.touchCount >= 2)
            {
                HandlePinchZoom();
            }
            else
            {
                HandleZoom();
                HandlePan();
            }

            ClampToBounds();

            // Layer the current shake back on top of the controlled position.
            _shakeOffsetApplied = _shakeOffset;
            _camera.transform.position += _shakeOffsetApplied;
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

        private void HandlePinchZoom()
        {
            var t0 = Input.GetTouch(0);
            var t1 = Input.GetTouch(1);

            var distance = Vector2.Distance(t0.position, t1.position);

            // Start of a pinch: seed the reference distance without zooming this frame.
            if (t0.phase == TouchPhase.Began || t1.phase == TouchPhase.Began)
            {
                _lastPinchDistance = distance;
                return;
            }

            var delta = distance - _lastPinchDistance;
            _lastPinchDistance = distance;

            // Keep the pan anchor current so lifting one finger back into a single-finger
            // drag doesn't snap the camera on the transition frame.
            _lastMousePosition = Input.mousePosition;

            if (Mathf.Approximately(delta, 0f))
                return;

            _camera.orthographicSize = Mathf.Clamp(
                _camera.orthographicSize - delta * _pinchZoomSpeed,
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
