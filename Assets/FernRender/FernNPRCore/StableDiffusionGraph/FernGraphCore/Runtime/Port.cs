using System;
using System.Collections.Generic;
using UnityEngine;

namespace FernGraph
{
    /// <summary>
    /// Direction of the port
    /// </summary>
    public enum PortDirection
    {
        Input = 0,
        Output = 1
    }

    /// <summary>
    /// Number of connections that can be made to the port
    /// </summary>
    public enum PortCapacity
    {
        Single = 0,
        Multiple = 1
    }

    /// <summary>
    /// Serializable edge information for a Port
    /// </summary>
    [Serializable]
    public class Connection
    {
        [SerializeField] private string nodeId;

        public string NodeID
        {
            get { return nodeId; }
            set { nodeId = value; }
        }

        [SerializeField] private string portName;

        public string PortName
        {
            get { return portName; }
            set { portName = value; }
        }

        [NonSerialized] private Port port;

        public Port Port
        {
            get { return port; }
            internal set { port = value; }
        }
    }

    [Serializable]
    public class Port : ISerializationCallbackReceiver
    {
        [NonSerialized] private Node node;

        public Node Node
        {
            get { return node; }
            internal set { node = value; }
        }

        [SerializeField] private string name;

        /// <summary>
        /// Display name for this port
        /// </summary>
        public string Name
        {
            get { return name; }
            set {
                name = value;
                RefreshInboundConnections();
                Node?.RefreshPortMap();
            }
        }

        [SerializeField] private string type;

        /// <summary>
        /// Allowable connection types made to this port.
        /// </summary>
        public Type Type { get; set; }

        [SerializeField] private PortCapacity capacity = PortCapacity.Single;

        /// <summary>
        /// Whether or not multiple edges can be connected
        /// between this port and other ports.
        /// </summary>
        public PortCapacity Capacity
        {
            get { return capacity; }
            set { capacity = value; }
        }

        [SerializeField] private PortDirection direction = PortDirection.Input;

        /// <summary>
        /// Whether to treat this as an input or output port.
        /// </summary>
        public PortDirection Direction
        {
            get { return direction; }
            set { direction = value; }
        }

        public int ConnectionCount
        {
            get { return connections.Count; }
        }

        internal List<Connection> Connections
        {
            get { return connections; }
        }

        [SerializeField] private List<Connection> connections = new List<Connection>();

        /// <summary>
        /// Enumerate all ports connected by edges to this port
        /// </summary>
        public IEnumerable<Port> ConnectedPorts
        {
            get
            {
                for (var i = 0; i < connections.Count; i++)
                {
                    yield return connections[i].Port;
                }
            }
        }

        public void OnBeforeSerialize()
        {
            if (Type != null)
            {
                type = Type.AssemblyQualifiedName;
            }
            else
            {
                //Debug.LogError( name + " Type is null");
            }
        }

        public void OnAfterDeserialize()
        {
            Type = Type.GetType(type);
        }

        /// <summary>
        /// Resolve the value on this port.
        ///
        /// If this is an input port that accepts multiple connections,
        /// only the first connection's output value will be returned.
        ///
        /// If this is an output port, then the node's <c>OnRequestValue()</c>
        /// will be executed and best effort will be made to convert
        /// to the requested type.
        /// </summary>
        public virtual T GetValue<T>(T defaultValue = default)
        {
            // If this is an input port, consume the
            // value from connected port.
            if (Direction == PortDirection.Input)
            {
                if (connections.Count > 0)
                {
                    return connections[0].Port.GetValue<T>();
                }

                return defaultValue;
            }

            // Otherwise, attempt resolution from the parent node.
            object value = Node.OnRequestValue(this);

            // Make sure we don't try to cast to a value type from null
            if (value == null && typeof(T).IsValueType)
            {
                throw new InvalidCastException(
                    $"Cannot cast null to value type `{typeof(T).FullName}`"
                );
            }

            // Short circuit Convert.ChangeType if we can cast quicker
            if (value == null || typeof(T).IsAssignableFrom(value.GetType()))
            {
                return (T)value;
            }

            // Try for IConvertible support
            try
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch (Exception e)
            {
                throw new InvalidCastException(
                    $"Cannot cast `{value.GetType()}` to `{typeof(T)}`. Error: {e}."
                );
            }
        }

        /// <summary>
        /// Return an iterator of connection values to this port
        /// where <c>GetValue</c> of each connected port is enumerated.
        /// </summary>
        public virtual IEnumerable<T> GetValues<T>()
        {
            if (connections.Count > 0)
            {
                for (var i = 0; i < connections.Count; i++)
                {
                    yield return connections[i].Port.GetValue<T>();
                }
            }
        }

        /// <summary>
        /// Remove all edges connected connected to this port.
        /// </summary>
        internal void DisconnectAll()
        {
            // Remove ourselves from all other connected ports
            foreach (var port in ConnectedPorts)
            {
                port.connections.RemoveAll((edge) =>
                    edge.NodeID == Node.ID && edge.PortName == Name
                );
            }

            connections.Clear();
        }

        /// <summary>
        /// Add an edge between this and the given Port.
        ///
        /// Use <c>Graph.AddEdge()</c> over this.
        /// </summary>
        internal void Connect(Port port)
        {
            // Skip if we're already connected
            if (GetConnection(port) != null)
            {
                return;
            }

            connections.Add(new Connection()
            {
                Port = port,
                NodeID = port.Node.ID,
                PortName = port.Name
            });

            port.connections.Add(new Connection()
            {
                Port = this,
                NodeID = Node.ID,
                PortName = Name
            });
        }

        /// <summary>
        /// Find a <c>Connection</c> to the given port if one exists.
        /// </summary>
        internal Connection GetConnection(Port port)
        {
            return connections.Find((edge) =>
                edge.NodeID == port.Node.ID && edge.PortName == port.Name
            );
        }

        /// <summary>
        /// Remove any edges between this and the given Port.
        ///
        /// Use <c>Graph.RemoveEdge()</c> externally
        /// </summary>
        internal void Disconnect(Port port)
        {
            // Remove all outbound connections to the other port
            connections.RemoveAll((edge) =>
                edge.NodeID == port.Node.ID && edge.PortName == port.Name
            );

            // Remove inbound connections to the other port
            port.connections.RemoveAll((edge) =>
                edge.NodeID == Node.ID && edge.PortName == Name
            );
        }

        /// <summary>
        /// Load Port class instances from the Graph for each connection,
        /// invalidating any connections that no longer exist.
        /// </summary>
        internal void UpdateConnections()
        {
            if (hasLoadedConnections)
            {
                return;
            }

            var graph = Node.Graph;
            for (var i = 0; i < connections.Count; i++)
            {
                var edge = connections[i];
                var connected = graph.GetNodeById(edge.NodeID);
                if (connected == null)
                {
                    Debug.LogError(
                        $"Could not locate connected node `{edge.NodeID}` from port `{Name}` of `{Node.Name}`"
                    );
                }
                else
                {
                    edge.Port = connected.GetPort(edge.PortName);
                    connections[i] = edge;
                }
            }

            hasLoadedConnections = true;
        }

        // Explicit non-serialized so that editor reloads wipe it
        [NonSerialized] private bool hasLoadedConnections;

        internal void OnEnable()
        {
            UpdateConnections();
        }

        internal void OnValidate()
        {
            UpdateConnections();
        }

        /// <summary>
        /// Update PortName on inbound connections to match an updated <c>Name</c>
        /// </summary>
        private void RefreshInboundConnections()
        {
            for (var i = 0; i < connections.Count; i++)
            {
                var port = connections[i].Port;

                // This is inbound, so we need to update the connection
                // entry for the *other* port.
                foreach (var edge in port.connections)
                {
                    if (edge.Port == this)
                    {
                        edge.NodeID = Node.ID;
                        edge.PortName = Name;
                    }
                }
            }
        }

        public override string ToString()
        {
            return $"{GetType()}({Name}, {Node?.ID})";
        }
    }
}
