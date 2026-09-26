namespace ReplicaProjects.Arrows
{
    /// <summary>One frame of pointer/touch input in screen pixels. A value snapshot, so reading it never allocates.</summary>
    public struct PointerFrame
    {
        public float X;
        public float Y;
        public bool Pressed;    // went down this frame
        public bool Held;       // is down
        public bool Released;   // went up this frame
        public float Scroll;

        // Pinch (valid when TouchCount >= 2)
        public int TouchCount;
        public float PinchDistance;
        public bool PinchBegan;
    }
}
