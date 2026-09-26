using UnityEngine;

namespace ReplicaProjects.Arrows
{
    /// <summary>Legacy Input Manager adapter: mouse (or emulated touch) plus two-finger pinch.</summary>
    public sealed class UnityInputSource : IInputSource
    {
        public PointerFrame Read()
        {
            var position = Input.mousePosition;
            var frame = new PointerFrame
            {
                X = position.x,
                Y = position.y,
                Pressed = Input.GetMouseButtonDown(0),
                Held = Input.GetMouseButton(0),
                Released = Input.GetMouseButtonUp(0),
                Scroll = Input.mouseScrollDelta.y,
                TouchCount = Input.touchCount
            };

            if (frame.TouchCount >= 2)
            {
                var t0 = Input.GetTouch(0);
                var t1 = Input.GetTouch(1);
                frame.PinchDistance = Vector2.Distance(t0.position, t1.position);
                frame.PinchBegan = t0.phase == TouchPhase.Began || t1.phase == TouchPhase.Began;
            }

            return frame;
        }
    }
}
