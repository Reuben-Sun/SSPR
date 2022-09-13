using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class SSPRRenderPassFeature : ScriptableRendererFeature
{
    class SSPRRenderPass : ScriptableRenderPass
    {
        private ShaderTagId SSPR_LightMode = new ShaderTagId("SSPR_LightMode");
        private static readonly string _SSPR_CmdName = "SSPR";
        
        private SSPR _SSPR;
        private RenderTargetIdentifier _currentTarget;

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
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

            #region ReflectionRT

            var cmd = CommandBufferPool.Get(_SSPR_CmdName);
            
            Camera camera = renderingData.cameraData.camera;
            Matrix4x4 VP = GL.GetGPUProjectionMatrix(camera.projectionMatrix, true) * camera.worldToCameraMatrix;
            
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);

            #endregion
            
            #region 绘制Plane

            DrawingSettings drawingSettings = CreateDrawingSettings(SSPR_LightMode, ref renderingData, SortingCriteria.CommonOpaque);
            FilteringSettings filteringSettings = new FilteringSettings(RenderQueueRange.all);
            context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref filteringSettings);

            #endregion
            
        }
        
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


