using System.Collections.Generic;
using BlueGraph;
using BlueGraph.Editor;
using StableDiffusionGraph.SDGraph.Nodes;
using UnityEngine;
using UnityEngine.UIElements;

namespace StableDiffusionGraph.SDGraph.Editor
{
    [CustomNodeView(typeof(SDSamplerNode))]
    public class SDSamplerNodeView : NodeView
    {
        private LongField longField;
        protected override void OnInitialize()
        {
            styleSheets.Add(Resources.Load<StyleSheet>("SDGraphRes/SDNodeView"));
            AddToClassList("sdNodeView");
            PortView inView = GetInputPort("SDFlowIn");
            PortView outView = GetOutputPort("SDFlowOut");
            if (inView != null) inView.AddToClassList("SDFlowInPortView");
            if (outView != null) outView.AddToClassList("SDFlowOutPortView");
            
            var samplerNode = Target as SDSamplerNode;
            if(samplerNode == null) return;

            samplerNode.OnUpdateSeedField = null;
            samplerNode.OnUpdateSeedField += OnUpadteSeed;

            List<string> samplerMethodList = new List<string>();
            samplerMethodList.AddRange(SDDataHandle.samplers);

            var samplerMethodDropdown = new DropdownField(samplerMethodList, 0);
            samplerMethodDropdown.RegisterValueChangedCallback(e =>
            {
                samplerNode.SamplerMethod = e.newValue;
            });
            samplerMethodDropdown.style.flexGrow = 1;
            samplerMethodDropdown.style.maxWidth = 140;
            
            var label = new Label("Method");
            label.style.width = StyleKeyword.Auto;
            label.style.marginRight = 5;
            
            var containerSampleMethod = new VisualElement();
            containerSampleMethod.style.flexDirection = FlexDirection.Row;
            containerSampleMethod.style.alignItems = Align.Center;
            containerSampleMethod.Add(label);
            containerSampleMethod.Add(samplerMethodDropdown);
            
            extensionContainer.Add(containerSampleMethod);
            
            // seed
            var labelSeed = new Label("Seed    ");
            labelSeed.style.width = StyleKeyword.Auto;
            labelSeed.style.marginRight = 5;
            
            longField = new LongField();
            longField.RegisterValueChangedCallback((e) =>
            {
                samplerNode.Seed = e.newValue;
            });
            
            longField.style.flexGrow = 1;
            longField.style.maxWidth = 140;
            var containerSeed = new VisualElement();
            containerSeed.style.flexDirection = FlexDirection.Row;
            containerSeed.style.alignItems = Align.Center;
            containerSeed.Add(labelSeed);
            containerSeed.Add(longField);
            extensionContainer.Add(containerSeed);
            
            RefreshExpandedState();
        }

        private void OnUpadteSeed(long seed)
        {
            var samplerNode = Target as SDSamplerNode;
            if(samplerNode == null) return;
            samplerNode.Seed = seed;
            longField.value = seed;
        }
    }
}
