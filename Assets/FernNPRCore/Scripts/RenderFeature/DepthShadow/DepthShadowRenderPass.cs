using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class DepthShadowRenderPass : ScriptableRenderPass
{
    private static readonly ShaderTagId k_ShaderTagId = new ShaderTagId("DepthOnly");

    private ProfilingSampler m_ProfilingSampler = new ProfilingSampler("DepthShadowPrepass");


    private RenderTargetHandle depthAttachmentHandle { get; set; }
    internal RenderTextureDescriptor descriptor;
    internal bool allocateDepth { get; set; } = true;
    internal ShaderTagId shaderTagId { get; set; } = k_ShaderTagId;

    internal int downSampler;

    FilteringSettings m_FilteringSettings;

    /// <summary>
    /// Create the DepthOnlyPass
    /// </summary>
    public DepthShadowRenderPass(RenderPassEvent evt, RenderQueueRange renderQueueRange, LayerMask layerMask,
        int downSampler)
    {
        base.profilingSampler = new ProfilingSampler(nameof(DepthShadowRenderPass));
        m_FilteringSettings = new FilteringSettings(renderQueueRange, layerMask);
        renderPassEvent = evt;
        this.downSampler = downSampler;
    }

    /// <summary>
    /// Configure the pass
    /// </summary>
    public void Setup(
        RenderTextureDescriptor baseDescriptor,
        RenderTargetHandle depthAttachmentHandle)
    {
        this.depthAttachmentHandle = depthAttachmentHandle;
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
        if (this.allocateDepth)
            cmd.GetTemporaryRT(depthAttachmentHandle.id, descriptor, FilterMode.Point);
        var desc = renderingData.cameraData.cameraTargetDescriptor;

        RenderTargetIdentifier targetIdentifier =
            new RenderTargetIdentifier(depthAttachmentHandle.Identifier(), 0, CubemapFace.Unknown, -1);

        ConfigureTarget(targetIdentifier);
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

    /// <inheritdoc/>
    public override void OnCameraCleanup(CommandBuffer cmd)
    {
        if (cmd == null)
            throw new ArgumentNullException("cmd");

        if (depthAttachmentHandle != RenderTargetHandle.CameraTarget)
        {
            if (this.allocateDepth)
                cmd.ReleaseTemporaryRT(depthAttachmentHandle.id);
            depthAttachmentHandle = RenderTargetHandle.CameraTarget;
        }
    }
}