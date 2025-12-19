# Unity C# 软光栅渲染器

这是一个完整的基于C#的软件光栅化渲染器，设计用于Unity引擎，参考了现有的C++软光栅渲染实现。

## 功能特性

### 核心渲染管线
- ✅ **完整的软件渲染管线**：顶点着色→图元装配→裁剪→透视除法→视口变换→面剔除→光栅化→片元着色→深度测试→混合
- ✅ **多图元支持**：点、线、三角形
- ✅ **视锥体裁剪**：6平面裁剪，支持部分裁剪
- ✅ **透视校正插值**：正确的varying数据插值
- ✅ **重心坐标光栅化**：精确的三角形填充

### 高级特性
- ✅ **MSAA 4x多重采样抗锯齿**：Per-sample深度和颜色存储
- ✅ **Mipmap支持**：自动生成、Trilinear过滤
- ✅ **PCF软阴影**：Shadow Map的9点采样软化
- ✅ **Early-Z优化**：提前深度拒绝
- ✅ **视锥体剔除**：AABB包围盒剔除
- ✅ **多线程预留接口**：架构支持并行光栅化（当前单线程实现）

### 着色器系统
- ✅ **Basic Shader**：基础颜色着色器
- ✅ **Blinn-Phong Shader**：经典光照模型，支持Albedo/Normal/Emissive/AO贴图
- ✅ **Skybox Shader**：CubeMap采样天空盒
- ✅ **Shadow Map支持**：PCF软阴影采样

### 渲染抽象层
- ✅ **多后端架构**：抽象接口层，方便扩展OpenGL/Vulkan等其他渲染后端
- ✅ **接口设计**：`IRenderer`, `IFrameBuffer`, `ITexture`, `IShaderProgram`, `IPipelineStates`
- ✅ **软件实现**：`SoftwareRenderer`, `FrameBufferSoft`, `TextureSoft`等完整实现

## 项目结构

```
Assets/SoftwareRenderer/
├── Core/                      # 核心基础模块
│   ├── Buffer.cs             # 缓冲区类（Buffer<T>, ImageBuffer<T>, RGBA）
│   ├── RenderTypes.cs        # 渲染类型定义（枚举、结构体）
│   └── Math/                 # 数学工具
│       ├── Geometry.cs       # 几何工具（包围盒、视锥体、重心坐标）
│       └── MathUtils.cs      # 数学工具（矩阵、投影、插值）
│
├── Render/                    # 渲染抽象层
│   ├── IRenderer.cs          # 渲染器接口
│   ├── IFrameBuffer.cs       # 帧缓冲接口
│   ├── ITexture.cs           # 纹理接口
│   ├── IShaderProgram.cs     # 着色器程序接口
│   ├── IPipelineStates.cs    # 管线状态接口
│   ├── IVertex.cs            # 顶点数组对象接口
│   └── Software/             # 软件渲染实现
│       ├── SoftwareRenderer.cs     # 核心渲染器（完整管线）
│       ├── FrameBufferSoft.cs      # 帧缓冲实现
│       ├── TextureSoft.cs          # 纹理实现（支持Mipmap）
│       ├── SamplerSoft.cs          # 采样器（2D/Cube, Bilinear/Trilinear）
│       ├── ShaderProgramSoft.cs    # 着色器程序管理
│       ├── VertexArraySoft.cs      # 顶点数组对象
│       ├── UniformSoft.cs          # Uniform数据管理
│       ├── DepthSoft.cs            # 深度测试
│       ├── BlendSoft.cs            # 颜色混合
│       └── PipelineStatesSoft.cs   # 管线状态
│
├── Shaders/                   # 着色器实现
│   ├── ShaderBase.cs         # Shader基类
│   ├── BasicShader.cs        # 基础颜色着色器
│   ├── BlinnPhongShader.cs   # Blinn-Phong光照着色器
│   └── SkyboxShader.cs       # 天空盒着色器
│
├── Scene/                     # 场景管理
│   ├── Camera.cs             # 相机（透视投影、LookAt）
│   ├── SceneManager.cs       # 场景管理器
│   └── Material.cs           # 材质系统（在SceneManager.cs中）
│
└── Unity/                     # Unity集成
    ├── SoftwareRenderManager.cs  # 主渲染管理器（MonoBehaviour）
    ├── MeshImporter.cs           # Unity Mesh导入
    └── TextureImporter.cs        # Unity Texture导入
```

## 快速开始

### 1. 创建场景

1. 创建新场景或打开现有场景
2. 创建空GameObject，命名为"SoftwareRenderer"
3. 添加`SoftwareRenderManager`组件

### 2. 设置输出

1. 创建UI Canvas（如果没有）
2. 在Canvas下创建RawImage用于显示渲染结果
3. 将RawImage拖到`SoftwareRenderManager`的`Output Image`字段

### 3. 添加测试模型

1. 在场景中创建一些简单的几何体（Cube, Sphere等）
2. 将这些物体的MeshFilter组件拖到`SoftwareRenderManager`的`Test Meshes`列表中

### 4. 运行

点击Play按钮，软渲染器将开始渲染场景并输出到RawImage。

## 渲染流程

### 完整管线

```
输入: 顶点数据 + Shader
  ↓
1. 顶点着色器 (Vertex Shader)
   - 变换顶点到裁剪空间
   - 输出Varying数据
  ↓
2. 图元装配 (Primitive Assembly)
   - 组装点/线/三角形
  ↓
3. 裁剪 (Clipping)
   - 视锥体6平面裁剪
   - Sutherland-Hodgman算法
  ↓
4. 透视除法 (Perspective Divide)
   - 齐次坐标 → NDC
   - 保存1/w用于透视校正
  ↓
5. 视口变换 (Viewport Transform)
   - NDC → 屏幕空间
  ↓
6. 面剔除 (Face Culling)
   - 背面剔除（可选）
  ↓
7. 光栅化 (Rasterization)
   - 计算AABB包围盒
   - 遍历像素，计算重心坐标
   - 透视校正插值Varying数据
  ↓
8. 片元着色器 (Fragment Shader)
   - 计算光照、纹理采样
   - 输出颜色
  ↓
9. 深度测试 (Depth Test)
   - Early-Z优化（可选）
   - 深度比较和写入
  ↓
10. 颜色混合 (Blending)
    - Alpha混合（可选）
  ↓
11. MSAA Resolve
    - 多重采样合并
  ↓
输出: 帧缓冲
```

## 性能优化

### 已实现
- **Early-Z测试**：在2x2 Pixel Quad级别提前拒绝遮挡像素
- **视锥体剔除**：模型级别AABB包围盒剔除
- **MSAA**：4x多重采样，平滑边缘

### 预留接口
- **多线程光栅化**：`RasterizationContext`结构隔离任务，方便并行化
- **SIMD优化**：预留使用Unity.Mathematics的SIMD向量类型

## 技术细节

### 重心坐标插值

用于三角形内部的varying数据插值：

```csharp
// 计算重心坐标
Vector3 barycentric = CalculateBarycentric(v0, v1, v2, p);

// 透视校正插值
float invW = barycentric.x * invW0 + barycentric.y * invW1 + barycentric.z * invW2;
float value = (v0 * invW0 * barycentric.x + 
               v1 * invW1 * barycentric.y + 
               v2 * invW2 * barycentric.z) / invW;
```

### Shadow Map PCF

9点采样软化阴影边缘：

```csharp
for (int x = -1; x <= 1; x++)
{
    for (int y = -1; y <= 1; y++)
    {
        float pcfDepth = shadowMap.Sample(uv + offset);
        if (currentDepth - bias > pcfDepth)
            shadow += 1.0f;
    }
}
shadow /= 9.0f;
```

### Mipmap LOD计算

基于UV梯度自动计算LOD级别：

```csharp
Vector2 dx = ddx * texSize;
Vector2 dy = ddy * texSize;
float d = Max(Dot(dx, dx), Dot(dy, dy));
float lod = Max(0.5f * Log2(d), 0.0f);
```

## 扩展其他渲染后端

得益于良好的抽象设计，可以轻松添加其他渲染后端：

1. 实现`IRenderer`接口
2. 实现对应的`IFrameBuffer`, `ITexture`, `IShaderProgram`等
3. 在`SoftwareRenderManager`中切换渲染器类型

```csharp
// 示例：添加OpenGL后端
public class OpenGLRenderer : IRenderer
{
    public RendererType Type => RendererType.OpenGL;
    
    public IFrameBuffer CreateFrameBuffer(bool offscreen)
    {
        return new FrameBufferOpenGL(offscreen);
    }
    
    // ... 实现其他接口方法
}
```

## 限制和已知问题

1. **性能**：纯C#软件渲染，性能有限（建议分辨率≤1280x720）
2. **单线程**：当前实现为单线程，多线程版本需要额外实现
3. **简化裁剪**：Sutherland-Hodgman裁剪算法简化实现
4. **固定Varying大小**：当前varying数据大小固定，需要根据shader调整

## 未来改进

- [ ] 完整的Sutherland-Hodgman裁剪算法
- [ ] 多线程光栅化（使用C# Job System）
- [ ] SIMD优化（Unity.Mathematics）
- [ ] PBR着色器完整实现（IBL环境光）
- [ ] FXAA后处理
- [ ] 更多的Shader示例

## 参考

本项目参考了C++版本的软光栅渲染器实现，并针对C#/Unity进行了适配和优化。

## 许可

MIT License

---

**注意**: 这是一个教学/演示项目，展示软件光栅化渲染的完整流程。实际生产环境建议使用Unity的内置渲染管线或其他硬件加速方案。

