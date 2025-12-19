using SoftwareRenderer.Core;

namespace SoftwareRenderer.Render
{
    /// <summary>
    /// 纹理接口
    /// </summary>
    public interface ITexture
    {
        int Id { get; }
        int Width { get; }
        int Height { get; }
        ETextureDesc Desc { get; }
        ETextureType Type { get; }
        ETextureFormat Format { get; }

        void SetData(byte[] data, int layer = 0, int level = 0);
        void SetData(float[] data, int layer = 0, int level = 0);
        byte[] GetData(int layer = 0, int level = 0);
        void GenerateMipmaps();
    }
}

