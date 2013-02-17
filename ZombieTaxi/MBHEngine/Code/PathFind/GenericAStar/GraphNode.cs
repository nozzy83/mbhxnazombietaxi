using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MBHEngine.Behaviour;
using Microsoft.Xna.Framework;

namespace MBHEngine.PathFind.GenericAStar
{
    /// <summary>
    /// A single node in the search graph. It knows nothing of what it is actually representing, but instead
    /// just knows about the neighbouring GraphNode objects. These links between GraphNode objects creating
    /// the paths which can be searching using a Planner object.
    /// </summary>
    public abstract class GraphNode
    {
        /// <summary>
        /// Neighbouring GraphNode objects get stored in this wrapper so that things like
        /// the cost to travel there can be cached.
        /// </summary>
        public class Neighbour
        {
            /// <summary>
            /// The GraphNode that this object wraps.
            /// </summary>
            public GraphNode mGraphNode;

            /// <summary>
            /// How much is costs to travel from the GraphNode containing this neighbour,
            /// to this neighbour. This is precomputed and chached for quicker lookups later.
            /// </summary>
            public Single mCostToTravel;
        }

        /// <summary>
        /// A list of all the GraphNodes that this GraphNode connects to directly, both
        /// inter-connections and intra-connections.
        /// </summary>
        private List<Neighbour> mNeighbours;

        /// <summary>
        /// Constructor.
        /// </summary>
        public GraphNode()
        {
            // On average we suspect there will be 8 neighbours.
            mNeighbours = new List<Neighbour>(8);
        }

        /// <summary>
        /// Adds a neighbour to this GraphNode.
        /// </summary>
        /// <param name="node">The neighbouring GraphNode.</param>
        /// <param name="cost">
        /// The cost to travel from this GraphNode to <paramref name="node"/>. If not supplied the cost will 
        /// be the distance between the two GraphNode objects.
        /// </param>
        public virtual void AddNeighbour(GraphNode node, Single cost = -1.0f)
        {
            // Create a new neighbour to wrap the node passed in.
            Neighbour temp = new Neighbour();

            temp.mGraphNode = node;

            if (cost >= 0)
            {
                temp.mCostToTravel = cost;
            }
            else
            {
                // This assumes that nodes never move.
                temp.mCostToTravel = Vector2.Distance(node.pPosition, pPosition);
            }

#if DEBUG
            for (Int32 i = 0; i < mNeighbours.Count; i++)
            {
                if (mNeighbours[i].mGraphNode == node)
                {
                    System.Diagnostics.Debug.Assert(false, "Dupe GraphNode added the neighbouring list.");
                }

                if (mNeighbours[i].mGraphNode.pPosition == node.pPosition)
                {
                    System.Diagnostics.Debug.Assert(false, "GraphNode at dupe position added the neighbouring list.");
                }
            }
#endif

            mNeighbours.Add(temp);
        }

        /// <summary>
        /// Removes a Neighbour by looking at the GraphNode stored in each one.
        /// </summary>
        /// <param name="neighbour">The GraphNode in the Neighbour that should be removed.</param>
        public virtual void RemoveNeighbour(GraphNode neighbour)
        {
            for (Int32 i = 0; i < mNeighbours.Count; i++)
            {
                if (mNeighbours[i].mGraphNode == neighbour)
                {
                    mNeighbours.RemoveAt(i);

                    // Should be no dupes so return after finding one.
                    return;
                }
            }
        }

        /// <summary>
        /// Check if this GraphNode can be passed; eg. is it solid of empty?
        /// </summary>
        /// <param name="startingNode">The node we are travelling from.</param>
        /// <returns>True if if the node can be travelled to.</returns>
        public abstract Boolean IsPassable(GraphNode startingNode);

        /// <summary>
        /// Check if this node can be travelled to.
        /// </summary>
        /// <returns></returns>
        public abstract Boolean IsEmpty();

        /// <summary>
        /// Where does this GraphNode sit in world space?
        /// </summary>
        public abstract Vector2 pPosition { get; }

        /// <summary>
        /// Access to a list of all the neighbours.
        /// </summary>
        public List<Neighbour> pNeighbours
        {
            get
            {
                return mNeighbours;
            }
        }

        /// <summary>
        /// GraphNode objects often wrap a piece of data (eg. a Level.Tile). pData is a generic way
        /// to access that data, where it can then be cast back to the type it is known to be.
        /// </summary>
        public abstract object pData { get; }
    }
}
