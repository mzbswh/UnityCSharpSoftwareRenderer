using UnityEngine;
using SoftwareRenderer.Core;

namespace SoftwareRenderer.Render.Software
{
    /// <summary>
    /// 单个采样点上下文
    /// </summary>
    public class SampleContext
    {
        public bool Inside = false;
        public Vector2Int FboCoord = Vector2Int.zero;
        public Vector4 Position = Vector4.zero;
        public Vector4 Barycentric = Vector4.zero;
    }

    /// <summary>
    /// 单个像素上下文
    /// </summary>
    public class PixelContext
    {
        // MSAA 4x采样点位置 (相对于像素中心)
        private static readonly Vector2[] _sampleLocations4X = new Vector2[]
        {
            new Vector2(0.375f, 0.875f),
            new Vector2(0.875f, 0.625f),
            new Vector2(0.125f, 0.375f),
            new Vector2(0.625f, 0.125f)
        };

        public bool Inside = false;
        public float[] VaryingsFrag = null;
        public SampleContext[] Samples;
        public SampleContext SampleShading = null;
        public int SampleCount = 0;
        public int Coverage = 0;

        public void Init(float x, float y, int sampleCnt = 1)
        {
            Inside = false;
            Coverage = 0;

            // 复用已存在的数组，避免每次都重新分配
            if (SampleCount != sampleCnt)
            {
                SampleCount = sampleCnt;
                
                if (SampleCount > 1)
                {
                    Samples = new SampleContext[SampleCount + 1]; // 最后一个存储中心采样
                    for (int i = 0; i <= SampleCount; i++)
                    {
                        Samples[i] = new SampleContext();
                    }
                }
                else
                {
                    Samples = new SampleContext[1];
                    Samples[0] = new SampleContext();
                }
            }

            // 重置所有采样点的状态
            if (Samples != null)
            {
                for (int i = 0; i < Samples.Length; i++)
                {
                    Samples[i].Inside = false;
                }
            }

            if (SampleCount > 1 && SampleCount == 4)
            {
                int ix = (int)x;
                int iy = (int)y;
                
                // 设置4个子采样点（避免重复创建 Vector2Int 和 Vector4）
                for (int i = 0; i < 4; i++)
                {
                    Samples[i].FboCoord.x = ix;
                    Samples[i].FboCoord.y = iy;
                    Samples[i].Position.x = _sampleLocations4X[i].x + x;
                    Samples[i].Position.y = _sampleLocations4X[i].y + y;
                    Samples[i].Position.z = 0.0f;
                    Samples[i].Position.w = 0.0f;
                }
                // 像素中心
                Samples[4].FboCoord.x = ix;
                Samples[4].FboCoord.y = iy;
                Samples[4].Position.x = x + 0.5f;
                Samples[4].Position.y = y + 0.5f;
                Samples[4].Position.z = 0.0f;
                Samples[4].Position.w = 0.0f;
                SampleShading = Samples[4];
            }
            else
            {
                // 单采样模式
                Samples[0].FboCoord.x = (int)x;
                Samples[0].FboCoord.y = (int)y;
                Samples[0].Position.x = x + 0.5f;
                Samples[0].Position.y = y + 0.5f;
                Samples[0].Position.z = 0.0f;
                Samples[0].Position.w = 0.0f;
                SampleShading = Samples[0];
            }
        }

        public bool InitCoverage()
        {
            if (SampleCount > 1)
            {
                Coverage = 0;
                Inside = false;
                for (int i = 0; i < SampleCount; i++)
                {
                    if (Samples[i].Inside)
                    {
                        Coverage++;
                    }
                }
                Inside = Coverage > 0;
            }
            else
            {
                Coverage = 1;
                Inside = Samples[0].Inside;
            }
            return Inside;
        }

        public void InitShadingSample()
        {
            if (SampleShading.Inside)
            {
                return;
            }
            foreach (var sample in Samples)
            {
                if (sample.Inside)
                {
                    SampleShading = sample;
                    break;
                }
            }
        }
    }

    /// <summary>
    /// 2x2像素块上下文（用于导数计算和Early-Z优化）
    /// </summary>
    public class PixelQuadContext
    {
        /**
         *   p2--p3
         *   |   |
         *   p0--p1
         */
        public PixelContext[] Pixels = new PixelContext[4];

        // 三角形顶点屏幕空间位置
        public Vector4[] VertPos = new Vector4[3];

        // 三角形顶点的1/w值（用于透视校正）
        public Vector4 VertW = Vector4.zero;

        // 三角形顶点shader varyings
        public float[][] VertVaryings = new float[3][];

        // 三角形朝向
        public bool FrontFacing = true;

        // Shader程序
        public ShaderProgramSoft ShaderProgram = null;

        private int _varyingsAlignedCnt = 0;
        private float[] _varyingPool = null;

        public void SetVaryingsSize(int size)
        {
            if (_varyingsAlignedCnt != size)
            {
                _varyingsAlignedCnt = size;
                _varyingPool = new float[size * 4];
                
                // 确保Pixels数组已初始化
                for (int i = 0; i < 4; i++)
                {
                    if (Pixels[i] == null)
                    {
                        Pixels[i] = new PixelContext();
                    }
                    Pixels[i].VaryingsFrag = new float[_varyingsAlignedCnt];
                }
            }
        }

        public void Init(float x, float y, int sampleCnt = 1)
        {
            // 确保Pixels数组已初始化（只在第一次调用时分配）
            for (int i = 0; i < 4; i++)
            {
                if (Pixels[i] == null)
                {
                    Pixels[i] = new PixelContext();
                }
            }

            Pixels[0].Init(x, y, sampleCnt);
            Pixels[1].Init(x + 1, y, sampleCnt);
            Pixels[2].Init(x, y + 1, sampleCnt);
            Pixels[3].Init(x + 1, y + 1, sampleCnt);
        }

        public bool CheckInside()
        {
            return Pixels[0].Inside || Pixels[1].Inside || Pixels[2].Inside || Pixels[3].Inside;
        }
    }
}

