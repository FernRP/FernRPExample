using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using UnityEngine.Rendering;
using AquaSys.SmoothNormals;

namespace AquaSys.TextureBaker
{
    public enum SourceColorChannel
    {
        RGBA,
        RGB,
        RG,
        Red,
        Green,
        Blue,
        Alpha,
        Grayscale
    }

    public enum TargetColorChannel
    {
        None = 0,
        R = 1 << 0,
        G = 1 << 1,
        B = 1 << 2,
        A = 1 << 3
    }

    public enum TargetVertexAttribute
    {
        UV3 = 2,
        UV4,
        UV5,
        UV6,
        UV7,
        UV8,
        Color
    }

    public enum DataSource
    {
        MaterialTexture,
        CustomTexture,
        SmoothedNormal
    }

    public enum ChannelDataWriteType
    {
        Merge,
        Override,
    }

    public class AquaTextureBaker
    {
        static ComputeShader computeShader;
        static ComputeShader ComputeShader
        {
            get
            {
                if (computeShader == null)
                {
                    computeShader = Resources.Load<ComputeShader>("SampleTextureToColor");
                }
                return computeShader;
            }
        }

        public static Vector4[] SmoothedNormal(Mesh meh)
        {
            var originData = SmoothNormals.AquaSmoothNormals.ComputeSmoothedNormalsV3(meh);
            Vector4[] res = new Vector4 [originData.Length];
            for (int i = 0; i < originData.Length; i++)
            {
                res [i] = originData [i];
            }
            return res;
        }

        public static Vector4[] SmoothedNormalV2(Mesh meh)
        {
            var originData = SmoothNormals.AquaSmoothNormals.ComputeSmoothedNormals(meh);
            Vector4[] res = new Vector4[originData.Length];
            for (int i = 0; i < originData.Length; i++)
            {
                res[i] = originData[i];
            }
            return res;
        }

        public static Vector4[] SampleTexture(Mesh mesh, Texture texture)
        {
            int vertexCount = mesh.vertices.Length;
            List<Vector2> uvs = new List<Vector2>();
            mesh.GetUVs(0, uvs);

            ComputeBuffer colorBuffer = new ComputeBuffer(vertexCount, sizeof(float) * 4, ComputeBufferType.Default);
            ComputeBuffer uvBuffer = new ComputeBuffer(vertexCount, sizeof(float) * 2, ComputeBufferType.Structured);
            uvBuffer.SetData(uvs);

            int kernel = ComputeShader.FindKernel("SampleTextureToColor");
            ComputeShader.SetTexture(kernel, "InputTexture", texture);
            ComputeShader.SetBuffer(kernel, "UVs", uvBuffer);
            ComputeShader.SetBuffer(kernel, "OutputColor", colorBuffer);

            int threadGroupSize = Mathf.CeilToInt(vertexCount / 8f);

            ComputeShader.Dispatch(kernel, threadGroupSize, 1, 1);
            uvBuffer.Release();
            Vector4[] datas = new Vector4[vertexCount];
            colorBuffer.GetData(datas);
            colorBuffer.Release();
            return datas;
        }     

        public static void BakeData(Mesh mesh, TargetVertexAttribute targetChannel, List<Vector4[]> targetVecs, List<TargetColorChannel> colorChannels,List<bool> overrides)
        {
            int vertexCount = mesh.vertexCount;
            NativeArray<Vector4> originalVectors = new NativeArray<Vector4>(vertexCount, Allocator.Persistent);
            NativeArray<Vector4> targetVectors = new NativeArray<Vector4>(vertexCount, Allocator.Persistent);
            NativeArray<Vector4> resultVectors = new NativeArray<Vector4>(vertexCount, Allocator.Persistent);

            if (targetChannel == TargetVertexAttribute.Color)
            {
                bool hasOriginalColors = false;
                if (mesh.HasVertexAttribute(VertexAttribute.Color))
                {
                    hasOriginalColors = true;
                    var originColorVec = new Vector4[vertexCount];
                    for (int i = 0; i < vertexCount; i++)
                    {
                        originColorVec[i] = new Vector4(mesh.colors[i].r, mesh.colors[i].g, mesh.colors[i].b, mesh.colors[i].a);
                    }
                    originalVectors.CopyFrom(originColorVec);
                }
                for (int i = 0; i < colorChannels.Count; i++)
                {
                    targetVectors.CopyFrom(targetVecs[i]);

                    if (hasOriginalColors && !overrides[i])
                    {
                        CombineVector4ChannelJob combineVectorChannelJob = new CombineVector4ChannelJob((int)colorChannels[i], originalVectors, targetVectors, resultVectors);
                        combineVectorChannelJob.Schedule(vertexCount, 100).Complete();
                    }
                    else
                    {
                        CombineVectorChannelWithoutOriginalVectorsJob combineColorChannelJob = new CombineVectorChannelWithoutOriginalVectorsJob((int)colorChannels[i], targetVectors, resultVectors);
                        combineColorChannelJob.Schedule(vertexCount, 100).Complete();
                    }
                    if (i != colorChannels.Count - 1)
                    {
                        originalVectors.CopyFrom(resultVectors);
                        hasOriginalColors = true;
                    }
                }

                var result = new Vector4[vertexCount];
                resultVectors.CopyTo(result);
                Color[] colors = new Color[vertexCount];
                for (int i = 0; i < vertexCount; i++)
                {
                    colors[i] = new Color(result[i].x, result[i].y, result[i].z, result[i].w);
                }
                mesh.colors= colors;
                originalVectors.Dispose();
                targetVectors.Dispose();
                resultVectors.Dispose();
            }
            else
            {
                VertexAttribute vertexAttribute = (VertexAttribute)((int)targetChannel + 4);

                for (int i = 0; i < colorChannels.Count; i++)
                {
                    targetVectors.CopyFrom(targetVecs[i]);
                    if (i == 0)
                    {
                        if (mesh.HasVertexAttribute(vertexAttribute) && overrides[i])
                        {
                            try
                            {
                                List<Vector4> vec4uvs = new List<Vector4>();
                                mesh.GetUVs((int)targetChannel, vec4uvs);
                                originalVectors.CopyFrom(vec4uvs.ToArray());

                                CombineVector4ChannelJob combineVectorChannelJob = new CombineVector4ChannelJob((int)colorChannels[i], originalVectors, targetVectors, resultVectors);

                                combineVectorChannelJob.Schedule(vertexCount, 100).Complete();
                            }
                            catch (System.ArgumentException)
                            {
                                try
                                {
                                    NativeArray<Vector3> originalVectorsV3 = new NativeArray<Vector3>(vertexCount, Allocator.Persistent);

                                    List<Vector3> vec3uvs = new List<Vector3>();
                                    mesh.GetUVs((int)targetChannel, vec3uvs);
                                    originalVectorsV3.CopyFrom(vec3uvs.ToArray());


                                    CombineVector3ChannelJob combineVectorChannelJob = new CombineVector3ChannelJob((int)colorChannels[i], originalVectorsV3, targetVectors, resultVectors);

                                    combineVectorChannelJob.Schedule(vertexCount, 100).Complete();

                                    originalVectorsV3.Dispose();

                                }
                                catch (System.ArgumentException)
                                {
                                    NativeArray<Vector2> originalVectorsV2 = new NativeArray<Vector2>(vertexCount, Allocator.Persistent);

                                    List<Vector2> vec2uvs = new List<Vector2>();
                                    mesh.GetUVs((int)targetChannel, vec2uvs);

                                    originalVectorsV2.CopyFrom(vec2uvs.ToArray());

                                    CombineVector2ChannelJob combineVectorChannelJob = new CombineVector2ChannelJob((int)colorChannels[i], originalVectorsV2, targetVectors, resultVectors);

                                    combineVectorChannelJob.Schedule(vertexCount, 100).Complete();

                                    originalVectorsV2.Dispose();
                                }
                            }

                        }
                        else
                        {
                            CombineVectorChannelWithoutOriginalVectorsJob combineColorChannelJob = new CombineVectorChannelWithoutOriginalVectorsJob((int)colorChannels[i], targetVectors, resultVectors);
                            combineColorChannelJob.Schedule(vertexCount, 100).Complete();
                        }
                    }
                    else
                    {

                        CombineVector4ChannelJob combineVectorChannelJob = new CombineVector4ChannelJob((int)colorChannels[i], originalVectors, targetVectors, resultVectors);
                        combineVectorChannelJob.Schedule(vertexCount, 100).Complete();
                    }

                    if (i != colorChannels.Count - 1)
                    {
                        originalVectors.CopyFrom(resultVectors);
                    }
                }

                var result = new Vector4[vertexCount];
                resultVectors.CopyTo(result);
             
                mesh.SetUVs((int)targetChannel, result);
                originalVectors.Dispose();
                targetVectors.Dispose();
                resultVectors.Dispose();
            }

        }

        #region Jobs
        struct CombineVector4ChannelJob : IJobParallelFor
        {
            [ReadOnly] public int colorChannel;
            [ReadOnly] public NativeArray<Vector4> originlVectors;
            [ReadOnly] public NativeArray<Vector4> targetVectors;
            [WriteOnly] public NativeArray<Vector4> resultVectors;

            public CombineVector4ChannelJob(int colorChannel,
                NativeArray<Vector4> originlVectors,
                NativeArray<Vector4> targetVectors,
                NativeArray<Vector4> resultVectors)
            {
                this.colorChannel = colorChannel;
                this.originlVectors = originlVectors;
                this.targetVectors = targetVectors;
                this.resultVectors = resultVectors;
            }

            void IJobParallelFor.Execute(int index)
            {
                Vector4 vec = originlVectors[index];
                switch (colorChannel)
                {
                    case (int)(TargetColorChannel.R | TargetColorChannel.G | TargetColorChannel.B | TargetColorChannel.A):
                        vec.x = targetVectors[index].x;
                        vec.y = targetVectors[index].y;
                        vec.z = targetVectors[index].z;
                        vec.w = targetVectors[index].w;
                        break;
                    case (int)(TargetColorChannel.R | TargetColorChannel.G | TargetColorChannel.B):
                        vec.x = targetVectors[index].x;
                        vec.y = targetVectors[index].y;
                        vec.z = targetVectors[index].z;
                        break;
                    case (int)(TargetColorChannel.R | TargetColorChannel.G):
                        vec.x = targetVectors[index].x;
                        vec.y = targetVectors[index].y;
                        break;
                    case (int)(TargetColorChannel.R):
                        vec.x = targetVectors[index].x;
                        break;
                    case (int)(TargetColorChannel.G):
                        vec.y = targetVectors[index].y;
                        break;
                    case (int)(TargetColorChannel.B):
                        vec.z = targetVectors[index].z;
                        break;
                    case (int)(TargetColorChannel.A):
                        vec.w = targetVectors[index].w;
                        break;
                }

                resultVectors[index] = vec;
            }
        }

        struct CombineVector3ChannelJob : IJobParallelFor
        {
            [ReadOnly] public int colorChannel;
            [ReadOnly] public NativeArray<Vector3> originlVectors;
            [ReadOnly] public NativeArray<Vector4> targetVectors;
            [WriteOnly] public NativeArray<Vector4> resultVectors;

            public CombineVector3ChannelJob(int colorChannel, 
                NativeArray<Vector3> originlVectors,
                NativeArray<Vector4> targetVectors,
                NativeArray<Vector4> resultVectors)
            {
                this.colorChannel = colorChannel;
                this.originlVectors = originlVectors;
                this.targetVectors = targetVectors;
                this.resultVectors = resultVectors;
            }

            void IJobParallelFor.Execute(int index)
            {
                Vector4 vec = originlVectors[index];
                switch (colorChannel)
                {
                    case (int)(TargetColorChannel.R | TargetColorChannel.G | TargetColorChannel.B | TargetColorChannel.A):
                        vec.x = targetVectors[index].x;
                        vec.y = targetVectors[index].y;
                        vec.z = targetVectors[index].z;
                        vec.w = targetVectors[index].w;
                        break;
                    case (int)(TargetColorChannel.R | TargetColorChannel.G | TargetColorChannel.B):
                        vec.x = targetVectors[index].x;
                        vec.y = targetVectors[index].y;
                        vec.z = targetVectors[index].z;
                        break;
                    case (int)(TargetColorChannel.R | TargetColorChannel.G):
                        vec.x = targetVectors[index].x;
                        vec.y = targetVectors[index].y;
                        break;
                    case (int)(TargetColorChannel.R):
                        vec.x = targetVectors[index].x;
                        break;
                    case (int)(TargetColorChannel.G):
                        vec.y = targetVectors[index].y;
                        break;
                    case (int)(TargetColorChannel.B):
                        vec.z = targetVectors[index].z;
                        break;
                    case (int)(TargetColorChannel.A):
                        vec.w = targetVectors[index].w;
                        break;
                }

                resultVectors[index] = vec;
            }
        }

        struct CombineVector2ChannelJob : IJobParallelFor
        {
            [ReadOnly] public int colorChannel;
            [ReadOnly] public NativeArray<Vector2> originlVectors;
            [ReadOnly] public NativeArray<Vector4> targetVectors;
            [WriteOnly] public NativeArray<Vector4> resultVectors;

            public CombineVector2ChannelJob(int colorChannel, 
                NativeArray<Vector2> originlVectors,
                NativeArray<Vector4> targetVectors,
                NativeArray<Vector4> resultVectors)
            {
                this.colorChannel = colorChannel;
                this.originlVectors = originlVectors;
                this.targetVectors = targetVectors;
                this.resultVectors = resultVectors;
            }

            void IJobParallelFor.Execute(int index)
            {
                Vector4 vec = originlVectors[index];
                switch (colorChannel)
                {
                    case (int)(TargetColorChannel.R | TargetColorChannel.G | TargetColorChannel.B | TargetColorChannel.A):
                        vec.x = targetVectors[index].x;
                        vec.y = targetVectors[index].y;
                        vec.z = targetVectors[index].z;
                        vec.w = targetVectors[index].w;
                        break;
                    case (int)(TargetColorChannel.R | TargetColorChannel.G | TargetColorChannel.B):
                        vec.x = targetVectors[index].x;
                        vec.y = targetVectors[index].y;
                        vec.z = targetVectors[index].z;
                        break;
                    case (int)(TargetColorChannel.R | TargetColorChannel.G):
                        vec.x = targetVectors[index].x;
                        vec.y = targetVectors[index].y;
                        break;
                    case (int)(TargetColorChannel.R):
                        vec.x = targetVectors[index].x;
                        break;
                    case (int)(TargetColorChannel.G):
                        vec.y = targetVectors[index].y;
                        break;
                    case (int)(TargetColorChannel.B):
                        vec.z = targetVectors[index].z;
                        break;
                    case (int)(TargetColorChannel.A):
                        vec.w = targetVectors[index].w;
                        break;
                }

                resultVectors[index] = vec;
            }
        }

        struct CombineVectorChannelWithoutOriginalVectorsJob : IJobParallelFor
        {
            [ReadOnly] public int colorChannel;
            [ReadOnly] public NativeArray<Vector4> targetVectors;
            [WriteOnly] public NativeArray<Vector4> resultVectors;

            public CombineVectorChannelWithoutOriginalVectorsJob(
                int colorChannel,
                 NativeArray<Vector4> targetVectors,
                NativeArray<Vector4> resultVectors)
            {
                this.colorChannel = colorChannel;
                this.targetVectors = targetVectors;
                this.resultVectors = resultVectors;
            }

            void IJobParallelFor.Execute(int index)
            {
                Vector4 vec = Vector4.zero;

                switch (colorChannel)
                {
                    case (int)(TargetColorChannel.R | TargetColorChannel.G | TargetColorChannel.B | TargetColorChannel.A):
                        vec.x = targetVectors[index].x;
                        vec.y = targetVectors[index].y;
                        vec.z = targetVectors[index].z;
                        vec.w = targetVectors[index].w;
                        break;
                    case (int)(TargetColorChannel.R | TargetColorChannel.G | TargetColorChannel.B):
                        vec.x = targetVectors[index].x;
                        vec.y = targetVectors[index].y;
                        vec.z = targetVectors[index].z;
                        break;
                    case (int)(TargetColorChannel.R | TargetColorChannel.G):
                        vec.x = targetVectors[index].x;
                        vec.y = targetVectors[index].y;
                        break;
                    case (int)(TargetColorChannel.R):
                        vec.x = targetVectors[index].x;
                        break;
                    case (int)(TargetColorChannel.G):
                        vec.y = targetVectors[index].y;
                        break;
                    case (int)(TargetColorChannel.B):
                        vec.z = targetVectors[index].z;
                        break;
                    case (int)(TargetColorChannel.A):
                        vec.w = targetVectors[index].w;
                        break;
                }

                resultVectors[index] = vec;
            }
        }
        #endregion
    }
}