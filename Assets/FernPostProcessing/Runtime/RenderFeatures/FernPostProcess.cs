using System;
using System.Collections.Generic;
using Fern.PostProcess;

namespace UnityEngine.Rendering.Universal.PostProcessing {

    [Serializable]
    public class FernPostProcess : ScriptableRendererFeature
    {   
        private FernPostProcessRenderPass m_AfterOpaqueAndSkyPass, m_BeforePostProcessPass, m_AfterPostProcessPass;

        private RenderTargetHandle m_AfterPostProcessColor;

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if(renderingData.cameraData.postProcessEnabled) {
                if(m_AfterOpaqueAndSkyPass.HasPostProcessRenderers && m_AfterOpaqueAndSkyPass.PrepareRenderers(ref renderingData)){
                    m_AfterOpaqueAndSkyPass.Setup(renderer.cameraColorTarget, renderer.cameraColorTarget);
                    renderer.EnqueuePass(m_AfterOpaqueAndSkyPass);
                }
                // if(m_BeforePostProcessPass.HasPostProcessRenderers && m_BeforePostProcessPass.PrepareRenderers(ref renderingData)){
                //     m_BeforePostProcessPass.Setup(renderer.cameraColorTarget, renderer.cameraColorTarget);
                //     renderer.EnqueuePass(m_BeforePostProcessPass);
                // }
                // if(m_AfterPostProcessPass.HasPostProcessRenderers && m_AfterPostProcessPass.PrepareRenderers(ref renderingData)){
                //     var source = renderingData.cameraData.resolveFinalTarget ? m_AfterPostProcessColor.Identifier() : renderer.cameraColorTarget;
                //     m_AfterPostProcessPass.Setup(source, source);
                //     renderer.EnqueuePass(m_AfterPostProcessPass);
                // }
            }
        }

        public override void Create()
        {
            m_AfterPostProcessColor.Init("_AfterPostProcessTexture");
            Dictionary<string, FernPostProcessRenderer> shared = new Dictionary<string, FernPostProcessRenderer>();
            List<FernPostProcessRenderer> afterOpaqueAndSkyRenderers = new List<FernPostProcessRenderer>();
            afterOpaqueAndSkyRenderers.Add(new EdgeDetectionEffectRenderer());
            List<FernPostProcessRenderer> beforePostProcessRenderers = new List<FernPostProcessRenderer>();
            beforePostProcessRenderers.Add(new EdgeDetectionEffectRenderer());
            List<FernPostProcessRenderer> afterPostProcessRenderers = new List<FernPostProcessRenderer>();
            afterPostProcessRenderers.Add(new EdgeDetectionEffectRenderer());
            m_AfterOpaqueAndSkyPass = new FernPostProcessRenderPass(FernPostProcessInjectionPoint.AfterOpaqueAndSky, afterOpaqueAndSkyRenderers);
            m_BeforePostProcessPass = new FernPostProcessRenderPass(FernPostProcessInjectionPoint.BeforePostProcess, beforePostProcessRenderers);
            m_AfterPostProcessPass = new FernPostProcessRenderPass(FernPostProcessInjectionPoint.AfterPostProcess, afterPostProcessRenderers);
        }
    }

    /// <summary>
    /// A render pass for executing custom post processing renderers.
    /// </summary>
    public class FernPostProcessRenderPass : ScriptableRenderPass
    {
        /// <summary>
        /// The injection point of the pass
        /// </summary>
        private FernPostProcessInjectionPoint injectionPoint;

        /// <summary>
        /// The pass name which will be displayed on the command buffer in the frame debugger.
        /// </summary>
        private string m_PassName;

        /// <summary>
        /// List of all post process renderer instances.
        /// </summary>
        private List<FernPostProcessRenderer> m_PostProcessRenderers;

        /// <summary>
        /// List of all post process renderer instances that are active for the current camera.
        /// </summary>
        private List<int> m_ActivePostProcessRenderers;

        /// <summary>
        /// Array of 2 intermediate render targets used to hold intermediate results.
        /// </summary>
        private RenderTargetHandle m_Intermediate;

        /// <summary>
        /// The texture descriptor for the intermediate render targets.
        /// </summary>
        private RenderTextureDescriptor m_IntermediateDesc;

        /// <summary>
        /// The source of the color data for the render pass
        /// </summary>
        private RenderTargetIdentifier m_Source;

        /// <summary>
        /// The destination of the color data for the render pass
        /// </summary>
        private RenderTargetIdentifier m_Destination;

        /// <summary>
        /// A list of profiling samplers, one for each post process renderer
        /// </summary>
        private List<ProfilingSampler> m_ProfilingSamplers;

        /// <summary>
        /// Gets whether this render pass has any post process renderers to execute
        /// </summary>
        public bool HasPostProcessRenderers => m_PostProcessRenderers.Count != 0;

        /// <summary>
        /// Construct the render pass
        /// </summary>
        /// <param name="injectionPoint">The post processing injection point</param>
        /// <param name="classes">The list of classes for the renderers to be executed by this render pass</param>
        public FernPostProcessRenderPass(FernPostProcessInjectionPoint injectionPoint, List<FernPostProcessRenderer> renderers){
            this.injectionPoint = injectionPoint;
            this.m_ProfilingSamplers = new List<ProfilingSampler>(renderers.Count);
            this.m_PostProcessRenderers = renderers;
            foreach(var renderer in renderers){
                // Get renderer name and add it to the names list
                var attribute = FernPostProcessAttribute.GetAttribute(renderer.GetType());
                m_ProfilingSamplers.Add(new ProfilingSampler(attribute?.Name));
            }
            // Pre-allocate a list for active renderers
            this.m_ActivePostProcessRenderers = new List<int>(renderers.Count);
            // Set render pass event and name based on the injection point.
            switch(injectionPoint){
                case FernPostProcessInjectionPoint.AfterOpaqueAndSky: 
                    renderPassEvent = RenderPassEvent.AfterRenderingSkybox; 
                    m_PassName = "Fern PostProcess after Opaque & Sky";
                    break;
                case FernPostProcessInjectionPoint.BeforePostProcess: 
                    renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
                    m_PassName = "Fern PostProcess before PostProcess";
                    break;
                case FernPostProcessInjectionPoint.AfterPostProcess:
                    // NOTE: This was initially "AfterRenderingPostProcessing" but it made the builtin post-processing to blit directly to the camera target.
                    renderPassEvent = RenderPassEvent.AfterRendering;
                    m_PassName = "Fern PostProcess after PostProcess";
                    break;
            }
            // Initialize the IDs and allocation state of the intermediate render targets
            m_Intermediate = new RenderTargetHandle();
            m_Intermediate.Init("_IntermediateRT0");
        }

        /// <summary>
        /// Gets the corresponding intermediate RT and allocates it if not already allocated
        /// </summary>
        /// <param name="cmd">The command buffer to use for allocation</param>
        /// <returns></returns>
        private RenderTargetIdentifier GetIntermediate(CommandBuffer cmd){
            cmd.GetTemporaryRT(m_Intermediate.id, m_IntermediateDesc);
            return m_Intermediate.Identifier();
        }

        /// <summary>
        /// Release allocated intermediate RTs
        /// </summary>
        /// <param name="cmd">The command buffer to use for deallocation</param>
        private void CleanupIntermediate(CommandBuffer cmd){
            cmd.ReleaseTemporaryRT(m_Intermediate.id);
        }

        /// <summary>
        /// Setup the source and destination render targets
        /// </summary>
        /// <param name="source">Source render target</param>
        /// <param name="destination">Destination render target</param>
        public void Setup(RenderTargetIdentifier source, RenderTargetIdentifier destination){
            this.m_Source = source;
            this.m_Destination = destination;
        }

        /// <summary>
        /// Prepares the renderer for executing on this frame and checks if any of them actually requires rendering
        /// </summary>
        /// <param name="renderingData">Current rendering data</param>
        /// <returns>True if any renderer will be executed for the given camera. False Otherwise.</returns>
        public bool PrepareRenderers(ref RenderingData renderingData){
            // See if current camera is a scene view camera to skip renderers with "visibleInSceneView" = false.
            bool isSceneView = renderingData.cameraData.cameraType == CameraType.SceneView;

            // Here, we will collect the inputs needed by all the custom post processing effects
            ScriptableRenderPassInput passInput = ScriptableRenderPassInput.None;

            // Collect the active renderers
            m_ActivePostProcessRenderers.Clear();
            for(int index = 0; index < m_PostProcessRenderers.Count; index++){
                var ppRenderer = m_PostProcessRenderers[index];
                // Skips current renderer if "visibleInSceneView" = false and the current camera is a scene view camera. 
                if(isSceneView && !ppRenderer.visibleInSceneView) continue;
                // Setup the camera for the renderer and if it will render anything, add to active renderers and get its required inputs
                if(ppRenderer.Setup(ref renderingData, injectionPoint)){
                    m_ActivePostProcessRenderers.Add(index);
                    passInput |= ppRenderer.input;
                }
            }

            // Configure the pass to tell the renderer what inputs we need
            ConfigureInput(passInput);

            // return if no renderers are active
            return m_ActivePostProcessRenderers.Count != 0;
        } 

        /// <summary>
        /// Execute the custom post processing renderers
        /// </summary>
        /// <param name="context">The scriptable render context</param>
        /// <param name="renderingData">Current rendering data</param>
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            m_IntermediateDesc = renderingData.cameraData.cameraTargetDescriptor;
            m_IntermediateDesc.msaaSamples = 1; // intermediate RT don't need multisampling and depth buffer
            m_IntermediateDesc.depthBufferBits = 0;

            CommandBuffer cmd = CommandBufferPool.Get(m_PassName);
            
            context.ExecuteCommandBuffer(cmd);

            int width = m_IntermediateDesc.width;
            int height = m_IntermediateDesc.height;
            cmd.SetGlobalVector("_ScreenSize", new Vector4(width, height, 1.0f/width, 1.0f/height));
            
            // The variable will be true if the last renderer couldn't blit to destination.
            // This happens if there is only 1 renderer and the source is the same as the destination.
            // The current intermediate RT to use as a source.
            for(int index = 0; index < m_ActivePostProcessRenderers.Count; ++index){
                var rendererIndex = m_ActivePostProcessRenderers[index];
                var renderer = m_PostProcessRenderers[rendererIndex];
                
                RenderTargetIdentifier source, destination;
                // If this is the first renderers then the source will be the external source (not intermediate).
                source = m_Source;
                destination = GetIntermediate(cmd);
                
                using(new ProfilingScope(cmd, m_ProfilingSamplers[rendererIndex]))
                {
                    // If the renderer was not already initialized, initialize it.
                    if(!renderer.Initialized)
                        renderer.InitializeInternal();
                    // Execute the renderer.
                    renderer.Render(cmd, source, destination, m_Destination, ref renderingData, injectionPoint);
                }
            }

            // Release allocated Intermediate RTs.
            CleanupIntermediate(cmd);

            // Send command buffer for execution, then release it.
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}