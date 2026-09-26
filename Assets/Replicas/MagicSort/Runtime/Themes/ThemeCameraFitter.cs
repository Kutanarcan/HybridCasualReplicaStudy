using UnityEngine;

namespace ReplicaProjects.MagicSort
{
    /// <summary>
    /// Centres and zooms the orthographic camera on a bar grid. Each bar contributes two points
    /// (bar origin + bottomY and + topY) projected into camera space; the camera slides along its
    /// own right/up axes, so it must start from the pose the theme expects (see SortThemeManager.ResetCamera).
    /// </summary>
    public static class ThemeCameraFitter
    {
        public static void Fit(Camera camera, Transform barsRoot, in BarGridLayout layout, int barCount,
                               float bottomY, float topY, Vector2 padding)
        {
            Transform camT = camera.transform;
            float minX = float.MaxValue, maxX = float.MinValue;
            float minY = float.MaxValue, maxY = float.MinValue;

            for (int i = 0; i < barCount; i++)
            {
                layout.Position(i, out float x, out float y, out float z);
                Vector3 origin = barsRoot.TransformPoint(new Vector3(x, y, z));

                Include(camT.InverseTransformPoint(origin + Vector3.up * bottomY), ref minX, ref maxX, ref minY, ref maxY);
                Include(camT.InverseTransformPoint(origin + Vector3.up * topY), ref minX, ref maxX, ref minY, ref maxY);
            }

            camT.position += camT.right * ((minX + maxX) * 0.5f) + camT.up * ((minY + maxY) * 0.5f);

            float halfHeight = (maxY - minY) * 0.5f + padding.y;
            float halfWidth = (maxX - minX) * 0.5f + padding.x;
            camera.orthographicSize = Mathf.Max(halfHeight, halfWidth / camera.aspect);
        }

        private static void Include(Vector3 p, ref float minX, ref float maxX, ref float minY, ref float maxY)
        {
            minX = Mathf.Min(minX, p.x); maxX = Mathf.Max(maxX, p.x);
            minY = Mathf.Min(minY, p.y); maxY = Mathf.Max(maxY, p.y);
        }
    }
}
