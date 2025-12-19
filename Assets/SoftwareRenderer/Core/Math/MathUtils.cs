using UnityEngine;

namespace SoftwareRenderer.Core.Math
{
    /// <summary>
    /// 数学工具类
    /// </summary>
    public static class MathUtils
    {
        public const float PI = Mathf.PI;
        public const float EPSILON = 1e-6f;

        /// <summary>
        /// 安全除法
        /// </summary>
        public static float SafeDiv(float a, float b, float defaultValue = 0f)
        {
            return Mathf.Abs(b) > EPSILON ? a / b : defaultValue;
        }

        /// <summary>
        /// 钳制到[0,1]
        /// </summary>
        public static float Saturate(float value)
        {
            return Mathf.Clamp01(value);
        }

        public static Vector2 Saturate(Vector2 value)
        {
            return new Vector2(Saturate(value.x), Saturate(value.y));
        }

        public static Vector3 Saturate(Vector3 value)
        {
            return new Vector3(Saturate(value.x), Saturate(value.y), Saturate(value.z));
        }

        public static Vector4 Saturate(Vector4 value)
        {
            return new Vector4(Saturate(value.x), Saturate(value.y), Saturate(value.z), Saturate(value.w));
        }

        /// <summary>
        /// 创建LookAt矩阵
        /// </summary>
        public static Matrix4x4 LookAt(Vector3 eye, Vector3 center, Vector3 up)
        {
            Vector3 f = (center - eye).normalized;
            Vector3 s = Vector3.Cross(f, up).normalized;
            Vector3 u = Vector3.Cross(s, f);

            Matrix4x4 result = Matrix4x4.identity;
            result.m00 = s.x;
            result.m10 = s.y;
            result.m20 = s.z;
            result.m01 = u.x;
            result.m11 = u.y;
            result.m21 = u.z;
            result.m02 = -f.x;
            result.m12 = -f.y;
            result.m22 = -f.z;
            result.m03 = -Vector3.Dot(s, eye);
            result.m13 = -Vector3.Dot(u, eye);
            result.m23 = Vector3.Dot(f, eye);

            return result;
        }

        /// <summary>
        /// 创建透视投影矩阵
        /// </summary>
        public static Matrix4x4 Perspective(float fovy, float aspect, float near, float far, bool reverseZ = false)
        {
            float tanHalfFovy = Mathf.Tan(fovy / 2f);

            Matrix4x4 result = Matrix4x4.zero;
            result.m00 = 1f / (aspect * tanHalfFovy);
            result.m11 = 1f / tanHalfFovy;
            result.m32 = -1f;

            if (reverseZ)
            {
                // Reverse-Z: [far, near] -> [0, 1]
                result.m22 = near / (far - near);
                result.m23 = -(far * near) / (far - near);
            }
            else
            {
                // Standard: [near, far] -> [0, 1]
                result.m22 = far / (near - far);
                result.m23 = -(far * near) / (far - near);
            }

            return result;
        }

        /// <summary>
        /// 创建正交投影矩阵
        /// </summary>
        public static Matrix4x4 Ortho(float left, float right, float bottom, float top, float near, float far)
        {
            Matrix4x4 result = Matrix4x4.identity;
            result.m00 = 2f / (right - left);
            result.m11 = 2f / (top - bottom);
            result.m22 = -1f / (far - near);
            result.m03 = -(right + left) / (right - left);
            result.m13 = -(top + bottom) / (top - bottom);
            result.m23 = -near / (far - near);

            return result;
        }

        /// <summary>
        /// 计算法线矩阵（用于法线变换）
        /// </summary>
        public static Matrix4x4 NormalMatrix(Matrix4x4 modelMatrix)
        {
            // 法线矩阵 = transpose(inverse(modelMatrix))
            // 对于正交矩阵，可以简化为modelMatrix本身
            // 这里使用完整计算
            return modelMatrix.inverse.transpose;
        }

        /// <summary>
        /// 角度转弧度
        /// </summary>
        public static float Deg2Rad(float degrees)
        {
            return degrees * Mathf.Deg2Rad;
        }

        /// <summary>
        /// 弧度转角度
        /// </summary>
        public static float Rad2Deg(float radians)
        {
            return radians * Mathf.Rad2Deg;
        }

        /// <summary>
        /// 平滑步进函数
        /// </summary>
        public static float SmoothStep(float edge0, float edge1, float x)
        {
            float t = Saturate((x - edge0) / (edge1 - edge0));
            return t * t * (3f - 2f * t);
        }

        /// <summary>
        /// 混合
        /// </summary>
        public static float Mix(float x, float y, float a)
        {
            return x * (1f - a) + y * a;
        }

        public static Vector2 Mix(Vector2 x, Vector2 y, float a)
        {
            return x * (1f - a) + y * a;
        }

        public static Vector3 Mix(Vector3 x, Vector3 y, float a)
        {
            return x * (1f - a) + y * a;
        }

        public static Vector4 Mix(Vector4 x, Vector4 y, float a)
        {
            return x * (1f - a) + y * a;
        }

        /// <summary>
        /// 计算mipmap级别
        /// </summary>
        public static float CalculateMipLevel(Vector2 texCoord, Vector2 texSize, Vector2 ddx, Vector2 ddy)
        {
            Vector2 dx = ddx * texSize;
            Vector2 dy = ddy * texSize;
            float d = Mathf.Max(Vector2.Dot(dx, dx), Vector2.Dot(dy, dy));
            return Mathf.Max(0.5f * Mathf.Log(d, 2f), 0f);
        }

        /// <summary>
        /// Gamma校正
        /// </summary>
        public static float LinearToGamma(float linear)
        {
            return Mathf.Pow(linear, 1f / 2.2f);
        }

        public static Vector3 LinearToGamma(Vector3 linear)
        {
            return new Vector3(
                LinearToGamma(linear.x),
                LinearToGamma(linear.y),
                LinearToGamma(linear.z)
            );
        }

        public static float GammaToLinear(float gamma)
        {
            return Mathf.Pow(gamma, 2.2f);
        }

        public static Vector3 GammaToLinear(Vector3 gamma)
        {
            return new Vector3(
                GammaToLinear(gamma.x),
                GammaToLinear(gamma.y),
                GammaToLinear(gamma.z)
            );
        }
    }
}

