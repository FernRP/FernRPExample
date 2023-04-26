using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class PreprocessLineURP : ScriptableRendererFeature {
    PreprocessLineURPPass preprocessLineURPPass;
    public override void Create() {
        preprocessLineURPPass = new PreprocessLineURPPass();
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) {
        renderer.EnqueuePass(preprocessLineURPPass);
    }
}
