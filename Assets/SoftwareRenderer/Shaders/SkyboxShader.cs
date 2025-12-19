using UnityEngine;
using SoftwareRenderer.Render.Software;
using SoftwareRenderer.Core;

namespace SoftwareRenderer.Shaders
{
    /// <summary>
    /// 天空盒着色器
    /// </summary>
    public static class SkyboxShader
    {
        public struct Attributes
        {
            public Vector3 Position;
        }

        public struct Uniforms
        {
            public Matrix4x4 ViewProjectionMatrix;
            public SamplerCubeSoft SkyboxCube;
        }

        public struct Varyings
        {
            public Vector3 Direction;
        }

        public class VS : VertexShaderBase
        {
            public Attributes Attr;
            public Uniforms Uniforms;
            public Varyings Varyings;

            public override void Execute()
            {
                Varyings.Direction = Attr.Position;

                Vector4 pos = Uniforms.ViewProjectionMatrix * new Vector4(Attr.Position.x, Attr.Position.y, Attr.Position.z, 1f);
                Builtin.Position = pos.WithZ(pos.w); // 强制深度为最远
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

            public override void Execute()
            {
                if (Uniforms.SkyboxCube != null)
                {
                    Builtin.FragColor = Uniforms.SkyboxCube.Sample(Varyings.Direction);
                }
                else
                {
                    Builtin.FragColor = new ERGBA(0, 0, 255, 255); // blue
                }
            }

            public override Render.IFragmentShader Clone()
            {
                return new FS();
            }
        }
    }
}

public static class Vector4Helper
{
    public static Vector4 WithZ(this Vector4 v, float z)
    {
        return new Vector4(v.x, v.y, z, v.w);
    }
}

