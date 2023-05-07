using System.Collections;
using FernGraph;
using UnityEngine;

namespace StableDiffusionGraph.SDGraph.Nodes
{
    [Node(Path = "Standard")]
    [Tags("SD Node")]
    public class SDPrompt : Node, ICanExecuteSDFlow
    {
        [Input] public string Positive = "";
        [Input] public string Negative = "";
        [Output] public Prompt Prompt;

        public SDPrompt()
        {
            Prompt = new Prompt();
        }

        public override object OnRequestValue(Port port)
        {
            if (Prompt == null) Prompt = new Prompt();
            Positive = GetInputValue("Positive", this.Positive);
            Negative = GetInputValue("Negative", this.Negative);
            Prompt.positive = Positive;
            Prompt.negative = Negative;
            return Prompt;
        }

        public IEnumerator Execute()
        {
            yield return null;
        }

        public ICanExecuteSDFlow GetNext()
        {
            return null;
        }
    }
}
