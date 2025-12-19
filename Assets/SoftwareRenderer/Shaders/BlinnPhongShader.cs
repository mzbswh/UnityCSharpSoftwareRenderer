using UnityEngine;
using SoftwareRenderer.Render.Software;
using SoftwareRenderer.Core;

namespace SoftwareRenderer.Shaders
{
    /// <summary>
    /// Blinn-Phong光照着色器
    /// </summary>
    public static class BlinnPhongShader
    {
        public struct Attributes
        {
            public Vector3 Position;
            public Vector3 Normal;
            public Vector2 TexCoord;
            public Vector3 Tangent;
        }

        public struct Uniforms
        {
            // Model uniforms
            public Matrix4x4 ModelMatrix;
            public Matrix4x4 ModelViewProjectionMatrix;
            public Matrix4x4 NormalMatrix;
            public Matrix4x4 ShadowMVPMatrix;

            // Scene uniforms
            public Vector3 AmbientColor;
            public Vector3 CameraPosition;
            public Vector3 LightPosition;
            public Vector3 LightColor;

            // Material uniforms
            public bool EnableLight;
            public bool EnableShadow;
            public float SpecularPower;
            public ERGBA BaseColor;

            // Samplers
            public Sampler2DSoft AlbedoMap;
            public Sampler2DSoft NormalMap;
            public Sampler2DSoft EmissiveMap;
            public Sampler2DSoft AoMap;
            public Sampler2DSoft ShadowMap;
        }

        public struct Varyings
        {
            public Vector2 TexCoord;
            public Vector3 WorldPos;
            public Vector3 Normal;
            public Vector3 Tangent;
            public Vector4 ShadowFragPos;
        }

        public class VS : VertexShaderBase
        {
            public Attributes Attr;
            public Uniforms Uniforms;
            public Varyings Varyings;

            public override void Execute()
            {
                Vector4 pos = new Vector4(Attr.Position.x, Attr.Position.y, Attr.Position.z, 1f);
                Builtin.Position = Uniforms.ModelViewProjectionMatrix * pos;

                Varyings.TexCoord = Attr.TexCoord;
                Varyings.WorldPos = (Uniforms.ModelMatrix * pos).ToVector3();
                Varyings.Normal = (Uniforms.NormalMatrix * new Vector4(Attr.Normal.x, Attr.Normal.y, Attr.Normal.z, 0f)).normalized.ToVector3();
                Varyings.Tangent = (Uniforms.NormalMatrix * new Vector4(Attr.Tangent.x, Attr.Tangent.y, Attr.Tangent.z, 0f)).normalized.ToVector3();
                Varyings.ShadowFragPos = Uniforms.ShadowMVPMatrix * pos;
            }

            public override Render.IVertexShader Clone()
            {
                return new VS();
            }
        }

        public class FS : FragmentShaderBase
        {
            public Varyings Varyings;
            public Uniforms Uniforms;

            private const float SPECULAR_EXPONENT = 128f;
            private const float POINT_LIGHT_RANGE_INVERSE = 1f / 5f;

            public override void Execute()
            {
                // 基础颜色
                ERGBA baseColor = Uniforms.BaseColor;
                if (Uniforms.AlbedoMap != null)
                {
                    baseColor = Uniforms.AlbedoMap.Sample(Varyings.TexCoord);
                }

                // 法线
                Vector3 N = Varyings.Normal.normalized;
                if (Uniforms.NormalMap != null)
                {
                    N = GetNormalFromMap();
                }

                // 环境光
                float ao = 1f;
                if (Uniforms.AoMap != null)
                {
                    ao = Uniforms.AoMap.Sample(Varyings.TexCoord).R / 255f;
                }
                Vector3 ambient = new Vector3(
                    baseColor.R / 255f * Uniforms.AmbientColor.x * ao,
                    baseColor.G / 255f * Uniforms.AmbientColor.y * ao,
                    baseColor.B / 255f * Uniforms.AmbientColor.z * ao
                );

                Vector3 finalColor = ambient;

                if (Uniforms.EnableLight)
                {
                    // 光照计算
                    Vector3 lightDir = (Uniforms.LightPosition - Varyings.WorldPos) * POINT_LIGHT_RANGE_INVERSE;
                    float attenuation = Mathf.Clamp01(1f - Vector3.Dot(lightDir, lightDir));
                    lightDir = (Uniforms.LightPosition - Varyings.WorldPos).normalized;

                    // 漫反射
                    float diffuse = Mathf.Max(Vector3.Dot(N, lightDir), 0f);
                    Vector3 diffuseColor = new Vector3(
                        Uniforms.LightColor.x * baseColor.R / 255f * diffuse * attenuation,
                        Uniforms.LightColor.y * baseColor.G / 255f * diffuse * attenuation,
                        Uniforms.LightColor.z * baseColor.B / 255f * diffuse * attenuation
                    );

                    // 镜面反射
                    Vector3 viewDir = (Uniforms.CameraPosition - Varyings.WorldPos).normalized;
                    Vector3 halfVector = (lightDir + viewDir).normalized;
                    float specAngle = Mathf.Max(Vector3.Dot(N, halfVector), 0f);
                    float spec = Mathf.Pow(specAngle, SPECULAR_EXPONENT) * Uniforms.SpecularPower;
                    Vector3 specular = new Vector3(spec, spec, spec);

                    // 阴影
                    float shadow = 1f;
                    if (Uniforms.EnableShadow && Uniforms.ShadowMap != null)
                    {
                        shadow = 1f - CalculateShadow();
                    }

                    finalColor += (diffuseColor + specular) * shadow;
                }

                // 自发光
                if (Uniforms.EmissiveMap != null)
                {
                    ERGBA emissive = Uniforms.EmissiveMap.Sample(Varyings.TexCoord);
                    finalColor += new Vector3(emissive.R / 255f, emissive.G / 255f, emissive.B / 255f);
                }

                Builtin.FragColor = new ERGBA(finalColor.x, finalColor.y, finalColor.z, baseColor.A / 255f);
            }

            private Vector3 GetNormalFromMap()
            {
                Vector3 N = Varyings.Normal.normalized;
                Vector3 T = Varyings.Tangent.normalized;
                T = (T - Vector3.Dot(T, N) * N).normalized;
                Vector3 B = Vector3.Cross(T, N);

                ERGBA normalSample = Uniforms.NormalMap.Sample(Varyings.TexCoord);
                Vector3 tangentNormal = new Vector3(normalSample.R / 255f, normalSample.G / 255f, normalSample.B / 255f) * 2f - Vector3.one;

                Matrix4x4 TBN = Matrix4x4.identity;
                TBN.SetColumn(0, T);
                TBN.SetColumn(1, B);
                TBN.SetColumn(2, N);

                return (TBN * tangentNormal).normalized;
            }

            private float CalculateShadow()
            {
                Vector3 projCoords = Varyings.ShadowFragPos.ToVector3() / Varyings.ShadowFragPos.w;
                float currentDepth = projCoords.z;

                if (currentDepth < 0f || currentDepth > 1f)
                    return 0f;

                float bias = 0.0005f;
                float shadow = 0f;

                // PCF软阴影
                Vector2 texSize = new Vector2(1024, 1024); // 假设shadow map大小
                Vector2 pixelOffset = Vector2.one / texSize;

                for (int x = -1; x <= 1; x++)
                {
                    for (int y = -1; y <= 1; y++)
                    {
                        Vector2 offset = new Vector2(x, y) * pixelOffset;
                        float pcfDepth = Uniforms.ShadowMap.SampleLod(new Vector2(projCoords.x, projCoords.y) + offset, 0f).R / 255f;

                        if (currentDepth - bias > pcfDepth)
                            shadow += 1f;
                    }
                }

                return shadow / 9f;
            }

            public override Render.IFragmentShader Clone()
            {
                return new FS();
            }
        }
    }
}

// 扩展方法
public static class Vector4Extensions
{
    public static Vector3 ToVector3(this Vector4 v)
    {
        return new Vector3(v.x, v.y, v.z);
    }
}

