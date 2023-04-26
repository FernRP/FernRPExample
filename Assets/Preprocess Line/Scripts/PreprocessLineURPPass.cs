using System.Collections;
using System.Collections.Generic;
using PreprocessLine;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PreprocessLineURPPass : ScriptableRenderPass {
    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) {
        CommandBuffer command = CommandBufferPool.Get("PreprocessLineURPPass");
        
        var cartoonLines = PreprocessLineCore.GetCollection();
        foreach (var line in cartoonLines) {
            if (line.IsVisible()) {
                line.Draw(command);
            }
        }

        context.ExecuteCommandBuffer(command);
        CommandBufferPool.Release(command);
    }
}
