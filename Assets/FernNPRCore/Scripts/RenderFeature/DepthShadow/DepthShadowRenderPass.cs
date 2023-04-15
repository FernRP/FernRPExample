using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace FernNPRCore.Scripts.RenderFeature.DepthShadow
{
    public class DepthShadowRenderPass : ScriptableRenderPass
    {
        private static readonly ShaderTagId k_ShaderTagId = new ShaderTagId("DepthShadowOnly");

        private ProfilingSampler m_ProfilingSampler = new ProfilingSampler("DepthShadowPrepass");

        RTHandle depthShadowRTHandle;

        private RenderTextureDescriptor descriptor;
        private bool allocateDepth { get; set; } = true;
        private ShaderTagId shaderTagId { get; set; } = k_ShaderTagId;

        private int downSampler;

        FilteringSettings m_FilteringSettings;

        /// <summary>
        /// Create the DepthOnlyPass
        /// </summary>
        public DepthShadowRenderPass(RenderPassEvent evt, RenderQueueRange renderQueueRange, int downSampler)
        {
            base.profilingSampler = new ProfilingSampler(nameof(DepthShadowRenderPass));
            m_FilteringSettings = new FilteringSettings(renderQueueRange);
            renderPassEvent = evt;
            this.downSampler = downSampler;
        }

        /// <summary>
        /// Configure the pass
        /// </summary>
        public void Setup(
            RenderTextureDescriptor baseDescriptor)
        {
            baseDescriptor.colorFormat = RenderTextureFormat.Depth;
            baseDescriptor.depthBufferBits = 16;

            // Depth-Only pass don't use MSAA
            baseDescriptor.msaaSamples = 1;
            descriptor = baseDescriptor;
            descriptor.width >>= downSampler;
            descriptor.height >>= downSampler;

            this.allocateDepth = true;
            this.shaderTagId = k_ShaderTagId;

        }
    
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            var desc = renderingData.cameraData.cameraTargetDescriptor;
        
            RenderingUtils.ReAllocateIfNeeded(ref depthShadowRTHandle, desc, FilterMode.Bilinear,
                TextureWrapMode.Clamp, name: "_CameraDepthShadowTexture");
            cmd.SetGlobalTexture(depthShadowRTHandle.name, depthShadowRTHandle.nameID);

            ConfigureTarget(depthShadowRTHandle);
            // Only clear depth here so we don't clear any bound color target. It might be unused by this pass but that doesn't mean we can just clear it. (e.g. in case of overlay cameras + depth priming)
            ConfigureClear(ClearFlag.Depth, Color.black);
        }
    
        /// <inheritdoc/>
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            // NOTE: Do NOT mix ProfilingScope with named CommandBuffers i.e. CommandBufferPool.Get("name").
            // Currently there's an issue which results in mismatched markers.
            CommandBuffer cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, m_ProfilingSampler))
            {
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                var sortFlags = renderingData.cameraData.defaultOpaqueSortFlags;
                var drawSettings = CreateDrawingSettings(this.shaderTagId, ref renderingData, sortFlags);
                drawSettings.perObjectData = PerObjectData.None;
            
                context.DrawRenderers(renderingData.cullResults, ref drawSettings, ref m_FilteringSettings);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public void Dispose()
        {
            depthShadowRTHandle?.Release();
        }
    }
}