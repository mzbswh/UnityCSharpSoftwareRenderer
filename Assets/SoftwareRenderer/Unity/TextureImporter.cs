using UnityEngine;
using SoftwareRenderer.Render;
using SoftwareRenderer.Core;

namespace SoftwareRenderer.Unity
{
    /// <summary>
    /// Unity Texture2D导入器
    /// </summary>
    public static class TextureImporter
    {
        public static void ImportTexture2D(ITexture softTexture, Texture2D unityTexture)
        {
            if (softTexture == null || unityTexture == null) return;

            // 确保纹理可读
            if (!unityTexture.isReadable)
            {
                Debug.LogWarning($"Texture {unityTexture.name} is not readable. Please enable Read/Write in import settings.");
                return;
            }

            Color[] pixels = unityTexture.GetPixels();
            byte[] data = new byte[pixels.Length * 4];

            for (int i = 0; i < pixels.Length; i++)
            {
                data[i * 4 + 0] = (byte)(pixels[i].r * 255);
                data[i * 4 + 1] = (byte)(pixels[i].g * 255);
                data[i * 4 + 2] = (byte)(pixels[i].b * 255);
                data[i * 4 + 3] = (byte)(pixels[i].a * 255);
            }

            softTexture.SetData(data);
        }

        public static ITexture CreateFromUnityTexture(IRenderer renderer, Texture2D unityTexture, bool generateMipmaps = false)
        {
            if (renderer == null || unityTexture == null) return null;

            ETextureDesc desc = ETextureDesc.Default2D(unityTexture.width, unityTexture.height);
            desc.GenerateMipmaps = generateMipmaps;
            desc.Format = ETextureFormat.RGBA8;

            ITexture softTexture = renderer.CreateTexture(desc);
            ImportTexture2D(softTexture, unityTexture);

            if (generateMipmaps)
            {
                softTexture.GenerateMipmaps();
            }

            return softTexture;
        }

        public static Texture2D ExportToUnityTexture(ITexture softTexture)
        {
            if (softTexture == null) return null;

            Texture2D unityTexture = new Texture2D(softTexture.Width, softTexture.Height, UnityEngine.TextureFormat.RGBA32, false);
            unityTexture.filterMode = FilterMode.Point;

            byte[] data = softTexture.GetData();
            if (data != null)
            {
                Color[] pixels = new Color[softTexture.Width * softTexture.Height];

                for (int i = 0; i < pixels.Length; i++)
                {
                    pixels[i] = new Color(
                        data[i * 4 + 0] / 255f,
                        data[i * 4 + 1] / 255f,
                        data[i * 4 + 2] / 255f,
                        data[i * 4 + 3] / 255f
                    );
                }

                unityTexture.SetPixels(pixels);
                unityTexture.Apply();
            }

            return unityTexture;
        }
    }
}

