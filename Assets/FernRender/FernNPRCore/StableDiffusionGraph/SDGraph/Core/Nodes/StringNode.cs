using System.Collections;
using FernGraph;
using UnityEngine;

namespace FernNPRCore.StableDiffusionGraph
{
    [Node(Path = "SD Standard")]
    [Tags("SD Node")]
    public class StringNode : Node, ICanExecuteSDFlow
    {
        [Output] public string Text;
        

        public override object OnRequestValue(Port port)
        {
            return Text;
        }

        public IEnumerator Execute()
        {
            throw new System.NotImplementedException();
        }

        public ICanExecuteSDFlow GetNext()
        {
            throw new System.NotImplementedException();
        }
    }
}
