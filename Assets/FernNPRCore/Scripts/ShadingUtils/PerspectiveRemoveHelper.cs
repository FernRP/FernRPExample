using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace FernNPRCore.Scripts.ShadingUtils
{
    public class PerspectiveRemoveHelper : MonoBehaviour
    {
        public Renderer targetRender;
        public Camera targetCamera;
        public float inFluenceRange = 0.16f;
        public int subMeshIndex = 0;
        public bool allSubMesh = false;

        private static readonly int FocusPoint = Shader.PropertyToID("_FocusPoint");
        private static readonly int PerspectiveRemoveRange = Shader.PropertyToID("_PerspectiveRemoveRange");
        private static readonly int PmRemove = Shader.PropertyToID("PMRemove");

        private float fovAngle;
        private float tanHalf;
        private MaterialPropertyBlock materialBlock;
        private Renderer renderer;
        private void Awake()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }
            
            fovAngle = targetCamera.fieldOfView * Mathf.Deg2Rad;
            tanHalf = Mathf.Tan(fovAngle * 0.5f);

            materialBlock = new MaterialPropertyBlock();
            renderer = GetComponent<Renderer>();
        }

        void Update()
        {
            if (targetCamera != null && transform != null && renderer != null)
            {
                //Get the distance on camera forward between target and current camera
                var position = targetCamera.transform.position;
                Vector3 linepoint = transform.position - position;
                Vector3 cForward = targetCamera.transform.forward;
                Vector3 p = Vector3.Project(linepoint, cForward);
                float targetDistance = p.magnitude;

                float a = targetCamera.aspect;
                float s = targetDistance * tanHalf;
                float width = a * s;
                float height = s;

                //Create OrthoProjectMatrix
                Matrix4x4 orthoProjectMatrix = Matrix4x4.Ortho(-width, width, height, -height,
                    targetCamera.nearClipPlane, targetCamera.farClipPlane);
                orthoProjectMatrix = GL.GetGPUProjectionMatrix(orthoProjectMatrix, false);

                //Create ViewMatrix
                Matrix4x4 viewMatrix = Matrix4x4.TRS(position, targetCamera.transform.rotation, Vector3.one).inverse;
                if (SystemInfo.usesReversedZBuffer)
                {
                    viewMatrix.m20 = -viewMatrix.m20;
                    viewMatrix.m21 = -viewMatrix.m21;
                    viewMatrix.m22 = -viewMatrix.m22;
                    viewMatrix.m23 = -viewMatrix.m23;
                }

                //Create VP Martix
                Matrix4x4 viewProject = orthoProjectMatrix * viewMatrix;
                materialBlock.SetMatrix(PmRemove, viewProject);
                materialBlock.SetVector(FocusPoint, transform.position);
                materialBlock.SetFloat(PerspectiveRemoveRange, inFluenceRange);
                if (allSubMesh)
                {
                    renderer.SetPropertyBlock(materialBlock);
                }
                else
                {
                    renderer.SetPropertyBlock(materialBlock, subMeshIndex);
                }
            }
        }
        
        
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, inFluenceRange);
        }
    }
}