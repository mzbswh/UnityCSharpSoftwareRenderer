using SoftwareRenderer.Core;

namespace SoftwareRenderer.Render
{
    /// <summary>
    /// 管线状态接口
    /// </summary>
    public interface IPipelineStates
    {
        RenderStates RenderStates { get; }
    }
}

