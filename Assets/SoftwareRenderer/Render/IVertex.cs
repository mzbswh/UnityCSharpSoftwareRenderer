using UnityEngine;
using System.Collections.Generic;

namespace SoftwareRenderer.Render
{
    /// <summary>
    /// 顶点属性
    /// </summary>
    public struct VertexAttribute
    {
        public int Location;
        public int Size;        // 分量数量 (1, 2, 3, 4)
        public int Offset;      // 在顶点数据中的偏移
        public string Name;
    }

    /// <summary>
    /// 顶点数组描述
    /// </summary>
    public struct VertexArray
    {
        public float[] VertexData;          // 交错存储的顶点数据
        public int[] Indices;               // 索引数据
        public int VertexCount;
        public int IndexCount;
        public int Stride;                  // 每个顶点的字节数
        public List<VertexAttribute> Attributes;
    }

    /// <summary>
    /// 顶点数组对象接口
    /// </summary>
    public interface IVertexArrayObject
    {
        VertexArray VertexArray { get; }
        int VertexCount { get; }
        int IndexCount { get; }
    }
}

