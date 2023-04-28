using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace FernGraph.Editor
{
    /// <summary>
    /// Converts graph data into a format that can be stored on the clipboard for copy/paste
    /// </summary>
    public class CopyPasteGraph : ScriptableObject
    {
        public List<Node> Nodes 
        { 
            get { return nodes; }
            set { nodes = value; }
        }

        [SerializeReference]
        private List<Node> nodes = new List<Node>();
        
        public List<Comment> Comments
        {
            get { return comments; }
        }

        [SerializeField]
        private List<Comment> comments = new List<Comment>();
        
        /// <summary>
        /// Serialize a set of graph elements (nodes, comments, etc) into a string.
        /// </summary>
        public static string Serialize(IEnumerable<GraphElement> elements)
        {
            var graph = CreateInstance<CopyPasteGraph>();

            foreach (var element in elements)
            {
                if (element is NodeView node)
                {
                    graph.Nodes.Add(node.Target);
                }
                else if (element is CommentView comment)
                {
                    graph.comments.Add(comment.Target);
                }
            }

            string json = JsonUtility.ToJson(graph, true);
            DestroyImmediate(graph);

            return json;
        }

        /// <summary>
        /// Test if a string on the clipboard can be deserialized into a CopyPasteGraph
        /// </summary>
        public static bool CanDeserialize(string data)
        {
            try
            {
                var graph = CreateInstance<CopyPasteGraph>();
                JsonUtility.FromJsonOverwrite(data, graph);
                DestroyImmediate(graph);

                return true;
            } 
            catch 
            { 
                // noop
            }
        
            return false;
        }

        /// <summary>
        /// Deserialize a string back into a CopyPasteGraph.
        /// 
        /// If <c>includeTags</c> are empty, no filtering will be done. Otherwise,
        /// only nodes with an intersection to one or more tags will be kept.
        /// </summary>
        public static CopyPasteGraph Deserialize(string data, IEnumerable<string> includeTags)
        {
            var graph = CreateInstance<CopyPasteGraph>();
            JsonUtility.FromJsonOverwrite(data, graph);
            
            // Remove nodes that aren't on the allow list for tags
            var allowedAllNodes = true;
            if (includeTags.Count() > 0)
            {
                graph.Nodes = graph.Nodes.FindAll((node) => {
                    var reflectedNode = NodeReflection.GetNodeType(node.GetType());
                    var allowed = includeTags.Intersect(reflectedNode.Tags).Count() > 0;
                    allowedAllNodes = allowedAllNodes && allowed;
                    
                    return allowed;
                });
            }

            // If we're excluding any from the paste content, notify the user. 
            if (!allowedAllNodes)
            {
                Debug.LogWarning("Could not paste one or more nodes - not allowed by the target graph");
            }

            // Generate new unique IDs for each node in the list
            // in case we're copy+pasting back onto the same graph
            var idMap = new Dictionary<string, string>();
            
            foreach (var node in graph.Nodes)
            {
                var newId = Guid.NewGuid().ToString();
                idMap[node.ID] = newId;
                node.ID = newId;
            }

            // Remap connections to new node IDs and drop any connections
            // that were to nodes outside of the subset of pasted nodes
            foreach (var node in graph.Nodes)
            {
                foreach (var port in node.Ports.Values)
                {
                    var edges = new List<Connection>(port.Connections);
                    port.Connections.Clear();

                    // Only re-add connections that are in the new pasted subset
                    foreach (var edge in edges)
                    {
                        if (idMap.ContainsKey(edge.NodeID))
                        {
                            port.Connections.Add(new Connection
                            {
                                NodeID = idMap[edge.NodeID],
                                PortName = edge.PortName
                            });
                        }
                    }
                }
            }
            
            return graph;
        }
    }
}
