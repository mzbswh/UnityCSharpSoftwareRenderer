namespace SoftwareRenderer.Render
{
    /// <summary>
    /// 帧缓冲附件
    /// </summary>
    public struct FrameBufferAttachment
    {
        public ITexture Texture;
        public int Level;
        public int Layer;
    }

    /// <summary>
    /// 帧缓冲接口
    /// </summary>
    public interface IFrameBuffer
    {
        int Id { get; }
        bool IsValid { get; }
        bool IsOffscreen { get; }

        void AttachColor(ITexture texture, int level = 0, int layer = 0);
        void AttachDepth(ITexture texture, int level = 0, int layer = 0);
        FrameBufferAttachment GetColorAttachment();
        FrameBufferAttachment GetDepthAttachment();
    }
}

