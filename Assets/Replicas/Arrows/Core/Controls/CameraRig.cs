using System;
using ReplicaProjects.Common;

namespace ReplicaProjects.Arrows
{
    /// <summary>
    /// Orthographic board camera state: frames the board, zooms (wheel / pinch), pans by drag and
    /// keeps the view inside the padded board bounds. The Unity shell only copies X/Y/size to the camera.
    /// </summary>
    public sealed class CameraRig : ITickable
    {
        private const float Epsilon = 1e-6f;

        private readonly IInputSource _input;
        private readonly IViewport _viewport;
        private readonly CameraRigSettings _settings;

        private float _centerX, _centerY, _extentX, _extentY;
        private float _maxOrthographicSize; // = fit size = max zoom-out
        private float _lastPointerX, _lastPointerY;
        private float _lastPinchDistance;

        public float X { get; private set; }
        public float Y { get; private set; }
        public float OrthographicSize { get; private set; }

        public CameraRig(IInputSource input, IViewport viewport, CameraRigSettings settings)
        {
            _input = input;
            _viewport = viewport;
            _settings = settings;
        }

        /// <summary>Frames a width x height board (cells at integer coordinates), fully zoomed out and centred.</summary>
        public void Frame(int width, int height)
        {
            _centerX = (width - 1) * 0.5f;
            _centerY = (height - 1) * 0.5f;
            _extentX = width * 0.5f + _settings.BoundsPaddingX;
            _extentY = height * 0.5f + _settings.BoundsPaddingY;

            // orthographicSize is a half-height, so the width constraint is divided by aspect — on a
            // tall phone (aspect < 1) that's what drives the zoom-out.
            _maxOrthographicSize = Math.Max(_extentY, _extentX / _viewport.Aspect);
            OrthographicSize = _maxOrthographicSize;
            X = _centerX;
            Y = _centerY;
        }

        public void Tick(float deltaTime)
        {
            var frame = _input.Read();

            // Two fingers down => pinch zoom; suppress pan so the camera doesn't jump
            // as the mouse-emulated touch tracks the average finger position.
            if (frame.TouchCount >= 2)
            {
                Pinch(frame);
            }
            else
            {
                ZoomBy(frame.Scroll * _settings.ZoomSpeed);
                Pan(frame);
            }

            ClampToBounds();
        }

        private void Pinch(in PointerFrame frame)
        {
            // Start of a pinch: seed the reference distance without zooming this frame.
            if (frame.PinchBegan)
            {
                _lastPinchDistance = frame.PinchDistance;
                return;
            }

            float delta = frame.PinchDistance - _lastPinchDistance;
            _lastPinchDistance = frame.PinchDistance;

            // Keep the pan anchor current so lifting one finger back into a single-finger
            // drag doesn't snap the camera on the transition frame.
            _lastPointerX = frame.X;
            _lastPointerY = frame.Y;

            ZoomBy(delta * _settings.PinchZoomSpeed);
        }

        private void ZoomBy(float amount)
        {
            if (Math.Abs(amount) < Epsilon)
                return;

            OrthographicSize = Math.Clamp(OrthographicSize - amount,
                _settings.MinOrthographicSize, _maxOrthographicSize);
        }

        // Drag the content under the pointer: camera moves opposite to the pointer.
        private void Pan(in PointerFrame frame)
        {
            if (frame.Pressed)
            {
                _lastPointerX = frame.X;
                _lastPointerY = frame.Y;
                return;
            }

            if (!frame.Held)
                return;

            float worldPerPixel = 2f * OrthographicSize / _viewport.ScreenHeight;
            X -= (frame.X - _lastPointerX) * worldPerPixel;
            Y -= (frame.Y - _lastPointerY) * worldPerPixel;

            _lastPointerX = frame.X;
            _lastPointerY = frame.Y;
        }

        private void ClampToBounds()
        {
            float halfHeight = OrthographicSize;
            float halfWidth = halfHeight * _viewport.Aspect;

            X = ClampAxis(X, _centerX, _extentX, halfWidth);
            Y = ClampAxis(Y, _centerY, _extentY, halfHeight);
        }

        // Locks to the board center when the view is wider than the board on this axis,
        // otherwise keeps the view edges inside the board.
        private static float ClampAxis(float value, float center, float extent, float half)
        {
            if (half >= extent)
                return center;

            return Math.Clamp(value, center - extent + half, center + extent - half);
        }
    }
}
