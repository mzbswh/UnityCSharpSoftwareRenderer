using System;
using System.Numerics;
using UnityEngine;

namespace SoftwareRenderer.Core
{
    /// <summary>
    /// 通用缓冲区类，用于存储任意类型的数组数据
    /// </summary>
    public class Buffer<T> where T : struct
    {
        protected T[] data;
        protected int width;
        protected int height;

        public int Width => width;
        public int Height => height;
        public int Length => data.Length;
        public T[] Data => data;

        public Buffer(int width, int height)
        {
            this.width = width;
            this.height = height;
            this.data = new T[width * height];
        }

        public Buffer(int size)
        {
            this.width = size;
            this.height = 1;
            this.data = new T[size];
        }

        public T this[int index]
        {
            get => data[index];
            set => data[index] = value;
        }

        public T this[int x, int y]
        {
            get => data[y * width + x];
            set => data[y * width + x] = value;
        }

        public void SetAll(T value)
        {
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = value;
            }
        }

        public void Clear()
        {
            Array.Clear(data, 0, data.Length);
        }

        // public unsafe T* GetRawDataPtr()
        // {
        //     unsafe
        //     {
        //         fixed (T* ptr = data)
        //         {
        //             return ptr;
        //         }
        //     }
        // }
    }

    /// <summary>
    /// 2D图像缓冲区，支持多级LOD（Mipmap）
    /// </summary>
    public class ImageBuffer<T> where T : struct
    {
        public Buffer<T> Buffer;
        public Buffer<T>[] Mipmaps;
        public int Width;
        public int Height;
        public int MipLevels;

        // 多重采样支持
        public bool MultiSample;
        public int SampleCnt;
        public Buffer<Vector4<T>> BufferMs4x;

        public ImageBuffer(int width, int height, bool generateMipmaps = false, bool multiSample = false, int sampleCnt = 1)
        {
            this.Width = width;
            this.Height = height;
            this.MultiSample = multiSample;
            this.SampleCnt = sampleCnt;

            Buffer = new Buffer<T>(width, height);

            if (multiSample && sampleCnt == 4)
            {
                BufferMs4x = new Buffer<Vector4<T>>(width, height);
            }

            if (generateMipmaps)
            {
                GenerateMipmaps();
            }
        }

        public Buffer<T> GetBuffer(int level = 0)
        {
            if (level == 0) return Buffer;
            if (Mipmaps != null && level < MipLevels)
            {
                return Mipmaps[level - 1];
            }
            return Buffer;
        }

        public void GenerateMipmaps()
        {
            MipLevels = CalculateMipLevels(Width, Height);
            if (MipLevels <= 1) return;

            Mipmaps = new Buffer<T>[MipLevels - 1];

            int mipWidth = Width;
            int mipHeight = Height;

            for (int i = 0; i < MipLevels - 1; i++)
            {
                mipWidth = Mathf.Max(1, mipWidth / 2);
                mipHeight = Mathf.Max(1, mipHeight / 2);
                Mipmaps[i] = new Buffer<T>(mipWidth, mipHeight);
            }
        }

        public static int CalculateMipLevels(int width, int height)
        {
            int maxDim = Mathf.Max(width, height);
            int levels = 1;
            while (maxDim > 1)
            {
                maxDim /= 2;
                levels++;
            }
            return levels;
        }
    }

    /// <summary>
    /// 用于存储4个采样点的结构（MSAA 4x）
    /// </summary>
    public struct Vector4<T> where T : struct
    {
        public T X, Y, Z, W;

        public Vector4(T value)
        {
            X = Y = Z = W = value;
        }

        public Vector4(T x, T y, T z, T w)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
            this.W = w;
        }

        public T this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0: return X;
                    case 1: return Y;
                    case 2: return Z;
                    case 3: return W;
                    default: throw new IndexOutOfRangeException();
                }
            }
            set
            {
                switch (index)
                {
                    case 0: X = value; break;
                    case 1: Y = value; break;
                    case 2: Z = value; break;
                    case 3: W = value; break;
                    default: throw new IndexOutOfRangeException();
                }
            }
        }
    }

    /// <summary>
    /// RGBA颜色结构（字节类型）
    /// </summary>
    /// <summary>
    /// RGBA颜色值（每个分量8位）
    /// </summary>
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, Pack = 1)]
    public struct ERGBA
    {
        public byte R, G, B, A;

        public ERGBA(byte r, byte g, byte b, byte a = 255)
        {
            this.R = r;
            this.G = g;
            this.B = b;
            this.A = a;
        }

        public ERGBA(float r, float g, float b, float a = 1f)
        {
            this.R = (byte)Mathf.Clamp(r * 255f, 0, 255);
            this.G = (byte)Mathf.Clamp(g * 255f, 0, 255);
            this.B = (byte)Mathf.Clamp(b * 255f, 0, 255);
            this.A = (byte)Mathf.Clamp(a * 255f, 0, 255);
        }

        public ERGBA(Color color)
        {
            R = (byte)Mathf.Clamp(color.r * 255f, 0, 255);
            G = (byte)Mathf.Clamp(color.g * 255f, 0, 255);
            B = (byte)Mathf.Clamp(color.b * 255f, 0, 255);
            A = (byte)Mathf.Clamp(color.a * 255f, 0, 255);
        }

        public Color ToColor()
        {
            return new Color(R / 255f, G / 255f, B / 255f, A / 255f);
        }

        public UnityEngine.Vector4 ToVector4()
        {
            return new UnityEngine.Vector4(R, G, B, A);
        }

        public static ERGBA Lerp(ERGBA a, ERGBA b, float t)
        {
            return new ERGBA(
                (byte)(a.R + (b.R - a.R) * t),
                (byte)(a.G + (b.G - a.G) * t),
                (byte)(a.B + (b.B - a.B) * t),
                (byte)(a.A + (b.A - a.A) * t)
            );
        }
    }
}

