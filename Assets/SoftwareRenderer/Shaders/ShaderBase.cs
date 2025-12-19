using UnityEngine;
using System;
using System.Collections.Generic;
using SoftwareRenderer.Render.Software;
using SoftwareRenderer.Core;

namespace SoftwareRenderer.Shaders
{
    /// <summary>
    /// 导数上下文（用于2x2像素块的偏导数计算）
    /// </summary>
    public struct DerivativeContext
    {
        public float[] P0; // 左下像素的varying数据
        public float[] P1; // 右下像素的varying数据
        public float[] P2; // 左上像素的varying数据
        public float[] P3; // 右上像素的varying数据
    }

    /// <summary>
    /// Shader内置变量
    /// </summary>
    public class ShaderBuiltin
    {
        // 顶点着色器输出
        public Vector4 Position;
        public float PointSize = 1f;

        // 片元着色器输入
        public Vector4 FragCoord;
        public bool FrontFacing;

        // 片元着色器输出
        public ERGBA FragColor;
        public bool Discard;

        // 导数上下文（用于纹理LOD计算）
        public DerivativeContext DfCtx;
    }

    /// <summary>
    /// Shader基类
    /// </summary>
    public abstract class ShaderBase
    {
        public ShaderBuiltin Builtin = new ShaderBuiltin();

        public abstract void Execute();
        public virtual void PrepareExecution() { }
    }

    /// <summary>
    /// 顶点着色器基类
    /// </summary>
    public abstract class VertexShaderBase : ShaderBase, Render.IVertexShader
    {
        public abstract Render.IVertexShader Clone();
    }

    /// <summary>
    /// 片元着色器基类
    /// </summary>
    public abstract class FragmentShaderBase : ShaderBase, Render.IFragmentShader
    {
        public abstract Render.IFragmentShader Clone();
    }
}

