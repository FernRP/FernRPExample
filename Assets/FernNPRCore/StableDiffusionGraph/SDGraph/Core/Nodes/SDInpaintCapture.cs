using System.Collections.Generic;
using FernGraph;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace StableDiffusionGraph.SDGraph.Nodes
{
    [Node(Path = "SD Standard")]
    [Tags("SD Node")]
    public class SDInpaintCapture : Node
    {
        [Output("Capture")] public Texture2D Capture;

        public Camera currentCamere;
        private RenderTexture cameraRT;
        private RenderPipelineAsset sdUniversal;

        public override void OnEnable()
        {
            base.OnEnable();
            sdUniversal = Resources.Load<RenderPipelineAsset>("UniversalData/SDUniversalRenderPipeline");
        }

        public override object OnRequestValue(Port port)
        {
            var resolution = SDUtil.GetMainGameViewSize();

            if (cameraRT == null)
            {
                cameraRT = RenderTexture.GetTemporary((int)resolution.x, (int)resolution.y, 24, RenderTextureFormat.Default);
            } else
            {
                cameraRT.Release();
                cameraRT = RenderTexture.GetTemporary((int)resolution.x, (int)resolution.y, 24, RenderTextureFormat.Default);
            }
            if (currentCamere == null)
            {
                currentCamere = Camera.main;
            }

            var cameraUniversalData = currentCamere.GetComponent<UniversalAdditionalCameraData>();
            
            // temp car param
            var normalBackGround = Camera.main.backgroundColor;
            var normalClearFlags = Camera.main.clearFlags;
            var normalRenderPipelineAsset = GraphicsSettings.renderPipelineAsset;
            var normalLayer = Camera.main.cullingMask;

            if (sdUniversal == null)
            {
                sdUniversal = Resources.Load<RenderPipelineAsset>("UniversalData/SDUniversalRenderPipeline");
            }
            GraphicsSettings.renderPipelineAsset = sdUniversal;
            cameraUniversalData.SetRenderer(1);

            currentCamere.clearFlags = CameraClearFlags.Color;
            currentCamere.backgroundColor = Color.clear;
            var tempRT = currentCamere.targetTexture;
            currentCamere.targetTexture = cameraRT;
            currentCamere.Render();
            Capture = RenderTextureToTexture2D(cameraRT);

            // restore
            currentCamere.targetTexture = tempRT;
            currentCamere.backgroundColor = normalBackGround;
            currentCamere.clearFlags = normalClearFlags;
            GraphicsSettings.renderPipelineAsset = normalRenderPipelineAsset;
            cameraUniversalData.SetRenderer(0);
            return Capture;
        }
        
        public Texture2D RenderTextureToTexture2D(RenderTexture renderTexture)
        {
            Texture2D texture2D = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGBA32, false);
            RenderTexture.active = renderTexture;
            texture2D.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            texture2D.Apply();
            RenderTexture.active = null;
            return texture2D;
        }
    }
}
