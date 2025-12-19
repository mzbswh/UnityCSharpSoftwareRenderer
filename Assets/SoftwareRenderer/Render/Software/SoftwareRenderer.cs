using System;
using System.Collections.Generic;
using UnityEngine;
using SoftwareRenderer.Core;
using SoftwareRenderer.Core.Math;

namespace SoftwareRenderer.Render.Software
{
    /// <summary>
    /// 顶点Holder（包含裁剪空间、屏幕空间和varying数据）
    /// </summary>
    public class VertexHolder
    {
        public Vector4 ClipPos;      // 裁剪空间位置
        public Vector4 ScreenPos;    // 屏幕空间位置
        public float[] Varyings;     // Varying数据
        public int ClipMask;         // 裁剪掩码
    }

    /// <summary>
    /// 图元Holder
    /// </summary>
    public class PrimitiveHolder
    {
        public List<VertexHolder> Vertices = new List<VertexHolder>();
    }

    /// <summary>
    /// 软件渲染器核心实现
    /// </summary>
    public class SoftwareRenderer : IRenderer
    {
        public ERendererType Type => ERendererType.Software;

        // 渲染状态
        private Viewport _viewport;
        private FrameBufferSoft _currentFbo;
        private ImageBuffer<ERGBA> _fboColor;
        private ImageBuffer<float> _fboDepth;
        private VertexArraySoft _currentVao;
        private ShaderProgramSoft _currentShader;
        private RenderStates _currentRenderStates;

        // 渲染数据
        private List<VertexHolder> _vertices = new List<VertexHolder>();
        private List<PrimitiveHolder> _primitives = new List<PrimitiveHolder>();
        private int _rasterSamples = 1;

        // Early-Z优化开关
        private bool _enableEarlyZ = true;

        public void SetEnableEarlyZ(bool enable)
        {
            _enableEarlyZ = enable;
        }

        public bool Create() { return true; }
        public void Destroy() { }

        #region 资源创建

        public IFrameBuffer CreateFrameBuffer(bool offscreen)
        {
            return new FrameBufferSoft(offscreen);
        }

        public ITexture CreateTexture(ETextureDesc desc)
        {
            return new TextureSoft(desc);
        }

        public IVertexArrayObject CreateVertexArrayObject(VertexArray vertexArray)
        {
            return new VertexArraySoft(vertexArray);
        }

        public IShaderProgram CreateShaderProgram()
        {
            return new ShaderProgramSoft();
        }

        public IPipelineStates CreatePipelineStates(RenderStates states)
        {
            return new PipelineStatesSoft(states);
        }

        public IUniformBlock CreateUniformBlock(string name, int size)
        {
            return new UniformBlockSoft(name, size);
        }

        public IUniformSampler CreateUniformSampler(string name, ETextureDesc desc)
        {
            return new UniformSamplerSoft(name);
        }

        #endregion

        #region 渲染管线接口

        public void BeginRenderPass(IFrameBuffer frameBuffer, ClearStates states)
        {
            _currentFbo = frameBuffer as FrameBufferSoft;
            if (_currentFbo == null) return;

            _fboColor = _currentFbo.GetColorBuffer();
            _fboDepth = _currentFbo.GetDepthBuffer();

            // 清除颜色缓冲
            if (states.ColorFlag && _fboColor != null)
            {
                ERGBA clearColor = new ERGBA(states.ClearColor);
                if (_fboColor.MultiSample && _fboColor.SampleCnt == 4)
                {
                    var clearVec = new Vector4<ERGBA>(clearColor);
                    for (int i = 0; i < _fboColor.BufferMs4x.Length; i++)
                    {
                        _fboColor.BufferMs4x[i] = clearVec;
                    }
                }
                else
                {
                    _fboColor.Buffer.SetAll(clearColor);
                }
            }

            // 清除深度缓冲
            if (states.DepthFlag && _fboDepth != null)
            {
                if (_fboDepth.MultiSample && _fboDepth.SampleCnt == 4)
                {
                    var clearVec = new Vector4<float>(states.ClearDepth);
                    for (int i = 0; i < _fboDepth.BufferMs4x.Length; i++)
                    {
                        _fboDepth.BufferMs4x[i] = clearVec;
                    }
                }
                else
                {
                    _fboDepth.Buffer.SetAll(states.ClearDepth);
                }
            }
        }

        public void SetViewport(int x, int y, int width, int height)
        {
            _viewport = Viewport.Create(x, y, width, height);
        }

        public void SetVertexArrayObject(IVertexArrayObject vao)
        {
            _currentVao = vao as VertexArraySoft;
        }

        public void SetShaderProgram(IShaderProgram program)
        {
            _currentShader = program as ShaderProgramSoft;
        }

        public void SetShaderResources(IShaderResources resources)
        {
            if (_currentShader != null && resources != null)
            {
                _currentShader.BindResources(resources);
            }
        }

        public void SetPipelineStates(IPipelineStates states)
        {
            _currentRenderStates = states.RenderStates;
        }

        public void Draw()
        {
            if (_currentFbo == null || _currentVao == null || _currentShader == null) return;

            _fboColor = _currentFbo.GetColorBuffer();
            _fboDepth = _currentFbo.GetDepthBuffer();

            if (_fboColor != null)
            {
                _rasterSamples = _fboColor.SampleCnt;
            }

            // 渲染管线流程
            ProcessVertexShader();
            ProcessPrimitiveAssembly();
            ProcessClipping();
            ProcessPerspectiveDivide();
            ProcessViewportTransform();
            ProcessFaceCulling();
            ProcessRasterization();

            // 清理
            _vertices.Clear();
            _primitives.Clear();
        }

        public void EndRenderPass()
        {
            // Resolve MSAA
            if (_fboColor != null && _fboColor.MultiSample && _fboColor.SampleCnt == 4)
            {
                MultiSampleResolve();
            }
        }

        public void WaitIdle() { }

        #endregion

        #region 顶点处理

        private void ProcessVertexShader()
        {
            _vertices.Clear();
            var vao = _currentVao.VertexArray;
            int varyingSize = _currentShader.GetVaryingSize();

            for (int i = 0; i < vao.IndexCount; i++)
            {
                int vertexIndex = vao.Indices[i];
                var vertex = new VertexHolder
                {
                    Varyings = new float[varyingSize]
                };

                // 执行顶点着色器
                _currentShader.ExecuteVertexShader(vao, vertexIndex, ref vertex.ClipPos, vertex.Varyings);

                _vertices.Add(vertex);
            }
        }

        #endregion

        #region 图元装配

        private void ProcessPrimitiveAssembly()
        {
            _primitives.Clear();

            switch (_currentRenderStates.PrimitiveType)
            {
                case EPrimitiveType.Triangle:
                    ProcessTriangleAssembly();
                    break;
                case EPrimitiveType.Line:
                    ProcessLineAssembly();
                    break;
                case EPrimitiveType.Point:
                    ProcessPointAssembly();
                    break;
            }
        }

        private void ProcessTriangleAssembly()
        {
            for (int i = 0; i < _vertices.Count; i += 3)
            {
                if (i + 2 < _vertices.Count)
                {
                    var primitive = new PrimitiveHolder();
                    primitive.Vertices.Add(_vertices[i]);
                    primitive.Vertices.Add(_vertices[i + 1]);
                    primitive.Vertices.Add(_vertices[i + 2]);
                    _primitives.Add(primitive);
                }
            }
        }

        private void ProcessLineAssembly()
        {
            for (int i = 0; i < _vertices.Count; i += 2)
            {
                if (i + 1 < _vertices.Count)
                {
                    var primitive = new PrimitiveHolder();
                    primitive.Vertices.Add(_vertices[i]);
                    primitive.Vertices.Add(_vertices[i + 1]);
                    _primitives.Add(primitive);
                }
            }
        }

        private void ProcessPointAssembly()
        {
            foreach (var vertex in _vertices)
            {
                var primitive = new PrimitiveHolder();
                primitive.Vertices.Add(vertex);
                _primitives.Add(primitive);
            }
        }

        #endregion

        #region 裁剪

        private void ProcessClipping()
        {
            // 计算裁剪掩码
            foreach (var vertex in _vertices)
            {
                vertex.ClipMask = CountFrustumClipMask(vertex.ClipPos);
            }

            // 对三角形进行裁剪
            if (_currentRenderStates.PrimitiveType == EPrimitiveType.Triangle)
            {
                List<PrimitiveHolder> clippedPrimitives = new List<PrimitiveHolder>();

                foreach (var primitive in _primitives)
                {
                    ClipTriangle(primitive, clippedPrimitives);
                }

                _primitives = clippedPrimitives;
            }
        }

        private int CountFrustumClipMask(Vector4 clipPos)
        {
            int mask = 0;
            float w = clipPos.w;

            if (clipPos.x < -w) mask |= (1 << 0); // left
            if (clipPos.x > w) mask |= (1 << 1); // right
            if (clipPos.y < -w) mask |= (1 << 2); // bottom
            if (clipPos.y > w) mask |= (1 << 3); // top
            if (clipPos.z < -w) mask |= (1 << 4); // near
            if (clipPos.z > w) mask |= (1 << 5); // far

            return mask;
        }

        private void ClipTriangle(PrimitiveHolder triangle, List<PrimitiveHolder> output)
        {
            // 简化裁剪：如果三个顶点都在视锥体内，直接输出
            if (triangle.Vertices[0].ClipMask == 0 &&
                triangle.Vertices[1].ClipMask == 0 &&
                triangle.Vertices[2].ClipMask == 0)
            {
                output.Add(triangle);
                return;
            }

            // 如果三个顶点都在同一平面外侧，丢弃
            if ((triangle.Vertices[0].ClipMask & triangle.Vertices[1].ClipMask & triangle.Vertices[2].ClipMask) != 0)
            {
                return;
            }

            // 对于部分在视锥体内的三角形，简单处理：直接输出
            // 完整的Sutherland-Hodgman裁剪算法较复杂，这里简化
            output.Add(triangle);
        }

        #endregion

        #region 透视除法和视口变换

        private void ProcessPerspectiveDivide()
        {
            foreach (var vertex in _vertices)
            {
                if (Mathf.Abs(vertex.ClipPos.w) > 1e-6f)
                {
                    float invW = 1f / vertex.ClipPos.w;
                    vertex.ScreenPos = new Vector4(
                        vertex.ClipPos.x * invW,
                        vertex.ClipPos.y * invW,
                        vertex.ClipPos.z * invW,
                        invW
                    );
                }
                else
                {
                    vertex.ScreenPos = Vector4.zero;
                }
            }
        }

        private void ProcessViewportTransform()
        {
            foreach (var vertex in _vertices)
            {
                // NDC [-1,1] -> Screen [0, width/height]
                vertex.ScreenPos.x = (vertex.ScreenPos.x + 1f) * 0.5f * _viewport.Width + _viewport.X;
                vertex.ScreenPos.y = (vertex.ScreenPos.y + 1f) * 0.5f * _viewport.Height + _viewport.Y;
                vertex.ScreenPos.z = vertex.ScreenPos.z * _viewport.InnerP.z + _viewport.InnerO.z;
                // w分量保存1/w用于透视校正插值
            }
        }

        #endregion

        #region 面剔除

        private void ProcessFaceCulling()
        {
            if (!_currentRenderStates.CullFace) return;
            if (_currentRenderStates.PrimitiveType != EPrimitiveType.Triangle) return;

            List<PrimitiveHolder> culledPrimitives = new List<PrimitiveHolder>();

            foreach (var primitive in _primitives)
            {
                if (primitive.Vertices.Count >= 3)
                {
                    Vector2 v0 = new Vector2(primitive.Vertices[0].ScreenPos.x, primitive.Vertices[0].ScreenPos.y);
                    Vector2 v1 = new Vector2(primitive.Vertices[1].ScreenPos.x, primitive.Vertices[1].ScreenPos.y);
                    Vector2 v2 = new Vector2(primitive.Vertices[2].ScreenPos.x, primitive.Vertices[2].ScreenPos.y);

                    // 计算叉积判断朝向
                    float area = (v1.x - v0.x) * (v2.y - v0.y) - (v2.x - v0.x) * (v1.y - v0.y);

                    // 背面剔除（逆时针为正面）
                    if (area > 0)
                    {
                        culledPrimitives.Add(primitive);
                    }
                }
            }

            _primitives = culledPrimitives;
        }

        #endregion

        #region 光栅化

        private void ProcessRasterization()
        {
            switch (_currentRenderStates.PrimitiveType)
            {
                case EPrimitiveType.Triangle:
                    RasterizeTriangles();
                    break;
                case EPrimitiveType.Point:
                    RasterizePoints();
                    break;
                case EPrimitiveType.Line:
                    RasterizeLines();
                    break;
            }
        }

        private void RasterizeTriangles()
        {
            foreach (var primitive in _primitives)
            {
                if (primitive.Vertices.Count >= 3)
                {
                    RasterizeTriangle(primitive.Vertices[0], primitive.Vertices[1], primitive.Vertices[2]);
                }
            }
        }

        private void RasterizeTriangle(VertexHolder v0, VertexHolder v1, VertexHolder v2)
        {
            // 计算三角形AABB包围盒
            GeometryUtils.TriangleBoundingBox(v0.ScreenPos, v1.ScreenPos, v2.ScreenPos,
                _viewport.Width, _viewport.Height, out int minX, out int minY, out int maxX, out int maxY);

            // 使用2x2像素块进行光栅化（支持Early-Z和导数计算）
            if (_enableEarlyZ && _rasterSamples == 1)
            {
                RasterizeTriangleWithPixelQuad(v0, v1, v2, minX, minY, maxX, maxY);
            }
            else
            {
                RasterizeTrianglePerPixel(v0, v1, v2, minX, minY, maxX, maxY);
            }
        }

        /// <summary>
        /// 使用2x2像素块光栅化（支持Early-Z和导数）
        /// </summary>
        private void RasterizeTriangleWithPixelQuad(VertexHolder v0, VertexHolder v1, VertexHolder v2,
            int minX, int minY, int maxX, int maxY)
        {
            PixelQuadContext quad = new PixelQuadContext();
            int varyingSize = _currentShader.GetVaryingSize();
            quad.SetVaryingsSize(varyingSize);
            quad.ShaderProgram = _currentShader;
            quad.FrontFacing = true; // 假设面向前方，实际应计算

            // 存储顶点数据
            quad.VertPos[0] = v0.ScreenPos;
            quad.VertPos[1] = v1.ScreenPos;
            quad.VertPos[2] = v2.ScreenPos;
            quad.VertW = new Vector4(v0.ScreenPos.w, v1.ScreenPos.w, v2.ScreenPos.w, 0);
            quad.VertVaryings[0] = v0.Varyings;
            quad.VertVaryings[1] = v1.Varyings;
            quad.VertVaryings[2] = v2.Varyings;

            // 以2x2块为单位遍历
            for (int y = minY; y <= maxY; y += 2)
            {
                for (int x = minX; x <= maxX; x += 2)
                {
                    quad.Init(x, y, _rasterSamples);

                    // 检查2x2块中每个像素是否在三角形内
                    bool anyInside = false;
                    for (int py = 0; py < 2 && y + py <= maxY; py++)
                    {
                        for (int px = 0; px < 2 && x + px <= maxX; px++)
                        {
                            int pixelIdx = py * 2 + px;
                            PixelContext pixel = quad.Pixels[pixelIdx];

                            Vector2 p = new Vector2(x + px + 0.5f, y + py + 0.5f);
                            Vector2 a = new Vector2(v0.ScreenPos.x, v0.ScreenPos.y);
                            Vector2 b = new Vector2(v1.ScreenPos.x, v1.ScreenPos.y);
                            Vector2 c = new Vector2(v2.ScreenPos.x, v2.ScreenPos.y);

                            if (GeometryUtils.Barycentric2D(a, b, c, p, out Vector3 barycentric))
                            {
                                pixel.Samples[0].Inside = true;
                                pixel.Samples[0].Barycentric = new Vector4(barycentric.x, barycentric.y, barycentric.z, 0);
                                pixel.InitCoverage();
                                anyInside = true;
                            }
                        }
                    }

                    if (!anyInside) continue;

                    // Early-Z测试（2x2块）
                    if (_enableEarlyZ && _currentRenderStates.DepthTest && EarlyZTest(quad))
                    {
                        continue; // 整个块被剔除
                    }

                    // 处理2x2块中的每个像素
                    ProcessPixelQuad(quad);
                }
            }
        }

        /// <summary>
        /// Early-Z测试（2x2像素块）
        /// </summary>
        private bool EarlyZTest(PixelQuadContext quad)
        {
            if (_fboDepth == null) return false;

            int rejectedCount = 0;
            for (int i = 0; i < 4; i++)
            {
                PixelContext pixel = quad.Pixels[i];
                if (!pixel.Inside) continue;

                var sample = pixel.Samples[0];
                Vector3 bc = new Vector3(sample.Barycentric.x, sample.Barycentric.y, sample.Barycentric.z);

                // 计算深度
                float depth = GeometryUtils.BarycentricInterpolate(
                    quad.VertPos[0].z, quad.VertPos[1].z, quad.VertPos[2].z, bc);

                // 获取当前深度缓冲值
                int x = sample.FboCoord.x;
                int y = sample.FboCoord.y;
                float bufferDepth = GetDepth(x, y, 0);

                // 深度测试
                if (!DepthTest.Test(depth, bufferDepth, _currentRenderStates.DepthFunc))
                {
                    rejectedCount++;
                }
            }

            // 如果所有像素都被拒绝，则剔除整个块
            return rejectedCount == 4;
        }

        /// <summary>
        /// 处理2x2像素块（计算导数并执行片元着色器）
        /// </summary>
        private void ProcessPixelQuad(PixelQuadContext quad)
        {
            int varyingSize = _currentShader.GetVaryingSize();

            // 为每个像素插值varying（用于导数计算）
            for (int i = 0; i < 4; i++)
            {
                PixelContext pixel = quad.Pixels[i];
                if (!pixel.Inside) continue;

                var sample = pixel.Samples[0];
                Vector3 bc = new Vector3(sample.Barycentric.x, sample.Barycentric.y, sample.Barycentric.z);

                // 透视校正插值
                float invW0 = quad.VertW.x;
                float invW1 = quad.VertW.y;
                float invW2 = quad.VertW.z;
                float invW = GeometryUtils.BarycentricInterpolate(invW0, invW1, invW2, bc);

                for (int j = 0; j < varyingSize; j++)
                {
                    float v0Val = quad.VertVaryings[0][j] * invW0;
                    float v1Val = quad.VertVaryings[1][j] * invW1;
                    float v2Val = quad.VertVaryings[2][j] * invW2;
                    float interpolated = GeometryUtils.BarycentricInterpolate(v0Val, v1Val, v2Val, bc);
                    pixel.VaryingsFrag[j] = interpolated / invW;
                }
            }

            // 执行片元着色器（使用导数上下文）
            for (int i = 0; i < 4; i++)
            {
                PixelContext pixel = quad.Pixels[i];
                if (!pixel.Inside) continue;

                var sample = pixel.Samples[0];
                Vector3 bc = new Vector3(sample.Barycentric.x, sample.Barycentric.y, sample.Barycentric.z);

                float depth = GeometryUtils.BarycentricInterpolate(
                    quad.VertPos[0].z, quad.VertPos[1].z, quad.VertPos[2].z, bc);

                int x = sample.FboCoord.x;
                int y = sample.FboCoord.y;

                // 设置导数上下文（用于纹理LOD计算）
                var dfCtx = new Shaders.DerivativeContext
                {
                    P0 = quad.Pixels[0].VaryingsFrag,
                    P1 = quad.Pixels[1].VaryingsFrag,
                    P2 = quad.Pixels[2].VaryingsFrag,
                    P3 = quad.Pixels[3].VaryingsFrag
                };

                ProcessFragmentWithDerivative(x, y, depth, pixel.VaryingsFrag, dfCtx, 0);
            }
        }

        /// <summary>
        /// 逐像素光栅化（传统方式，用于MSAA）
        /// </summary>
        private void RasterizeTrianglePerPixel(VertexHolder v0, VertexHolder v1, VertexHolder v2,
            int minX, int minY, int maxX, int maxY)
        {
            // 遍历包围盒内的像素
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    // 计算重心坐标
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                    Vector2 a = new Vector2(v0.ScreenPos.x, v0.ScreenPos.y);
                    Vector2 b = new Vector2(v1.ScreenPos.x, v1.ScreenPos.y);
                    Vector2 c = new Vector2(v2.ScreenPos.x, v2.ScreenPos.y);

                    if (GeometryUtils.Barycentric2D(a, b, c, p, out Vector3 barycentric))
                    {
                        // 透视校正插值
                        float invW0 = v0.ScreenPos.w;
                        float invW1 = v1.ScreenPos.w;
                        float invW2 = v2.ScreenPos.w;
                        float invW = GeometryUtils.BarycentricInterpolate(invW0, invW1, invW2, barycentric);

                        // 插值深度
                        float depth = GeometryUtils.BarycentricInterpolate(v0.ScreenPos.z, v1.ScreenPos.z, v2.ScreenPos.z, barycentric);

                        // 插值varying数据（透视校正）
                        int varyingSize = _currentShader.GetVaryingSize();
                        float[] varyings = new float[varyingSize];

                        for (int i = 0; i < varyingSize; i++)
                        {
                            float v0Val = v0.Varyings[i] * invW0;
                            float v1Val = v1.Varyings[i] * invW1;
                            float v2Val = v2.Varyings[i] * invW2;
                            float interpolated = GeometryUtils.BarycentricInterpolate(v0Val, v1Val, v2Val, barycentric);
                            varyings[i] = interpolated / invW;
                        }

                        // 多重采样处理
                        for (int sample = 0; sample < _rasterSamples; sample++)
                        {
                            ProcessFragment(x, y, depth, varyings, sample);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 处理片元（带导数上下文）
        /// </summary>
        private void ProcessFragmentWithDerivative(int x, int y, float depth, float[] varyings,
            Shaders.DerivativeContext dfCtx, int sample)
        {
            // 深度测试
            if (_currentRenderStates.DepthTest)
            {
                float bufferDepth = GetDepth(x, y, sample);
                if (!DepthTest.Test(depth, bufferDepth, _currentRenderStates.DepthFunc))
                {
                    return;
                }
            }

            // 执行片元着色器（带导数上下文）
            ERGBA fragColor = _currentShader.ExecuteFragmentShaderWithDerivative(
                new Vector4(x, y, depth, 1f), true, varyings, dfCtx);

            // 写入深度
            if (_currentRenderStates.DepthMask && _fboDepth != null)
            {
                SetDepth(x, y, depth, sample);
            }

            // 颜色混合
            if (_currentRenderStates.Blend && _fboColor != null)
            {
                ERGBA dstColor = GetColor(x, y, sample);
                fragColor = BlendOp.Blend(fragColor, dstColor, _currentRenderStates.BlendParams);
            }

            // 写入颜色
            if (_fboColor != null)
            {
                SetColor(x, y, fragColor, sample);
            }
        }

        private void RasterizePoints()
        {
            // 简化实现
            foreach (var primitive in _primitives)
            {
                if (primitive.Vertices.Count > 0)
                {
                    var v = primitive.Vertices[0];
                    int x = Mathf.RoundToInt(v.ScreenPos.x);
                    int y = Mathf.RoundToInt(v.ScreenPos.y);

                    if (x >= 0 && x < _viewport.Width && y >= 0 && y < _viewport.Height)
                    {
                        ProcessFragment(x, y, v.ScreenPos.z, v.Varyings, 0);
                    }
                }
            }
        }

        private void RasterizeLines()
        {
            // 简化实现：Bresenham直线算法
            foreach (var primitive in _primitives)
            {
                if (primitive.Vertices.Count >= 2)
                {
                    var v0 = primitive.Vertices[0];
                    var v1 = primitive.Vertices[1];

                    int x0 = Mathf.RoundToInt(v0.ScreenPos.x);
                    int y0 = Mathf.RoundToInt(v0.ScreenPos.y);
                    int x1 = Mathf.RoundToInt(v1.ScreenPos.x);
                    int y1 = Mathf.RoundToInt(v1.ScreenPos.y);

                    DrawLine(x0, y0, x1, y1, v0, v1);
                }
            }
        }

        private void DrawLine(int x0, int y0, int x1, int y1, VertexHolder v0, VertexHolder v1)
        {
            int dx = Mathf.Abs(x1 - x0);
            int dy = Mathf.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;

            while (true)
            {
                float t = 0f;
                if (dx + dy > 0)
                {
                    float dist = Mathf.Sqrt((x0 - x1) * (x0 - x1) + (y0 - y1) * (y0 - y1));
                    float totalDist = Mathf.Sqrt(dx * dx + dy * dy);
                    t = 1f - dist / totalDist;
                }

                float depth = Mathf.Lerp(v0.ScreenPos.z, v1.ScreenPos.z, t);
                float[] varyings = new float[v0.Varyings.Length];
                for (int i = 0; i < varyings.Length; i++)
                {
                    varyings[i] = Mathf.Lerp(v0.Varyings[i], v1.Varyings[i], t);
                }

                ProcessFragment(x0, y0, depth, varyings, 0);

                if (x0 == x1 && y0 == y1) break;

                int e2 = 2 * err;
                if (e2 > -dy) { err -= dy; x0 += sx; }
                if (e2 < dx) { err += dx; y0 += sy; }
            }
        }

        #endregion

        #region 片元处理

        private void ProcessFragment(int x, int y, float depth, float[] varyings, int sample)
        {
            // 深度测试
            if (_currentRenderStates.DepthTest)
            {
                float bufferDepth = GetDepth(x, y, sample);
                if (!DepthTest.Test(depth, bufferDepth, _currentRenderStates.DepthFunc))
                {
                    return;
                }
            }

            // 执行片元着色器
            ERGBA fragColor = _currentShader.ExecuteFragmentShader(new Vector4(x, y, depth, 1f), true, varyings);

            // 写入深度
            if (_currentRenderStates.DepthMask && _fboDepth != null)
            {
                SetDepth(x, y, depth, sample);
            }

            // 颜色混合
            if (_currentRenderStates.Blend && _fboColor != null)
            {
                ERGBA dstColor = GetColor(x, y, sample);
                fragColor = BlendOp.Blend(fragColor, dstColor, _currentRenderStates.BlendParams);
            }

            // 写入颜色
            if (_fboColor != null)
            {
                SetColor(x, y, fragColor, sample);
            }
        }

        #endregion

        #region 帧缓冲访问

        private ERGBA GetColor(int x, int y, int sample)
        {
            if (_fboColor == null) return new ERGBA(0, 0, 0, 0);

            if (_fboColor.MultiSample && sample < 4)
            {
                return _fboColor.BufferMs4x[x, y][sample];
            }
            else
            {
                return _fboColor.Buffer[x, y];
            }
        }

        private void SetColor(int x, int y, ERGBA color, int sample)
        {
            if (_fboColor == null) return;

            ERGBA rgba = color;

            if (_fboColor.MultiSample && sample < 4)
            {
                var vec4 = _fboColor.BufferMs4x[x, y];
                vec4[sample] = rgba;
                _fboColor.BufferMs4x[x, y] = vec4;
            }
            else
            {
                _fboColor.Buffer[x, y] = rgba;
            }
        }

        private float GetDepth(int x, int y, int sample)
        {
            if (_fboDepth == null) return 1f;

            if (_fboDepth.MultiSample && sample < 4)
            {
                return _fboDepth.BufferMs4x[x, y][sample];
            }
            else
            {
                return _fboDepth.Buffer[x, y];
            }
        }

        private void SetDepth(int x, int y, float depth, int sample)
        {
            if (_fboDepth == null) return;

            if (_fboDepth.MultiSample && sample < 4)
            {
                var vec4 = _fboDepth.BufferMs4x[x, y];
                vec4[sample] = depth;
                _fboDepth.BufferMs4x[x, y] = vec4;
            }
            else
            {
                _fboDepth.Buffer[x, y] = depth;
            }
        }

        private void MultiSampleResolve()
        {
            if (_fboColor == null || !_fboColor.MultiSample) return;

            for (int y = 0; y < _fboColor.Height; y++)
            {
                for (int x = 0; x < _fboColor.Width; x++)
                {
                    var samples = _fboColor.BufferMs4x[x, y];

                    int r = 0, g = 0, b = 0, a = 0;
                    for (int i = 0; i < 4; i++)
                    {
                        r += samples[i].R;
                        g += samples[i].G;
                        b += samples[i].B;
                        a += samples[i].A;
                    }

                    _fboColor.Buffer[x, y] = new ERGBA((byte)(r / 4), (byte)(g / 4), (byte)(b / 4), (byte)(a / 4));
                }
            }
        }

        #endregion
    }
}

