using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Suite of reflection methods and caching for retrieving available
/// graph nodes and their associated editor views
/// </summary>
namespace FernGraph.Editor
{
    /// <summary>
    /// Reflection data for a field with an [Input] or [Output] attribute
    /// </summary>
    public class PortReflectionData
    {
        /// <summary>
        /// Associated class field if generated via Input/Output attributes
        /// </summary>
        public FieldInfo Field { get; set; }

        /// <summary>
        /// Display name for this port
        /// </summary>
        public string Name { get; set; }

        public Type Type { get; set; }

        public PortDirection Direction { get; set; }

        public PortCapacity Capacity { get; set; }

        public bool HasControlElement { get; set; }

        /// <summary>
        /// Is this.name just the this.field or set via the attribute
        /// </summary>
        public bool IsUsingFieldName { get; set; }

        /// <summary>
        /// Create a VisualElement for this port's inline editor based on the field data type.
        ///
        /// This returns null if the port is not marked as <c>isEditable</c> or the type
        /// could not be resolved to a supported control element.
        /// </summary>
        public VisualElement GetControlElement(NodeView view)
        {
            if (!HasControlElement)
            {
                return null;
            }

            return ControlElementFactory.CreateControl(Field, view);
        }
    }

    /// <summary>
    /// Reflection data for a field with an [Editable] attribute.
    /// </summary>
    public class EditableReflectionData
    {
        public string Name { get; set; }

        public FieldInfo Field { get; set; }

        /// <summary>
        /// Create a VisualElement for this editable field's inline editor based on the field data type.
        ///
        /// This returns null if the type could not be resolved to a supported control element.
        /// </summary>
        public VisualElement GetControlElement(NodeView view)
        {
            return ControlElementFactory.CreateControl(Field, view, Name);
        }
    }

    /// <summary>
    /// Reflection data for a class with a [Node] attribute
    /// </summary>
    public class NodeReflectionData
    {
        /// <summary>
        /// Class type to instantiate for the node
        /// </summary>
        public Type Type { get; set; }

        /// <summary>
        /// Module path for grouping nodes together in the search
        /// </summary>
        public IEnumerable<string> Path { get; set; }

        /// <summary>
        /// List of tags associated with a Node
        /// </summary>
        public List<string> Tags { get; set; } = new List<string>();

        /// <summary>
        /// Human-readable display name of the node. Will come from the last
        /// part of the path parsed out of node information - or be the class name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Content for node usage instructions
        /// </summary>
        public string Help { get; set; }

        /// <summary>
        /// Can this node be deleted from the graph
        /// </summary>
        public bool Deletable { get; set; }
        /// <summary>
        /// Can this node be moved in from graph
        /// </summary>
        public bool Moveable { get; set; }

        /// <summary>
        /// Metadata about ports declared through <see cref="InputAttribute"/>
        /// and <see cref="OutputAttribute"/> on fields.
        /// </summary>
        public List<PortReflectionData> Ports { get; set; } = new List<PortReflectionData>();

        /// <summary>
        /// Metadata about editables declared through <see cref="EditableAttribute"/> on fields.
        /// </summary>
        public List<EditableReflectionData> Editables { get; set; } = new List<EditableReflectionData>();

        /// <summary>
        /// Cache of FieldInfo entries on the node class
        /// </summary>
        public List<FieldInfo> Fields { get; set; } = new List<FieldInfo>();

        /// <summary>
        /// Implemented NodeView Type.
        /// <para>
        /// Null if there is no NodeView implemented for this node <see cref="Type"/>
        /// </para>
        /// </summary>
        public Type EditorType { get; private set; }

        /// <summary>
        /// Implemented Script of the node <seealso cref="Type"/>.
        ///
        /// You can use GetClass() to get the implemented class of the script
        /// </summary>
        public MonoScript NodeScript { get; private set; }

        /// <summary>
        /// NodeView Base of the implementations of node.
        ///
        /// You can use GetClass() to get the implemented class of the script
        ///
        /// <para>
        /// Returns null if there is no NodeView implemented for this node <see cref="Type"/>
        /// </para>
        /// </summary>
        public MonoScript NodeViewScript { get; private set; }

        private Dictionary<ContextMenu, MethodInfo> contextMethods;

        /// <summary>
        /// All <seealso cref="MethodInfo"/>'s and <seealso cref="ContextMenu"/>'s of the node.
        ///
        /// Use this to see all methods that using ContexMethos Atrribute it this node type
        /// <para></para>
        /// If there are none, an empty Dictionary will be returned
        /// </summary>
        public IReadOnlyDictionary<ContextMenu, MethodInfo> ContextMethods {
            get {
                return contextMethods;
            }
        }

        public NodeReflectionData(Type type, NodeAttribute nodeAttr)
        {
            Type = type;
            Name = nodeAttr.Name ?? ObjectNames.NicifyVariableName(type.Name);
            Path = nodeAttr.Path?.Split('/');
            Help = nodeAttr.Help;
            Deletable = nodeAttr.Deletable;
            Moveable = nodeAttr.Moveable;
            EditorType = NodeReflection.GetNodeEditorType(type);
            contextMethods = new Dictionary<ContextMenu, MethodInfo>();

            var attrs = type.GetCustomAttributes(true);
            foreach (var attr in attrs)
            {
                if (attr is TagsAttribute tagAttr)
                {
                    // Load any tags associated with the node
                    Tags.AddRange(tagAttr.Tags);
                }
                else if (attr is OutputAttribute output)
                {
                    // Load any Outputs defined at the class level
                    Ports.Add(new PortReflectionData()
                    {
                        Name = output.Name,
                        Type = output.Type,
                        Direction = PortDirection.Output,
                        Capacity = output.Multiple ? PortCapacity.Multiple : PortCapacity.Single,
                        HasControlElement = false
                    });
                }
            }

            // Load additional data from class fields
            AddFieldsFromClass(type);
            SetScriptNodeType();
            SetScriptNodeViewType();
            LoadContextMethods();
        }

        public bool HasInputOfType(Type type)
        {
            foreach (var port in Ports)
            {
                if (port.Direction == PortDirection.Output) continue;

                // Cast direction type -> port input
                if (type.IsCastableTo(port.Type, true))
                {
                    return true;
                }
            }

            return false;
        }

        public bool HasOutputOfType(Type type)
        {
            foreach (var port in Ports)
            {
                if (port.Direction == PortDirection.Input) continue;

                // Cast direction port output -> type
                if (port.Type.IsCastableTo(type, true))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Add ports based on attributes on the class fields.
        ///
        /// This iterates through fields of a class and adds ports, editable fields, etc
        /// based on the attributes attached to each field.
        /// </summary>
        public void AddFieldsFromClass(Type type)
        {
            Fields.AddRange(type.GetFields(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
            ));

            // Extract port and editable metadata from each tagged field
            for (int i = 0; i < Fields.Count; i++)
            {
                var attribs = Fields[i].GetCustomAttributes(true);
                for (int j = 0; j < attribs.Length; j++)
                {
                    if (attribs[j] is InputAttribute input)
                    {
                        Ports.Add(new PortReflectionData()
                        {
                            Name = input.Name ?? Fields[i].Name,
                            Field = Fields[i],
                            Type = Fields[i].FieldType,
                            Direction = PortDirection.Input,
                            Capacity = input.Multiple ? PortCapacity.Multiple : PortCapacity.Single,
                            HasControlElement = input.Editable,
                            IsUsingFieldName = input.Name != null
                        });
                    }
                    else if (attribs[j] is OutputAttribute output)
                    {
                        Ports.Add(new PortReflectionData()
                        {
                            Name = output.Name ?? Fields[i].Name,
                            Field = Fields[i],
                            Type = Fields[i].FieldType,
                            Direction = PortDirection.Output,
                            Capacity = output.Multiple ? PortCapacity.Multiple : PortCapacity.Single,
                            HasControlElement = false,
                            IsUsingFieldName = output.Name != null
                        });
                    }
                    else if (attribs[j] is EditableAttribute editable)
                    {
                        Editables.Add(new EditableReflectionData()
                        {
                            Name = editable.Name ?? Fields[i].Name,
                            Field = Fields[i]
                        });
                    }
                }
            }
        }

        /// <summary>
        /// Create a node instance from the reflected type data
        /// </summary>
        public Node CreateInstance()
        {
            var node = Activator.CreateInstance(Type) as Node;
            node.Name = Name;

            // Create runtime ports from reflection data
            foreach (var port in Ports)
            {
                node.AddPort(new Port
                {
                    Type = port.Type,
                    Name = port.Name,
                    Capacity = port.Capacity,
                    Direction = port.Direction
                });
            }

            return node;
        }

        public PortReflectionData GetPortByName(string name)
        {
            return Ports.Find((port) => port.Name == name);
        }

        private void SetScriptNodeType()
        {
            NodeScript = FindScriptFromClassName(Type.Name);

            if (NodeScript == null)
            {
                NodeScript = FindScriptFromClassName(Type.Name + "Node");
            }
        }

        private void SetScriptNodeViewType()
        {
            if (EditorType == null)
            {
                return;
            }

            // Try find the class name with View Or NodeView name at the end
            NodeViewScript = FindEditorScriptFromClassName(EditorType.Name);
            if (NodeViewScript == null)
            {
                NodeViewScript = FindEditorScriptFromClassName(EditorType.Name + "View");
            }

            if (NodeViewScript == null)
            {
                NodeViewScript = FindEditorScriptFromClassName(EditorType.Name + "NodeView");
            }
        }

        private void LoadContextMethods()
        {
            foreach (var method in Type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                var contextAttr = method.GetCustomAttribute<ContextMenu>();
                if (contextAttr != null)
                {
                    contextMethods.Add(contextAttr, method);
                }
            }
        }

        private MonoScript FindScriptFromClassName(string className)
        {
            var scriptGUIDs = AssetDatabase.FindAssets($"t:script {className}");

            if (scriptGUIDs.Length == 0)
                return null;

            foreach (var scriptGUID in scriptGUIDs)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(scriptGUID);
                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(assetPath);

                if (script != null && string.Equals(className, System.IO.Path.GetFileNameWithoutExtension(assetPath), StringComparison.OrdinalIgnoreCase))
                {
                    if (script.GetClass().Namespace == Type.Namespace)
                        return script;
                }
            }

            return null;
        }

        private MonoScript FindEditorScriptFromClassName(string className)
        {
            var scriptGUIDs = AssetDatabase.FindAssets($"t:script {className}");

            if (scriptGUIDs.Length == 0)
                return null;

            foreach (var scriptGUID in scriptGUIDs)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(scriptGUID);
                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(assetPath);

                if (script != null && string.Equals(className, System.IO.Path.GetFileNameWithoutExtension(assetPath), StringComparison.OrdinalIgnoreCase))
                {
                    if (script.GetClass().Namespace == EditorType.Namespace)
                        return script;
                }
            }

            return null;
        }

        public override string ToString()
        {
            var inputs = new List<string>();
            var outputs = new List<string>();

            foreach (var port in Ports)
            {
                if (port.Direction == PortDirection.Input)
                {
                    inputs.Add(port.Name);
                }
                else if (!port.HasControlElement)
                {
                    outputs.Add(port.Name);
                }
            }

            return $"<{Name}, IN: {string.Join(", ", inputs)}, OUT: {string.Join(", ", outputs)}>";
        }
    }

    public static class NodeReflection
    {
        private static Dictionary<string, NodeReflectionData> cachedReflectionMap = null;

        /// <summary>
        /// Mapping between an AbstractNode type (key) and a custom editor type (value)
        /// </summary>
        private static Dictionary<Type, Type> cachedEditorMap = null;

        /// <summary>
        /// All search providers in the application that could be
        /// registered in the CanvasView for a graph
        /// </summary>
        private static List<ISearchProvider> cachedSearchProviders = null;

        public static List<ISearchProvider> SearchProviders
        {
            get
            {
                if (cachedSearchProviders == null)
                {
                    LoadSearchProviders();
                }

                return cachedSearchProviders;
            }
        }

        /// <summary>
        /// Retrieve reflection data for a given node class type
        /// </summary>
        public static NodeReflectionData GetNodeType(Type type)
        {
            var types = GetNodeTypes();
            types.TryGetValue(type.FullName, out NodeReflectionData result);
            return result;
        }

        /// <summary>
        /// Get all types derived from the base node
        /// </summary>
        public static Dictionary<string, NodeReflectionData> GetNodeTypes()
        {
            // Load cache if we got it
            if (cachedReflectionMap != null)
            {
                return cachedReflectionMap;
            }

            var baseType = typeof(Node);
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var nodes = new Dictionary<string, NodeReflectionData>();

            foreach (var assembly in assemblies)
            {

                foreach (var t in assembly.GetTypes())
                {
                    if (!t.IsAbstract && baseType.IsAssignableFrom(t))
                    {
                        // Aggregate [Node] inherited from baseType
                        var attr = t.GetCustomAttribute<NodeAttribute>();
                        if (attr != null)
                        {
                            nodes[t.FullName] = new NodeReflectionData(t, attr);
                        }


                    }
                }
            }

            cachedReflectionMap = nodes;
            return cachedReflectionMap;
        }

        public static Type GetNodeEditorType(Type type)
        {
            if (cachedEditorMap == null)
            {
                LoadNodeEditorTypes();
            }

            cachedEditorMap.TryGetValue(type, out Type editorType);
            if (editorType != null)
            {
                return editorType;
            }

            // If it's not found, go up the inheritance tree until we find one
            while (type != typeof(Node))
            {
                type = type.BaseType;

                cachedEditorMap.TryGetValue(type, out editorType);
                if (editorType != null)
                {
                    return editorType;
                }
            }

            // Default to the base node editor
            return typeof(NodeView);
        }

        /// <summary>
        /// Load and cache a mapping between AbstractNode classes and their
        /// NodeView editor equivalent, if a custom editor has been defined.
        /// </summary>
        private static void LoadNodeEditorTypes()
        {
            var baseType = typeof(NodeView);
            var types = new List<Type>();
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (var assembly in assemblies)
            {
                try
                {
                    types.AddRange(assembly.GetTypes().Where(
                        (t) => !t.IsAbstract && baseType.IsAssignableFrom(t)
                    ).ToArray());
                }
                catch (ReflectionTypeLoadException)
                {
                    // noop
                }
            }

            var nodeEditors = new Dictionary<Type, Type>();
            foreach (var t in types)
            {
                // We only look at direct attributes here for associations.
                // GetNodeEditorType() handles walking up the inheritance tree.
                var attrs = t.GetCustomAttributes<CustomNodeViewAttribute>(false);
                foreach (var attr in attrs)
                {
                    nodeEditors[attr.NodeType] = t;
                }
            }

            cachedEditorMap = nodeEditors;
        }

        private static void LoadSearchProviders()
        {
            // TODO: Combine with LoadNodeEditorTypes / GetNodeTypes
            // for a single assemblies scan. All three are typically
            // ran at the same time.
            cachedSearchProviders = new List<ISearchProvider>();

            var baseType = typeof(ISearchProvider);
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                try
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        if (!type.IsAbstract && baseType.IsAssignableFrom(type))
                        {
                            cachedSearchProviders.Add(
                                Activator.CreateInstance(type) as ISearchProvider
                            );
                        }
                    }
                }
                catch (ReflectionTypeLoadException)
                {
                    // noop
                }
            }
        }

        /// <summary>
        /// Instantiate a new node by type
        /// </summary>
        public static T Instantiate<T>() where T : Node
        {
            return GetNodeType(typeof(T)).CreateInstance() as T;
        }

        /// <summary>
        /// Instantiate a new node by type
        /// </summary>
        public static Node Instantiate(Type type)
        {
            return GetNodeType(type).CreateInstance();
        }

    }
}
