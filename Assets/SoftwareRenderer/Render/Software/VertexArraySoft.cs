using System.Collections.Generic;

namespace SoftwareRenderer.Render.Software
{
    /// <summary>
    /// 软件顶点数组对象
    /// </summary>
    public class VertexArraySoft : IVertexArrayObject
    {
        private VertexArray _vertexArray;

        public VertexArray VertexArray => _vertexArray;
        public int VertexCount => _vertexArray.VertexCount;
        public int IndexCount => _vertexArray.IndexCount;

        public VertexArraySoft(VertexArray vertexArray)
        {
            _vertexArray = vertexArray;
        }
    }
}

