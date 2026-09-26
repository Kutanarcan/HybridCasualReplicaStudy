namespace ReplicaProjects.Arrows.Tests
{
    /// <summary>Hand-written fake: screen space == world space scaled by PixelsPerUnit.</summary>
    public sealed class FakeViewport : IViewport
    {
        public float Aspect { get; set; } = 1f;
        public float ScreenHeight { get; set; } = 1000f;
        public float PixelsPerUnit { get; set; } = 100f;

        public void ScreenToWorld(float screenX, float screenY, out float worldX, out float worldY)
        {
            worldX = screenX / PixelsPerUnit;
            worldY = screenY / PixelsPerUnit;
        }
    }
}
