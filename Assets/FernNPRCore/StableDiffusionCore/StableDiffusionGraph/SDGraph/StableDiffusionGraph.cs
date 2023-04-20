using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BlueGraph;
using BlueGraph.Editor;
using BlueGraphSamples;
using StableDiffusionGraph.SDGraph.Nodes;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.Rendering;

namespace StableDiffusionGraph.SDGraph
{
    
    /// <summary>
    /// Data to be passed between ICanExecuteDialogFlow nodes
    /// </summary>
    public class SDFlowData
    {
        
    }
    
    public interface ICanExecuteSDFlow
    {
        /// <summary>
        /// Run the logic for this node as a coroutine, yielding if pending work
        /// </summary>
        IEnumerator Execute();

        /// <summary>
        /// Get the next node that should be executed in the flow
        /// </summary>
        ICanExecuteSDFlow GetNext();
    }

    public interface IUpdateNode
    {
        public void Update();
    }
    
    [CreateAssetMenu(
        menuName = "BlueGraph/AI/FernUI", 
        fileName = "New SD Graph"
    )]
    [IncludeTags("Math", "Executable", "Flow Control", "SD Events", "SD Config", "SD Node")]
    public class StableDiffusionGraph : Graph
    {
        public Dictionary<SDInpaintCapture, RTHandle> InpaintDict = new Dictionary<SDInpaintCapture, RTHandle>();

        public SDStart sdStart;
        public override string Title {
            get {
                // Show a custom title for this class
                return "SD Graph";
            }
        }
        
        /// <summary>
        /// On asset creation in the editor, initialize with some default event nodes.
        /// </summary>
        protected override void OnGraphEnable()
        {
            base.OnGraphEnable();
            InpaintDict.Clear();
            if (Nodes.Count > 0)
            {
                sdStart = GetNode<SDStart>();
                return;
            } 

            if (GetNode<SDStart>() == null)
            {
                sdStart = NodeReflection.Instantiate<SDStart>();
                AddNode(sdStart);
            }
        }

        public override void Update()
        {
            base.Update();
            var allNodes = GetNodes<Node>();
            foreach (var currentNode in allNodes)
            {
                if (currentNode is IUpdateNode node)
                {
                    node.Update();
                }
            }
        }

        public override void ExecuteGraph()
        {
            EditorCoroutineUtility.StartCoroutine(Execute(), this);
        }

        public IEnumerator Execute()
        {
            var current = GetNode<SDStart>() as ICanExecuteSDFlow;
            while (current != null)
            {
                yield return current.Execute();
                current = current.GetNext();
            }

            var allNodes = GetNodes<Node>();
            foreach (var n in allNodes)
            {
                n.OnValidate();
            }
            
            yield return null;
        }
    }
}
