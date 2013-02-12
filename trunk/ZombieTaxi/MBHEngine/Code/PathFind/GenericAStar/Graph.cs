using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MBHEngine.Debug;
using Microsoft.Xna.Framework;

namespace MBHEngine.PathFind.GenericAStar
{
    /// <summary>
    /// Stores a complete set off all GraphNode data. This gets fed into a Planner.
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
        public void AddNode(GraphNode node)
        {
            mNodes.Add(node);
        }

        /// <summary>
        /// Visualize what this graph looks like in world space.
        /// </summary>
        /// <param name="showLinks">True to show links between GraphNode objects.</param>
        public void DebugDraw(Boolean showLinks)
        {
            // Loop through al the nodes.
            for (Int32 i = 0; i < mNodes.Count; i++)
            {
                // Draw the node a circle.
                DebugShapeDisplay.pInstance.AddCircle(mNodes[i].pPosition, 4.0f, Color.DarkRed);

                // If requested show the links between this node and all his neighbours.
                if (showLinks)
                {
                    for (Int32 j = 0; j < mNodes[i].pNeighbours.Count; j++)
                    {
                        DebugShapeDisplay.pInstance.AddSegment(mNodes[i].pPosition, mNodes[i].pNeighbours[j].mGraphNode.pPosition, Color.DarkRed);
                    }
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
