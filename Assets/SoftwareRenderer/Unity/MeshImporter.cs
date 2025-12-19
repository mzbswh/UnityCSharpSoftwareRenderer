using UnityEngine;
using SoftwareRenderer.Render;
using SoftwareRenderer.Core;
using System.Collections.Generic;

namespace SoftwareRenderer.Unity
{
    /// <summary>
    /// Unity Mesh导入器
    /// </summary>
    public static class MeshImporter
    {
        public static VertexArray ImportMesh(Mesh mesh)
        {
            if (mesh == null) return default;

            Vector3[] positions = mesh.vertices;
            Vector3[] normals = mesh.normals;
            Vector2[] uvs = mesh.uv;
            Vector4[] tangents = mesh.tangents;
            int[] indices = mesh.triangles;

            // 构建交错顶点数据
            // 格式: position(3) + normal(3) + uv(2) + tangent(3) = 11 floats per vertex
            int stride = 11;
            float[] vertexData = new float[positions.Length * stride];

            for (int i = 0; i < positions.Length; i++)
            {
                int offset = i * stride;

                // Position
                vertexData[offset + 0] = positions[i].x;
                vertexData[offset + 1] = positions[i].y;
                vertexData[offset + 2] = positions[i].z;

                // Normal
                if (normals != null && i < normals.Length)
                {
                    vertexData[offset + 3] = normals[i].x;
                    vertexData[offset + 4] = normals[i].y;
                    vertexData[offset + 5] = normals[i].z;
                }

                // UV
                if (uvs != null && i < uvs.Length)
                {
                    vertexData[offset + 6] = uvs[i].x;
                    vertexData[offset + 7] = uvs[i].y;
                }

                // Tangent
                if (tangents != null && i < tangents.Length)
                {
                    vertexData[offset + 8] = tangents[i].x;
                    vertexData[offset + 9] = tangents[i].y;
                    vertexData[offset + 10] = tangents[i].z;
                }
            }

            // 构建顶点属性描述
            List<VertexAttribute> attributes = new List<VertexAttribute>
            {
                new VertexAttribute { Location = 0, Size = 3, Offset = 0, Name = "position" },
                new VertexAttribute { Location = 1, Size = 3, Offset = 3, Name = "normal" },
                new VertexAttribute { Location = 2, Size = 2, Offset = 6, Name = "texCoord" },
                new VertexAttribute { Location = 3, Size = 3, Offset = 8, Name = "tangent" }
            };

            return new VertexArray
            {
                VertexData = vertexData,
                Indices = indices,
                VertexCount = positions.Length,
                IndexCount = indices.Length,
                Stride = stride * sizeof(float),
                Attributes = attributes
            };
        }

        public static Core.Math.BoundingBox CalculateBoundingBox(Mesh mesh)
        {
            if (mesh == null || mesh.vertices.Length == 0)
                return new Core.Math.BoundingBox(Vector3.zero, Vector3.zero);

            Vector3 min = mesh.vertices[0];
            Vector3 max = mesh.vertices[0];

            foreach (var vertex in mesh.vertices)
            {
                min = Vector3.Min(min, vertex);
                max = Vector3.Max(max, vertex);
            }

            return new Core.Math.BoundingBox(min, max);
        }
    }
}

