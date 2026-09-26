namespace ReplicaProjects.Arrows
{
    public sealed class CameraRigSettings
    {
        public float MinOrthographicSize = 2f;
        public float ZoomSpeed = 0.5f;
        public float PinchZoomSpeed = 0.01f;

        // Let the view drift a little past the edges so corner/edge cells are easier to reach.
        public float BoundsPaddingX = 0.5f;
        public float BoundsPaddingY = 2.5f;
    }
}
