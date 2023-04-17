using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor.Experimental.GraphView;
using UnityEditor.Experimental;

namespace BlueGraph.Editor
{
    public class SearchWindow : ScriptableObject, ISearchWindowProvider
    {

        public CanvasView Target { get; set; }

        public PortView SourcePort { get; set; }

        /// <summary>
        /// If non-empty, only nodes with these tags may be included in search results.
        /// </summary>
        public List<string> IncludeTags { get; set; } = new List<string>();

        private readonly HashSet<ISearchProvider> providers = new HashSet<ISearchProvider>();
        
        public void ClearTags()
        {
            IncludeTags.Clear();
        }

        public void ClearSearchProviders()
        {
            providers.Clear();
        }

        public void AddSearchProvider(ISearchProvider provider)
        {
            providers.Add(provider);
        }

        private IEnumerable<SearchResult> FilterSearchProviders(SearchFilter filter)
        {
            foreach (var provider in providers)
            {
                foreach (var result in provider.GetSearchResults(filter))
                {
                    result.Provider = provider;
                    yield return result;
                }
            }
        }

        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            var filter = new SearchFilter
            {
                Graph = Target.Graph,
                SourcePort = SourcePort?.Target,
                IncludeTags = IncludeTags
            };

            // First item is the title of the window
            var tree = new List<SearchTreeEntry>();
            tree.Add(new SearchTreeGroupEntry(new GUIContent("Add Node"), 0));
            
            // Construct a tree of available nodes by module path
            var groups = new SearchGroup(1);
            
            // Aggregate search providers and get nodes matching the filter
            foreach (var result in FilterSearchProviders(filter))
            {
                var path = result.Path;
                var group = groups;

                if (path != null)
                {
                    // If a path is defined, drill down into nested
                    // SearchGroup entries until we find the matching directory
                    foreach (var directory in path)
                    {
                        if (!group.Subgroups.ContainsKey(directory))
                        {
                            group.Subgroups.Add(directory, new SearchGroup(group.Depth + 1));
                        }

                        group = group.Subgroups[directory];
                    }
                }

                group.Results.Add(result);
            }
            
            groups.AddToTree(tree);

            return tree;
        }

        public bool OnSelectEntry(SearchTreeEntry entry, SearchWindowContext context)
        {
            var result = entry.userData as SearchResult;
            var node = result.Provider.Instantiate(result);

            Target.AddNodeFromSearch(
                node,
                context.screenMousePosition, 
                SourcePort
            );

            return true;
        }
    }
    
    internal class SearchGroup
    {
        private static Texture folderIcon = EditorResources.Load<Texture>("d_Folder Icon");

        public SearchGroup(int depth)
        {
            Depth = depth;
        }
            
        internal int Depth { get; private set; }

        internal SortedDictionary<string, SearchGroup> Subgroups { get; } = new SortedDictionary<string, SearchGroup>();

        internal List<SearchResult> Results { get; } = new List<SearchResult>();

        internal void AddToTree(List<SearchTreeEntry> tree)
        {
            SearchTreeEntry entry;
                
            // Add subgroups
            foreach (var group in Subgroups)
            {
                GUIContent content = new GUIContent(" " + group.Key, folderIcon);
                entry = new SearchTreeGroupEntry(content)
                {
                    level = Depth
                };

                tree.Add(entry);
                group.Value.AddToTree(tree);
            }

            // Add nodes
            foreach (var result in Results)
            {
                GUIContent content = new GUIContent("      " + result.Name);
                entry = new SearchTreeEntry(content)
                {
                    level = Depth,
                    userData = result
                };

                tree.Add(entry);
            }
        }
    }
}
