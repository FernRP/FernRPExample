using UnityEngine;
using BlueGraph;

#if UNITY_EDITOR
using BlueGraph.Editor;
#endif 

namespace BlueGraphSamples
{
    /// <summary>
    /// Execution data that passes through execution ports on nodes
    /// </summary>
    public class ExecutionFlowData
    {
        // Whatever you want can go here. This data 
        // will be passed through each executed node
    }
    
    /// <summary>
    /// Example of a graph that supports forward execution of nodes
    /// through a specialized "Execution edge", similar to UE4 Blueprints.
    /// 
    /// This technique allows us to create nodes that can have more
    /// complex flow control within a graph (branching, looping, etc).
    /// 
    /// This particular example uses MonoBehaviour events to start execution
    /// from different entry point event nodes. 
    /// </summary>
    [CreateAssetMenu(
        menuName = "BlueGraph Samples/MonoBehaviourGraph", 
        fileName = "New MonoBehaviourGraph"
    )]
    [IncludeTags("Math", "Executable", "Flow Control", "MonoBehaviour Events", "MonoBehaviour Subgraph")]
    public class MonoBehaviourGraph : Graph, IExecutes
    { 
        public override string Title {
            get {
                // Show a custom title for this class
                return "MONOBEHAVIOUR";
            }
        }

        /// <summary>
        /// Cache of the onUpdate node to avoid GetNode lookups each tick
        /// </summary>
        private OnUpdate onUpdateNode;

#if UNITY_EDITOR
        /// <summary>
        /// On asset creation in the editor, initialize with some default event nodes.
        /// </summary>
        protected override void OnGraphEnable()
        {
            if (Nodes.Count > 0) return;

            if (GetNode<OnEnable>() == null)
            {
                var node = NodeReflection.Instantiate<OnEnable>();
                AddNode(node);
            }

            if (GetNode<OnUpdate>() == null)
            {
                var node = NodeReflection.Instantiate<OnUpdate>();
                node.Position = new Vector2(0, 100);
                AddNode(node);
            }
        }
#endif

        public void OnBehaviourEnable()
        {
            Execute(GetNode<OnEnable>(), new ExecutionFlowData {
                // Whatever your data looks like
            });
        }

        public void OnBehaviourDisable()
        {
            Execute(GetNode<OnDisable>(), new ExecutionFlowData {
                // Whatever your data looks like
            });
        }
        
        public void OnBehaviourStart()
        {
            onUpdateNode = GetNode<OnUpdate>();

            Execute(GetNode<OnStart>(), new ExecutionFlowData {
                // Whatever your data looks like
            });
        }

        public void OnBehaviourUpdate()
        {
            Execute(onUpdateNode, new ExecutionFlowData {
                // Whatever your data looks like
            });
        }
        
        /// <summary>
        /// Execute the graph starting from the given parent node
        /// </summary>
        public void Execute(IExecutableNode root, ExecutionFlowData data)
        {
            // Execute through the graph until we run out of nodes to execute.
            // Each node will return the next node to be executed in the path. 
            IExecutableNode next = root;
            int iterations = 0;
            while (next != null)
            {
                next = next.Execute(data);

                iterations++;
                if (iterations > 2000)
                {
                    Debug.LogError("Potential infinite loop detected. Stopping early.", this);
                    break;
                }
            }
        }
    }
}
