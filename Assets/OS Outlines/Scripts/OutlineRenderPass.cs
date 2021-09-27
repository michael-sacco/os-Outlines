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
            this.outlineMaterial = outlineMaterial;
            objectIDRT.Init("_ObjectIDPass");
            outlineRT.Init("_OutlinePass");
        }
        
        public void AssignShaderVars(Outline outlineStackComponent)
        {
            outlineMaterial.SetFloat(ShaderIDs._Opacity, outlineStackComponent.opacity.value);
            outlineMaterial.SetColor(ShaderIDs._Color, outlineStackComponent.outlineColor.value);
            outlineMaterial.SetColor(ShaderIDs._Overwrite_Background_Color, outlineStackComponent.overwriteColor.value);
            outlineMaterial.SetFloat(ShaderIDs._Overwrite_Background_Alpha, outlineStackComponent.ovewriteOpacity.value);
            outlineMaterial.SetFloat(ShaderIDs._Depth_Threshold, outlineStackComponent.depthDetectionThreshold.value);
            outlineMaterial.SetFloat(ShaderIDs._Normal_Threshold, outlineStackComponent.normalDetectionThreshold.value);
            outlineMaterial.SetInt(ShaderIDs._ObjectID_On, outlineStackComponent.objectIDDetection.value == true ? 1 : 0);

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

        internal static class ShaderIDs
        {
            internal readonly static int _Color = Shader.PropertyToID("_OUTLINE_COLOR");
            internal readonly static int _Opacity = Shader.PropertyToID("_OUTLINE_OPACITY");
            internal readonly static int _Normal_Threshold = Shader.PropertyToID("_OUTLINE_NORMAL_THRESHOLD");
            internal readonly static int _Depth_Threshold = Shader.PropertyToID("_OUTLINE_DEPTH_THRESHOLD");
            internal readonly static int _ObjectID_On = Shader.PropertyToID("_OUTLINE_OBJECT_ID_DETECTION_ON");
            internal readonly static int _Overwrite_Background_Color = Shader.PropertyToID("_OUTLINE_OVERWRITE_COLOR");
            internal readonly static int _Overwrite_Background_Alpha = Shader.PropertyToID("_OUTLINE_OVERWRITE_OPACITY");
        }
    }

    [System.Serializable]
    public class Settings
    {
        
    }
    public Settings settings = new Settings();
    CustomRenderPass m_ScriptablePass;
    Outline outlineStackComponent;

    public override void Create()
    {
        Material objectIDMaterial = CoreUtils.CreateEngineMaterial("Shader Graphs/ObjectID Shader");
        Material outlineMaterial = CoreUtils.CreateEngineMaterial("Shader Graphs/Outline Shader");
        m_ScriptablePass = new CustomRenderPass(objectIDMaterial, outlineMaterial);

        // Configures where the render pass should be injected.
        m_ScriptablePass.renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if(outlineStackComponent == null)
        {
            
            Debug.Log("Ping");
        }
        var stack = VolumeManager.instance.stack;
        outlineStackComponent = stack.GetComponent<Outline>();

        if (outlineStackComponent.IsActive())
        {
            m_ScriptablePass.AssignShaderVars(outlineStackComponent);
            m_ScriptablePass.source = renderer.cameraColorTarget;
            renderer.EnqueuePass(m_ScriptablePass);
        }
        
    }

    public Material GetObjectIDMaterial()
    {
        return new Material(Shader.Find("Shader Graphs/ObjectID Shader"));
    }
    public Material GetOutlineMaterial()
    {
        return new Material(Shader.Find("Shader Graphs/Outline Shader"));
    }

    
}