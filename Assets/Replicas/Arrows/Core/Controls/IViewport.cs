namespace ReplicaProjects.Arrows
{
    /// <summary>The rendering camera as seen by Core: shape of the screen and screen-to-world projection.</summary>
    public interface IViewport
    {
        float Aspect { get; }
        float ScreenHeight { get; }
        void ScreenToWorld(float screenX, float screenY, out float worldX, out float worldY);
    }
}
