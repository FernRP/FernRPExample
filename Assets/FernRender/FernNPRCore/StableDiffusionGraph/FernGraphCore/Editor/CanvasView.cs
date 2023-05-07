using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

using UnityEditor.Experimental.GraphView;
using GraphViewPort = UnityEditor.Experimental.GraphView.Port;
using GraphViewEdge = UnityEditor.Experimental.GraphView.Edge;
using GraphViewSearchWindow = UnityEditor.Experimental.GraphView.SearchWindow;

namespace FernGraph.Editor
{
    /// <summary>
    /// Graph view that contains the nodes, edges, etc. 
    /// </summary>
    public class CanvasView : GraphView
    {

        private readonly string variablesStyle = "FernGraphEditor/Variables";
        private readonly string canvasViewStyle = "FernGraphEditor/CanvasView";
        private readonly string canvasViewName = "FernGraph-canvas";
        
        public GraphEditorWindow EditorWindow { get; private set; }

        public Graph Graph { get; private set; }

        private readonly Label title;
        private readonly List<CommentView> commentViews = new List<CommentView>();
        private readonly SearchWindow searchWindow;
        private readonly EdgeConnectorListener edgeConnectorListener;
        private readonly HashSet<ICanDirty> dirtyElements = new HashSet<ICanDirty>();

        private SerializedObject serializedGraph;
        private Vector2 lastMousePosition;

        public CanvasView(GraphEditorWindow window)
        {
            EditorWindow = window;
            name = canvasViewName;
            
            styleSheets.Add(Resources.Load<StyleSheet>(variablesStyle));
            styleSheets.Add(Resources.Load<StyleSheet>(canvasViewStyle));
            AddToClassList("canvasView");

            // Set up edge connector and search window
            edgeConnectorListener = new EdgeConnectorListener(this);
            searchWindow = ScriptableObject.CreateInstance<SearchWindow>();
            searchWindow.Target = this;
            
            // Set up zoom and manipulators
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());
            this.AddManipulator(new ClickSelector());
            this.AddManipulator(new EdgeManipulator());
            
            // Set up event handlers
            RegisterCallback<KeyUpEvent>(OnGraphKeyUp);
            RegisterCallback<MouseMoveEvent>(OnGraphMouseMove);
            graphViewChanged = OnGraphViewChanged;
            RegisterCallback<AttachToPanelEvent>(c => { Undo.undoRedoPerformed += OnUndoRedo; });
            RegisterCallback<DetachFromPanelEvent>(c => { Undo.undoRedoPerformed -= OnUndoRedo; });
            
            // Set up node creation and (de)serialization handlers
            nodeCreationRequest = (ctx) => OpenSearch(ctx.screenMousePosition);
            serializeGraphElements = OnSerializeGraphElements;
            canPasteSerializedData = OnTryPasteSerializedData;
            unserializeAndPaste = OnUnserializeAndPaste;

            RegisterCallback<GeometryChangedEvent>(OnFirstResize);
            
            // Set up title and grid background
            title = new Label("FernGraph");
            title.AddToClassList("canvasViewTitle");
            Add(title);
            Insert(0, new GridBackground());
        }

        private void OnUndoRedo()
        {
            Reload();
        }

        private void OnGraphMouseMove(MouseMoveEvent evt)
        {
            lastMousePosition = evt.mousePosition;
        }

        /// <summary>
        /// Event handler to frame the graph view on initial layout
        /// </summary>
        private void OnFirstResize(GeometryChangedEvent evt)
        {
            UnregisterCallback<GeometryChangedEvent>(OnFirstResize);
            FrameAll();
        }

        private GraphViewChange OnGraphViewChanged(GraphViewChange change)
        {
            if (serializedGraph == null)
            {
                return change;
            }

            if (change.movedElements != null)
            {
                // Moved nodes will update their underlying models automatically.
                EditorUtility.SetDirty(Graph);
            }

            if (change.elementsToRemove != null)
            {
                foreach (var element in change.elementsToRemove)
                {
                    if (element is NodeView node)
                    {
                        RemoveNode(node);
                    }
                    else if (element is GraphViewEdge edge)
                    {
                        RemoveEdge(edge, true);
                    }
                    else if (element is CommentView comment)
                    {
                        RemoveComment(comment);
                    }
                    if (element is ICanDirty canDirty)
                    {
                        dirtyElements.Remove(canDirty);
                    }
                }
            }
            return change;
        }

        private void OnGraphKeyUp(KeyUpEvent evt)
        {
            if (evt.target != this)
            {
                return;
            }

            // C: Add a new comment around the selected nodes (or just at mouse position)
            if (evt.keyCode == KeyCode.C && !evt.ctrlKey && !evt.commandKey)
            {
                AddComment();
            }

            // H: Horizontally align selected nodes
            if (evt.keyCode == KeyCode.H && !evt.ctrlKey && !evt.commandKey)
            {
                HorizontallyAlignSelectedNodes();
            }

            if (evt.keyCode == KeyCode.V && !evt.ctrlKey && !evt.commandKey)
            {
                VerticallyAlignSelectedNodes();
            }
        }

        protected void HorizontallyAlignSelectedNodes()
        {
            float sum = 0;
            int count = 0;

            foreach (var selectable in selection)
            {
                if (selectable is NodeView node)
                {
                    sum += node.GetPosition().xMin;
                    count++;
                }
            }

            float xAvg = sum / count;
            foreach (var selectable in selection)
            {
                if (selectable is NodeView node)
                {
                    var pos = node.GetPosition();
                    pos.xMin = xAvg;
                    node.SetPosition(pos);
                }
            }
        }

        protected void VerticallyAlignSelectedNodes()
        {
            float sum = 0;
            int count = 0;

            foreach (var selectable in selection)
            {
                if (selectable is NodeView node)
                {
                    sum += node.GetPosition().yMin;
                    count++;
                }
            }

            float yAvg = sum / count;
            foreach (var selectable in selection)
            {
                if (selectable is NodeView node)
                {
                    var pos = node.GetPosition();
                    pos.yMin = yAvg;
                    node.SetPosition(pos);
                }
            }
        }

        /// <summary>
        /// Add a new provider to populate the search window
        /// </summary>
        public void AddSearchProvider(ISearchProvider provider)
        {
            searchWindow.AddSearchProvider(provider);
        }

        public void Load(Graph graph)
        {
            Graph = graph;
            serializedGraph = new SerializedObject(Graph);
            title.text = graph.Title;
            SetupZoom(graph.ZoomMinScale, graph.ZoomMaxScale);

            AddNodeViews(graph.Nodes);
            AddCommentViews(graph.Comments);

            ResetSearchWindow();
           

            // TODO: Move into reflection
            var attrs = graph.GetType().GetCustomAttributes(true);

            foreach (var attr in attrs)
            {
                switch (attr)
                {
                    //Add Tags for search provider
                    case IncludeTagsAttribute include:
                        searchWindow.IncludeTags.AddRange(include.Tags);
                        break;
                    //Add Required nodes from GraphAttributes
                    case RequireNodeAttribute required:
                    {
                        Node node = graph.GetNode(required.type);
                        if (node == null)
                        {
                            node = NodeReflection.Instantiate(required.type);
                            node.Graph = graph;
                            node.Name = required.nodeName;
                            node.Position = required.position;
                            AddNodeFromSearch(node, node.Position, null, false);
                        }

                        break;
                    }
                }
            }
        }
        
        private void ResetSearchWindow()
        {
            // Reset the search to a new set of tags and providers
            searchWindow.ClearSearchProviders();
            searchWindow.ClearTags();

            foreach (var provider in NodeReflection.SearchProviders)
            {
                if (provider.IsSupported(Graph))
                {
                    searchWindow.AddSearchProvider(provider);
                }
            }
        }
        

        /// <summary>
        /// Create a new node from reflection data and insert into the Graph.
        /// </summary>
        internal void AddNodeFromSearch(Node node, Vector2 screenPosition, PortView connectedPort = null, bool registerUndo = true)
        {
            // Calculate where to place this node on the graph
            var windowRoot = EditorWindow.rootVisualElement;
            var windowMousePosition = EditorWindow.rootVisualElement.ChangeCoordinatesTo(
                windowRoot.parent,
                screenPosition - EditorWindow.position.position
            );

            var graphMousePosition = contentViewContainer.WorldToLocal(windowMousePosition);

            // Track undo and add to the graph
            if (registerUndo)
            {
                Undo.RegisterCompleteObjectUndo(Graph, $"Add Node {node.Name}");
            }

            node.Position = graphMousePosition;

            Graph.AddNode(node);
            serializedGraph.Update();
            EditorUtility.SetDirty(Graph);

            // Add a node to the visual graph
            var editorType = NodeReflection.GetNodeEditorType(node.GetType());
            var element = Activator.CreateInstance(editorType) as NodeView;
            element.Initialize(node, this, edgeConnectorListener);

            AddElement(element);

            // If there was a provided existing port to connect to, find the best 
            // candidate port on the new node and connect. 
            if (connectedPort != null)
            {
                var edge = new GraphViewEdge();

                if (connectedPort.direction == Direction.Input)
                {
                    edge.input = connectedPort;
                    edge.output = element.GetCompatibleOutputPort(connectedPort);
                }
                else
                {
                    edge.output = connectedPort;
                    edge.input = element.GetCompatibleInputPort(connectedPort);
                }

                AddEdge(edge, false);
            }

            Dirty(element);
        }

        /// <summary>
        /// Remove a node from both the canvas view and the graph model
        /// </summary>
        public void RemoveNode(NodeView node)
        {
            Undo.RegisterCompleteObjectUndo(Graph, $"Delete Node {node.name}");

            Graph.RemoveNode(node.Target);
            serializedGraph.Update();
            EditorUtility.SetDirty(Graph);

            RemoveElement(node);
        }
        
        /// <summary>
        /// Add a new edge to both the canvas view and the underlying graph model
        /// </summary>
        public void AddEdge(GraphViewEdge edge, bool registerAsNewUndo)
        {
            try
            {
                if (edge.input == null || edge.output == null)
                {
                    return;
                }

                if (registerAsNewUndo)
                {
                    Undo.RegisterCompleteObjectUndo(Graph, "Add Edge");
                }

                // Handle single connection ports on either end. 
                var edgesToRemove = new List<GraphViewEdge>();
                if (edge.input.capacity == GraphViewPort.Capacity.Single)
                {
                    foreach (var conn in edge.input.connections)
                    {
                        edgesToRemove.Add(conn);
                    }
                }

                if (edge.output.capacity == GraphViewPort.Capacity.Single)
                {
                    foreach (var conn in edge.output.connections)
                    {
                        edgesToRemove.Add(conn);
                    }
                }

                foreach (var edgeToRemove in edgesToRemove)
                {
                    RemoveEdge(edgeToRemove, false);
                }

                var input = edge.input as PortView;
                var output = edge.output as PortView;

                // Connect the ports in the model
                Graph.AddEdge(input.Target, output.Target);
                serializedGraph.Update();
                EditorUtility.SetDirty(Graph);

                // Add a matching edge view onto the canvas
                var newEdge = input.ConnectTo(output);
                newEdge.RegisterCallback<MouseDownEvent>(OnEdgeMouseDown);
                AddElement(newEdge);

                // Dirty the affected node views
                Dirty(input.node as NodeView);
                Dirty(output.node as NodeView);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        public void CreateRedirectNode(Vector2 position, Edge edgeTarget)
        {
           Debug.Log("Todo: Add RedirectNode");
        }
        
        private void OnEdgeMouseDown(MouseDownEvent evt)
        {
            if (evt.button == (int)MouseButton.LeftMouse && evt.clickCount == 2)
            {
                if (evt.target is Edge edgeTarget)
                {
                    Vector2 pos = evt.mousePosition;
                    this.CreateRedirectNode(pos, edgeTarget);
                }
            }
        }

        /// <summary>
        /// Remove an edge from both the canvas view and the underlying graph model
        /// </summary>
        public void RemoveEdge(GraphViewEdge edge, bool registerAsNewUndo)
        {
            var input = edge.input as PortView;
            var output = edge.output as PortView;

            if (registerAsNewUndo)
            {
                Undo.RegisterCompleteObjectUndo(Graph, "Remove Edge");
            }

            // Disconnect the ports in the model
            Graph.RemoveEdge(input.Target, output.Target);
            serializedGraph.Update();
            EditorUtility.SetDirty(Graph);

            // Remove the edge view
            edge.input.Disconnect(edge);
            edge.output.Disconnect(edge);
            edge.input = null;
            edge.output = null;
            RemoveElement(edge);

            // Dirty the affected node views
            Dirty(input.node as NodeView);
            Dirty(output.node as NodeView);
        }

        /// <summary>
        /// Reload a fresh serialized copy of the graph.
        /// </summary>
        public void Reload()
        {
            serializedGraph = null;

            DeleteElements(graphElements.ToList());
            Load(Graph);
        }

        /// <summary>
        /// Mark a node and all dependents as dirty for the next refresh. 
        /// </summary>
        public void Dirty(ICanDirty element)
        {
            dirtyElements.Add(element);

            // TODO: Not the best place for this.
            EditorUtility.SetDirty(Graph);

            element.Dirty();

            // Also dirty outputs if a NodeView
            if (element is NodeView node)
            {
                foreach (var port in node.Outputs)
                {
                    foreach (var conn in port.connections)
                    {
                        if (!dirtyElements.Contains(conn.input.node as NodeView))
                        {
                            Dirty(conn.input.node as NodeView);
                        }

                    }
                }
            }
        }


        /// <summary>
        /// Dirty all nodes on the canvas for a complete refresh
        /// </summary>
        public void DirtyAll()
        {
            graphElements.ForEach((element) =>
            {
                if (element is ICanDirty cd)
                {
                    cd.Dirty();
                    dirtyElements.Add(cd);
                }
            });
        }

        public void Update()
        {
            // Propagate update on dirty elements
            foreach (var element in dirtyElements)
            {
                element.Update();
            }

            dirtyElements.Clear();
        }

        public void OpenSearch(Vector2 screenPosition, PortView connectedPort = null)
        {
            searchWindow.SourcePort = connectedPort;
            GraphViewSearchWindow.Open(new SearchWindowContext(screenPosition), searchWindow);
        }

        /// <summary>
        /// Append views for a set of nodes
        /// </summary>
        private void AddNodeViews(IEnumerable<Node> nodes, bool selectOnceAdded = false, bool centerOnMouse = false)
        {
            // Add views of each node from the graph
            var nodeMap = new Dictionary<Node, NodeView>();

            foreach (var node in nodes)
            {
                if (!Graph.Nodes.Contains(node))
                {
                    Debug.LogError("Cannot add NodeView: Node is not indexed on the graph");
                }
                else
                {
                    var editorType = NodeReflection.GetNodeEditorType(node.GetType());
                    var element = Activator.CreateInstance(editorType) as NodeView;

                    element.Initialize(node, this, edgeConnectorListener);
                    AddElement(element);

                    nodeMap.Add(node, element);
                    Dirty(element);

                    if (selectOnceAdded)
                    {
                        AddToSelection(element);
                    }
                }
            }

            if (centerOnMouse)
            {
                var bounds = GetBounds(nodeMap.Values);
                var worldPosition = contentViewContainer.WorldToLocal(lastMousePosition);
                var delta = worldPosition - bounds.center;

                foreach (var node in nodeMap)
                {
                    node.Value.SetPosition(new Rect(node.Key.Position + delta, Vector2.one));
                }
            }

            // Sync edges on the graph with our graph's connections 
            // TODO: Deal with trash connections from bad imports
            // and try to just refactor this whole thing tbh
            foreach (var node in nodeMap)
            {
                foreach (var port in node.Key.Ports.Values)
                {
                    if (port.Direction == PortDirection.Output)
                    {
                        continue;
                    }

                    foreach (var conn in port.ConnectedPorts)
                    {
                        var connectedNode = conn?.Node;
                        if (connectedNode == null)
                        {
                            Debug.LogError(
                                 $"Could not connect `{node.Value.title}:{port.Name}`: " +
                                 $"Connected node no longer exists."
                            );
                            continue;
                        }

                        // Only add if the linked node is in the collection
                        // TODO: This shouldn't be a problem
                        if (!nodeMap.ContainsKey(connectedNode))
                        {
                            Debug.LogError(
                                 $"Could not connect `{node.Value.title}:{port.Name}` -> `{connectedNode.Name}:{conn.Name}`. " +
                                 $"Target node does not exist in the NodeView map"
                            );
                            continue;
                        }

                        var inPort = node.Value.GetInputPort(port.Name);
                        var outPort = nodeMap[connectedNode].GetOutputPort(conn.Name);

                        if (inPort == null)
                        {
                            Debug.LogError(
                                $"Could not connect `{node.Value.title}:{port.Name}` -> `{connectedNode.Name}:{conn.Name}`. " +
                                $"Input port `{port.Name}` no longer exists."
                            );
                        }
                        else if (outPort == null)
                        {
                            Debug.LogError(
                                $"Could not connect `{connectedNode.Name}:{conn.Name}` to `{node.Value.name}:{port.Name}`. " +
                                $"Output port `{conn.Name}` no longer exists."
                            );
                        }
                        else
                        {
                            var edge = inPort.ConnectTo(outPort);
                            edge.RegisterCallback<MouseDownEvent>(OnEdgeMouseDown);
                            AddElement(edge);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Append views for comments from a Graph
        /// </summary>
        private void AddCommentViews(IEnumerable<Comment> comments)
        {
            foreach (var comment in comments)
            {
                var commentView = new CommentView(comment);
                commentViews.Add(commentView);
                AddElement(commentView);
                Dirty(commentView);
            }
        }

        /// <summary>
        /// Calculate the bounding box for a set of nodes
        /// </summary>
        private Rect GetBounds(IEnumerable<ISelectable> items)
        {
            var contentRect = Rect.zero;

            foreach (var item in items)
            {
                if (item is NodeView ele)
                {
                    var boundingRect = ele.GetPosition();
                    boundingRect.width = Mathf.Max(boundingRect.width, 1);
                    boundingRect.height = Mathf.Max(boundingRect.height, 1);

                    boundingRect = ele.parent.ChangeCoordinatesTo(contentViewContainer, boundingRect);

                    if (contentRect.width < 1 || contentRect.height < 1)
                    {
                        contentRect = boundingRect;
                    }
                    else
                    {
                        contentRect = RectUtils.Encompass(contentRect, boundingRect);
                    }
                }
            }

            return contentRect;
        }

        /// <summary>
        /// Add a new comment to the canvas and the associated Graph
        /// 
        /// If there are selected nodes, this'll encapsulate the selection with
        /// the comment box. Otherwise, it'll add at defaultPosition.
        /// </summary>
        private void AddComment()
        {
            Undo.RegisterCompleteObjectUndo(Graph, "Add Comment");

            // Pad out the bounding box a bit more on the selection
            var padding = 30f; // TODO: Remove hardcoding

            var bounds = GetBounds(selection);

            if (bounds.width < 1 || bounds.height < 1)
            {
                Vector2 worldPosition = contentViewContainer.WorldToLocal(lastMousePosition);
                bounds.x = worldPosition.x;
                bounds.y = worldPosition.y;

                // TODO: For some reason CSS minWidth/minHeight isn't being respected. 
                // Maybe I need to wait for CSS to load before setting bounds?
                bounds.width = 150 - (padding * 2);
                bounds.height = 100 - (padding * 3);
            }

            bounds.x -= padding;
            bounds.y -= padding * 2;
            bounds.width += padding * 2;
            bounds.height += padding * 3;

            // Add the model
            var comment = new Comment();
            comment.Text = "New Comment";
            comment.Region = bounds;

            Graph.Comments.Add(comment);
            serializedGraph.Update();
            EditorUtility.SetDirty(Graph);

            // Add the view
            var commentView = new CommentView(comment);
            commentViews.Add(commentView);
            AddElement(commentView);

            Dirty(commentView);

            // Focus the title editor on first load
            commentView.EditTitle();
        }

        /// <summary>
        /// Remove a comment from both the canvas view and the graph model
        /// </summary>
        public void RemoveComment(CommentView comment)
        {
            Undo.RegisterCompleteObjectUndo(Graph, "Delete Comment");

            // Remove the model
            Graph.Comments.Remove(comment.Target);
            serializedGraph.Update();
            EditorUtility.SetDirty(Graph);

            // Remove the view
            RemoveElement(comment);
            commentViews.Remove(comment);
        }

        /// <summary>
        /// Handler to deserialize a string back into a CopyPasteGraph
        /// </summary>
        private void OnUnserializeAndPaste(string operationName, string data)
        {
            Undo.RegisterCompleteObjectUndo(Graph, "Paste Subgraph");

            var cpg = CopyPasteGraph.Deserialize(data, searchWindow.IncludeTags);
            Graph.AddNodes(cpg.Nodes);

            foreach (var comment in cpg.Comments)
            {
                Graph.Comments.Add(comment);
            }

            serializedGraph.Update();
            EditorUtility.SetDirty(Graph);

            // Add views for all the new elements
            ClearSelection();
            AddNodeViews(cpg.Nodes, true, true);
            AddCommentViews(cpg.Comments);

            ScriptableObject.DestroyImmediate(cpg);
        }

        private bool OnTryPasteSerializedData(string data)
        {
            return CopyPasteGraph.CanDeserialize(data);
        }

        /// <summary>
        /// Serialize a selection to support cut/copy/duplicate
        /// </summary>
        private string OnSerializeGraphElements(IEnumerable<GraphElement> elements)
        {
            return CopyPasteGraph.Serialize(elements);
        }

        /// <summary>
        /// Replacement of the base AddElement() to undo the hardcoded border 
        /// style that's overriding USS files. Should probably report this as dumb. 
        /// </summary>
        public new void AddElement(GraphElement graphElement)
        {
            // See: https://github.com/Unity-Technologies/UnityCsReference/blob/02d565cf3dd0f6b15069ba976064c75dc2705b08/Modules/GraphViewEditor/Views/GraphView.cs#L1222

            var borderBottomWidth = graphElement.style.borderBottomWidth;
            base.AddElement(graphElement);

            if (graphElement.IsResizable())
            {
                graphElement.style.borderBottomWidth = borderBottomWidth;
            }
        }

        public override List<GraphViewPort> GetCompatiblePorts(GraphViewPort startPort, NodeAdapter nodeAdapter)
        {
            var compatiblePorts = new List<GraphViewPort>();
            var startPortView = startPort as PortView;

            ports.ForEach((port) =>
            {
                var portView = port as PortView;
                if (portView.IsCompatibleWith(startPortView))
                {
                    compatiblePorts.Add(portView);
                }
            });

            return compatiblePorts;
        }
    }
}
