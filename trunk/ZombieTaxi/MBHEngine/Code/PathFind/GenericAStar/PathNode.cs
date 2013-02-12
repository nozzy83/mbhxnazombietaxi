using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MBHEngine.PathFind.GenericAStar
{
    /// <summary>
    /// Wrapper for a GraphNode with additional data detailing the path and cost data
    /// for travelling to and from this node.
    /// </summary>
    public class PathNode
    {
        /// <summary>
        /// The main object this class wraps.
        /// </summary>
        private GraphNode mGraphNode;

        /// <summary>
        /// The estimated 'cost' to travel from this GraphNode to the destination.
        /// </summary>
        private Single mCostToDestination;

        /// <summary>
        /// The actual 'cost' to travel from the starting point to this GraphNode
        /// along mPrevious.
        /// </summary>
        private Single mCostFromStart;

        /// <summary>
        /// Points back to the previous PathNode that should be taken to reach this
        /// point in what is currently known to be the shortest path.
        /// </summary>
        private PathNode mPrevious;

        /// <summary>
        /// Used by the client to keep track of whether or not the object using this path has 
        /// reached this node yet.
        /// </summary>
        private Boolean mReached;

        /// <summary>
        /// If this is the start of the current best path, than this will be true or false, to
        /// indicate whether or not a complete path to the destination has been found.
        /// </summary>
        private Boolean mPathSolved;

        /// <summary>
        /// Constructor.
        /// </summary>
        public PathNode()
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="node">The GraphNode this PathNode will wrap.</param>
        public PathNode(GraphNode node)
        {
            mGraphNode = node;
        }

        /// <summary>
        /// These nodes get used over and over again, so we need to make sure they 
        /// get completely reset between uses.
        /// </summary>
        public void Reset()
        {
            mGraphNode = null;
            mPrevious = null;
            mCostFromStart = 0;
            mCostToDestination = 0;
            mReached = false;
            mPathSolved = false;
        }

        /// <summary>
        /// The combined const to travel from the start of the path to this Node, and the
        /// estimated cost to travel from here to the destination.
        /// </summary>
        public Single pFinalCost
        {
            get
            {
                return mCostToDestination + mCostFromStart;
            }
        }

        /// <summary>
        /// The cost to travel to this point along linked mPrevious nodes.
        /// </summary>
        public Single pCostFromStart
        {
            get
            {
                return mCostFromStart;
            }
            set
            {
                mCostFromStart = value;
            }
        }

        /// <summary>
        /// An estimated cost to reach the destination.
        /// </summary>
        public Single pCostToEnd
        {
            get
            {
                return mCostToDestination;
            }
            set
            {
                mCostToDestination = value;
            }
        }

        /// <summary>
        /// Direct access to the GraphNode this PathNode wraps.
        /// </summary>
        public GraphNode pGraphNode
        {
            get
            {
                return mGraphNode;
            }
            set
            {
                mGraphNode = value;
            }
        }

        /// <summary>
        /// The PathNode that comes before this one when travelling from the start of
        /// the path along the currently known shortest route.
        /// </summary>
        public PathNode pPrevious
        {
            get
            {
                return mPrevious;
            }
            set
            {
                mPrevious = value;
            }
        }

        /// <summary>
        /// Bit of a hack used by clients to keep track of whether or not this point in the path
        /// has been reached yet, in regards to the object actually travelling down it.
        /// </summary>
        public Boolean pReached
        {
            get
            {
                return mReached;
            }
            set
            {
                mReached = value;
            }
        }

        /// <summary>
        /// Bit of a hack to tell clients whether or not this node is part of a solved path. This is
        /// only ever set for the end node.
        /// </summary>
        public Boolean pPathSolved
        {
            get
            {
                return mPathSolved;
            }
            set
            {
                mPathSolved = value;
            }
        }
    }
}
