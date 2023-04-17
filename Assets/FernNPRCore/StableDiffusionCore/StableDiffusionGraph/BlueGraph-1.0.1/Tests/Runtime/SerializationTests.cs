using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BlueGraph.Tests
{
    public class SerializationTests
    {
        /// <summary>
        /// Test for proper polymorphic node serialization through 
        /// Unity's [SerializeReference] attribute by instantiating SOs
        /// </summary>
        [Test]
        public void CanCloneWithInstantiation()
        {
            var original = ScriptableObject.CreateInstance<TestGraph>();
            
            var node1 = new EmptyNode();
            var node2 = new EmptyNode();

            node1.AddPort(new Port { 
                Name = "Output", 
                Direction = PortDirection.Output,
                Type = typeof(float), 
            });

            node2.AddPort(new Port { 
                Name = "Input",
                Direction = PortDirection.Input,
                Type = typeof(float), 
            });
            
            original.AddNode(node1);
            original.AddNode(node2);
            original.AddEdge(
                node1.GetPort("Output"), 
                node2.GetPort("Input")
            );
            

            // ---- Clone via Instantiate ----

            var clone = Object.Instantiate(original);
            

            // ---- Check Integrity ----
            
            var cloneNode1 = clone.GetNodeById(node1.ID);
            var cloneNode2 = clone.GetNodeById(node2.ID);

            Assert.AreEqual(2, clone.Nodes.Count);
            
            // Check class deserialization
            Assert.IsInstanceOf<EmptyNode>(clone.Nodes.ElementAt(0));
            Assert.IsInstanceOf<EmptyNode>(clone.Nodes.ElementAt(1));

            Assert.AreNotSame(cloneNode1, node1);
            Assert.AreEqual(node1.ID, cloneNode1.ID);
            
            Assert.AreNotSame(cloneNode2, node2);
            Assert.AreEqual(node2.ID, cloneNode2.ID);
            
            // Check port deserialization
            Assert.IsInstanceOf<Port>(cloneNode1.GetPort("Output"));
            Assert.IsInstanceOf<Port>(cloneNode2.GetPort("Input"));
            
            // Check connections
            var outputsFromNode1 = cloneNode1.GetPort("Output").ConnectedPorts;
            var inputsToNode2 = cloneNode2.GetPort("Input").ConnectedPorts;

            Assert.AreEqual(1, outputsFromNode1.Count());
            Assert.AreEqual(1, inputsToNode2.Count());
            
            Assert.AreSame(cloneNode2, outputsFromNode1.First().Node);
            Assert.AreSame(cloneNode1, inputsToNode2.First().Node);
        }
        
        /// <summary>
        /// Test for proper polymorphic node serialization through 
        /// Unity's [SerializeReference] attribute and JSONUtility
        /// </summary>
        [Test]
        public void CanCloneWithJsonSerialize()
        {
            var original = ScriptableObject.CreateInstance<TestGraph>();
            
            var node1 = new EmptyNode();
            var node2 = new EmptyNode();

            node1.AddPort(new Port { 
                Name = "Output", 
                Direction = PortDirection.Output,
                Type = typeof(float), 
            });

            node2.AddPort(new Port { 
                Name = "Input",
                Direction = PortDirection.Input,
                Type = typeof(float), 
            });
            
            original.AddNode(node1);
            original.AddNode(node2);
            original.AddEdge(
                node1.GetPort("Output"), 
                node2.GetPort("Input")
            );
            

            // ---- Clone via JsonUtility ----

            var json = JsonUtility.ToJson(original, true);

            var clone = ScriptableObject.CreateInstance<TestGraph>();
            JsonUtility.FromJsonOverwrite(json, clone);


            // ---- Check Integrity ----
            
            var cloneNode1 = clone.GetNodeById(node1.ID);
            var cloneNode2 = clone.GetNodeById(node2.ID);

            Assert.AreEqual(2, clone.Nodes.Count);
            
            // Check class deserialization
            Assert.IsInstanceOf<EmptyNode>(clone.Nodes.ElementAt(0));
            Assert.IsInstanceOf<EmptyNode>(clone.Nodes.ElementAt(1));

            Assert.AreNotSame(cloneNode1, node1);
            Assert.AreEqual(node1.ID, cloneNode1.ID);
            
            Assert.AreNotSame(cloneNode2, node2);
            Assert.AreEqual(node2.ID, cloneNode2.ID);
            
            // Check port deserialization
            Assert.IsInstanceOf<Port>(cloneNode1.GetPort("Output"));
            Assert.IsInstanceOf<Port>(cloneNode2.GetPort("Input"));
            
            // Check connections
            var outputsFromNode1 = cloneNode1.GetPort("Output").ConnectedPorts;
            var inputsToNode2 = cloneNode2.GetPort("Input").ConnectedPorts;

            Assert.AreEqual(1, outputsFromNode1.Count());
            Assert.AreEqual(1, inputsToNode2.Count());
            
            // TODO: These are pointing to node1 and node2 because
            // the graph reference stored in AbstractNode.graph 
            // still points to the old instance when cloned,
            // thus the ports read the wrong graph when retrieving
            // connected node information.
            Assert.AreSame(cloneNode2, outputsFromNode1.First().Node);
            Assert.AreSame(cloneNode1, inputsToNode2.First().Node);
        }
    }
}
