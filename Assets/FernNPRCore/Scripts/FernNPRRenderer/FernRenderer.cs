using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;


namespace FernRender.Universal
{
    [ExecuteAlways]
    public class FernRenderer : MonoBehaviour
    {
        public RenderPipelineAsset renderPipelineAsset;
        public Camera FernCamera;

        private UniversalAdditionalCameraData FernCameraData;
        private Vector4 depthSourceSize = Vector4.one;
        private float cameraAspect = 0;
        private float cameraFov = 0;
        
        private static readonly int ShaderID_DepthTextureSourceSize = Shader.PropertyToID("_DepthTextureSourceSize");
        private static readonly int ShaderID_CameraAspect = Shader.PropertyToID("_CameraAspect");
        private static readonly int ShaderID_CameraFOV = Shader.PropertyToID("_CameraFOV");

        private void OnEnable()
        {
            if (FernCamera == null)
            {
                FernCamera = Camera.main;
                FernCameraData = FernCamera.GetComponent<UniversalAdditionalCameraData>();
            }

            if (renderPipelineAsset == null)
            {
                if (FernCamera != null)
                {
                    renderPipelineAsset = GraphicsSettings.renderPipelineAsset;
                }
            }
            else
            {
                GraphicsSettings.renderPipelineAsset = renderPipelineAsset;
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (Math.Abs(cameraAspect - FernCamera.aspect) > 1e-5)
            {
                var aspect = FernCamera.aspect;
                cameraAspect = aspect;
                Shader.SetGlobalFloat(ShaderID_CameraAspect, 1.0f / aspect);
            }
    
            if (Math.Abs(cameraFov - FernCamera.fieldOfView) > 1e-5)
            {
                cameraFov = FernCamera.fieldOfView;
                Shader.SetGlobalFloat(ShaderID_CameraFOV, 1.0f / (FernCamera.orthographic? FernCamera.orthographicSize * 100 : FernCamera.fieldOfView));
            }

            // TODO: should get all cameras and then set sourceSize individually before camera rendering
            if (!depthSourceSize.z.Equals(FernCamera.pixelWidth) || !depthSourceSize.w.Equals(FernCamera.pixelHeight))
            {
                depthSourceSize.x = 1.0f / FernCamera.pixelWidth;
                depthSourceSize.y = 1.0f / FernCamera.pixelHeight;
                depthSourceSize.z = Screen.width;
                depthSourceSize.w = Screen.height;
                Shader.SetGlobalVector(ShaderID_DepthTextureSourceSize, depthSourceSize);
            }
        }
    }
}

