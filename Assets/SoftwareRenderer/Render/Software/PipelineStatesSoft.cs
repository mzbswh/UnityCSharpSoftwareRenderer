using SoftwareRenderer.Core;

namespace SoftwareRenderer.Render.Software
{
    /// <summary>
    /// 管线状态软件实现
    /// </summary>
    public class PipelineStatesSoft : IPipelineStates
    {
        private RenderStates _renderStates;

        public RenderStates RenderStates => _renderStates;

        public PipelineStatesSoft(RenderStates states)
        {
            _renderStates = states;
        }
    }
}

