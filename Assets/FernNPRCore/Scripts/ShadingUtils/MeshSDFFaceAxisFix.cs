using System;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;


namespace FernRender.MaterialTool
{
    [ExecuteAlways]
    [ExecuteInEditMode]
    public class MeshSDFFaceAxisFix : MonoBehaviour
    {
        public enum FaceMeshAxisEnum
        {
            X,
            Y,
            Z,
            ReverseX,
            ReverseY,
            ReverseZ
        }

        [Header("Face Forward(Object Space)")] public FaceMeshAxisEnum forwardEnum = FaceMeshAxisEnum.ReverseY;
        [Header("Face Right(Object Space)")] public FaceMeshAxisEnum rightEnum = FaceMeshAxisEnum.X;

        private static readonly int faceObjectToWorld = Shader.PropertyToID("_FaceObjectToWorld");
        private new MeshRenderer renderer;
        private MaterialPropertyBlock faceMaterialblock;

        private Vector3 forward;
        private Vector3 right;

        private void OnEnable()
        {
            renderer = GetComponent<MeshRenderer>();
            if (faceMaterialblock == null)
            {
                faceMaterialblock = new MaterialPropertyBlock();
            }

            SetupFaceObjectAxis();
        }

        private void SetupFaceAxis()
        {
            forward = forwardEnum switch
            {
                FaceMeshAxisEnum.X => Vector3.right,
                FaceMeshAxisEnum.Y => Vector3.up,
                FaceMeshAxisEnum.Z => Vector3.forward,
                FaceMeshAxisEnum.ReverseX => -Vector3.right,
                FaceMeshAxisEnum.ReverseY => -Vector3.up,
                FaceMeshAxisEnum.ReverseZ => -Vector3.forward,
                _ => forward
            };

            right = rightEnum switch
            {
                FaceMeshAxisEnum.X => Vector3.right,
                FaceMeshAxisEnum.Y => Vector3.up,
                FaceMeshAxisEnum.Z => Vector3.forward,
                FaceMeshAxisEnum.ReverseX => -Vector3.right,
                FaceMeshAxisEnum.ReverseY => -Vector3.up,
                FaceMeshAxisEnum.ReverseZ => -Vector3.forward,
                _ => right
            };
        }

        private void SetupFaceObjectAxis()
        {
            if (renderer == null) return;

            SetupFaceAxis();

            forward = transform.TransformDirection(forward);
            right = transform.TransformDirection(right);
        }

        private void Update()
        {
            if (renderer == null) return;

#if UNITY_EDITOR
            SetupFaceObjectAxis();
#endif
            renderer.GetPropertyBlock(faceMaterialblock);
            Matrix4x4 faceObjectToWorldMatrix = Matrix4x4.zero;
            faceObjectToWorldMatrix.SetColumn(0, right);
            faceObjectToWorldMatrix.SetColumn(1, Vector4.zero);
            faceObjectToWorldMatrix.SetColumn(2, forward);
            faceMaterialblock.SetMatrix(faceObjectToWorld, faceObjectToWorldMatrix);
            renderer.SetPropertyBlock(faceMaterialblock);
        }
    }
}