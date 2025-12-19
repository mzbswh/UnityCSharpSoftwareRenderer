using System;
using SoftwareRenderer.Core;
using UnityEngine;

namespace SoftwareRenderer.Render.Software
{
    /// <summary>
    /// 2D纹理采样器
    /// </summary>
    public class Sampler2DSoft
    {
        private TextureSoft _texture;
        private Core.EWrapMode _wrapModeU;
        private Core.EWrapMode _wrapModeV;
        private Core.EFilterMode _filterMode;
        private Func<float> _lodFunc; // 用于自动计算LOD

        public TextureSoft Texture => _texture;

        public Sampler2DSoft(TextureSoft texture, Core.EWrapMode wrapU, Core.EWrapMode wrapV, Core.EFilterMode filter)
        {
            _texture = texture;
            _wrapModeU = wrapU;
            _wrapModeV = wrapV;
            _filterMode = filter;
        }

        public void SetLodFunc(Func<float> func)
        {
            _lodFunc = func;
        }

        /// <summary>
        /// 从导数上下文计算纹理LOD
        /// </summary>
        public static float CalculateLod(Shaders.DerivativeContext dfCtx, int varyingOffset, Vector2 texSize)
        {
            if (dfCtx.P0 == null || dfCtx.P1 == null || dfCtx.P2 == null || dfCtx.P3 == null)
            {
                return 0f;
            }

            // 从varying数据中提取UV坐标（假设是Vector2）
            Vector2 uv0 = new Vector2(dfCtx.P0[varyingOffset], dfCtx.P0[varyingOffset + 1]);
            Vector2 uv1 = new Vector2(dfCtx.P1[varyingOffset], dfCtx.P1[varyingOffset + 1]);
            Vector2 uv2 = new Vector2(dfCtx.P2[varyingOffset], dfCtx.P2[varyingOffset + 1]);
            
            // 计算x和y方向的导数
            Vector2 ddx = uv1 - uv0; // 右边-左边
            Vector2 ddy = uv2 - uv0; // 上边-左边

            // 乘以纹理尺寸
            ddx *= texSize;
            ddy *= texSize;

            // 计算LOD: 0.5 * log2(max(ddx·ddx, ddy·ddy))
            float d = Mathf.Max(Vector2.Dot(ddx, ddx), Vector2.Dot(ddy, ddy));
            return Mathf.Max(0.5f * Mathf.Log(d, 2f), 0f);
        }

        public ERGBA Sample(Vector2 uv)
        {
            float lod = _lodFunc != null ? _lodFunc() : 0f;
            return SampleLod(uv, lod);
        }

        public ERGBA SampleLod(Vector2 uv, float lod)
        {
            if (_texture.Format == Core.ETextureFormat.RGBA8)
            {
                return SampleRGBALod(uv, lod);
            }
            else if (_texture.Format == Core.ETextureFormat.Float32)
            {
                float value = SampleFloatLod(uv, lod);
                byte gray = (byte)Mathf.Clamp(value * 255f, 0, 255);
                return new ERGBA(gray, gray, gray, 255);
            }

            return new ERGBA(0, 0, 0, 0);
        }

        private ERGBA SampleRGBALod(Vector2 uv, float lod)
        {
            var image = _texture.GetImageRGBA(0);
            if (image == null) return new ERGBA(0, 0, 0, 0);

            int level0 = Mathf.FloorToInt(lod);
            int level1 = Mathf.CeilToInt(lod);
            float t = lod - level0;

            level0 = Mathf.Clamp(level0, 0, image.MipLevels - 1);
            level1 = Mathf.Clamp(level1, 0, image.MipLevels - 1);

            if (_filterMode == Core.EFilterMode.Trilinear && level0 != level1)
            {
                ERGBA sample0 = SampleRGBALevel(uv, level0);
                ERGBA sample1 = SampleRGBALevel(uv, level1);
                return LerpERGBA(sample0, sample1, t);
            }
            else
            {
                return SampleRGBALevel(uv, level0);
            }
        }

        private ERGBA SampleRGBALevel(Vector2 uv, int level)
        {
            var image = _texture.GetImageRGBA(0);
            var buffer = image.GetBuffer(level);

            uv = WrapUV(uv);

            float x = uv.x * buffer.Width;
            float y = uv.y * buffer.Height;

            if (_filterMode == Core.EFilterMode.Point)
            {
                int ix = Mathf.FloorToInt(x) % buffer.Width;
                int iy = Mathf.FloorToInt(y) % buffer.Height;
                ERGBA color = buffer[ix, iy];
                return color;
            }
            else // Bilinear或Trilinear
            {
                int x0 = Mathf.FloorToInt(x);
                int y0 = Mathf.FloorToInt(y);
                int x1 = x0 + 1;
                int y1 = y0 + 1;

                float fx = x - x0;
                float fy = y - y0;

                x0 = ClampCoord(x0, buffer.Width);
                x1 = ClampCoord(x1, buffer.Width);
                y0 = ClampCoord(y0, buffer.Height);
                y1 = ClampCoord(y1, buffer.Height);

                ERGBA c00 = buffer[x0, y0];
                ERGBA c10 = buffer[x1, y0];
                ERGBA c01 = buffer[x0, y1];
                ERGBA c11 = buffer[x1, y1];

                Vector4 v00 = c00.ToVector4() / 255f;
                Vector4 v10 = c10.ToVector4() / 255f;
                Vector4 v01 = c01.ToVector4() / 255f;
                Vector4 v11 = c11.ToVector4() / 255f;

                Vector4 v0 = Vector4.Lerp(v00, v10, fx);
                Vector4 v1 = Vector4.Lerp(v01, v11, fx);
                Vector4 result = Vector4.Lerp(v0, v1, fy);

                return new ERGBA(result.x, result.y, result.z, result.w);
            }
        }

        public float SampleFloatLod(Vector2 uv, float lod)
        {
            var image = _texture.GetImageFloat(0);
            if (image == null) return 0f;

            int level = Mathf.Clamp(Mathf.RoundToInt(lod), 0, image.MipLevels - 1);
            var buffer = image.GetBuffer(level);

            uv = WrapUV(uv);

            float x = uv.x * buffer.Width;
            float y = uv.y * buffer.Height;

            if (_filterMode == Core.EFilterMode.Point)
            {
                int ix = Mathf.FloorToInt(x) % buffer.Width;
                int iy = Mathf.FloorToInt(y) % buffer.Height;
                return buffer[ix, iy];
            }
            else
            {
                int x0 = Mathf.FloorToInt(x);
                int y0 = Mathf.FloorToInt(y);
                int x1 = x0 + 1;
                int y1 = y0 + 1;

                float fx = x - x0;
                float fy = y - y0;

                x0 = ClampCoord(x0, buffer.Width);
                x1 = ClampCoord(x1, buffer.Width);
                y0 = ClampCoord(y0, buffer.Height);
                y1 = ClampCoord(y1, buffer.Height);

                float v00 = buffer[x0, y0];
                float v10 = buffer[x1, y0];
                float v01 = buffer[x0, y1];
                float v11 = buffer[x1, y1];

                float v0 = Mathf.Lerp(v00, v10, fx);
                float v1 = Mathf.Lerp(v01, v11, fx);
                return Mathf.Lerp(v0, v1, fy);
            }
        }

        private Vector2 WrapUV(Vector2 uv)
        {
            uv.x = WrapCoord(uv.x, _wrapModeU);
            uv.y = WrapCoord(uv.y, _wrapModeV);
            return uv;
        }

        private float WrapCoord(float coord, Core.EWrapMode mode)
        {
            switch (mode)
            {
                case Core.EWrapMode.Repeat:
                    return coord - Mathf.Floor(coord);
                case Core.EWrapMode.Clamp:
                    return Mathf.Clamp01(coord);
                case Core.EWrapMode.Mirror:
                    float t = coord - Mathf.Floor(coord);
                    return ((int)Mathf.Floor(coord) % 2) == 0 ? t : 1f - t;
                default:
                    return coord;
            }
        }

        private int ClampCoord(int coord, int size)
        {
            return Mathf.Clamp(coord, 0, size - 1);
        }

        private ERGBA LerpERGBA(ERGBA a, ERGBA b, float t)
        {
            return new ERGBA(
                (byte)Mathf.Lerp(a.R, b.R, t),
                (byte)Mathf.Lerp(a.G, b.G, t),
                (byte)Mathf.Lerp(a.B, b.B, t),
                (byte)Mathf.Lerp(a.A, b.A, t)
            );
        }
    }

    /// <summary>
    /// Cube纹理采样器
    /// </summary>
    public class SamplerCubeSoft
    {
        private TextureSoft _texture;
        private Core.EFilterMode _filterMode;

        public TextureSoft Texture => _texture;

        public SamplerCubeSoft(TextureSoft texture, Core.EFilterMode filter)
        {
            _texture = texture;
            _filterMode = filter;
        }

        public ERGBA Sample(Vector3 direction)
        {
            return SampleLod(direction, 0f);
        }

        public ERGBA SampleLod(Vector3 direction, float lod)
        {
            // 确定采样哪个面
            int face = GetCubeFace(direction, out Vector2 uv);

            var image = _texture.GetImageRGBA(face);
            if (image == null) return new ERGBA(0, 0, 0, 0);

            int level = Mathf.Clamp(Mathf.RoundToInt(lod), 0, image.MipLevels - 1);
            var buffer = image.GetBuffer(level);

            // 采样
            float x = uv.x * buffer.Width;
            float y = uv.y * buffer.Height;

            int ix = Mathf.Clamp(Mathf.FloorToInt(x), 0, buffer.Width - 1);
            int iy = Mathf.Clamp(Mathf.FloorToInt(y), 0, buffer.Height - 1);

            ERGBA color = buffer[ix, iy];
            return color;
        }

        private int GetCubeFace(Vector3 dir, out Vector2 uv)
        {
            Vector3 absDir = new Vector3(Mathf.Abs(dir.x), Mathf.Abs(dir.y), Mathf.Abs(dir.z));

            int face = 0;
            float ma, u, v;

            if (absDir.x >= absDir.y && absDir.x >= absDir.z)
            {
                // X major
                if (dir.x > 0) { face = 0; ma = absDir.x; u = -dir.z; v = -dir.y; } // +X
                else { face = 1; ma = absDir.x; u = dir.z; v = -dir.y; }            // -X
            }
            else if (absDir.y >= absDir.z)
            {
                // Y major
                if (dir.y > 0) { face = 2; ma = absDir.y; u = dir.x; v = dir.z; }   // +Y
                else { face = 3; ma = absDir.y; u = dir.x; v = -dir.z; }            // -Y
            }
            else
            {
                // Z major
                if (dir.z > 0) { face = 4; ma = absDir.z; u = dir.x; v = -dir.y; }  // +Z
                else { face = 5; ma = absDir.z; u = -dir.x; v = -dir.y; }           // -Z
            }

            uv = new Vector2((u / ma + 1f) * 0.5f, (v / ma + 1f) * 0.5f);
            return face;
        }
    }
}

