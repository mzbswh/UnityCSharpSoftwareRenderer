using SoftwareRenderer.Core;
using UnityEngine;

namespace SoftwareRenderer.Render.Software
{
    /// <summary>
    /// 颜色混合工具类
    /// </summary>
    public static class BlendOp
    {
        public static ERGBA Blend(ERGBA src, ERGBA dst, BlendParameters blendParams)
        {
            Vector4 srcVec = new Vector4(src.R / 255f, src.G / 255f, src.B / 255f, src.A / 255f);
            Vector4 dstVec = new Vector4(dst.R / 255f, dst.G / 255f, dst.B / 255f, dst.A / 255f);

            // RGB混合
            float srcFactorRgb = GetBlendFactor(blendParams.BlendSrcRgb, srcVec, dstVec, true);
            float dstFactorRgb = GetBlendFactor(blendParams.BlendDstRgb, srcVec, dstVec, true);

            Vector3 srcRgb = new Vector3(srcVec.x, srcVec.y, srcVec.z) * srcFactorRgb;
            Vector3 dstRgb = new Vector3(dstVec.x, dstVec.y, dstVec.z) * dstFactorRgb;
            Vector3 resultRgb = ApplyBlendFunc(srcRgb, dstRgb, blendParams.BlendFuncRgb);

            // Alpha混合
            float srcFactorAlpha = GetBlendFactor(blendParams.BlendSrcAlpha, srcVec, dstVec, false);
            float dstFactorAlpha = GetBlendFactor(blendParams.BlendDstAlpha, srcVec, dstVec, false);

            float srcAlpha = srcVec.w * srcFactorAlpha;
            float dstAlpha = dstVec.w * dstFactorAlpha;
            float resultAlpha = ApplyBlendFunc(srcAlpha, dstAlpha, blendParams.BlendFuncAlpha);

            return new ERGBA(
                (byte)Mathf.Clamp(resultRgb.x * 255f, 0, 255),
                (byte)Mathf.Clamp(resultRgb.y * 255f, 0, 255),
                (byte)Mathf.Clamp(resultRgb.z * 255f, 0, 255),
                (byte)Mathf.Clamp(resultAlpha * 255f, 0, 255)
            );
        }

        private static float GetBlendFactor(EBlendFactor factor, Vector4 src, Vector4 dst, bool rgb)
        {
            switch (factor)
            {
                case EBlendFactor.Zero:
                    return 0f;
                case EBlendFactor.One:
                    return 1f;
                case EBlendFactor.SrcColor:
                    return rgb ? src.x : src.w; // 简化处理
                case EBlendFactor.SrcAlpha:
                    return src.w;
                case EBlendFactor.DstColor:
                    return rgb ? dst.x : dst.w;
                case EBlendFactor.DstAlpha:
                    return dst.w;
                case EBlendFactor.OneMinusSrcColor:
                    return rgb ? 1f - src.x : 1f - src.w;
                case EBlendFactor.OneMinusSrcAlpha:
                    return 1f - src.w;
                case EBlendFactor.OneMinusDstColor:
                    return rgb ? 1f - dst.x : 1f - dst.w;
                case EBlendFactor.OneMinusDstAlpha:
                    return 1f - dst.w;
                default:
                    return 1f;
            }
        }

        private static Vector3 ApplyBlendFunc(Vector3 src, Vector3 dst, EBlendFunction func)
        {
            switch (func)
            {
                case EBlendFunction.Add:
                    return src + dst;
                case EBlendFunction.Subtract:
                    return src - dst;
                case EBlendFunction.ReverseSubtract:
                    return dst - src;
                case EBlendFunction.Min:
                    return Vector3.Min(src, dst);
                case EBlendFunction.Max:
                    return Vector3.Max(src, dst);
                default:
                    return src + dst;
            }
        }

        private static float ApplyBlendFunc(float src, float dst, EBlendFunction func)
        {
            switch (func)
            {
                case EBlendFunction.Add:
                    return src + dst;
                case EBlendFunction.Subtract:
                    return src - dst;
                case EBlendFunction.ReverseSubtract:
                    return dst - src;
                case EBlendFunction.Min:
                    return Mathf.Min(src, dst);
                case EBlendFunction.Max:
                    return Mathf.Max(src, dst);
                default:
                    return src + dst;
            }
        }
    }
}

