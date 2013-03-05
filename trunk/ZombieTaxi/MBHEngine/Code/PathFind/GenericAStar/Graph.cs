using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MBHEngine.Debug;
using Microsoft.Xna.Framework;
using MBHEngine.Render;

namespace MBHEngine.PathFind.GenericAStar
{
    /// <summary>
    /// Stores a complete set off all GraphNode data. This is not actually needed for path planning,
    /// as the GraphNodes hold all the information and links they need, but the Graph is a convenient
    /// place to store them all in one place, and hold onto references to them all.
    /// </summary>
    public class Graph
    {
        /// <summary>
        /// A list of all nodes in the Graph. For the most part this entire list isn't needed
        /// since nodes link to one another, but there are cases where the entire list is needed.
        /// </summary>
        private List<GraphNode> mNodes;

        /// <summary>
        /// Construcor.
        /// </summary>
        public Graph()
        {
            mNodes = new List<GraphNode>(64);
        }

        /// <summary>
        /// Adds a GraphNode to the Graph.
        /// </summary>
        /// <param name="node">The node to add.</param>
        public virtual void AddNode(GraphNode node)
        {
            mNodes.Add(node);
        }

        /// <summary>
        /// Removes a GraphNode from the Graph.
        /// </summary>
        /// <param name="node"></param>
        public virtual void RemoveNode(GraphNode node)
        {
            mNodes.Remove(node);
        }

        /// <summary>
        /// Visualize what this graph looks like in world space.
        /// </summary>
        /// <param name="showLinks">True to show links between GraphNode objects.</param>
        public virtual void DebugDraw(Boolean showLinks)
        {
            Vector2 center = CameraManager.pInstance.pViewRect.pCenterPoint;

            // Loop through al the nodes.
            for (Int32 i = 0; i < mNodes.Count; i++)
            {
                // Only draw nodeds that are close to the center of the screen.
                if (Vector2.DistanceSquared(center, mNodes[i].pPosition) < 50 * 50) //80
                {
                    DrawNode(mNodes[i], showLinks);
                }
            }
        }

        /// <summary>
        /// Renders a single GraphNode and optionally links to all it's neighbours. Derived Graph classes should try
        /// to use this function even if overriding DebugDraw so that there is a consistent look to Graph objects.
        /// </summary>
        /// <param name="node">The GraphNode to render.</param>
        /// <param name="showLinks">True to draw links to neighbouring GraphNode objects.</param>
        protected virtual void DrawNode(GraphNode node, Boolean showLinks)
        {                    
            // Draw the node a circle.
            DebugShapeDisplay.pInstance.AddCircle(node.pPosition, 4.0f, Color.DarkRed);

            // If requested show the links between this node and all his neighbours.
            if (showLinks)
            {
                for (Int32 j = 0; j < node.pNeighbours.Count; j++)
                {
                    Color costColor = Color.Lerp(Color.White, Color.Red, (node.pNeighbours[j].mCostToTravel - 8.0f) / (3.0f * 11.134f));
                    DebugShapeDisplay.pInstance.AddSegment(node.pPosition, node.pNeighbours[j].mGraphNode.pPosition, costColor);
                }
            }
        }

        /// <summary>
        /// Direct access to the list of all nodes in the Graph.
        /// </summary>
        public List<GraphNode> pNodes
        {
            get
            {
                return mNodes;
            }
        }
    }
}
