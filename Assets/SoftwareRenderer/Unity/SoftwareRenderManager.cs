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

        // 复用的缓冲区，避免每帧分配
        private byte[] _byteBuffer;

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

            // 创建输出纹理（使用RGBA32格式）
            _outputTexture = new Texture2D(RenderWidth, RenderHeight, UnityEngine.TextureFormat.RGBA32, false);
            _outputTexture.filterMode = FilterMode.Point;

            // 分配复用的字节缓冲区（RGBA = 4 bytes per pixel）
            _byteBuffer = new byte[RenderWidth * RenderHeight * 4];

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
                    Debug.Log($"Added mesh: {meshFilter.name}, vertices: {vertexArray.VertexCount}, indices: {vertexArray.IndexCount}");
                }
            }

            // 创建Uniform blocks
            _uniformBlockScene = _renderer.CreateUniformBlock("Scene", 256);
            _uniformBlockModel = _renderer.CreateUniformBlock("Model", 256);
            _uniformBlockMaterial = _renderer.CreateUniformBlock("Material", 256);

            Debug.Log($"Scene setup complete. Total models: {_sceneManager.Models.Count}");
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
            var shaderProgram = _renderer.CreateShaderProgram();

            // 创建并设置一个简单的顶点着色器
            var vs = new SimpleVertexShader
            {
                ViewProjectionMatrix = _sceneManager.MainCamera.GetViewProjectionMatrix()
            };
            shaderProgram.SetVertexShader(vs);

            // 创建并设置一个简单的片元着色器
            var fs = new SimpleFragmentShader
            {
                BaseColor = material.BaseColor
            };
            shaderProgram.SetFragmentShader(fs);

            // 链接shader program
            shaderProgram.Link();

            material.ShaderProgram = shaderProgram;

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

        // 简单的顶点着色器
        private class SimpleVertexShader : Shaders.VertexShaderBase
        {
            public Matrix4x4 ViewProjectionMatrix;
            public Vector3 Position;

            public override void Execute()
            {
                Vector4 pos = new Vector4(Position.x, Position.y, Position.z, 1f);
                Builtin.Position = ViewProjectionMatrix * pos;
            }

            public override Render.IVertexShader Clone()
            {
                return new SimpleVertexShader { ViewProjectionMatrix = this.ViewProjectionMatrix };
            }
        }

        // 简单的片元着色器
        private class SimpleFragmentShader : Shaders.FragmentShaderBase
        {
            public Color BaseColor;

            public override void Execute()
            {
                Builtin.FragColor = new ERGBA(
                    (byte)(BaseColor.r * 255),
                    (byte)(BaseColor.g * 255),
                    (byte)(BaseColor.b * 255),
                    (byte)(BaseColor.a * 255)
                );
            }

            public override Render.IFragmentShader Clone()
            {
                return new SimpleFragmentShader { BaseColor = this.BaseColor };
            }
        }

        void Update()
        {
            RenderFrame();
            UpdateOutput();
        }

        void RenderFrame()
        {
            if (_renderer == null || _mainFrameBuffer == null) return;

            // 更新相机矩阵
            Matrix4x4 vpMatrix = _sceneManager.MainCamera.GetViewProjectionMatrix();

            // 开始渲染Pass
            ClearStates clearStates = ClearStates.Default();
            clearStates.ColorFlag = true;
            clearStates.DepthFlag = true;
            clearStates.ClearColor = ClearColor;
            clearStates.ClearDepth = 1f;

            _renderer.BeginRenderPass(_mainFrameBuffer, clearStates);
            _renderer.SetViewport(0, 0, RenderWidth, RenderHeight);

            int drawnModels = 0;
            // 渲染所有模型
            foreach (var model in _sceneManager.Models)
            {
                if (model.Vao != null && model.Material != null)
                {
                    _renderer.SetVertexArrayObject(model.Vao);

                    if (model.Material.ShaderProgram != null)
                    {
                        // 更新shader中的MVP矩阵
                        UpdateShaderMatrices(model.Material.ShaderProgram, model.Transform, vpMatrix);

                        _renderer.SetShaderProgram(model.Material.ShaderProgram);
                    }

                    if (model.Material.PipelineStates != null)
                    {
                        _renderer.SetPipelineStates(model.Material.PipelineStates);
                    }

                    _renderer.Draw();
                    drawnModels++;
                }
            }

            _renderer.EndRenderPass();

            // 每60帧输出一次调试信息
            if (Time.frameCount % 60 == 0)
            {
                Debug.Log($"Frame {Time.frameCount}: Drew {drawnModels} models");
            }
        }

        void UpdateShaderMatrices(IShaderProgram shaderProgram, Matrix4x4 modelMatrix, Matrix4x4 vpMatrix)
        {
            // 计算MVP矩阵
            Matrix4x4 mvpMatrix = vpMatrix * modelMatrix;

            // 更新shader中的矩阵
            if (shaderProgram is Render.Software.ShaderProgramSoft softShader)
            {
                if (softShader.VertexShader is SimpleVertexShader simpleVS)
                {
                    simpleVS.ViewProjectionMatrix = mvpMatrix;
                }
            }
        }

        void UpdateOutput()
        {
            if (_colorTexture != null && _outputTexture != null && _byteBuffer != null)
            {
                // 零拷贝 - 直接获取ERGBA数组引用
                ERGBA[] colorData = _colorTexture.GetRawData() as ERGBA[];
                if (colorData != null)
                {
#if ENABLE_UNSAFE_CODE
                    // 使用 unsafe 代码进行超快速拷贝
                    unsafe
                    {
                        fixed (ERGBA* srcPtr = colorData)
                        fixed (byte* dstPtr = _byteBuffer)
                        {
                            byte* src = (byte*)srcPtr;
                            byte* dst = dstPtr;
                            int size = colorData.Length * 4;
                            System.Buffer.MemoryCopy(src, dst, size, size);
                        }
                    }
#else
                    // 不使用unsafe的快速版本
                    int pixelCount = colorData.Length;
                    int dstIdx = 0;
                    for (int i = 0; i < pixelCount; i++)
                    {
                        ERGBA pixel = colorData[i];
                        _byteBuffer[dstIdx++] = pixel.R;
                        _byteBuffer[dstIdx++] = pixel.G;
                        _byteBuffer[dstIdx++] = pixel.B;
                        _byteBuffer[dstIdx++] = pixel.A;
                    }
#endif

                    // 直接使用LoadRawTextureData上传
                    _outputTexture.LoadRawTextureData(_byteBuffer);
                    _outputTexture.Apply(false);
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

