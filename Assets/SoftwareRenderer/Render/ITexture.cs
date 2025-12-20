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
        void GetData(byte[] buffer, int layer = 0, int level = 0); // 无GC分配版本
        object GetRawData(int layer = 0, int level = 0); // 获取原始数据引用（零拷贝）
        void GenerateMipmaps();
    }
}

