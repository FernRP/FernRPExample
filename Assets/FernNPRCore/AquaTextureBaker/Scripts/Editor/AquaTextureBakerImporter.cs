using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace AquaSys.TextureBaker.Editor
{
    public class AquaTextureBakerImporter : AssetPostprocessor
    {
        void OnPostprocessModel(GameObject root)
        {
            if (AquaTextureBakerWindow.Instance == null)
                return;
            if (AquaTextureBakerWindow.CheckLabel(assetImporter))
            {
                if (AquaTextureBakerWindow.toClearLabel)
                {
                    AquaTextureBakerWindow.RemoveLabel(assetImporter);
                }
                else
                {
                    var mesheDatas = Utils.GetMesh(root);

                    foreach (var pipeline in AquaTextureBakerWindow.Instance.TextureBlockPipline)
                    {
                        foreach (var mesheData in mesheDatas)
                        {
                            var buffers = new List<Vector4[]>();
                            var targetColorChannels = new List<TargetColorChannel>();
                            var overrides= new List<bool>();
                            for (int i = 0; i < pipeline.Value.Count; i++)
                            {
                                var texblock = pipeline.Value[i];
                                Texture tex = null;
                                Vector4[] buffer = null;
                                switch (texblock.DataSource)
                                {
                                    case DataSource.MaterialTexture:
                                        tex = mesheData.Value.GetTexture(texblock.TextureName);
                                        buffer = AquaTextureBaker.SampleTexture(mesheData.Key, tex);
                                        break;
                                    case DataSource.CustomTexture:
                                        tex = texblock.CustomTexture;
                                        buffer = AquaTextureBaker.SampleTexture(mesheData.Key, tex);
                                        break;
                                    case DataSource.SmoothedNormal:
                                        if(texblock.SourceColorChannel== SourceColorChannel.RG)
                                        {
                                            buffer = AquaTextureBaker.SmoothedNormalV2(mesheData.Key);
                                        }
                                        else
                                        {
                                            buffer = AquaTextureBaker.SmoothedNormal(mesheData.Key);
                                        }
                                        break;
                                }
                                buffers.Add(buffer);
                                targetColorChannels.Add(texblock.TargetColorChannel);
                                overrides.Add(texblock.DataWriteType == ChannelDataWriteType.Override);
                            }
                            AquaTextureBaker.BakeData(mesheData.Key,
                                pipeline.Key,
                                buffers,
                                targetColorChannels,
                                overrides);
                        }
                       
                    }

                }

            }

            else
            {
                if (AquaTextureBakerWindow.toAddLabel)
                    AquaTextureBakerWindow.AddLabel(assetImporter);
            }

        }
    }
}