using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace FernNPRCore.Scripts.RenderFeature.DepthShadow
{
    public class DepthShadowRenderFeature : ScriptableRendererFeature
    {
        [Serializable]
        internal class DepthShadowSetting
        {
            [Range(0, 4)] public int downSample = 1;
        }

        [SerializeField] private DepthShadowSetting depthShadowSetting = new DepthShadowSetting();
        DepthShadowRenderPass m_DepthShadowPass;
        private RTHandle m_depthShadowRTHandle;

        /// <inheritdoc/>
        public override void Create()
        {
            m_DepthShadowPass = new DepthShadowRenderPass(RenderPassEvent.BeforeRenderingPrePasses, RenderQueueRange.opaque, depthShadowSetting.downSample);
        }

        // Here you can inject one or multiple render passes in the renderer.
        // This method is called when setting up the renderer once per-camera.
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            m_DepthShadowPass.Setup(renderingData.cameraData.cameraTargetDescriptor);
            renderer.EnqueuePass(m_DepthShadowPass);
        }

        protected override void Dispose(bool disposing)
        {
            m_DepthShadowPass?.Dispose();
            m_DepthShadowPass = null;
        }
    }
}