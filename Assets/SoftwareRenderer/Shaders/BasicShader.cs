using UnityEngine;
using SoftwareRenderer.Render.Software;
using SoftwareRenderer.Core;

namespace SoftwareRenderer.Shaders
{
    /// <summary>
    /// 基础颜色着色器（用于测试）
    /// </summary>
    public static class BasicShader
    {
        public struct Attributes
        {
            public Vector3 Position;
            public Vector4 Color;
        }

        public struct Uniforms
        {
            public Matrix4x4 ModelViewProjectionMatrix;
        }

        public struct Varyings
        {
            public Vector4 Color;
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
                Varyings.Color = Attr.Color;
            }

            public override Render.IVertexShader Clone()
            {
                return new VS();
            }
        }

        public class FS : FragmentShaderBase
        {
            public Varyings Varyings;

            public override void Execute()
            {
                Builtin.FragColor = new ERGBA(Varyings.Color.x, Varyings.Color.y, Varyings.Color.z, Varyings.Color.w);
            }

            public override Render.IFragmentShader Clone()
            {
                return new FS();
            }
        }
    }
}

