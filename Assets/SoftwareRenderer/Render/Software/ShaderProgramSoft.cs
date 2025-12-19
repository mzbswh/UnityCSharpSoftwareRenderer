using System;
using System.Collections.Generic;
using UnityEngine;
using SoftwareRenderer.Core;

namespace SoftwareRenderer.Render.Software
{
    /// <summary>
    /// Shader程序软件实现
    /// </summary>
    public class ShaderProgramSoft : IShaderProgram
    {
        private IVertexShader _vertexShader;
        private IFragmentShader _fragmentShader;
        private IShaderResources _resources;
        private bool _isLinked;

        public bool IsValid => _isLinked;

        public bool SetVertexShader(IVertexShader vs)
        {
            _vertexShader = vs;
            return true;
        }

        public bool SetFragmentShader(IFragmentShader fs)
        {
            _fragmentShader = fs;
            return true;
        }

        public bool Link()
        {
            _isLinked = _vertexShader != null && _fragmentShader != null;
            return _isLinked;
        }

        public void BindResources(IShaderResources res)
        {
            _resources = res;
        }

        public int GetVaryingSize()
        {
            // 假设varying数据大小固定（位置、法线、UV、切线等）
            return 64; // 足够存储常用的varying数据
        }

        public void ExecuteVertexShader(VertexArray vao, int vertexIndex, ref Vector4 clipPos, float[] varyings)
        {
            if (_vertexShader == null) return;

            // 这里需要通过反射或其他方式调用顶点着色器
            // 简化实现：假设_vertexShader有Execute方法
            _vertexShader.Execute();

            // 从shader获取输出（这里需要shader基类支持）
        }

        public ERGBA ExecuteFragmentShader(Vector4 fragCoord, bool frontFacing, float[] varyings)
        {
            if (_fragmentShader == null) return new ERGBA(0, 0, 0, 255);

            // 准备并执行shader
            if (_fragmentShader is Shaders.FragmentShaderBase fsBase)
            {
                fsBase.Builtin.FragCoord = fragCoord;
                fsBase.Builtin.FrontFacing = frontFacing;
                fsBase.Builtin.Discard = false;

                fsBase.PrepareExecution();
                fsBase.Execute();

                if (fsBase.Builtin.Discard)
                {
                    return new ERGBA(0, 0, 0, 0);
                }

                return fsBase.Builtin.FragColor;
            }

            // 默认返回白色
            return new ERGBA(255, 255, 255, 255);
        }

        /// <summary>
        /// 执行片元着色器（带导数上下文）
        /// </summary>
        public ERGBA ExecuteFragmentShaderWithDerivative(Vector4 fragCoord, bool frontFacing, 
            float[] varyings, Shaders.DerivativeContext dfCtx)
        {
            if (_fragmentShader == null) return new ERGBA(0, 0, 0, 255);

            // 准备并执行shader
            if (_fragmentShader is Shaders.FragmentShaderBase fsBase)
            {
                fsBase.Builtin.FragCoord = fragCoord;
                fsBase.Builtin.FrontFacing = frontFacing;
                fsBase.Builtin.Discard = false;
                fsBase.Builtin.DfCtx = dfCtx; // 设置导数上下文

                fsBase.PrepareExecution();
                fsBase.Execute();

                if (fsBase.Builtin.Discard)
                {
                    return new ERGBA(0, 0, 0, 0);
                }

                return fsBase.Builtin.FragColor;
            }

            // 默认返回白色
            return new ERGBA(255, 255, 255, 255);
        }
    }
}

