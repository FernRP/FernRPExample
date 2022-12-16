using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.Universal.PostProcessing;
using UnityEngine.Serialization;

namespace Fern.PostProcess {

    [System.Serializable, VolumeComponentMenu("FernPostProcess/Edge Detection Outline")]
    public class EdgeDetectionOutlineEffect : VolumeComponent
    {
        [Tooltip("Controls the Effect Intensity")]
        public ClampedFloatParameter intensity = new ClampedFloatParameter(0, 0, 1);
        
        [Tooltip("Controls the edge thickness.")]
        public ClampedFloatParameter thickness = new ClampedFloatParameter(1, 0, 8);
        
        [FormerlySerializedAs("normalThreshold")] [Tooltip("Controls the threshold of the normal difference in degrees.")]
        public ClampedFloatParameter angleThreshold = new ClampedFloatParameter(1, 1, 179.9f);

        [Tooltip("Controls the threshold of the depth difference in world units.")]
        public ClampedFloatParameter depthThreshold = new ClampedFloatParameter(0.01f, 0.001f, 1);

        [Tooltip("Controls the edge color.")]
        public ColorParameter color = new ColorParameter(Color.black, true, false, true);
        
        [Tooltip("Controls Sampler 4 Or 8 times")]
        public BoolParameter LowQuality = new BoolParameter(false);
    }

    [FernPostProcess("Edge Detection", FernPostProcessInjectionPoint.AfterOpaqueAndSky)]
    public class EdgeDetectionEffectRenderer : FernPostProcessRenderer
    {
        private EdgeDetectionOutlineEffect m_VolumeComponent;
        
        private Material m_Material;
        
        static class ShaderIDs {
            internal readonly static int Input = Shader.PropertyToID("_MainTex");
            internal readonly static int Threshold = Shader.PropertyToID("_Threshold");
            internal readonly static int Color = Shader.PropertyToID("_Color");
            internal readonly static string LowQuality = "_LowQuality";
        }
        
        public override bool visibleInSceneView => true;
        
        public override ScriptableRenderPassInput input => ScriptableRenderPassInput.Color | ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Normal;

        public override void Initialize()
        {
            m_Material = CoreUtils.CreateEngineMaterial("Hidden/FernNPR/PostProcess/EdgeDetectionOutline");
        }

        public override bool Setup(ref RenderingData renderingData, FernPostProcessInjectionPoint injectionPoint)
        {
            var stack = VolumeManager.instance.stack;
            m_VolumeComponent = stack.GetComponent<EdgeDetectionOutlineEffect>();
            return m_VolumeComponent.intensity.value > 0;
        }

        public override void Render(CommandBuffer cmd, RenderTargetIdentifier source, RenderTargetIdentifier destination, RenderTargetIdentifier cameraDestination, ref RenderingData renderingData, FernPostProcessInjectionPoint injectionPoint)
        {
            if(m_Material != null){
                float angleThreshold = m_VolumeComponent.angleThreshold.value;
                float depthThreshold = m_VolumeComponent.depthThreshold.value;
                Vector4 threshold = new Vector4(Mathf.Cos(angleThreshold * Mathf.Deg2Rad), m_VolumeComponent.thickness.value, depthThreshold, m_VolumeComponent.intensity.value);
                m_Material.SetVector(ShaderIDs.Threshold, threshold);
                m_Material.SetColor(ShaderIDs.Color, m_VolumeComponent.color.value);
                CoreUtils.SetKeyword(m_Material, ShaderIDs.LowQuality, m_VolumeComponent.LowQuality.value);
            }
            cmd.SetGlobalTexture(ShaderIDs.Input, source);
            CoreUtils.DrawFullScreen(cmd, m_Material, destination);
            // blit back for now, if there are more effect, may be don't blit back
            cmd.SetRenderTarget(cameraDestination);
            cmd.Blit(destination, cameraDestination);
        }
    }
}