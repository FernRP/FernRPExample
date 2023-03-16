using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace FernNPRCore.Scripts.FernNPRRenderer
{
    [ExecuteAlways]
    public class FernRenderer : MonoBehaviour
    {
        public RenderPipelineAsset renderPipelineAsset;

        private UniversalAdditionalCameraData FernCameraData;
        private Vector4 depthSourceSize = Vector4.one;
        private float cameraAspect = 0;
        private float cameraFov = 0;
        
        private static readonly int ShaderID_DepthTextureSourceSize = Shader.PropertyToID("_DepthTextureSourceSize");
        private static readonly int ShaderID_CameraAspect = Shader.PropertyToID("_CameraAspect");
        private static readonly int ShaderID_CameraFOV = Shader.PropertyToID("_CameraFOV");

        private void OnEnable()
        {

            GraphicsSettings.renderPipelineAsset = renderPipelineAsset;
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRender;
        }


        private void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRender;
        }

        private void OnBeginCameraRender(ScriptableRenderContext context, Camera camera)
        {
            if (Math.Abs(cameraAspect - camera.aspect) > 1e-5)
            {
                var aspect = camera.aspect;
                cameraAspect = aspect;
                Shader.SetGlobalFloat(ShaderID_CameraAspect, 1.0f / aspect);
            }
    
            if (Math.Abs(cameraFov - camera.fieldOfView) > 1e-5)
            {
                cameraFov = camera.fieldOfView;
                Shader.SetGlobalFloat(ShaderID_CameraFOV, 1.0f / (camera.orthographic? camera.orthographicSize * 100 : camera.fieldOfView));
            }

            // TODO: should get all cameras and then set sourceSize individually before camera rendering
            if (!depthSourceSize.z.Equals(camera.pixelWidth) || !depthSourceSize.w.Equals(camera.pixelHeight))
            {
                depthSourceSize.x = 1.0f / camera.pixelWidth;
                depthSourceSize.y = 1.0f / camera.pixelHeight;
                depthSourceSize.z = Screen.width;
                depthSourceSize.w = Screen.height;
                Shader.SetGlobalVector(ShaderID_DepthTextureSourceSize, depthSourceSize);
            }
        }
    }
}

