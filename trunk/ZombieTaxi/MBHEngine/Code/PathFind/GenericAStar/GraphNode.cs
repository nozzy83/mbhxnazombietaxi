using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MBHEngine.Behaviour;
using Microsoft.Xna.Framework;

namespace MBHEngine.PathFind.GenericAStar
{
    /// <summary>
    /// A single node in the search graph. It knows about the Level.Tile that it is on
    /// top of, and all of the other GraphNodes that it connects to directly.
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
        /// <param name="node"></param>
        public void AddNeighbour(GraphNode node)
        {
            // Create a new neighbour to wrap the node passed in.
            Neighbour temp = new Neighbour();

            temp.mGraphNode = node;

            // This assumes that nodes never move.
            temp.mCostToTravel = Vector2.Distance(node.pPosition, pPosition);

            mNeighbours.Add(temp);
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
