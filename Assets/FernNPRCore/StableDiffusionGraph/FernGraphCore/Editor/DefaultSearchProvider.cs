using System;
using System.Collections.Generic;
using System.Linq;

namespace FernGraph.Editor
{
    /// <summary>
    /// Default implementation of a SearchProvider for nodes. 
    /// 
    /// This provider provides all nodes found via NodeReflection.
    /// </summary>
    public class DefaultSearchProvider : ISearchProvider
    {
        public bool IsSupported(IGraph graph)
        {
            return true;
        }

        public IEnumerable<SearchResult> GetSearchResults(SearchFilter filter)
        {
            foreach (var entry in NodeReflection.GetNodeTypes())
            {
                var node = entry.Value;
                if (
                    IsCompatible(filter.SourcePort, node) && 
                    IsInSupportedTags(filter.IncludeTags, node.Tags)
                ) {
                    yield return new SearchResult
                    {
                        Name = node.Name,
                        Path = node.Path,
                        UserData = node,
                    };
                }
            }
        }

        public Node Instantiate(SearchResult result)
        {
            NodeReflectionData data = result.UserData as NodeReflectionData;
            return data.CreateInstance();
        }
        
        /// <summary>
        /// Returns true if the intersection between the tags and our allow
        /// list has more than one tag, OR if our allow list is empty.
        /// </summary>
        private bool IsInSupportedTags(IEnumerable<string> supported, IEnumerable<string> tags)
        {
            // If we have no include list, allow anything.
            if (supported.Count() < 1)
            {
                return true;
            }

            // Otherwise - only allow if at least one tag intersects. 
            return supported.Intersect(tags).Count() > 0;
        }

        private bool IsCompatible(Port sourcePort, NodeReflectionData node)
        {
            if (sourcePort == null)
            {
                return true;
            }

            if (sourcePort.Direction == PortDirection.Input)
            {
                return node.HasOutputOfType(sourcePort.Type);
            }

            return node.HasInputOfType(sourcePort.Type);
        }
    }
}
