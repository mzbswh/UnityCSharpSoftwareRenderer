using SoftwareRenderer.Core;

namespace SoftwareRenderer.Render
{
    /// <summary>
    /// 渲染器抽象接口
    /// </summary>
    public interface IRenderer
    {
        ERendererType Type { get; }

        bool Create();
        void Destroy();

        // 资源创建
        IFrameBuffer CreateFrameBuffer(bool offscreen);
        ITexture CreateTexture(ETextureDesc desc);
        IVertexArrayObject CreateVertexArrayObject(VertexArray vertexArray);
        IShaderProgram CreateShaderProgram();
        IPipelineStates CreatePipelineStates(RenderStates states);
        IUniformBlock CreateUniformBlock(string name, int size);
        IUniformSampler CreateUniformSampler(string name, ETextureDesc desc);

        // 渲染管线
        void BeginRenderPass(IFrameBuffer frameBuffer, ClearStates states);
        void SetViewport(int x, int y, int width, int height);
        void SetVertexArrayObject(IVertexArrayObject vao);
        void SetShaderProgram(IShaderProgram program);
        void SetShaderResources(IShaderResources resources);
        void SetPipelineStates(IPipelineStates states);
        void Draw();
        void EndRenderPass();
        void WaitIdle();
    }
}

