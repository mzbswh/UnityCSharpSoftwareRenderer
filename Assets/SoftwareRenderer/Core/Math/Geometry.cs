using UnityEngine;

namespace SoftwareRenderer.Core.Math
{
    /// <summary>
    /// AABB包围盒
    /// </summary>
    public struct BoundingBox
    {
        public Vector3 Min;
        public Vector3 Max;

        public BoundingBox(Vector3 min, Vector3 max)
        {
            this.Min = min;
            this.Max = max;
        }

        public Vector3 Center => (Min + Max) * 0.5f;
        public Vector3 Size => Max - Min;

        public void Expand(Vector3 point)
        {
            Min = Vector3.Min(Min, point);
            Max = Vector3.Max(Max, point);
        }

        public void Expand(BoundingBox other)
        {
            Min = Vector3.Min(Min, other.Min);
            Max = Vector3.Max(Max, other.Max);
        }

        public bool Contains(Vector3 point)
        {
            return point.x >= Min.x && point.x <= Max.x &&
                   point.y >= Min.y && point.y <= Max.y &&
                   point.z >= Min.z && point.z <= Max.z;
        }

        public bool Intersects(BoundingBox other)
        {
            return !(other.Min.x > Max.x || other.Max.x < Min.x ||
                     other.Min.y > Max.y || other.Max.y < Min.y ||
                     other.Min.z > Max.z || other.Max.z < Min.z);
        }

        public static BoundingBox FromPoints(Vector3[] points)
        {
            if (points == null || points.Length == 0)
                return new BoundingBox(Vector3.zero, Vector3.zero);

            Vector3 min = points[0];
            Vector3 max = points[0];

            for (int i = 1; i < points.Length; i++)
            {
                min = Vector3.Min(min, points[i]);
                max = Vector3.Max(max, points[i]);
            }

            return new BoundingBox(min, max);
        }

        public Vector3[] GetCorners()
        {
            return new Vector3[]
            {
                new Vector3(Min.x, Min.y, Min.z),
                new Vector3(Max.x, Min.y, Min.z),
                new Vector3(Min.x, Max.y, Min.z),
                new Vector3(Max.x, Max.y, Min.z),
                new Vector3(Min.x, Min.y, Max.z),
                new Vector3(Max.x, Min.y, Max.z),
                new Vector3(Min.x, Max.y, Max.z),
                new Vector3(Max.x, Max.y, Max.z)
            };
        }
    }

    /// <summary>
    /// 视锥体平面
    /// </summary>
    public struct Plane
    {
        public Vector3 Normal;
        public float Distance;

        public Plane(Vector3 normal, float distance)
        {
            this.Normal = normal.normalized;
            this.Distance = distance;
        }

        public Plane(Vector3 normal, Vector3 point)
        {
            this.Normal = normal.normalized;
            this.Distance = -Vector3.Dot(normal, point);
        }

        public float GetDistanceToPoint(Vector3 point)
        {
            return Vector3.Dot(Normal, point) + Distance;
        }

        public bool IsPointOnPositiveSide(Vector3 point)
        {
            return GetDistanceToPoint(point) > 0;
        }
    }

    /// <summary>
    /// 视锥体（6个平面）
    /// </summary>
    public struct Frustum
    {
        public Plane Left;
        public Plane Right;
        public Plane Bottom;
        public Plane Top;
        public Plane Near;
        public Plane Far;

        public Plane this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0: return Left;
                    case 1: return Right;
                    case 2: return Bottom;
                    case 3: return Top;
                    case 4: return Near;
                    case 5: return Far;
                    default: throw new System.IndexOutOfRangeException();
                }
            }
        }

        public static Frustum FromMatrix(Matrix4x4 vp)
        {
            Frustum frustum = new Frustum();

            // 从组合的View-Projection矩阵提取6个平面
            // Left plane
            frustum.Left = new Plane(
                new Vector3(vp.m30 + vp.m00, vp.m31 + vp.m01, vp.m32 + vp.m02),
                vp.m33 + vp.m03
            );

            // Right plane
            frustum.Right = new Plane(
                new Vector3(vp.m30 - vp.m00, vp.m31 - vp.m01, vp.m32 - vp.m02),
                vp.m33 - vp.m03
            );

            // Bottom plane
            frustum.Bottom = new Plane(
                new Vector3(vp.m30 + vp.m10, vp.m31 + vp.m11, vp.m32 + vp.m12),
                vp.m33 + vp.m13
            );

            // Top plane
            frustum.Top = new Plane(
                new Vector3(vp.m30 - vp.m10, vp.m31 - vp.m11, vp.m32 - vp.m12),
                vp.m33 - vp.m13
            );

            // Near plane
            frustum.Near = new Plane(
                new Vector3(vp.m30 + vp.m20, vp.m31 + vp.m21, vp.m32 + vp.m22),
                vp.m33 + vp.m23
            );

            // Far plane
            frustum.Far = new Plane(
                new Vector3(vp.m30 - vp.m20, vp.m31 - vp.m21, vp.m32 - vp.m22),
                vp.m33 - vp.m23
            );

            return frustum;
        }

        public bool IntersectsBoundingBox(BoundingBox box)
        {
            Vector3[] corners = box.GetCorners();

            // 对每个平面检查
            for (int i = 0; i < 6; i++)
            {
                Plane plane = this[i];
                bool allOutside = true;

                // 检查所有8个顶点是否都在平面外侧
                for (int j = 0; j < 8; j++)
                {
                    if (plane.GetDistanceToPoint(corners[j]) >= 0)
                    {
                        allOutside = false;
                        break;
                    }
                }

                // 如果所有顶点都在某个平面外侧，则包围盒完全在视锥体外
                if (allOutside)
                    return false;
            }

            return true;
        }

        public bool ContainsPoint(Vector3 point)
        {
            for (int i = 0; i < 6; i++)
            {
                if (this[i].GetDistanceToPoint(point) < 0)
                    return false;
            }
            return true;
        }
    }

    /// <summary>
    /// 几何工具类
    /// </summary>
    public static class GeometryUtils
    {
        /// <summary>
        /// 计算重心坐标
        /// </summary>
        public static bool Barycentric(Vector3 v0, Vector3 v1, Vector3 v2, Vector3 p, out Vector3 barycentric)
        {
            Vector3 e0 = v1 - v0;
            Vector3 e1 = v2 - v0;
            Vector3 e2 = p - v0;

            float d00 = Vector3.Dot(e0, e0);
            float d01 = Vector3.Dot(e0, e1);
            float d11 = Vector3.Dot(e1, e1);
            float d20 = Vector3.Dot(e2, e0);
            float d21 = Vector3.Dot(e2, e1);

            float denom = d00 * d11 - d01 * d01;
            if (Mathf.Abs(denom) < 1e-6f)
            {
                barycentric = Vector3.zero;
                return false;
            }

            float v = (d11 * d20 - d01 * d21) / denom;
            float w = (d00 * d21 - d01 * d20) / denom;
            float u = 1.0f - v - w;

            barycentric = new Vector3(u, v, w);
            return u >= 0 && v >= 0 && w >= 0;
        }

        /// <summary>
        /// 2D重心坐标（用于光栅化）
        /// </summary>
        public static bool Barycentric2D(Vector2 v0, Vector2 v1, Vector2 v2, Vector2 p, out Vector3 barycentric)
        {
            Vector2 e0 = v1 - v0;
            Vector2 e1 = v2 - v0;
            Vector2 e2 = p - v0;

            float d00 = Vector2.Dot(e0, e0);
            float d01 = Vector2.Dot(e0, e1);
            float d11 = Vector2.Dot(e1, e1);
            float d20 = Vector2.Dot(e2, e0);
            float d21 = Vector2.Dot(e2, e1);

            float denom = d00 * d11 - d01 * d01;
            if (Mathf.Abs(denom) < 1e-6f)
            {
                barycentric = Vector3.zero;
                return false;
            }

            float v = (d11 * d20 - d01 * d21) / denom;
            float w = (d00 * d21 - d01 * d20) / denom;
            float u = 1.0f - v - w;

            barycentric = new Vector3(u, v, w);
            return u >= -1e-6f && v >= -1e-6f && w >= -1e-6f;
        }

        /// <summary>
        /// 2D重心坐标（无GC分配版本，用于光栅化热路径）
        /// </summary>
        public static bool Barycentric2D(float v0x, float v0y, float v1x, float v1y, float v2x, float v2y, 
            float px, float py, out Vector3 barycentric)
        {
            float e0x = v1x - v0x;
            float e0y = v1y - v0y;
            float e1x = v2x - v0x;
            float e1y = v2y - v0y;
            float e2x = px - v0x;
            float e2y = py - v0y;

            float d00 = e0x * e0x + e0y * e0y;
            float d01 = e0x * e1x + e0y * e1y;
            float d11 = e1x * e1x + e1y * e1y;
            float d20 = e2x * e0x + e2y * e0y;
            float d21 = e2x * e1x + e2y * e1y;

            float denom = d00 * d11 - d01 * d01;
            if (Mathf.Abs(denom) < 1e-6f)
            {
                barycentric = Vector3.zero;
                return false;
            }

            float v = (d11 * d20 - d01 * d21) / denom;
            float w = (d00 * d21 - d01 * d20) / denom;
            float u = 1.0f - v - w;

            barycentric = new Vector3(u, v, w);
            return u >= -1e-6f && v >= -1e-6f && w >= -1e-6f;
        }

        /// <summary>
        /// 线性插值（用于Varying数据）
        /// </summary>
        public static float Lerp(float a, float b, float t)
        {
            return a + (b - a) * t;
        }

        public static Vector2 Lerp(Vector2 a, Vector2 b, float t)
        {
            return a + (b - a) * t;
        }

        public static Vector3 Lerp(Vector3 a, Vector3 b, float t)
        {
            return a + (b - a) * t;
        }

        public static Vector4 Lerp(Vector4 a, Vector4 b, float t)
        {
            return a + (b - a) * t;
        }

        /// <summary>
        /// 重心坐标插值
        /// </summary>
        public static float BarycentricInterpolate(float a, float b, float c, Vector3 barycentric)
        {
            return a * barycentric.x + b * barycentric.y + c * barycentric.z;
        }

        public static Vector2 BarycentricInterpolate(Vector2 a, Vector2 b, Vector2 c, Vector3 barycentric)
        {
            return a * barycentric.x + b * barycentric.y + c * barycentric.z;
        }

        public static Vector3 BarycentricInterpolate(Vector3 a, Vector3 b, Vector3 c, Vector3 barycentric)
        {
            return a * barycentric.x + b * barycentric.y + c * barycentric.z;
        }

        public static Vector4 BarycentricInterpolate(Vector4 a, Vector4 b, Vector4 c, Vector3 barycentric)
        {
            return a * barycentric.x + b * barycentric.y + c * barycentric.z;
        }

        /// <summary>
        /// 计算三角形的AABB包围盒（屏幕空间）
        /// </summary>
        public static void TriangleBoundingBox(Vector4 v0, Vector4 v1, Vector4 v2, float width, float height,
            out int minX, out int minY, out int maxX, out int maxY)
        {
            float xMin = Mathf.Min(v0.x, Mathf.Min(v1.x, v2.x));
            float xMax = Mathf.Max(v0.x, Mathf.Max(v1.x, v2.x));
            float yMin = Mathf.Min(v0.y, Mathf.Min(v1.y, v2.y));
            float yMax = Mathf.Max(v0.y, Mathf.Max(v1.y, v2.y));

            minX = Mathf.Max(0, Mathf.FloorToInt(xMin));
            minY = Mathf.Max(0, Mathf.FloorToInt(yMin));
            maxX = Mathf.Min(Mathf.FloorToInt(width) - 1, Mathf.CeilToInt(xMax));
            maxY = Mathf.Min(Mathf.FloorToInt(height) - 1, Mathf.CeilToInt(yMax));
        }
    }
}

