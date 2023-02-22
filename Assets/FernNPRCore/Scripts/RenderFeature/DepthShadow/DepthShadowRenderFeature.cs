using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Experimental.Rendering;

public class DepthShadowRenderFeature : ScriptableRendererFeature
{
    [Serializable]
    internal class DepthShadowSetting
    {
        public LayerMask m_LayerMask = -1;
        [Range(0, 4)] public int downSample = 1;
    }

    [SerializeField] private DepthShadowSetting depthShadowSetting = new DepthShadowSetting();
    DepthShadowRenderPass m_DepthShadowPass;
    RenderTargetHandle m_DepthShadowTexture;

    /// <inheritdoc/>
    public override void Create()
    {
        m_DepthShadowPass = new DepthShadowRenderPass(RenderPassEvent.BeforeRenderingPrePasses, RenderQueueRange.opaque,
            depthShadowSetting.m_LayerMask, depthShadowSetting.downSample);
        m_DepthShadowTexture.Init("_CameraDepthShadowTexture");
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        m_DepthShadowPass.Setup(renderingData.cameraData.cameraTargetDescriptor, m_DepthShadowTexture);
        renderer.EnqueuePass(m_DepthShadowPass);
    }
}