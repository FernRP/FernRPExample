using System;
using System.Collections;
using BlueGraph;
using BlueGraphSamples;
using UnityEngine;

namespace StableDiffusionGraph.SDGraph.Nodes
{
    [Node(Path = "SD Standard")]
    [Tags("SD Node")]
    public class SDPreview : Node
    {
        [Input("In Image", Editable = false)] public Texture2D Image;
        [Output("Out Image")] public Texture2D OutImage;
        public Action<Texture2D> OnUpdateAction;

        public override void OnValidate()
        {
            base.OnValidate();
            Image = GetInputValue("In Image", this.Image);
            OutImage = Image;
            OnUpdateAction?.Invoke(Image);
        }
        
        public override object OnRequestValue(Port port)
        {
            Image = GetInputValue("In Image", this.Image);
            OutImage = Image;
            return OutImage;
        }
    }
}
