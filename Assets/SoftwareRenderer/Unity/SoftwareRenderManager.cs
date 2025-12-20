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
        public EPolygonMode PolygonMode = EPolygonMode.Fill;
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

        // 相机控制
        [Header("相机控制")]
        public float CameraDistance = 5f;
        public float RotationSpeed = 0.3f;
        public float KeyboardRotationSpeed = 50f;
        public float KeyboardMoveSpeed = 3f;
        public float KeyboardZoomSpeed = 5f;
        public KeyCode ResetCameraKey = KeyCode.R;

        private float _initialDistance = 5f;
        private float _initialYaw = 0f;
        private float _initialPitch = 20f;

        private float _cameraYaw = 0f;
        private float _cameraPitch = 20f;
        private bool _isDragging = false;
        private Vector3 _lastMousePosition;

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

            // 保存初始相机参数
            _initialDistance = CameraDistance;
            _initialYaw = _cameraYaw;
            _initialPitch = _cameraPitch;

            // 初始化相机位置
            UpdateCameraPosition();

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
                        Material = CreateDefaultMaterial(),
                        SourceTransform = meshFilter.transform
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
            renderStates.PolygonMode = PolygonMode;

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
            HandleCameraInput();
            RenderFrame();
            UpdateOutput();
        }

        void RenderFrame()
        {
            if (_renderer == null || _mainFrameBuffer == null) return;

            // 更新所有模型的Transform（实时同步）
            foreach (var model in _sceneManager.Models)
            {
                if (model.SourceTransform != null)
                {
                    model.Transform = model.SourceTransform.localToWorldMatrix;
                }
            }

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
                        // 实时更新PolygonMode（如果Inspector中的设置改变了）
                        var currentStates = model.Material.PipelineStates.RenderStates;
                        if (currentStates.PolygonMode != PolygonMode)
                        {
                            // 创建新的RenderStates，更新PolygonMode
                            RenderStates newStates = new RenderStates
                            {
                                Blend = currentStates.Blend,
                                BlendParams = currentStates.BlendParams,
                                DepthTest = currentStates.DepthTest,
                                DepthMask = currentStates.DepthMask,
                                DepthFunc = currentStates.DepthFunc,
                                CullFace = currentStates.CullFace,
                                PrimitiveType = currentStates.PrimitiveType,
                                PolygonMode = PolygonMode,
                                LineWidth = currentStates.LineWidth
                            };
                            model.Material.PipelineStates = _renderer.CreatePipelineStates(newStates);
                        }
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

        #region 相机控制

        void HandleCameraInput()
        {
            // 按键重置相机
            if (Input.GetKeyDown(ResetCameraKey))
            {
                ResetCamera();
                return;
            }

            // 键盘控制
            HandleKeyboardInput();

            // 检测鼠标左键按下
            if (Input.GetMouseButtonDown(0))
            {
                _isDragging = true;
                _lastMousePosition = Input.mousePosition;
            }

            // 检测鼠标左键抬起
            if (Input.GetMouseButtonUp(0))
            {
                _isDragging = false;
            }

            // 拖动旋转
            if (_isDragging)
            {
                Vector3 mouseDelta = Input.mousePosition - _lastMousePosition;
                _lastMousePosition = Input.mousePosition;

                // 水平拖动控制Yaw（绕Y轴旋转）
                _cameraYaw += mouseDelta.x * RotationSpeed;

                // 垂直拖动控制Pitch（绕X轴旋转）
                _cameraPitch -= mouseDelta.y * RotationSpeed;

                // 限制俯仰角度，避免翻转
                _cameraPitch = Mathf.Clamp(_cameraPitch, -89f, 89f);

                // 更新相机位置
                UpdateCameraPosition();
            }

            // 鼠标滚轮控制距离
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f)
            {
                CameraDistance -= scroll * 10f;
                CameraDistance = Mathf.Clamp(CameraDistance, 1f, 20f);
                UpdateCameraPosition();
            }
        }

        void HandleKeyboardInput()
        {
            bool needUpdate = false;
            float deltaTime = Time.deltaTime;

            // A/D - 左右旋转（Yaw）
            if (Input.GetKey(KeyCode.A))
            {
                _cameraYaw += KeyboardRotationSpeed * deltaTime;
                needUpdate = true;
            }
            if (Input.GetKey(KeyCode.D))
            {
                _cameraYaw -= KeyboardRotationSpeed * deltaTime;
                needUpdate = true;
            }

            // W/S - 上下旋转（Pitch）
            if (Input.GetKey(KeyCode.W))
            {
                _cameraPitch += KeyboardMoveSpeed * deltaTime * 10f;
                _cameraPitch = Mathf.Clamp(_cameraPitch, -89f, 89f);
                needUpdate = true;
            }
            if (Input.GetKey(KeyCode.S))
            {
                _cameraPitch -= KeyboardMoveSpeed * deltaTime * 10f;
                _cameraPitch = Mathf.Clamp(_cameraPitch, -89f, 89f);
                needUpdate = true;
            }

            // Q/E - 缩放（Zoom）
            if (Input.GetKey(KeyCode.Q))
            {
                CameraDistance += KeyboardZoomSpeed * deltaTime;
                CameraDistance = Mathf.Clamp(CameraDistance, 1f, 20f);
                needUpdate = true;
            }
            if (Input.GetKey(KeyCode.E))
            {
                CameraDistance -= KeyboardZoomSpeed * deltaTime;
                CameraDistance = Mathf.Clamp(CameraDistance, 1f, 20f);
                needUpdate = true;
            }

            if (needUpdate)
            {
                UpdateCameraPosition();
            }
        }

        void ResetCamera()
        {
            CameraDistance = _initialDistance;
            _cameraYaw = _initialYaw;
            _cameraPitch = _initialPitch;
            UpdateCameraPosition();
            Debug.Log("Camera reset to initial position");
        }

        void UpdateCameraPosition()
        {
            // 将欧拉角转换为球坐标
            float yawRad = _cameraYaw * Mathf.Deg2Rad;
            float pitchRad = _cameraPitch * Mathf.Deg2Rad;

            // 计算相机位置（绕原点旋转）
            Vector3 cameraPos = new Vector3(
                CameraDistance * Mathf.Cos(pitchRad) * Mathf.Sin(yawRad),
                CameraDistance * Mathf.Sin(pitchRad),
                CameraDistance * Mathf.Cos(pitchRad) * Mathf.Cos(yawRad)
            );

            // 设置相机朝向原点
            _sceneManager.MainCamera.LookAt(cameraPos, Vector3.zero, Vector3.up);
        }

        void OnDrawGizmos()
        {
            if (_sceneManager == null || _sceneManager.MainCamera == null) return;

            // 获取相机位置
            Vector3 cameraPos = _sceneManager.MainCamera.Eye;
            Vector3 target = _sceneManager.MainCamera.Center;

            // 绘制相机位置（黄色球体）
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(cameraPos, 0.2f);

            // 绘制相机朝向（蓝色线）
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(cameraPos, target);

            // 绘制视锥体
            DrawCameraFrustum(cameraPos, target);

            // 绘制目标点（红色小球）
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(target, 0.1f);
        }

        void DrawCameraFrustum(Vector3 cameraPos, Vector3 target)
        {
            if (_sceneManager.MainCamera == null) return;

            // 获取相机参数
            float fov = _sceneManager.MainCamera.Fov;
            float aspect = _sceneManager.MainCamera.Aspect;
            float near = _sceneManager.MainCamera.Near;
            float far = Mathf.Min(_sceneManager.MainCamera.Far, 10f); // 限制远平面用于显示

            // 计算相机方向
            Vector3 forward = (target - cameraPos).normalized;
            Vector3 up = Vector3.up;
            Vector3 right = Vector3.Cross(forward, up).normalized;
            up = Vector3.Cross(right, forward).normalized;

            // 计算近平面和远平面的尺寸
            float nearHeight = 2f * Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad) * near;
            float nearWidth = nearHeight * aspect;
            float farHeight = 2f * Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad) * far;
            float farWidth = farHeight * aspect;

            // 近平面中心和角点
            Vector3 nearCenter = cameraPos + forward * near;
            Vector3 nearTL = nearCenter + up * (nearHeight * 0.5f) - right * (nearWidth * 0.5f);
            Vector3 nearTR = nearCenter + up * (nearHeight * 0.5f) + right * (nearWidth * 0.5f);
            Vector3 nearBL = nearCenter - up * (nearHeight * 0.5f) - right * (nearWidth * 0.5f);
            Vector3 nearBR = nearCenter - up * (nearHeight * 0.5f) + right * (nearWidth * 0.5f);

            // 远平面中心和角点
            Vector3 farCenter = cameraPos + forward * far;
            Vector3 farTL = farCenter + up * (farHeight * 0.5f) - right * (farWidth * 0.5f);
            Vector3 farTR = farCenter + up * (farHeight * 0.5f) + right * (farWidth * 0.5f);
            Vector3 farBL = farCenter - up * (farHeight * 0.5f) - right * (farWidth * 0.5f);
            Vector3 farBR = farCenter - up * (farHeight * 0.5f) + right * (farWidth * 0.5f);

            // 绘制视锥体
            Gizmos.color = new Color(0, 1, 1, 0.3f); // 半透明青色

            // 近平面
            Gizmos.DrawLine(nearTL, nearTR);
            Gizmos.DrawLine(nearTR, nearBR);
            Gizmos.DrawLine(nearBR, nearBL);
            Gizmos.DrawLine(nearBL, nearTL);

            // 远平面
            Gizmos.DrawLine(farTL, farTR);
            Gizmos.DrawLine(farTR, farBR);
            Gizmos.DrawLine(farBR, farBL);
            Gizmos.DrawLine(farBL, farTL);

            // 连接线
            Gizmos.DrawLine(nearTL, farTL);
            Gizmos.DrawLine(nearTR, farTR);
            Gizmos.DrawLine(nearBL, farBL);
            Gizmos.DrawLine(nearBR, farBR);
        }

        #endregion
    }
}

