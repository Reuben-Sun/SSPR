using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class SSPRRenderPassFeature : ScriptableRendererFeature
{
    class SSPRRenderPass : ScriptableRenderPass
    {
        private ShaderTagId SSPR_LightMode = new ShaderTagId("SSPR_LightMode");
        
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
        }
        
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            DrawingSettings drawingSettings = CreateDrawingSettings(SSPR_LightMode, ref renderingData, SortingCriteria.CommonOpaque);
            FilteringSettings filteringSettings = new FilteringSettings(RenderQueueRange.all);
            context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref filteringSettings);
        }
        
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
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


