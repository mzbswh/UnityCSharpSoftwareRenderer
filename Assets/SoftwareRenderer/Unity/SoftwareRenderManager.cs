using UnityEngine;
using UnityEngine.UI;
using SoftwareRenderer.Render;
using SoftwareRenderer.Scene;
using SoftwareRenderer.Core;
using System.Collections.Generic;

namespace SoftwareRenderer.Unity
{
    /// <summary>
    /// 软件渲染管理器 - Unity集成的主要入口点
    /// </summary>
    public class SoftwareRenderManager : MonoBehaviour
    {
        [Header("输出设置")]
        public RawImage OutputImage;
        public int RenderWidth = 1280;
        public int RenderHeight = 720;

        [Header("渲染设置")]
        public bool EnableMSAA = false;
        public bool EnableDepthTest = true;
        public bool EnableFaceCulling = true;
        public Color ClearColor = Color.black;

        [Header("测试场景")]
        public List<MeshFilter> TestMeshes = new List<MeshFilter>();
        public List<Texture2D> TestTextures = new List<Texture2D>();

        // 渲染器
        private IRenderer _renderer;
        private SceneManager _sceneManager;

        // 帧缓冲和纹理
        private IFrameBuffer _mainFrameBuffer;
        private ITexture _colorTexture;
        private ITexture _depthTexture;
        private Texture2D _outputTexture;

        // Uniform blocks
        private IUniformBlock _uniformBlockScene;
        private IUniformBlock _uniformBlockModel;
        private IUniformBlock _uniformBlockMaterial;

        void Start()
        {
            InitializeRenderer();
            SetupFrameBuffers();
            SetupScene();
        }

        void InitializeRenderer()
        {
            _renderer = new Render.Software.SoftwareRenderer();
            _renderer.Create();

            _sceneManager = new SceneManager();
            _sceneManager.MainCamera.SetPerspective(60f, (float)RenderWidth / RenderHeight, 0.1f, 100f);
            _sceneManager.MainCamera.LookAt(
                new Vector3(0, 2, 5),
                Vector3.zero,
                Vector3.up
            );

            Debug.Log("Software Renderer initialized");
        }

        void SetupFrameBuffers()
        {
            // 创建颜色纹理
            ETextureDesc colorDesc = ETextureDesc.Default2D(RenderWidth, RenderHeight);
            colorDesc.Format = ETextureFormat.RGBA8;
            colorDesc.MultiSample = EnableMSAA;
            colorDesc.SampleCount = EnableMSAA ? 4 : 1;
            _colorTexture = _renderer.CreateTexture(colorDesc);

            // 创建深度纹理
            ETextureDesc depthDesc = ETextureDesc.Default2D(RenderWidth, RenderHeight);
            depthDesc.Format = ETextureFormat.Float32;
            depthDesc.MultiSample = EnableMSAA;
            depthDesc.SampleCount = EnableMSAA ? 4 : 1;
            _depthTexture = _renderer.CreateTexture(depthDesc);

            // 创建帧缓冲
            _mainFrameBuffer = _renderer.CreateFrameBuffer(false);
            _mainFrameBuffer.AttachColor(_colorTexture);
            _mainFrameBuffer.AttachDepth(_depthTexture);

            // 创建输出纹理
            _outputTexture = new Texture2D(RenderWidth, RenderHeight, UnityEngine.TextureFormat.RGBA32, false);
            _outputTexture.filterMode = FilterMode.Point;

            if (OutputImage != null)
            {
                OutputImage.texture = _outputTexture;
            }

            Debug.Log($"Frame buffers setup: {RenderWidth}x{RenderHeight}, MSAA={EnableMSAA}");
        }

        void SetupScene()
        {
            // 导入测试网格
            foreach (var meshFilter in TestMeshes)
            {
                if (meshFilter != null && meshFilter.sharedMesh != null)
                {
                    VertexArray vertexArray = MeshImporter.ImportMesh(meshFilter.sharedMesh);
                    IVertexArrayObject vao = _renderer.CreateVertexArrayObject(vertexArray);

                    var modelData = new ModelData
                    {
                        Vao = vao,
                        Transform = meshFilter.transform.localToWorldMatrix,
                        BoundingBox = MeshImporter.CalculateBoundingBox(meshFilter.sharedMesh),
                        Material = CreateDefaultMaterial()
                    };

                    _sceneManager.AddModel(modelData);
                    Debug.Log($"Added mesh: {meshFilter.name}");
                }
            }

            // 创建Uniform blocks
            _uniformBlockScene = _renderer.CreateUniformBlock("Scene", 256);
            _uniformBlockModel = _renderer.CreateUniformBlock("Model", 256);
            _uniformBlockMaterial = _renderer.CreateUniformBlock("Material", 256);
        }

        Scene.Material CreateDefaultMaterial()
        {
            var material = new Scene.Material
            {
                ShadingModel = EShadingModel.BaseColor,
                BaseColor = Color.white,
                AlphaMode = EAlphaMode.Opaque
            };

            // 创建shader program
            material.ShaderProgram = _renderer.CreateShaderProgram();

            // 创建pipeline states
            RenderStates renderStates = RenderStates.Default();
            renderStates.DepthTest = EnableDepthTest;
            renderStates.DepthFunc = EDepthFunction.Less;
            renderStates.CullFace = EnableFaceCulling;
            renderStates.PrimitiveType = EPrimitiveType.Triangle;
            renderStates.PolygonMode = EPolygonMode.Fill;

            material.PipelineStates = _renderer.CreatePipelineStates(renderStates);

            return material;
        }

        void Update()
        {
            RenderFrame();
            UpdateOutput();
        }

        void RenderFrame()
        {
            if (_renderer == null || _mainFrameBuffer == null) return;

            // 开始渲染Pass
            ClearStates clearStates = ClearStates.Default();
            clearStates.ColorFlag = true;
            clearStates.DepthFlag = true;
            clearStates.ClearColor = ClearColor;
            clearStates.ClearDepth = 1f;

            _renderer.BeginRenderPass(_mainFrameBuffer, clearStates);
            _renderer.SetViewport(0, 0, RenderWidth, RenderHeight);

            // 渲染所有模型
            foreach (var model in _sceneManager.Models)
            {
                if (model.Vao != null && model.Material != null)
                {
                    _renderer.SetVertexArrayObject(model.Vao);

                    if (model.Material.ShaderProgram != null)
                    {
                        _renderer.SetShaderProgram(model.Material.ShaderProgram);
                    }

                    if (model.Material.PipelineStates != null)
                    {
                        _renderer.SetPipelineStates(model.Material.PipelineStates);
                    }

                    _renderer.Draw();
                }
            }

            _renderer.EndRenderPass();
        }

        void UpdateOutput()
        {
            if (_colorTexture != null && _outputTexture != null)
            {
                // 从软件渲染器导出到Unity纹理
                byte[] data = _colorTexture.GetData();
                if (data != null)
                {
                    Color[] pixels = new Color[RenderWidth * RenderHeight];

                    for (int i = 0; i < pixels.Length; i++)
                    {
                        pixels[i] = new Color(
                            data[i * 4 + 0] / 255f,
                            data[i * 4 + 1] / 255f,
                            data[i * 4 + 2] / 255f,
                            data[i * 4 + 3] / 255f
                        );
                    }

                    _outputTexture.SetPixels(pixels);
                    _outputTexture.Apply();
                }
            }
        }

        void OnDestroy()
        {
            if (_renderer != null)
            {
                _renderer.Destroy();
            }
        }

        void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.Label($"Software Renderer");
            GUILayout.Label($"Resolution: {RenderWidth}x{RenderHeight}");
            GUILayout.Label($"MSAA: {(EnableMSAA ? "4x" : "Off")}");
            GUILayout.Label($"Models: {_sceneManager.Models.Count}");
            GUILayout.Label($"FPS: {1f / Time.deltaTime:F1}");
            GUILayout.EndArea();
        }
    }
}

