using UnityEngine;

namespace ReplicaProjects.PixelFlow
{
    public class PngCubeGenerator : MonoBehaviour
    {
        public Texture2D sourceImage;
        public int downsampleSize = 16;

        void Start()
        {
            // 1. Downsample the PNG texture
            Texture2D lowResTex = DownsampleTexture(sourceImage, downsampleSize, downsampleSize);

            // 2. Create cubes based on the new pixel grid
            GenerateCubes(lowResTex);
        }

        Texture2D DownsampleTexture(Texture2D source, int targetWidth, int targetHeight)
        {
            RenderTexture rt = new RenderTexture(targetWidth, targetHeight, 0);
            RenderTexture.active = rt;

            // GPU scaling
            Graphics.Blit(source, rt);

            Texture2D result = new Texture2D(targetWidth, targetHeight, TextureFormat.RGBA32, false);
            result.ReadPixels(new Rect(0, 0, targetWidth, targetHeight), 0, 0);
            result.Apply();

            RenderTexture.active = null;
            rt.Release();

            return result;
        }

        void GenerateCubes(Texture2D tex)
        {
            Color[] pixels = tex.GetPixels();
            float spacing = 1.2f; // Gap between cubes

            for (int y = 0; y < tex.height; y++)
            {
                for (int x = 0; x < tex.width; x++)
                {
                    Color pixelColor = tex.GetPixel(x, y);

                    // Skip fully transparent pixels (optional)
                    if (pixelColor.a < 0.1f) continue;

                    // Create and position the cube
                    GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cube.transform.position = new Vector3(x * spacing, y * spacing, 0);

                    // Apply pixel color to the cube's material
                    cube.GetComponent<Renderer>().material.color = pixelColor;

                    // Optional: Name the cube based on coordinates
                    cube.name = $"Cube_{x}_{y}";
                }
            }
        }
    }
}
