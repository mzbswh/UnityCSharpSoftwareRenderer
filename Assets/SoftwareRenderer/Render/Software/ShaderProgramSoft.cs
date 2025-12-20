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
        
        // 添加公共访问器，方便外部更新shader状态
        public IVertexShader VertexShader => _vertexShader;
        public IFragmentShader FragmentShader => _fragmentShader;

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
            if (_vertexShader == null)
            {
                // 如果没有顶点着色器，使用默认实现
                // 读取位置属性（假设在location 0）
                int stride = vao.Stride / sizeof(float);
                int offset = vertexIndex * stride;
                
                if (offset + 2 < vao.VertexData.Length)
                {
                    Vector3 pos = new Vector3(
                        vao.VertexData[offset + 0],
                        vao.VertexData[offset + 1],
                        vao.VertexData[offset + 2]
                    );
                    clipPos = new Vector4(pos.x, pos.y, pos.z, 1f);
                }
                return;
            }

            // 准备顶点着色器输入 - 从VAO读取顶点数据
            if (_vertexShader is Shaders.VertexShaderBase vsBase)
            {
                // 读取顶点位置（假设在location 0，偏移为0）
                int stride = vao.Stride / sizeof(float);
                int offset = vertexIndex * stride;
                
                // 通过反射设置Position字段（这里简化处理）
                var posField = vsBase.GetType().GetField("Position");
                if (posField != null && offset + 2 < vao.VertexData.Length)
                {
                    Vector3 pos = new Vector3(
                        vao.VertexData[offset + 0],
                        vao.VertexData[offset + 1],
                        vao.VertexData[offset + 2]
                    );
                    posField.SetValue(vsBase, pos);
                }
                
                vsBase.PrepareExecution();
                vsBase.Execute();
                
                // 从shader获取输出
                clipPos = vsBase.Builtin.Position;
            }
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

