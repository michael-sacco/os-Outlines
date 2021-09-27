using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class OutlineRenderPass : ScriptableRendererFeature
{
    class CustomRenderPass : ScriptableRenderPass
    {
        public RenderTargetIdentifier source;

        private Material objectIDMaterial;
        private RenderTargetHandle objectIDRT;

        private Material outlineMaterial;
        private RenderTargetHandle outlineRT;
        
        public CustomRenderPass(Material objectIDMaterial, Material outlineMaterial)
        {
            this.objectIDMaterial = objectIDMaterial;
            objectIDRT.Init("_ObjectIDPass");

            this.outlineMaterial = outlineMaterial;
            outlineRT.Init("_OutlinePass");
        }

        // This method is called before executing the render pass.
        // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
        // When empty this render pass will render to the active camera render target.
        // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
        // The render pipeline will ensure target setup and clearing happens in an performance manner.
        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            RenderTextureDescriptor rtDescriptor = cameraTextureDescriptor;
            cmd.GetTemporaryRT(objectIDRT.id, rtDescriptor);
            ConfigureTarget(objectIDRT.Identifier());
            ConfigureClear(ClearFlag.All, Color.black);

            cmd.GetTemporaryRT(outlineRT.id, rtDescriptor);
        }

        // Here you can implement the rendering logic.
        // Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
        // https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
        // You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            //
            // Setup
            //
            
            List<ShaderTagId> shaderTagIds = new List<ShaderTagId>();
            shaderTagIds.Add(new ShaderTagId("UniversalForward"));
            shaderTagIds.Add(new ShaderTagId("SRPDefaultUnlit"));
            
            DrawingSettings drawingSettings = CreateDrawingSettings(shaderTagIds, ref renderingData, SortingCriteria.CommonOpaque);
            drawingSettings.overrideMaterial = objectIDMaterial;


            //
            // Pass 1
            //
            CommandBuffer cmd = CommandBufferPool.Get("OutlinePass");
            
            ConfigureTarget(objectIDRT.Identifier());
            FilteringSettings filteringSettings = new FilteringSettings(RenderQueueRange.opaque, -1);
            context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref filteringSettings);
            cmd.SetGlobalTexture("_MatID", objectIDRT.Identifier());
            

            // Blits
            Blit(cmd, source, outlineRT.Identifier(), outlineMaterial);
            Blit(cmd, outlineRT.Identifier(), source);


            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }

        /// Cleanup any allocated resources that were created during the execution of this render pass.
        public override void FrameCleanup(CommandBuffer cmd)
        {
            cmd.ReleaseTemporaryRT(objectIDRT.id);
            cmd.ReleaseTemporaryRT(outlineRT.id);
        }
    }

    [System.Serializable]
    public class Settings
    {
        public Material objectIDMaterial = null;
        public Material outlineMaterial = null;
    }
    public Settings settings = new Settings();
    CustomRenderPass m_ScriptablePass;

    public override void Create()
    {
        m_ScriptablePass = new CustomRenderPass(settings.objectIDMaterial, settings.outlineMaterial);

        // Configures where the render pass should be injected.
        m_ScriptablePass.renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        m_ScriptablePass.source = renderer.cameraColorTarget;
        renderer.EnqueuePass(m_ScriptablePass);
    }
}