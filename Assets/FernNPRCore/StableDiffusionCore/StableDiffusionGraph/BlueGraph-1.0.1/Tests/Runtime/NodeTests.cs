using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BlueGraph.Tests
{
    /// <summary>
    /// Test suite that focuses on AbstractNode methods
    /// </summary>
    public class NodeTests
    {
        [Test]
        public void CanAddPorts()
        {
            var node = new TestNodeA(); 
            var port1 = new OutputPort<float> { Name = "Test 1" };
            var port2 = new OutputPort<float> { Name = "Test 2" };

            node.AddPort(port1);
            node.AddPort(port2);
            
            // Test Node A comes with 2 ports by default
            Assert.AreEqual(4, node.Ports.Count);
            Assert.AreSame(port1, node.GetPort("Test 1"));
            Assert.AreSame(port2, node.GetPort("Test 2"));
        }
        
        [Test]
        public void CanRemovePorts()
        {
            var node = new TestNodeA();
            var port1 = new OutputPort<float> { Name = "Test 1" };
            var port2 = new OutputPort<float> { Name = "Test 2" };

            node.AddPort(port1);
            node.AddPort(port2);

            node.RemovePort(port1);
            
            // Test Node A comes with 2 ports by default
            Assert.AreEqual(3, node.Ports.Count);
            Assert.AreSame(port2, node.GetPort("Test 2"));
        }
        
        /// <summary>
        /// Ensure that calling RemovePort() will also remove edges to that port
        /// </summary>
        [Test]
        public void RemovingPortsAlsoRemovesEdges()
        {
            var graph = ScriptableObject.CreateInstance<TestGraph>();
            
            var node1 = new TestNodeA();
            var node2 = new TestNodeA();
            var node3 = new TestNodeA();
            
            graph.AddNode(node1);
            graph.AddNode(node2);
            graph.AddNode(node3);

            var portToRemove = node2.GetPort("Input");

            // Edge that should be deleted
            graph.AddEdge(
                node1.GetPort("Output"),
                node2.GetPort("Input")
            );
            
            // Unaffected edge
            graph.AddEdge(
                node2.GetPort("Output"), 
                node3.GetPort("Input")
            );
            
            node2.RemovePort(portToRemove);

            Assert.AreEqual(0, node1.GetPort("Output").ConnectionCount);
            Assert.AreEqual(1, node2.GetPort("Output").ConnectionCount);
            Assert.AreEqual(1, node3.GetPort("Input").ConnectionCount);
        }
        
        [Test]
        public void CanGetPorts()
        {
            var node = new TestNodeA(); 
            node.AddPort(new OutputPort<float> { Name = "Test 1" });
            node.AddPort(new OutputPort<float> { Name = "Test 2" });
            
            var actual = node.GetPort("Test 2");
            
            Assert.AreSame(node, actual.Node);
            Assert.AreSame("Test 2", actual.Name);
        }
        
        [Test]
        public void AddPortThrowsOnDuplicateName()
        {
            var node = new TestNodeA();
            node.AddPort(new OutputPort<float> { Name = "Test" });
            
            Assert.Throws<ArgumentException>(
                () => node.AddPort(new OutputPort<float> { Name = "Test" })
            );
        }
        
        [Test]
        public void ReturnsNullOnInvalidPortName()
        {
            var node = new TestNodeA();
            
            var actual = node.GetPort("Bad Port");

            Assert.IsNull(actual);
        }
        
        [Test]
        public void GetOutputPortThrowsOnInputPort()
        {
            var node = new TestNodeA();

            Assert.Throws<ArgumentException>(
                () => node.GetOutputValue<int>("Input")
            );
        }

        [Test]
        public void GetOutputPortThrowsOnUnknownPort()
        {
            var node = new TestNodeA();

            Assert.Throws<ArgumentException>(
                () => node.GetOutputValue<int>("Bad Port")
            );
        }

        [Test]
        public void GetInputValueDefaultsWithoutConnections()
        {
            var node = new TestNodeA();
            var actual = node.GetInputValue("Input", 2);

            Assert.AreEqual(2, actual);
        }
        
        [Test]
        public void GetInputValueReadsInputConnection()
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
            
            var actual = node2.GetInputValue("Input", 2);
            var expected = 5 + 1; // node1's OnRequestValue() result

            Assert.AreEqual(expected, actual);
        }
        
        [Test]
        public void GetInputValueAggregatesMultipleOutputs()
        {
            var graph = ScriptableObject.CreateInstance<TestGraph>();
            var node1 = new TestNodeA { aValue1 = 1 };
            var node2 = new TestNodeA { aValue1 = 2 };
            var node3 = new TestNodeA();
            
            graph.AddNode(node1);
            graph.AddNode(node2);
            graph.AddNode(node3);

            graph.AddEdge(
                node1.GetPort("Output"),
                node3.GetPort("Input")
            );
            
            graph.AddEdge(
                node2.GetPort("Output"),
                node3.GetPort("Input")
            );

            var expected = new int[] { 2, 3 };
            var actual = node3.GetInputValues<int>("Input").ToArray();

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void GetOutputValueDefaultsToInstanceField()
        {
            var node = new TestNodeA();
            var actual = node.GetOutputValue<int>("Output");

            Assert.AreEqual(6, actual);
        }

        [Test]
        public void GetOutputValueReadsInputPortValues()
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
            
            var actual = node2.GetOutputValue<int>("Output");

            // node1.OnRequestValue() + node2.OnRequestValue()
            var expected = (5 + 1) + 1; 

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void GetOutputValueCastsValueType()
        {
            var node = new TestNodeA();
            var actual = node.GetOutputValue<float>("Output");

            Assert.AreEqual(6f, actual);
        }

        [Test]
        public void GetOutputValueReturnsReferenceType()
        {
            var node = new TypeTestNode();
            var actual = node.GetOutputValue<TestClass>("classval");

            Assert.AreSame(node.testClassValue, actual);
        }

        [Test]
        public void GetOutputValueCastsReferenceTypeToInterface()
        {
            var node = new TypeTestNode();
            var actual = node.GetOutputValue<ITestClass>("classval");

            Assert.IsInstanceOf(typeof(ITestClass), actual);
        }

        [Test]
        public void CannotAddDuplicateEdges()
        {
            var graph = ScriptableObject.CreateInstance<TestGraph>();
            var node1 = new TestNodeA();
            var node2 = new TestNodeA();
            var output = node1.GetPort("Output");
            var input = node2.GetPort("Input");
            
            graph.AddNode(node1);
            graph.AddNode(node2);

            graph.AddEdge(output, input);
            
            // Add duplicate
            graph.AddEdge(output, input);

            // Make sure there's only one edge between the nodes
            Assert.AreEqual(1, output.ConnectionCount);
            Assert.AreEqual(1, input.ConnectionCount);
        }
    }
}
