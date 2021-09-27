using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class OutlineNormalsPrepass : ScriptableRendererFeature
{
    class CustomRenderPass : ScriptableRenderPass
    {
        private RenderTargetHandle normalsRT;
        private Material normalsMaterial;

        public CustomRenderPass(Material normalsMaterial)
        {
            this.normalsMaterial = normalsMaterial;
            normalsRT.Init("_NormalsPass");
        }

        // This method is called before executing the render pass.
        // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
        // When empty this render pass will render to the active camera render target.
        // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
        // The render pipeline will ensure target setup and clearing happens in an performance manner.
        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            RenderTextureDescriptor rtDescriptor = cameraTextureDescriptor;
            cmd.GetTemporaryRT(normalsRT.id, rtDescriptor);
            ConfigureTarget(normalsRT.Identifier());
            ConfigureClear(ClearFlag.All, Color.black);
        }

        // Here you can implement the rendering logic.
        // Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
        // https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
        // You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get("NormalsDataPass");

            // Note: Does not render backfaces
            List<ShaderTagId> shaderTagIds = new List<ShaderTagId>();
            shaderTagIds.Add(new ShaderTagId("DepthOnly"));

            DrawingSettings drawingSettings = CreateDrawingSettings(shaderTagIds, ref renderingData, SortingCriteria.CommonOpaque);
            drawingSettings.overrideMaterial = normalsMaterial;

            // Filtered
            FilteringSettings filteringSettings = new FilteringSettings(RenderQueueRange.opaque, -1);
            context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref filteringSettings);
            cmd.SetGlobalTexture("_NormalsData", normalsRT.Identifier());

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }

        /// Cleanup any allocated resources that were created during the execution of this render pass.
        public override void FrameCleanup(CommandBuffer cmd)
        {
            cmd.ReleaseTemporaryRT(normalsRT.id);
        }
    }
    

    CustomRenderPass m_ScriptablePass;

    public override void Create()
    {
        Material normalsMat = new Material(Shader.Find("Hidden/Internal-DepthNormalsTexture"));
        m_ScriptablePass = new CustomRenderPass(normalsMat);

        // Configures where the render pass should be injected.
        m_ScriptablePass.renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(m_ScriptablePass);
    }
}


