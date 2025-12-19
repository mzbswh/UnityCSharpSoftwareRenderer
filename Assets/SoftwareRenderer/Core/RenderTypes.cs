using UnityEngine;

namespace SoftwareRenderer.Core
{
    /// <summary>
    /// 渲染器类型
    /// </summary>
    public enum ERendererType
    {
        Software,
        OpenGL,
        Vulkan
    }

    /// <summary>
    /// 图元类型
    /// </summary>
    public enum EPrimitiveType
    {
        Point,
        Line,
        Triangle
    }

    /// <summary>
    /// 多边形模式
    /// </summary>
    public enum EPolygonMode
    {
        Point,
        Line,
        Fill
    }

    /// <summary>
    /// 深度测试函数
    /// </summary>
    public enum EDepthFunction
    {
        Never,
        Less,
        Equal,
        LessOrEqual,
        Greater,
        NotEqual,
        GreaterOrEqual,
        Always
    }

    /// <summary>
    /// 混合因子
    /// </summary>
    public enum EBlendFactor
    {
        Zero,
        One,
        SrcColor,
        SrcAlpha,
        DstColor,
        DstAlpha,
        OneMinusSrcColor,
        OneMinusSrcAlpha,
        OneMinusDstColor,
        OneMinusDstAlpha
    }

    /// <summary>
    /// 混合函数
    /// </summary>
    public enum EBlendFunction
    {
        Add,
        Subtract,
        ReverseSubtract,
        Min,
        Max
    }

    /// <summary>
    /// 纹理格式
    /// </summary>
    public enum ETextureFormat
    {
        RGBA8,
        Float32
    }

    /// <summary>
    /// 纹理类型
    /// </summary>
    public enum ETextureType
    {
        Texture2D,
        TextureCube
    }

    /// <summary>
    /// 纹理环绕模式
    /// </summary>
    public enum EWrapMode
    {
        Repeat,
        Clamp,
        Mirror
    }

    /// <summary>
    /// 纹理过滤模式
    /// </summary>
    public enum EFilterMode
    {
        Point,
        Bilinear,
        Trilinear
    }

    /// <summary>
    /// 着色模型
    /// </summary>
    public enum EShadingModel
    {
        Unknown,
        BaseColor,
        BlinnPhong,
        PBR,
        Skybox,
        ShadowMap,
        FXAA
    }

    /// <summary>
    /// Alpha模式
    /// </summary>
    public enum EAlphaMode
    {
        Opaque,
        Blend
    }

    /// <summary>
    /// 材质纹理类型
    /// </summary>
    public enum EMaterialTexType
    {
        None,
        Albedo,
        Normal,
        Emissive,
        AmbientOcclusion,
        MetalRoughness,
        Cube,
        Equirectangular,
        ShadowMap
    }

    /// <summary>
    /// 混合参数
    /// </summary>
    public struct BlendParameters
    {
        public EBlendFunction BlendFuncRgb;
        public EBlendFactor BlendSrcRgb;
        public EBlendFactor BlendDstRgb;
        public EBlendFunction BlendFuncAlpha;
        public EBlendFactor BlendSrcAlpha;
        public EBlendFactor BlendDstAlpha;

        public static BlendParameters Default()
        {
            return new BlendParameters
            {
                BlendFuncRgb = EBlendFunction.Add,
                BlendSrcRgb = EBlendFactor.One,
                BlendDstRgb = EBlendFactor.Zero,
                BlendFuncAlpha = EBlendFunction.Add,
                BlendSrcAlpha = EBlendFactor.One,
                BlendDstAlpha = EBlendFactor.Zero
            };
        }

        public void SetBlendFactor(EBlendFactor src, EBlendFactor dst)
        {
            BlendSrcRgb = src;
            BlendSrcAlpha = src;
            BlendDstRgb = dst;
            BlendDstAlpha = dst;
        }

        public void SetBlendFunc(EBlendFunction func)
        {
            BlendFuncRgb = func;
            BlendFuncAlpha = func;
        }
    }

    /// <summary>
    /// 渲染状态
    /// </summary>
    public struct RenderStates
    {
        public bool Blend;
        public BlendParameters BlendParams;
        public bool DepthTest;
        public bool DepthMask;
        public EDepthFunction DepthFunc;
        public bool CullFace;
        public EPrimitiveType PrimitiveType;
        public EPolygonMode PolygonMode;
        public float LineWidth;

        public static RenderStates Default()
        {
            return new RenderStates
            {
                Blend = false,
                BlendParams = BlendParameters.Default(),
                DepthTest = false,
                DepthMask = true,
                DepthFunc = EDepthFunction.Less,
                CullFace = false,
                PrimitiveType = EPrimitiveType.Triangle,
                PolygonMode = EPolygonMode.Fill,
                LineWidth = 1f
            };
        }
    }

    /// <summary>
    /// 清除状态
    /// </summary>
    public struct ClearStates
    {
        public bool DepthFlag;
        public bool ColorFlag;
        public Color ClearColor;
        public float ClearDepth;

        public static ClearStates Default()
        {
            return new ClearStates
            {
                DepthFlag = true,
                ColorFlag = true,
                ClearColor = Color.black,
                ClearDepth = 1f
            };
        }
    }

    /// <summary>
    /// 视口
    /// </summary>
    public struct Viewport
    {
        public float X, Y;
        public float Width, Height;
        public float MinDepth, MaxDepth;
        public float AbsMinDepth, AbsMaxDepth;
        public Vector4 InnerO; // 内部偏移
        public Vector4 InnerP; // 内部缩放

        public static Viewport Create(int x, int y, int width, int height)
        {
            var viewport = new Viewport
            {
                X = x,
                Y = y,
                Width = width,
                Height = height,
                MinDepth = 0f,
                MaxDepth = 1f
            };

            viewport.AbsMinDepth = Mathf.Min(viewport.MinDepth, viewport.MaxDepth);
            viewport.AbsMaxDepth = Mathf.Max(viewport.MinDepth, viewport.MaxDepth);
            viewport.InnerO = new Vector4(viewport.X + viewport.Width / 2f, viewport.Y + viewport.Height / 2f, viewport.MinDepth, 0f);
            viewport.InnerP = new Vector4(viewport.Width / 2f, viewport.Height / 2f, viewport.MaxDepth - viewport.MinDepth, 1f);

            return viewport;
        }
    }

    /// <summary>
    /// 纹理描述符
    /// </summary>
    public struct ETextureDesc
    {
        public ETextureType Type;
        public ETextureFormat Format;
        public int Width;
        public int Height;
        public bool GenerateMipmaps;
        public bool MultiSample;
        public int SampleCount;
        public EWrapMode WrapModeU;
        public EWrapMode WrapModeV;
        public EWrapMode WrapModeW;
        public EFilterMode FilterMode;

        public static ETextureDesc Default2D(int width, int height)
        {
            return new ETextureDesc
            {
                Type = ETextureType.Texture2D,
                Format = ETextureFormat.RGBA8,
                Width = width,
                Height = height,
                GenerateMipmaps = false,
                MultiSample = false,
                SampleCount = 1,
                WrapModeU = EWrapMode.Repeat,
                WrapModeV = EWrapMode.Repeat,
                WrapModeW = EWrapMode.Repeat,
                FilterMode = EFilterMode.Bilinear
            };
        }

        public static ETextureDesc DefaultCube(int size)
        {
            return new ETextureDesc
            {
                Type = ETextureType.TextureCube,
                Format = ETextureFormat.RGBA8,
                Width = size,
                Height = size,
                GenerateMipmaps = false,
                MultiSample = false,
                SampleCount = 1,
                WrapModeU = EWrapMode.Clamp,
                WrapModeV = EWrapMode.Clamp,
                WrapModeW = EWrapMode.Clamp,
                FilterMode = EFilterMode.Bilinear
            };
        }
    }
}

