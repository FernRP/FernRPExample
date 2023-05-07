using System.Collections.Generic;

namespace FernGraph.Editor
{
    /// <summary>
    /// Interface for a search provider that yields nodes that can be added to a graph.
    /// 
    /// All providers are instantiated the first time a graph is loaded in 
    /// the canvas editor and are reused for graphs that pass IsSupported().
    /// </summary>
    public interface ISearchProvider
    {
        /// <summary>
        /// Is this search provider supported on the given graph.
        /// 
        /// This is checked every time a graph is loaded or reloaded in the Canvas View.
        /// </summary>
        bool IsSupported(IGraph graph);

        /// <summary>
        /// Get results for the given search paramaeters
        /// </summary>
        IEnumerable<SearchResult> GetSearchResults(SearchFilter filter);

        Node Instantiate(SearchResult result);
    }

    public class SearchResult
    {
        public string Name { get; set; }

        public IEnumerable<string> Path { get; set; }

        public object UserData { get; set; }

        public ISearchProvider Provider { get; set; }
    }

    public class SearchFilter
    {
        /// <summary>
        /// The graph instance we're searching on
        /// </summary>
        public IGraph Graph { get; set; }

        /// <summary>
        /// If the user is dragging a port out to search for nodes
        /// that are compatible, this is that source port.
        /// </summary>
        public Port SourcePort { get; set; }

        /// <summary>
        /// List of tags in the Graph's [IncludeTags] attribute
        /// </summary>
        public IEnumerable<string> IncludeTags { get; set; }
    }
}
