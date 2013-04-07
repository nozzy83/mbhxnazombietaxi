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
        /// Static collection of Neighbours so that we don't make allocations during gameplay, and instead
        /// allocate a huge chunk of them on startup, and recycle them as needed.
        /// </summary>
        static private Queue<Neighbour> mUnusedNeighbours;

#if DEBUG
        /// <summary>
        /// Used for debugging to track if a node is actually currently being used (or does it think it is).
        /// </summary>
        public Boolean mInUse;
#endif // DEBUG

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

            /// <summary>
            /// Put this object into a default state.
            /// </summary>
            public void Reset()
            {
                mGraphNode = null;
                mCostToTravel = 0;
            }
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

            // The first GraphNode to be created will take the hit and allocate all Neighbour objects.
            if (mUnusedNeighbours == null)
            {
                // We need a LOT of Neighbours...
                const Int32 num = 500000;

                mUnusedNeighbours = new Queue<Neighbour>(num);

                for (Int32 i = 0; i < num; i++)
                {
                    Neighbour temp = new Neighbour();

                    mUnusedNeighbours.Enqueue(temp);
                }
            }
        }

        /// <summary>
        /// Returns a Node back to its default state. Needed for Nodes used in Factory.
        /// </summary>
        public virtual void Reset()
        {
            System.Diagnostics.Debug.Assert(pNeighbours.Count == 0, "Reseting node without empty Neighbours.");
            pData = null;
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
            //HPAStar.NavMesh.DebugCheckNode(node);

            // Create a new neighbour to wrap the node passed in.
            Neighbour temp = mUnusedNeighbours.Dequeue();

            // Put it back into a default state.
            temp.Reset();

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

            System.Diagnostics.Debug.Assert(node.pData != null, "Uninitialized node set as neighbour");

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
                    //HPAStar.NavMesh.DebugCheckNode(mNeighbours[i].mGraphNode);

                    mNeighbours[i].Reset();
                    
                    // Put this Neighbour back into the Queue so others can reuse it.
                    mUnusedNeighbours.Enqueue(mNeighbours[i]);

                    mNeighbours.RemoveAt(i);

                    // Should be no dupes so return after finding one.
                    return;
                }
            }
        }

        /// <summary>
        /// Helper method for checking if any of the Neighbours of this GraphNode contain a 
        /// supplied GraphNode.
        /// </summary>
        /// <param name="neighbour"></param>
        /// <returns></returns>
        public virtual Boolean HasNeighbour(GraphNode neighbour)
        {
            for (Int32 i = 0; i < mNeighbours.Count; i++)
            {
                if (mNeighbours[i].mGraphNode == neighbour)
                {
                    return true;
                }
            }

            return false;
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
        public abstract object pData { get; set; }

        /// <summary>
        /// Used for debug display.
        /// </summary>
        static public Int32 pNumUnusedNeighbours
        {
            get
            {
                return mUnusedNeighbours.Count;
            }
        }
    }
}
