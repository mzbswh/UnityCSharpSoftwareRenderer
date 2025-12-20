using System;
using SoftwareRenderer.Core;
using UnityEngine;

namespace SoftwareRenderer.Render.Software
{
    /// <summary>
    /// 软件纹理实现
    /// </summary>
    public class TextureSoft : ITexture
    {
        private static int _idCounter = 1;
        private int _id;
        private ETextureDesc _desc;
        private ImageBuffer<ERGBA>[] _imagesRGBA;     // RGBA8格式
        private ImageBuffer<float>[] _imagesFloat;   // Float32格式

        public int Id => _id;
        public ETextureDesc Desc => _desc;
        public int Width => _desc.Width;
        public int Height => _desc.Height;
        public ETextureType Type => _desc.Type;
        public ETextureFormat Format => _desc.Format;

        public TextureSoft(ETextureDesc desc)
        {
            _id = _idCounter++;
            this._desc = desc;

            int layerCount = desc.Type == ETextureType.TextureCube ? 6 : 1;

            if (desc.Format == ETextureFormat.RGBA8)
            {
                _imagesRGBA = new ImageBuffer<ERGBA>[layerCount];
                for (int i = 0; i < layerCount; i++)
                {
                    _imagesRGBA[i] = new ImageBuffer<ERGBA>(desc.Width, desc.Height, desc.GenerateMipmaps,
                        desc.MultiSample, desc.SampleCount);
                }
            }
            else if (desc.Format == ETextureFormat.Float32)
            {
                _imagesFloat = new ImageBuffer<float>[layerCount];
                for (int i = 0; i < layerCount; i++)
                {
                    _imagesFloat[i] = new ImageBuffer<float>(desc.Width, desc.Height, desc.GenerateMipmaps,
                        desc.MultiSample, desc.SampleCount);
                }
            }
        }

        public ImageBuffer<ERGBA> GetImageRGBA(int layer = 0)
        {
            if (_imagesRGBA != null && layer < _imagesRGBA.Length)
                return _imagesRGBA[layer];
            return null;
        }

        public ImageBuffer<float> GetImageFloat(int layer = 0)
        {
            if (_imagesFloat != null && layer < _imagesFloat.Length)
                return _imagesFloat[layer];
            return null;
        }

        public void SetData(byte[] data, int layer = 0, int level = 0)
        {
            if (_desc.Format == ETextureFormat.RGBA8 && _imagesRGBA != null)
            {
                var buffer = _imagesRGBA[layer].GetBuffer(level);
                int pixelCount = buffer.Width * buffer.Height;

                for (int i = 0; i < pixelCount && i * 4 < data.Length; i++)
                {
                    buffer[i] = new ERGBA(data[i * 4], data[i * 4 + 1], data[i * 4 + 2], data[i * 4 + 3]);
                }

                if (level == 0 && _desc.GenerateMipmaps)
                {
                    GenerateMipmapsRGBA(layer);
                }
            }
        }

        public void SetData(float[] data, int layer = 0, int level = 0)
        {
            if (_desc.Format == ETextureFormat.Float32 && _imagesFloat != null)
            {
                var buffer = _imagesFloat[layer].GetBuffer(level);
                int pixelCount = buffer.Width * buffer.Height;

                for (int i = 0; i < pixelCount && i < data.Length; i++)
                {
                    buffer[i] = data[i];
                }

                if (level == 0 && _desc.GenerateMipmaps)
                {
                    GenerateMipmapsFloat(layer);
                }
            }
        }

        public byte[] GetData(int layer = 0, int level = 0)
        {
            if (_desc.Format == ETextureFormat.RGBA8 && _imagesRGBA != null)
            {
                var buffer = _imagesRGBA[layer].GetBuffer(level);
                byte[] data = new byte[buffer.Width * buffer.Height * 4];

                for (int i = 0; i < buffer.Length; i++)
                {
                    ERGBA color = buffer[i];
                    data[i * 4] = color.R;
                    data[i * 4 + 1] = color.G;
                    data[i * 4 + 2] = color.B;
                    data[i * 4 + 3] = color.A;
                }

                return data;
            }

            return null;
        }

        /// <summary>
        /// 获取纹理数据（无GC分配版本，优化性能）
        /// </summary>
        public void GetData(byte[] buffer, int layer = 0, int level = 0)
        {
            if (_desc.Format == ETextureFormat.RGBA8 && _imagesRGBA != null)
            {
                var imageBuffer = _imagesRGBA[layer].GetBuffer(level);
                ERGBA[] srcData = imageBuffer.Data;
                int pixelCount = srcData.Length;
                int requiredSize = pixelCount * 4;

                if (buffer == null || buffer.Length < requiredSize)
                {
                    UnityEngine.Debug.LogError($"Buffer size insufficient. Required: {requiredSize}, Provided: {buffer?.Length ?? 0}");
                    return;
                }

                // 逐像素拷贝（兼容非unsafe模式）
                // 注意：如果启用了unsafe编译，可以用更快的指针拷贝
                int dstIdx = 0;
                for (int i = 0; i < pixelCount; i++)
                {
                    ERGBA pixel = srcData[i];
                    buffer[dstIdx++] = pixel.R;
                    buffer[dstIdx++] = pixel.G;
                    buffer[dstIdx++] = pixel.B;
                    buffer[dstIdx++] = pixel.A;
                }
            }
        }

        /// <summary>
        /// 获取原始数据引用（零拷贝，最快）
        /// 返回 ERGBA[] 数组的直接引用
        /// </summary>
        public object GetRawData(int layer = 0, int level = 0)
        {
            if (_desc.Format == ETextureFormat.RGBA8 && _imagesRGBA != null)
            {
                return _imagesRGBA[layer].GetBuffer(level).Data;
            }
            return null;
        }

        public void GenerateMipmaps()
        {
            if (!_desc.GenerateMipmaps) return;

            int layerCount = _desc.Type == ETextureType.TextureCube ? 6 : 1;

            for (int i = 0; i < layerCount; i++)
            {
                if (_desc.Format == ETextureFormat.RGBA8)
                    GenerateMipmapsRGBA(i);
                else if (_desc.Format == ETextureFormat.Float32)
                    GenerateMipmapsFloat(i);
            }
        }

        private void GenerateMipmapsRGBA(int layer)
        {
            if (_imagesRGBA == null || layer >= _imagesRGBA.Length) return;

            var image = _imagesRGBA[layer];
            if (image.Mipmaps == null) return;

            for (int level = 0; level < image.MipLevels - 1; level++)
            {
                var srcBuffer = image.GetBuffer(level);
                var dstBuffer = image.GetBuffer(level + 1);

                // Box filter下采样
                for (int y = 0; y < dstBuffer.Height; y++)
                {
                    for (int x = 0; x < dstBuffer.Width; x++)
                    {
                        int srcX = x * 2;
                        int srcY = y * 2;

                        int r = 0, g = 0, b = 0, a = 0;
                        int count = 0;

                        for (int dy = 0; dy < 2 && srcY + dy < srcBuffer.Height; dy++)
                        {
                            for (int dx = 0; dx < 2 && srcX + dx < srcBuffer.Width; dx++)
                            {
                                ERGBA color = srcBuffer[srcX + dx, srcY + dy];
                                r += color.R;
                                g += color.G;
                                b += color.B;
                                a += color.A;
                                count++;
                            }
                        }

                        dstBuffer[x, y] = new ERGBA(
                            (byte)(r / count),
                            (byte)(g / count),
                            (byte)(b / count),
                            (byte)(a / count)
                        );
                    }
                }
            }
        }

        private void GenerateMipmapsFloat(int layer)
        {
            if (_imagesFloat == null || layer >= _imagesFloat.Length) return;

            var image = _imagesFloat[layer];
            if (image.Mipmaps == null) return;

            for (int level = 0; level < image.MipLevels - 1; level++)
            {
                var srcBuffer = image.GetBuffer(level);
                var dstBuffer = image.GetBuffer(level + 1);

                // Box filter下采样
                for (int y = 0; y < dstBuffer.Height; y++)
                {
                    for (int x = 0; x < dstBuffer.Width; x++)
                    {
                        int srcX = x * 2;
                        int srcY = y * 2;

                        float sum = 0f;
                        int count = 0;

                        for (int dy = 0; dy < 2 && srcY + dy < srcBuffer.Height; dy++)
                        {
                            for (int dx = 0; dx < 2 && srcX + dx < srcBuffer.Width; dx++)
                            {
                                sum += srcBuffer[srcX + dx, srcY + dy];
                                count++;
                            }
                        }

                        dstBuffer[x, y] = sum / count;
                    }
                }
            }
        }
    }
}

