using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace BlueGraph.Tests
{
    public class UndoRedoTests
    {
        [Test]
        public void CanUndoAddNode()
        {
            var graph = ScriptableObject.CreateInstance<TestGraph>();
            var node1 = new TestNodeA();
            var node2 = new TestNodeA();
            
            graph.AddNode(node1);
            
            Undo.RegisterCompleteObjectUndo(graph, "Add Node 2");
            
            graph.AddNode(node2);

            Undo.PerformUndo();
            
            Assert.AreEqual(1, graph.Nodes.Count);

            // Not the same instance anymore due to undo - but the same data.
            Assert.AreEqual(graph.Nodes.ElementAt(0).ID, node1.ID);
        }
        
        [Test]
        public void CanUndoAddEdge()
        {
            var graph = ScriptableObject.CreateInstance<TestGraph>();
            var node1 = new TestNodeA();
            var node2 = new TestNodeA();
            
            graph.AddNode(node1);
            graph.AddNode(node2);
            
            Undo.RegisterCompleteObjectUndo(graph, "Add Edge 1 -> 2");
            
            graph.AddEdge(
                node1.GetPort("Output"),
                node2.GetPort("Input")
            );
            
            Undo.PerformUndo();
            
            Assert.AreEqual(2, graph.Nodes.Count);
            Assert.AreEqual(graph.Nodes.ElementAt(0).ID, node1.ID);
            Assert.AreEqual(graph.Nodes.ElementAt(1).ID, node2.ID);

            Assert.AreEqual(0, graph.Nodes.ElementAt(0).GetPort("Output").ConnectionCount);
            Assert.AreEqual(0, graph.Nodes.ElementAt(1).GetPort("Input").ConnectionCount);
        }

        /// <summary>
        /// Make sure an undo operation after adding a node/edge does not destroy
        /// unrelated connections and cleanly resets connections between nodes 
        /// to their previous state (i.e. no dangling edges)
        /// </summary>
        [Test] 
        public void UndoAddNodeDoesNotAffectUnrelatedConnections()
        {
            var graph = ScriptableObject.CreateInstance<TestGraph>();
            var node1 = new TestNodeA();
            var node2 = new TestNodeA();
            var node3 = new TestNodeA();
            
            graph.AddNode(node1);
            graph.AddNode(node2);
            graph.AddEdge(
                node1.GetPort("Output"), 
                node2.GetPort("Input")
            );
            
            Undo.RegisterCompleteObjectUndo(graph, "Add Node 3 and Edge 2 -> 3");
            
            graph.AddNode(node3);
            
            graph.AddEdge(
                node2.GetPort("Output"),
                node3.GetPort("Input")
            );
            
            Undo.PerformUndo();
            
            // Make sure an undo operation did not destroy unrelated connections and
            // cleanly reset connections to their previous state (no dangling edges)
            var outputs = graph.Nodes.ElementAt(0).GetPort("Output").ConnectedPorts;
            var inputs = graph.Nodes.ElementAt(1).GetPort("Input").ConnectedPorts;
            
            Assert.AreEqual(2, graph.Nodes.Count);
            Assert.AreEqual(1, outputs.Count());
            Assert.AreEqual(1, inputs.Count());
            
            Assert.AreSame(graph.Nodes.ElementAt(0), inputs.First().Node);
            Assert.AreSame(graph.Nodes.ElementAt(1), outputs.First().Node);
        }
    }
}
