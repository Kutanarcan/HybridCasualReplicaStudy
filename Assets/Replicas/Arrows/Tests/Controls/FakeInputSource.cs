namespace ReplicaProjects.Arrows.Tests
{
    /// <summary>Hand-written fake: returns whatever frame the test set last.</summary>
    public sealed class FakeInputSource : IInputSource
    {
        public PointerFrame Frame;

        public PointerFrame Read() => Frame;

        public void Press(float x, float y) => Frame = new PointerFrame { X = x, Y = y, Pressed = true, Held = true };
        public void Drag(float x, float y) => Frame = new PointerFrame { X = x, Y = y, Held = true };
        public void Release(float x, float y) => Frame = new PointerFrame { X = x, Y = y, Released = true };
        public void Idle() => Frame = default;
    }
}
