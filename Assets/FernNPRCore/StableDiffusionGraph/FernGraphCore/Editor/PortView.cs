using System;
using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using GraphViewPort = UnityEditor.Experimental.GraphView.Port;

namespace FernGraph.Editor
{
    public class PortView : GraphViewPort
    {
        public Port Target { get; set; }

        /// <summary>
        /// Should the inline editor field disappear once one or more
        /// connections have been made to this port view
        /// </summary>
        public bool HideEditorFieldOnConnection { get; set; } = true;

        private VisualElement editorField;
        
        public PortView(
            Orientation portOrientation, 
            Direction portDirection, 
            Capacity portCapacity, 
            Type type
        ) : base(portOrientation, portDirection, portCapacity, type)
        {
            styleSheets.Add(Resources.Load<StyleSheet>("FernGraphEditor/PortView"));
            AddToClassList("portView");

            visualClass = string.Empty;
            AddTypeClasses(type);

            tooltip = type.ToPrettyName();
        }
    
        public static PortView Create(Port port, IEdgeConnectorListener connectorListener) 
        {
            Direction direction = port.Direction == PortDirection.Input ? Direction.Input : Direction.Output;
            Capacity capacity = port.Capacity == PortCapacity.Multiple ? Capacity.Multi : Capacity.Single;

            var view = new PortView(Orientation.Horizontal, direction, capacity, port.Type) 
            {
                m_EdgeConnector = new EdgeConnector<Edge>(connectorListener),
                portName = port.Name,
                Target = port
            };

            view.AddManipulator(view.m_EdgeConnector);
            return view;
        }

        public void SetEditorField(VisualElement field)
        {
            if (editorField != null)
            {
                m_ConnectorBox.parent.Remove(editorField);
            }

            editorField = field;
            m_ConnectorBox.parent.Add(editorField);
        }
        
        /// <summary>
        /// Return true if this port can be connected with an edge to the given port
        /// </summary>
        public bool IsCompatibleWith(PortView other)
        {
            if (other.node == node || other.direction == direction)
            {
                return false;
            }
            
            // TODO: Loop detection to ensure nobody is making a cycle 
            // (for certain use cases, that is)
            
            // Check for type cast support in the direction of output port -> input port
            return (other.direction == Direction.Input && portType.IsCastableTo(other.portType, true)) ||
                    (other.direction == Direction.Output && other.portType.IsCastableTo(portType, true));
        }
        
        /// <summary>
        /// Add USS class names for the given type
        /// </summary>
        private void AddTypeClasses(Type type)
        {
            var classes = type.ToUSSClasses();
            foreach (var cls in classes) {
                AddToClassList(cls);
            }
        }

        /// <summary>
        /// Executed on change of a port connection. Perform any prep before the following
        /// OnUpdate() call during redraw. 
        /// </summary>
        public void OnDirty() { }
        
        /// <summary>
        /// Toggle visibility of the inline editable value based on whether we have connections
        /// </summary>
        public void OnUpdate()
        {
            portName = Target.Name;

            if (connected && editorField != null && HideEditorFieldOnConnection)
            {
                editorField.style.display = DisplayStyle.None;
            }

            if (!connected && editorField != null)
            {
                editorField.style.display = DisplayStyle.Flex;
            }
        }
    }
}
