using System;
using SoftwareRenderer.Core;
using UnityEngine;

namespace SoftwareRenderer.Render.Software
{
    /// <summary>
    /// 软件帧缓冲实现
    /// </summary>
    public class FrameBufferSoft : IFrameBuffer
    {
        private static int idCounter = 1;
        private int _id;
        private bool _offscreen;
        private FrameBufferAttachment _colorAttachment;
        private FrameBufferAttachment _depthAttachment;
        private bool _colorReady;
        private bool _depthReady;

        public int Id => _id;
        public bool IsValid => _colorReady || _depthReady;
        public bool IsOffscreen => _offscreen;

        public FrameBufferSoft(bool offscreen)
        {
            _id = idCounter++;
            _offscreen = offscreen;
        }

        public void AttachColor(ITexture texture, int level = 0, int layer = 0)
        {
            _colorAttachment = new FrameBufferAttachment
            {
                Texture = texture,
                Level = level,
                Layer = layer
            };
            _colorReady = true;
        }

        public void AttachDepth(ITexture texture, int level = 0, int layer = 0)
        {
            _depthAttachment = new FrameBufferAttachment
            {
                Texture = texture,
                Level = level,
                Layer = layer
            };
            _depthReady = true;
        }

        public FrameBufferAttachment GetColorAttachment() => _colorAttachment;
        public FrameBufferAttachment GetDepthAttachment() => _depthAttachment;

        public ImageBuffer<ERGBA> GetColorBuffer()
        {
            if (!_colorReady) return null;
            var tex = _colorAttachment.Texture as TextureSoft;
            return tex?.GetImageRGBA(_colorAttachment.Layer);
        }

        public ImageBuffer<float> GetDepthBuffer()
        {
            if (!_depthReady) return null;
            var tex = _depthAttachment.Texture as TextureSoft;
            return tex?.GetImageFloat(_depthAttachment.Layer);
        }
    }
}

