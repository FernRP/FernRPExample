using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace BlueGraph.Tests
{
    /// <summary>
    /// Tests for manipulating nodes and edges on a graph 
    /// </summary>
    public class GraphTests
    {
        [Test]
        public void CanAddNodes()
        {
            var graph = ScriptableObject.CreateInstance<TestGraph>();
            
            graph.AddNode(new TestNodeA());
            graph.AddNode(new TestNodeA());
            
            Assert.AreEqual(2, graph.Nodes.Count);
        }
        
        [Test]
        public void CanFindNodeById()
        {
            var graph = ScriptableObject.CreateInstance<TestGraph>();
            
            var node1 = new TestNodeA();
            var node2 = new TestNodeA();
            var expected = new TestNodeA();
            var node3 = new TestNodeA();

            graph.AddNode(node1);
            graph.AddNode(node2);
            graph.AddNode(expected);
            graph.AddNode(node3);
            
            var actual = graph.GetNodeById(expected.ID);
            
            Assert.AreSame(expected, actual);
        }
        
        [Test]
        public void CanFindNodeByType()
        {
            var graph = ScriptableObject.CreateInstance<TestGraph>();
            
            var node1 = new TestNodeA();
            var expected = new TestNodeB();
            var node2 = new TestNodeB();

            graph.AddNode(node1);
            graph.AddNode(expected);
            graph.AddNode(node2);
            
            var actual = graph.GetNode<TestNodeB>();

            Assert.AreSame(expected, actual);
        }

        [Test]
        public void CanFindNodeByBaseType()
        {
            var graph = ScriptableObject.CreateInstance<TestGraph>();
            
            var node1 = new TestNodeB();
            var expected = new InheritedTestNodeA();

            graph.AddNode(node1);
            graph.AddNode(expected);
            
            // Search using a base type (TestNodeA)
            var actual = graph.GetNode<TestNodeA>();

            Assert.AreSame(expected, actual);
        }
        
        [Test]
        public void CanFindMultipleNodesByType()
        {
            var graph = ScriptableObject.CreateInstance<TestGraph>();
            
            graph.AddNode(new TestNodeA());
            graph.AddNode(new TestNodeB());
            graph.AddNode(new TestNodeA());
            graph.AddNode(new TestNodeB());
            
            TestNodeA[] actual = graph.GetNodes<TestNodeA>().ToArray();

            Assert.AreEqual(2, actual.Length);

            Assert.IsInstanceOf<TestNodeA>(actual[0]);
            Assert.IsInstanceOf<TestNodeA>(actual[1]);
        }

        [Test]
        public void ReturnsNullOnInvalidNodeId()
        {
            var graph = ScriptableObject.CreateInstance<TestGraph>();
            
            var actual = graph.GetNodeById("BAD ID");

            Assert.IsNull(actual);
        }
        
        [Test]
        public void CanAddEdges()
        {
            var graph = ScriptableObject.CreateInstance<TestGraph>();
            
            var node1 = new TestNodeA();
            var node2 = new TestNodeA();
            
            graph.AddNode(node1);
            graph.AddNode(node2);
            graph.AddEdge(
                node1.GetPort("Output"), 
                node2.GetPort("Input")
            );
            
            var outputsFromNode1 = node1.GetPort("Output").ConnectedPorts;
            var inputsToNode2 = node2.GetPort("Input").ConnectedPorts;

            Assert.AreEqual(1, outputsFromNode1.Count());
            Assert.AreEqual(1, inputsToNode2.Count());
            
            Assert.AreSame(node2, outputsFromNode1.First().Node);
            Assert.AreSame(node1, inputsToNode2.First().Node);
        }

        [Test]
        public void CanRemoveNode()
        {
            var graph = ScriptableObject.CreateInstance<TestGraph>();
            
            var node1 = new TestNodeA();
            var nodeToRemove = new TestNodeA();
            var node2 = new TestNodeA();
            
            graph.AddNode(node1);
            graph.AddNode(nodeToRemove);
            graph.AddNode(node2);
            
            graph.RemoveNode(nodeToRemove);
            
            Assert.AreEqual(2, graph.Nodes.Count);
            Assert.IsNull(graph.GetNodeById(nodeToRemove.ID));
        }
        
        // [Test]
        public void OnDisableExecutes()
        {
            var graph = ScriptableObject.CreateInstance<TestGraph>();
            
            var nodeToRemove = new TestNodeA();
            
            // TODO: No mock support. How do I test for this?

            graph.AddNode(nodeToRemove);
            graph.RemoveNode(nodeToRemove);
        }

        /// <summary>
        /// Ensure that edges to a removed node are also removed
        /// at the same time.
        /// </summary>
        [Test]
        public void RemovingNodeAlsoRemovesEdges()
        {
            var graph = ScriptableObject.CreateInstance<TestGraph>();
            
            var node1 = new TestNodeA();
            var nodeToRemove = new TestNodeA();
            var node2 = new TestNodeA();
            
            graph.AddNode(node1);
            graph.AddNode(nodeToRemove);
            graph.AddNode(node2);

            graph.AddEdge(
                node1.GetPort("Output"),
                nodeToRemove.GetPort("Input")
            );
            
            graph.AddEdge(
                node2.GetPort("Output"), 
                nodeToRemove.GetPort("Input")
            );
            
            graph.RemoveNode(nodeToRemove);

            Assert.AreEqual(0, node1.GetPort("Output").ConnectionCount);
            Assert.AreEqual(0, node2.GetPort("Output").ConnectionCount);
            
            Assert.AreEqual(0, nodeToRemove.GetPort("Input").ConnectionCount);
        }

        [Test]
        public void CanRemoveEdge()
        {
            var graph = ScriptableObject.CreateInstance<TestGraph>();
            
            var node1 = new TestNodeA();
            var node2 = new TestNodeA();
            var node3 = new TestNodeA();
            
            graph.AddNode(node1);
            graph.AddNode(node2);
            graph.AddNode(node3);

            graph.AddEdge(
                node1.GetPort("Output"), 
                node2.GetPort("Input")
            );
            
            graph.AddEdge(
                node1.GetPort("Output"), 
                node3.GetPort("Input")
            );
            
            graph.RemoveEdge(
                node1.GetPort("Output"), 
                node3.GetPort("Input")
            );

            Assert.AreEqual(1, node1.GetPort("Output").ConnectionCount);
            Assert.AreEqual(0, node3.GetPort("Input").ConnectionCount);
        }
    }
}
