using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BlueGraph
{
    [Serializable]
    public abstract class Node
    {
        public event Action OnValidateEvent;
        public event Action OnErrorEvent;

        [SerializeField] private string id;

        public string ID
        {
            get {
                if (id == null)
                {
                    id = Guid.NewGuid().ToString();
                }
                return id;
            }
            set { id = value; }
        }

        [SerializeField] private string name;

        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        [SerializeField] private Graph graph;

        public Graph Graph
        {
            get { return graph; }
            internal set { graph = value; }
        }

        [SerializeField] private Vector2 position;

        /// <summary>
        /// Where this node is located on the Graph in CanvasView
        /// </summary>
        public Vector2 Position
        {
            get { return position; }
            set { position = value; }
        }

        [SerializeField] private Port[] ports;
        [NonSerialized] private Dictionary<string, Port> portMap;

        /// <summary>
        /// Accessor for ports and their connections to/from this node.
        /// </summary>
        public IReadOnlyDictionary<string, Port> Ports
        {
            get {
                if (portMap == null)
                {
                    RefreshPortMap();
                }

                return portMap;
            }
        }

        [NonSerialized] private string error;

        /// <summary>
        /// Error information associated with this node
        /// </summary>
        public string Error
        {
            get
            {
                return error;
            }
            set
            {
                error = value;
                OnError();
                OnErrorEvent?.Invoke();
            }
        }

        public void Enable()
        {
            // RefreshPortDictionary();

            // Ports are enabled first to ensure they're fully loaded
            // prior to enabling the node itself, in case the node needs
            // to query port data during OnEnable.
            foreach (var port in ports)
            {
                port.OnEnable();
            }

            OnEnable();
        }

        /// <summary>
        /// Called when the Graph's ScriptableObject gets the OnEnable message
        /// or when the node is added to the graph via <see cref="Graph.AddNode(Node)" />
        /// </summary>
        public virtual void OnEnable() { }

        public void Disable()
        {
            OnDisable();
        }

        /// <summary>
        /// Called when the Graph's ScriptableObject gets the OnDisable message
        /// or when the node is removed from the graph via <see cref="Graph.RemoveNode(Node)" />
        /// </summary>
        public virtual void OnDisable() { }

        public void Validate()
        {
            // Same as Enable(), we do ports first to make sure
            // everything is ready for the node's OnValidate
            foreach (var port in ports)
            {
                port.Node = this;
                port.OnValidate();
            }

            OnValidate();
            OnValidateEvent?.Invoke();
        }

        /// <summary>
        /// Called in the editor when the node or graph is revalidated.
        /// </summary>
        public virtual void OnValidate() { }

        /// <summary>
        /// Called after this node is added to a Graph via <see cref="Graph.AddNode(Node)"/>
        /// and before <see cref="OnEnable"/>.
        /// </summary>
        public virtual void OnAddedToGraph() { }

        /// <summary>
        /// Called before this node is removed from a Graph via
        /// <see cref="Graph.RemoveNode(Node)"/> and after <see cref="OnDisable"/>.
        /// </summary>
        public virtual void OnRemovedFromGraph() { }

        /// <summary>
        /// Called when the <see cref="Error"/> property is modified.
        /// </summary>
        public virtual void OnError() { }

        /// <summary>
        /// Resolve the return value associated with the given port.
        /// </summary>
        public abstract object OnRequestValue(Port port);

        /// <summary>
        /// Get either an input or output port by name.
        /// </summary>
        public Port GetPort(string name)
        {
            Ports.TryGetValue(name, out Port value);

            return value;
        }

        /// <summary>
        /// Add a new port to this node.
        /// </summary>
        public void AddPort(Port port)
        {
            var existing = GetPort(port.Name);
            if (existing != null)
            {
                throw new ArgumentException(
                    $"<b>[{Name}]</b> A port named `{port.Name}` already exists"
                );
            }

            port.Node = this;

            portMap[port.Name] = port;

            // Update the serializable port list
            ports = new Port[Ports.Count];
            portMap.Values.CopyTo(ports, 0);
        }

        /// <summary>
        /// Remove an existing port from this node.
        /// </summary>
        public void RemovePort(Port port)
        {
            port.DisconnectAll();
            port.Node = null;

            portMap.Remove(port.Name);

            // Update the serializable port list
            ports = new Port[Ports.Count];
            portMap.Values.CopyTo(ports, 0);
        }

        /// <summary>
        /// Rebuild the fast lookup map between names and <see cref="Port"/> instances.
        /// </summary>
        internal void RefreshPortMap()
        {
            portMap = new Dictionary<string, Port>();
            if (ports != null)
            {
                foreach (var port in ports)
                {
                    // Copy port references to our fast lookup dictionary
                    portMap[port.Name] = port;

                    // Add a backref to each child port of this node.
                    // We don't store this in the serialized copy to avoid cyclic refs.
                    port.Node = this;
                }
            }
        }

        /// <summary>
        /// Safely remove every edge going in and out of this node.
        /// </summary>
        public void DisconnectAllPorts()
        {
            foreach (var port in portMap.Values)
            {
                port.DisconnectAll();
            }
        }

        /// <summary>
        /// Get the value returned by an output port connected to the given port.
        ///
        /// This will return <c>defaultValue</c> if the port is disconnected.
        /// </summary>
        public T GetInputValue<T>(string portName, T defaultValue = default)
        {
            var port = GetPort(portName);
            return GetInputValue<T>(port, defaultValue);
        }

        /// <summary>
        /// Get the value returned by an output port connected to the given port.
        ///
        /// This will return <c>defaultValue</c> if the port is disconnected.
        /// </summary>
        public T GetInputValue<T>(Port port, T defaultValue = default)
        {
            if (port == null)
            {
                throw new ArgumentException(
                    $"<b>[{Name}]</b> Null input port parameter"
                );
            }

            if (port.Direction == PortDirection.Output)
            {
                throw new ArgumentException(
                    $"<b>[{Name}]</b> Wrong input port direction `{port.Name}`"
                );
            }

            return port.GetValue(defaultValue);
        }

        /// <summary>
        /// Get a list of output values for all output ports connected
        /// to the given input port.
        ///
        /// This will return an empty list if the port is disconnected.
        /// </summary>
        public IEnumerable<T> GetInputValues<T>(string portName)
        {
            var port = GetPort(portName);
            return GetInputValues<T>(port);
        }

        /// <summary>
        /// Get a list of output values for all output ports connected
        /// to the given input port.
        ///
        /// This will return an empty list if the port is disconnected.
        /// </summary>
        public IEnumerable<T> GetInputValues<T>(Port port)
        {
            if (port == null)
            {
                throw new ArgumentException(
                    $"<b>[{Name}]</b> Null input port parameter"
                );
            }

            if (port.Direction == PortDirection.Output)
            {
                throw new ArgumentException(
                    $"<b>[{Name}]</b> Wrong input port direction `{port.Name}`"
                );
            }

            return port.GetValues<T>();
        }

        /// <summary>
        /// Get the calculated value of a given output port.
        /// </summary>
        public T GetOutputValue<T>(string portName)
        {
            var port = GetPort(portName);
            return GetOutputValue<T>(port);
        }

        /// <summary>
        /// Get the calculated value of a given output port.
        /// </summary>
        public T GetOutputValue<T>(Port port)
        {
            if (port == null)
            {
                throw new ArgumentException(
                    $"<b>[{Name}]</b> Null output port parameter"
                );
            }

            if (port.Direction == PortDirection.Input)
            {
                throw new ArgumentException(
                    $"<b>[{Name}]</b> Wrong output port direction `{port.Name}`"
                );
            }

            return port.GetValue(default(T));
        }

        public override string ToString()
        {
            return $"{GetType()}({Name}, {ID})";
        }
    }
}
