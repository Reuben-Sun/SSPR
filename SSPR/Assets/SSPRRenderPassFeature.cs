using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class SSPRRenderPassFeature : ScriptableRendererFeature
{
    class SSPRRenderPass : ScriptableRenderPass
    {
        #region 常量
        
        private ShaderTagId SSPR_LightMode = new ShaderTagId("SSPR_LightMode");
        private static readonly string _SSPR_CmdName = "SSPR";
        
        const int SHADER_NUMTHREAD_X = 8; 
        const int SHADER_NUMTHREAD_Y = 8;

        private static readonly int _ColorRTShaderId = Shader.PropertyToID("_ColorRT");
        private static readonly int _UVRTShaderId = Shader.PropertyToID("_UVRT");

        #endregion
   
        
        private SSPR _SSPR;
        private ComputeShader _cs;
        private RenderTargetIdentifier _SSPR_ColorRT = new RenderTargetIdentifier(_ColorRTShaderId);
        private RenderTargetIdentifier _SSPR_UVRT = new RenderTargetIdentifier(_UVRTShaderId);
        
        public SSPRRenderPass()
        {
            _cs = (ComputeShader)Resources.Load("SSPRComputeShader");
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            #region 资源检查
            
            var stack = VolumeManager.instance.stack;
            _SSPR = stack.GetComponent<SSPR>();
            if (_SSPR == null)
            {
                return;
            }

            if (!_SSPR.IsActive())
            {
                return;
            }
            
            #endregion
            
            //初始化贴图
            RenderTextureDescriptor rtd = new RenderTextureDescriptor(GetRTWidth(), GetRTHeight(),RenderTextureFormat.Default, 0, 0);
            rtd.sRGB = false;
            rtd.enableRandomWrite = true;
            
            rtd.colorFormat = RenderTextureFormat.ARGB32;
            cmd.GetTemporaryRT(_ColorRTShaderId, rtd);

            rtd.colorFormat = RenderTextureFormat.RInt;
            cmd.GetTemporaryRT(_UVRTShaderId, rtd);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var cmd = CommandBufferPool.Get(_SSPR_CmdName);
            
            int dispatchThreadGroupXCount = GetRTWidth() / SHADER_NUMTHREAD_X; 
            int dispatchThreadGroupYCount = GetRTHeight() / SHADER_NUMTHREAD_Y; 
            int dispatchThreadGroupZCount = 1; 
            
            Camera camera = renderingData.cameraData.camera;
            Matrix4x4 VP = GL.GetGPUProjectionMatrix(camera.projectionMatrix, true) * camera.worldToCameraMatrix;

            #region Convert Uniform

            cmd.SetComputeVectorParam(_cs, Shader.PropertyToID("_RTSize"), new Vector2(GetRTWidth(), GetRTHeight()));
            cmd.SetComputeFloatParam(_cs, Shader.PropertyToID("_HorizontalPlaneHeightWS"), _SSPR.HorizontalReflectionPlaneHeightWS.value);
            cmd.SetComputeMatrixParam(_cs, "_VPMatrix", VP);
            cmd.SetComputeFloatParam(_cs, Shader.PropertyToID("_ScreenLRStretchIntensity"), _SSPR.ScreenLRStretchIntensity.value);
            cmd.SetComputeFloatParam(_cs, Shader.PropertyToID("_ScreenLRStretchThreshold"), _SSPR.ScreenLRStretchThreshold.value);
            cmd.SetComputeFloatParam(_cs, Shader.PropertyToID("_FadeOutScreenBorderWidthVerticle"), _SSPR.FadeOutScreenBorderWidthVerticle.value);
            cmd.SetComputeFloatParam(_cs, Shader.PropertyToID("_FadeOutScreenBorderWidthHorizontal"), _SSPR.FadeOutScreenBorderWidthHorizontal.value);
            cmd.SetComputeVectorParam(_cs, Shader.PropertyToID("_CameraDirection"), camera.transform.forward);
            cmd.SetComputeVectorParam(_cs, Shader.PropertyToID("_FinalTintColor"), _SSPR.TintColor.value);

            #endregion
            
            #region ClearRT

            int kernel_ClearRT = _cs.FindKernel("ClearRT");
            cmd.SetComputeTextureParam(_cs, kernel_ClearRT, "UVRT", _SSPR_UVRT);
            cmd.SetComputeTextureParam(_cs, kernel_ClearRT, "ColorRT", _SSPR_ColorRT);
            cmd.DispatchCompute(_cs, kernel_ClearRT, dispatchThreadGroupXCount, dispatchThreadGroupYCount, dispatchThreadGroupZCount);

            #endregion

            #region Render UV

            int kernel_RenderUV = _cs.FindKernel("RenderUV");
            cmd.SetComputeTextureParam(_cs, kernel_RenderUV, "UVRT", _SSPR_UVRT);
            cmd.SetComputeTextureParam(_cs, kernel_RenderUV, "_CameraDepthTexture", new RenderTargetIdentifier("_CameraDepthTexture"));
            cmd.DispatchCompute(_cs, kernel_RenderUV, dispatchThreadGroupXCount, dispatchThreadGroupYCount, dispatchThreadGroupZCount);

            #endregion

            #region Render Color

            int kernel_RenderColor = _cs.FindKernel("RenderColor");
            cmd.SetComputeTextureParam(_cs, kernel_RenderColor, "_CameraOpaqueTexture", new RenderTargetIdentifier("_CameraOpaqueTexture"));
            cmd.SetComputeTextureParam(_cs, kernel_RenderColor, "ColorRT", _SSPR_ColorRT);
            cmd.SetComputeTextureParam(_cs, kernel_RenderColor, "UVRT", _SSPR_UVRT);
            cmd.DispatchCompute(_cs, kernel_RenderColor, dispatchThreadGroupXCount, dispatchThreadGroupYCount, dispatchThreadGroupZCount);

            #endregion

            #region AA

            int kernel_FixHole = _cs.FindKernel("FixHole");
            cmd.SetComputeTextureParam(_cs, kernel_FixHole, "ColorRT", _SSPR_ColorRT);
            cmd.SetComputeTextureParam(_cs, kernel_FixHole, "UVRT", _SSPR_UVRT);
            cmd.DispatchCompute(_cs, kernel_FixHole, Mathf.CeilToInt(dispatchThreadGroupXCount / 2f), Mathf.CeilToInt(dispatchThreadGroupYCount / 2f), dispatchThreadGroupZCount);

            #endregion
            
            #region Sent out

            cmd.SetGlobalTexture(_ColorRTShaderId, _SSPR_ColorRT);

            #endregion
            
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);

            #region 绘制Plane

            DrawingSettings drawingSettings = CreateDrawingSettings(SSPR_LightMode, ref renderingData, SortingCriteria.CommonOpaque);
            FilteringSettings filteringSettings = new FilteringSettings(RenderQueueRange.all);
            context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref filteringSettings);

            #endregion
            
        }

        #region Untils

        int GetRTHeight()
        {
            return Mathf.CeilToInt(_SSPR.RTSize.value / (float)SHADER_NUMTHREAD_Y) * SHADER_NUMTHREAD_Y;
        }
        int GetRTWidth()
        {
            float aspect = (float)Screen.width / Screen.height;
            return Mathf.CeilToInt(GetRTHeight() * aspect / (float)SHADER_NUMTHREAD_X) * SHADER_NUMTHREAD_X;
        }

        #endregion
    }

    SSPRRenderPass _SSPRRenderPass;
    
    public override void Create()
    {
        _SSPRRenderPass = new SSPRRenderPass();
        _SSPRRenderPass.renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
    }
    
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(_SSPRRenderPass);
    }
}


