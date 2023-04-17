using System.Collections.Generic;
using BlueGraph;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace StableDiffusionGraph.SDGraph.Nodes
{
    [Node(Path = "SD Standard")]
    [Tags("SD Node")]
    public class SDCameraCapture : Node
    {
        [Output("Capture")] public Texture2D Capture;

        public Camera currentCamere;
        private RenderTexture cameraRT;

        public override object OnRequestValue(Port port)
        {
            var resolution = SDUtil.GetMainGameViewSize();
            Debug.Log($"SD Log: Camera Capture Width: {resolution.x} + Height: + {resolution.y}");

            if (currentCamere == null)
            {
                currentCamere = Camera.main;
            }
            if (cameraRT == null)
            {
                cameraRT = RenderTexture.GetTemporary((int)resolution.x, (int)resolution.y, 24, RenderTextureFormat.DefaultHDR);
            } else
            {
                cameraRT.Release();
                cameraRT = RenderTexture.GetTemporary((int)resolution.x, (int)resolution.y, 24, RenderTextureFormat.DefaultHDR);
            }

            var tempRT = currentCamere.targetTexture;
            currentCamere.targetTexture = cameraRT;
            currentCamere.Render();
            currentCamere.targetTexture = tempRT;
            Capture = RenderTextureToTexture2D(cameraRT);
            return Capture;
        }
        
        public Texture2D RenderTextureToTexture2D(RenderTexture renderTexture)
        {
            Texture2D texture2D = new Texture2D(renderTexture.width, renderTexture.height, DefaultFormat.HDR, TextureCreationFlags.None);
            RenderTexture.active = renderTexture;
            texture2D.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            texture2D.Apply();
            RenderTexture.active = null;
            return texture2D;
        }
    }
}
