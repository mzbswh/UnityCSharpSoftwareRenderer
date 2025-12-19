using SoftwareRenderer.Core;
using UnityEngine;

namespace SoftwareRenderer.Render.Software
{
    /// <summary>
    /// 深度测试工具类
    /// </summary>
    public static class DepthTest
    {
        public static bool Test(float depthValue, float bufferDepth, EDepthFunction func)
        {
            switch (func)
            {
                case EDepthFunction.Never:
                    return false;
                case EDepthFunction.Less:
                    return depthValue < bufferDepth;
                case EDepthFunction.Equal:
                    return Mathf.Approximately(depthValue, bufferDepth);
                case EDepthFunction.LessOrEqual:
                    return depthValue <= bufferDepth;
                case EDepthFunction.Greater:
                    return depthValue > bufferDepth;
                case EDepthFunction.NotEqual:
                    return !Mathf.Approximately(depthValue, bufferDepth);
                case EDepthFunction.GreaterOrEqual:
                    return depthValue >= bufferDepth;
                case EDepthFunction.Always:
                    return true;
                default:
                    return false;
            }
        }
    }
}

