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
        public void Setup(RenderTextureDescriptor baseDescriptor)
        {
            descriptor = baseDescriptor;

            descriptor.colorFormat = RenderTextureFormat.R8;
            descriptor.depthBufferBits = 16;

            // Depth-Only pass don't use MSAA
            descriptor.msaaSamples = 1;
            descriptor.width >>= downSampler;
            descriptor.height >>= downSampler;

            this.shaderTagId = k_ShaderTagId;

        }
    
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            RenderingUtils.ReAllocateIfNeeded(ref depthShadowRTHandle, descriptor, FilterMode.Point,TextureWrapMode.Clamp, name: "_CameraDepthShadowTexture");

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
                
                // TODO: Should use AABB on Face Mesh to take advantage of more pixels
                var sortFlags = renderingData.cameraData.defaultOpaqueSortFlags;
                var drawSettings = CreateDrawingSettings(this.shaderTagId, ref renderingData, sortFlags);
                drawSettings.perObjectData = PerObjectData.None;
            
                context.DrawRenderers(renderingData.cullResults, ref drawSettings, ref m_FilteringSettings);
                cmd.SetGlobalTexture(depthShadowRTHandle.name, depthShadowRTHandle.nameID);
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