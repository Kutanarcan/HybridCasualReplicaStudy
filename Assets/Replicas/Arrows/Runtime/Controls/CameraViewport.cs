using UnityEngine;

namespace ReplicaProjects.Arrows
{
    public sealed class CameraViewport : IViewport
    {
        private readonly Camera _camera;

        public CameraViewport(Camera camera) => _camera = camera;

        // camera.aspect can read 0 before the first render; fall back to the screen ratio.
        public float Aspect => _camera.aspect > 0f ? _camera.aspect : (float)Screen.width / Screen.height;

        public float ScreenHeight => Screen.height;

        public void ScreenToWorld(float screenX, float screenY, out float worldX, out float worldY)
        {
            var world = _camera.ScreenToWorldPoint(new Vector3(screenX, screenY, 0f));
            worldX = world.x;
            worldY = world.y;
        }
    }
}
