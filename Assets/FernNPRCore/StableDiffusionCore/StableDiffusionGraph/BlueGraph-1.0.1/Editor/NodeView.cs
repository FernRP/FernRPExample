using System.Text.RegularExpressions;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;

using UnityEditor.Experimental.GraphView;
using GraphViewNode = UnityEditor.Experimental.GraphView.Node;
using static UnityEngine.UIElements.DropdownMenuAction;

namespace BlueGraph.Editor
{
    public class NodeView : GraphViewNode, ICanDirty
    {
        public Node Target { get; private set; }

        public List<PortView> Inputs { get; set; } = new List<PortView>();

        public List<PortView> Outputs { get; set; } = new List<PortView>();

        protected EdgeConnectorListener ConnectorListener { get; set; }

        protected SerializedProperty SerializedNode { get; set; }

        protected NodeReflectionData ReflectionData { get; set; }

        protected CanvasView Canvas { get; set; }

        private Label errorMessage;

        internal void Initialize(Node node, CanvasView canvas, EdgeConnectorListener connectorListener)
        {
            viewDataKey = node.ID;
            Target = node;
            Canvas = canvas;
            ReflectionData = NodeReflection.GetNodeType(node.GetType());
            ConnectorListener = connectorListener;

            styleSheets.Add(Resources.Load<StyleSheet>("BlueGraphEditor/NodeView"));
            AddToClassList("nodeView");

            // Add a class name matching the node's name (e.g. `.node-My-Branch`)
            var ussSafeName = Regex.Replace(Target.Name, @"[^a-zA-Z0-9]+", "-").Trim('-');
            AddToClassList($"node-{ussSafeName}");

            var errorContainer = new VisualElement { name = "error" };
            errorContainer.Add(new VisualElement { name = "error-icon" });

            errorMessage = new Label { name = "error-label" };
            errorContainer.Add(errorMessage);

            Insert(0, errorContainer);

            SetPosition(new Rect(node.Position, Vector2.one));
            title = node.Name;

            if (!ReflectionData.Deletable)
            {
                capabilities &= ~Capabilities.Deletable;
            }
            if (!ReflectionData.Moveable)
            {
                capabilities &= ~Capabilities.Movable;
            }

            // Custom OnDestroy() handler via https://forum.unity.com/threads/request-for-visualelement-ondestroy-or-onremoved-event.718814/
            RegisterCallback<DetachFromPanelEvent>((e) => Destroy());
            RegisterCallback<TooltipEvent>(OnTooltip);

            node.OnErrorEvent += RefreshErrorState;
            node.OnValidateEvent += OnValidate;
            
            ReloadPorts();
            ReloadEditables();
            RefreshErrorState();

            OnInitialize();
        }

        /// <summary>
        /// Executed after receiving a node target and initial configuration
        /// but before being added to the graph.
        /// </summary>
        protected virtual void OnInitialize() { }

        internal void Destroy()
        {
            OnDestroy();
            Target.OnErrorEvent -= RefreshErrorState;
            Target.OnValidateEvent -= OnValidate;
        }

        /// <summary>
        /// Executed when we're about to detach this element from the graph.
        /// </summary>
        protected virtual void OnDestroy() { }

        protected void RefreshErrorState()
        {
            if (string.IsNullOrEmpty(Target.Error))
            {
                RemoveFromClassList("hasError");
                errorMessage.text = "";
            }
            else
            {
                AddToClassList("hasError");
                errorMessage.text = Target.Error;
            }

            OnError();
        }

        /// <summary>
        /// Called after the target node's <c>OnError</c> property is executed.
        /// </summary>
        protected virtual void OnError() { }

        /// <summary>
        /// Called after the target node's <c>OnValidate</c> is executed.
        /// </summary>
        protected virtual void OnValidate() { }

        /// <summary>
        /// Make sure our list of PortViews and editable controls sync up with our NodePorts
        /// </summary>
        protected void ReloadPorts()
        {
            foreach (var port in Target.Ports.Values)
            {
                if (port.Direction == PortDirection.Input)
                {
                    AddInputPort(port);
                }
                else
                {
                    AddOutputPort(port);
                }
            }

            // Update state classes
            EnableInClassList("hasInputs", Inputs.Count > 0);
            EnableInClassList("hasOutputs", Outputs.Count > 0);
        }

        protected void ReloadEditables()
        {
            var reflectionData = NodeReflection.GetNodeType(Target.GetType());
            if (reflectionData != null)
            {
                foreach (var editable in reflectionData.Editables)
                {
                    AddEditableField(editable);
                }
            }

            RefreshExpandedState();
        }

        protected void AddEditableField(EditableReflectionData editable)
        {
            var field = editable.GetControlElement(this);
            extensionContainer.Add(field);
        }

        protected virtual void AddInputPort(Port port)
        {
            var view = PortView.Create(port, ConnectorListener);

            // If we're exposing a control element via reflection: include it in the view
            var reflection = NodeReflection.GetNodeType(Target.GetType());
            var element = reflection.GetPortByName(port.Name)?.GetControlElement(this);

            if (element != null)
            {
                var container = new VisualElement();
                container.AddToClassList("property-field-container");
                container.Add(element);

                view.SetEditorField(container);
            }

            Inputs.Add(view);
            inputContainer.Add(view);
        }

        protected virtual void AddOutputPort(Port port)
        {
            var view = PortView.Create(port, ConnectorListener);

            Outputs.Add(view);
            outputContainer.Add(view);
        }

        public PortView GetInputPort(string name)
        {
            return Inputs.Find((port) => port.portName == name);
        }

        public PortView GetOutputPort(string name)
        {
            return Outputs.Find((port) => port.portName == name);
        }

        public PortView GetCompatibleInputPort(PortView output)
        {
            return Inputs.Find((port) => port.IsCompatibleWith(output));
        }

        public PortView GetCompatibleOutputPort(PortView input)
        {
            return Outputs.Find((port) => port.IsCompatibleWith(input));
        }

        /// <summary>
        /// A property has been updated, either by a port or a connection
        /// </summary>
        public virtual void OnPropertyChange()
        {
            Target.Validate();
            Canvas?.Dirty(this);
        }

        public void Dirty()
        {
            OnDirty();

            // Dirty all ports so they can refresh their state
            Inputs.ForEach(port => port.OnDirty());
            Outputs.ForEach(port => port.OnDirty());
        }

        public void Update()
        {
            OnUpdate();

            // Propagate update to all ports
            Inputs.ForEach(port => port.OnUpdate());
            Outputs.ForEach(port => port.OnUpdate());
        }

        /// <summary>
        /// Dirty this node in response to a change in connectivity or internal state.
        /// Invalidate any cache in prep for an OnUpdate() call.
        /// </summary>
        public virtual void OnDirty() { }

        /// <summary>
        /// Called when this node was dirtied and the UI is redrawing.
        /// </summary>
        public virtual void OnUpdate() { }

        public override Rect GetPosition()
        {
            // The default implementation doesn't give us back a valid position until layout is resolved.
            // See: https://github.com/Unity-Technologies/UnityCsReference/blob/master/Modules/GraphViewEditor/Elements/Node.cs#L131
            Rect position = base.GetPosition();
            if (position.width > 0 && position.height > 0)
            {
                return position;
            }

            return new Rect(Target.Position, Vector2.one);
        }

        public override void SetPosition(Rect newPos)
        {
            base.SetPosition(newPos);
            Target.Position = newPos.position;
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            //Add Edit Node Script and Edit Node View Script to context menu of the Node
            evt.menu.AppendSeparator("Edit");
            evt.menu.AppendAction("Edit/Node Script", (e) => EditNodeScript(), GetNodeScriptStatus);
            evt.menu.AppendAction("Edit/Node View Script", (e) => EditNodeViewScript(), GetNodeViewScriptStatus);

            //Add ContextMethods by Attributes from node
            var contextMethods = ReflectionData.ContextMethods;

            foreach (var attr in contextMethods.Keys)
            {
                string title = string.IsNullOrEmpty(attr.menuItem) ? contextMethods[attr].Name : attr.menuItem;
                var info = contextMethods[attr];

                evt.menu.AppendAction(title, (e) => OnContextMenuSelected(info));
            }
        }

        /// <summary>
        /// Open NodeScript at Script Editor
        /// </summary>
        public void EditNodeScript()
        {
            var script = ReflectionData.NodeScript;

            if (script != null)
                AssetDatabase.OpenAsset(script.GetInstanceID(), 0, 0);
        }

        /// <summary>
        /// Open NodeViewScript at Script Editor
        /// </summary>
        public void EditNodeViewScript()
        {
            var script = ReflectionData.NodeViewScript;

            if (script != null)
                AssetDatabase.OpenAsset(script.GetInstanceID(), 0, 0);
        }

        private Status GetNodeScriptStatus(DropdownMenuAction action)
        {
            if (ReflectionData.NodeScript != null)
                return Status.Normal;
            return Status.Disabled;
        }

        /// <summary>
        /// Utility method for <see cref="BuildContextualMenu(ContextualMenuPopulateEvent)"/> to toggle the context menu item
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        private Status GetNodeViewScriptStatus(DropdownMenuAction action)
        {
            if (ReflectionData.NodeViewScript)
                return Status.Normal;
            return Status.Disabled;
        }

        /// <summary>
        /// Event triggered when a contexxt method is clicked
        /// </summary>
        /// <param name="info"></param>
        private void OnContextMenuSelected(System.Reflection.MethodInfo info)
        {
            info.Invoke(Target, null);
        }

        protected void OnTooltip(TooltipEvent evt)
        {
            // TODO: Better implementation that can be styled
            if (evt.target == titleContainer.Q("title-label"))
            {
                var typeData = NodeReflection.GetNodeType(Target.GetType());
                evt.tooltip = typeData?.Help;

                // Float the tooltip above the node title bar
                var bound = titleContainer.worldBound;
                bound.x = 0;
                bound.y = 0;
                bound.height *= -1;

                evt.rect = titleContainer.LocalToWorld(bound);
            }
        }
    }
}
