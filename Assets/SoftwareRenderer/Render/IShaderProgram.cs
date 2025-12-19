using System.Collections.Generic;

namespace SoftwareRenderer.Render
{
    /// <summary>
    /// Shader程序接口
    /// </summary>
    public interface IShaderProgram
    {
        bool IsValid { get; }
        
        bool SetVertexShader(IVertexShader vertexShader);
        bool SetFragmentShader(IFragmentShader fragmentShader);
        bool Link();
        
        void BindResources(IShaderResources resources);
    }

    /// <summary>
    /// 顶点着色器接口
    /// </summary>
    public interface IVertexShader
    {
        void Execute();
        IVertexShader Clone();
    }

    /// <summary>
    /// 片元着色器接口
    /// </summary>
    public interface IFragmentShader
    {
        void Execute();
        IFragmentShader Clone();
        void PrepareExecution();
    }

    /// <summary>
    /// Shader资源（Uniform数据）
    /// </summary>
    public interface IShaderResources
    {
        void SetUniformBlock(string name, IUniformBlock block);
        void SetUniformSampler(string name, IUniformSampler sampler);
        IUniformBlock GetUniformBlock(string name);
        IUniformSampler GetUniformSampler(string name);
    }

    /// <summary>
    /// Uniform块接口
    /// </summary>
    public interface IUniformBlock
    {
        string Name { get; }
        int Size { get; }
        byte[] Data { get; }
        
        void SetData(byte[] data);
        void SetData<T>(T data) where T : struct;
    }

    /// <summary>
    /// Uniform采样器接口
    /// </summary>
    public interface IUniformSampler
    {
        string Name { get; }
        ITexture Texture { get; set; }
    }
}

