using System;
using UnityEngine;

namespace FernGraph
{
    /// <summary>
    /// A node that can be added to a Graph
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class NodeAttribute : Attribute
    {
        /// <summary>
        /// Display name of the node.
        ///
        /// If not supplied, this will be inferred based on the class name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Tooltip help content displayed for the node.
        /// </summary>
        public string Help { get; set; }

        /// <summary>
        /// Slash-delimited directory path to categorize this node in the search window.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Can this node be deleted from the graph.
        /// </summary>
        public bool Deletable { get; set; } = true;
        /// <summary>
        /// Can this node be moved in the graph.
        /// </summary>
        public bool Moveable { get; set; } = true;
        public NodeAttribute(string name = null)
        {
            Name = name;
        }
    }

    /// <summary>
    /// Tags associated with a Node. Can be used by a Graph's <c>[IncludeTags]</c>
    /// attribute to restrict what nodes can be added to the graph.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class TagsAttribute : Attribute
    {
        public string[] Tags { get; set; }

        public TagsAttribute(params string[] tags)
        {
            this.Tags = tags;
        }
    }

    /// <summary>
    /// An input port exposed on a Node
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class InputAttribute : Attribute
    {
        /// <summary>
        /// Display name of the input slot.
        ///
        /// If not supplied, this will default to the field name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Can this input accept multiple outputs at once.
        /// </summary>
        public bool Multiple { get; set; } = false;

        /// <summary>
        /// Can the associated field be directly modified when there are no connections.
        /// </summary>
        public bool Editable { get; set; } = true;

        public InputAttribute(string name = null)
        {
            Name = name;
        }
    }

    /// <summary>
    /// An output port exposed on a Node.
    ///
    /// This can either be defined on the class or associated with a specific field.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Class, AllowMultiple = true)]
    public class OutputAttribute : Attribute
    {
        /// <summary>
        /// Display name of the output slot.
        ///
        /// If not supplied, this will default to the field name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Can this output go to multiple inputs at once.
        /// </summary>
        public bool Multiple { get; set; } = true;

        /// <summary>
        /// If defined as a class attribute, this is the output type.
        ///
        /// When defined on a field, the output will automatically be inferred by the field.
        /// </summary>
        public Type Type { get; set; }

        public OutputAttribute(string name = null, Type type = null)
        {
            Name = name;
            Type = type;
        }
    }

    /// <summary>
    /// A field that can be edited directly from within the Canvas on a Node
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class EditableAttribute : Attribute
    {
        /// <summary>
        /// Display name of the editable field.
        ///
        /// If not supplied, this will be inferred based on the field name.
        /// </summary>
        public string Name { get; set; }

        public EditableAttribute(string name = null)
        {
            Name = name;
        }
    }

    /// <summary>
    /// Supported node tags for a given Graph.
    ///
    /// If defined, only nodes with a <c>[Tags]</c> attribute including
    /// one or more of these tags may be added to the Graph.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public class IncludeTagsAttribute : Attribute
    {
        public string[] Tags { get; set; }

        public IncludeTagsAttribute(params string[] tags)
        {
            Tags = tags;
        }
    }

    /// <summary>
    /// Required node for a given Graph.
    /// Will automatically instantiate the node when the graph is first created.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public class RequireNodeAttribute : Attribute
    {
        public Type type { get; private set; }
        public string nodeName { get; private set; }
        public Vector2 position { get; private set; }

        /// <summary>
        /// NodeType Required
        /// </summary>
        /// <param name="type">Type of the node</param>
        public RequireNodeAttribute(Type type)
        {
            this.type = type;

        }
        /// <summary>
        /// NodeType required
        /// </summary>
        /// <param name="type">Type of the node</param>
        /// <param name="nodeName">Header title name of the node at graph</param>
        public RequireNodeAttribute(Type type, string nodeName = "")
        {
            this.type = type;
            this.nodeName = nodeName;

        }

        /// <summary>
        /// NodeType required
        /// </summary>
        /// <param name="type">Type of the node</param>
        /// <param name="nodeName">Header title name of the node at graph</param>
        /// <param name="xPos">y position to creating</param>
        /// <param name="yPos">x position to creating</param>
        public RequireNodeAttribute(Type type, string nodeName = "", float xPos = 0, float yPos = 0)
        {
            this.type = type;
            this.nodeName = nodeName;
            position = new Vector2(xPos, yPos);

        }



    }

    /// <summary>
    /// Mark a node as deprecated and automatically migrate instances
    /// to a new class when encountered in the editor.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class DeprecatedAttribute : Attribute
    {
        public Type ReplaceWith { get; set; }
    }

    /// <summary>
    /// Mark a class inherited from <c>NodeView</c> as the primary view
    /// for a specific type of node.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class CustomNodeViewAttribute : Attribute
    {
        public Type NodeType { get; set; }

        public CustomNodeViewAttribute(Type nodeType)
        {
            NodeType = nodeType;
        }
    }
}
