using UnityEngine;
using UnityEditor.Experimental.GraphView;
using GraphViewEdge = UnityEditor.Experimental.GraphView.Edge;

namespace BlueGraph.Editor
{
    /// <summary>
    /// Custom connector listener so that we can link up nodes and 
    /// open a search box when the user drops an edge into the canvas
    /// </summary>
    public class EdgeConnectorListener : IEdgeConnectorListener
    {
        private CanvasView canvas;
    
        public EdgeConnectorListener(CanvasView canvas)
        {
            this.canvas = canvas;
        }
    
        /// <summary>
        /// Handle connecting nodes when an edge is dropped between two ports
        /// </summary>
        public void OnDrop(GraphView graphView, GraphViewEdge edge)
        {
            canvas.AddEdge(edge, true);
        }

        /// <summary>
        /// Activate the search dialog when an edge is dropped on an arbitrary location
        /// </summary>
        public void OnDropOutsidePort(GraphViewEdge edge, Vector2 position)
        {
            var screenPosition = GUIUtility.GUIToScreenPoint(
                Event.current.mousePosition
            );
            
            if (edge.output != null)
            {
                canvas.OpenSearch(
                    screenPosition, 
                    edge.output.edgeConnector.edgeDragHelper.draggedPort as PortView
                );
            }
            else if (edge.input != null)
            {
                canvas.OpenSearch(
                    screenPosition, 
                    edge.input.edgeConnector.edgeDragHelper.draggedPort as PortView
                );
            }
        }
    }
}
