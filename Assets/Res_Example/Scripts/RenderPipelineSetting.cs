using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteAlways]
public class RenderPipelineSetting : MonoBehaviour
{
    public RenderPipelineAsset renderPipelineAsset;

    private void OnEnable()
    {
        if(renderPipelineAsset != null) GraphicsSettings.renderPipelineAsset = renderPipelineAsset;
    }
}
