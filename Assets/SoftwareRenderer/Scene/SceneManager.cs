using System.Collections.Generic;
using UnityEngine;
using SoftwareRenderer.Core;
using SoftwareRenderer.Render;

namespace SoftwareRenderer.Scene
{
    /// <summary>
    /// 材质类
    /// </summary>
    public class Material
    {
        public EShadingModel ShadingModel = EShadingModel.BaseColor;
        public bool DoubleSided = false;
        public EAlphaMode AlphaMode = EAlphaMode.Opaque;
        public Color BaseColor = Color.white;
        public float PointSize = 1f;
        public float LineWidth = 1f;

        // 编译后的渲染对象
        public IShaderProgram ShaderProgram;
        public IPipelineStates PipelineStates;
        public IShaderResources ShaderResources;
    }

    /// <summary>
    /// 模型数据
    /// </summary>
    public class ModelData
    {
        public IVertexArrayObject Vao;
        public Material Material;
        public Matrix4x4 Transform = Matrix4x4.identity;
        public Core.Math.BoundingBox BoundingBox;
        
        // 保存原始Transform引用（用于实时同步）
        public UnityEngine.Transform SourceTransform;
    }

    /// <summary>
    /// 场景管理器
    /// </summary>
    public class SceneManager
    {
        public Camera MainCamera = new Camera();
        public List<ModelData> Models = new List<ModelData>();
        public Vector3 AmbientColor = new Vector3(0.2f, 0.2f, 0.2f);
        public Vector3 LightDirection = new Vector3(0, -1, -1);
        public Color LightColor = Color.white;

        // 天空盒
        public ITexture SkyboxTexture;
        public Material SkyboxMaterial;

        public void AddModel(ModelData model)
        {
            Models.Add(model);
        }

        public void RemoveModel(ModelData model)
        {
            Models.Remove(model);
        }

        public void Clear()
        {
            Models.Clear();
        }
    }
}

