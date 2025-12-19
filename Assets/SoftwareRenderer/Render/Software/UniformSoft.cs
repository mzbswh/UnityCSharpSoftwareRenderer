using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace SoftwareRenderer.Render.Software
{
    /// <summary>
    /// Uniform块软件实现
    /// </summary>
    public class UniformBlockSoft : IUniformBlock
    {
        private string _name;
        private byte[] _data;

        public string Name => _name;
        public int Size => _data.Length;
        public byte[] Data => _data;

        public UniformBlockSoft(string name, int size)
        {
            _name = name;
            _data = new byte[size];
        }

        public void SetData(byte[] newData)
        {
            if (newData.Length <= _data.Length)
            {
                Array.Copy(newData, _data, newData.Length);
            }
        }

        public void SetData<T>(T value) where T : struct
        {
            int size = Marshal.SizeOf(typeof(T));
            if (size <= _data.Length)
            {
                IntPtr ptr = Marshal.AllocHGlobal(size);
                try
                {
                    Marshal.StructureToPtr(value, ptr, false);
                    Marshal.Copy(ptr, _data, 0, size);
                }
                finally
                {
                    Marshal.FreeHGlobal(ptr);
                }
            }
        }
    }

    /// <summary>
    /// Uniform采样器软件实现
    /// </summary>
    public class UniformSamplerSoft : IUniformSampler
    {
        private string _name;
        private ITexture _texture;

        public string Name => _name;
        public ITexture Texture 
        { 
            get => _texture;
            set => _texture = value;
        }

        public UniformSamplerSoft(string name)
        {
            _name = name;
        }
    }

    /// <summary>
    /// Shader资源软件实现
    /// </summary>
    public class ShaderResourcesSoft : IShaderResources
    {
        private Dictionary<string, IUniformBlock> uniformBlocks = new Dictionary<string, IUniformBlock>();
        private Dictionary<string, IUniformSampler> uniformSamplers = new Dictionary<string, IUniformSampler>();

        public void SetUniformBlock(string name, IUniformBlock block)
        {
            uniformBlocks[name] = block;
        }

        public void SetUniformSampler(string name, IUniformSampler sampler)
        {
            uniformSamplers[name] = sampler;
        }

        public IUniformBlock GetUniformBlock(string name)
        {
            return uniformBlocks.TryGetValue(name, out var block) ? block : null;
        }

        public IUniformSampler GetUniformSampler(string name)
        {
            return uniformSamplers.TryGetValue(name, out var sampler) ? sampler : null;
        }
    }
}

