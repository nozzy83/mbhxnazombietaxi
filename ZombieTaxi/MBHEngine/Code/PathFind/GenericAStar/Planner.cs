using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MBHEngine.PathFind.GenericAStar;
using Microsoft.Xna.Framework;
using MBHEngine.Debug;

namespace MBHEngine.PathFind.GenericAStar
{
    public class Planner
    {
        /// <summary>
        /// The different ways that the Planner can end an Update().
        /// </summary>
        public enum Result
        {
            Solved = 0,
            Failed,
            NotStarted,
        };

        /// <summary>
        /// Tracks all the PathNodes that have been discovered, but not yet fully explored.
        /// </summary>
        private List<PathNode> mOpenNodes = new List<PathNode>();

        /// <summary>
        /// Tracks all the PathNodes which have been discovered and explored.
        /// </summary>
        private List<PathNode> mClosedNodes = new List<PathNode>();

        /// <summary>
        /// A static stack of nodes shared by all PathFind Behaviour instances.  This is to avoid
        /// having to allocate new path nodes during gameplay and potentially triggering a GC.
        /// It is very important that when nodes are removed from the closed and open lists, they
        /// get Reset and pushed back onto this stack.
        /// </summary>
        private static Stack<PathNode> mUnusedNodes;

        /// <summary>
        /// The last PathNode in the current best path.
        /// </summary>
        private PathNode mBestPathEnd;

        /// <summary>
        /// The GraphNode we are travelling from.
        /// </summary>
        private GraphNode mStart;

        /// <summary>
        /// The GraphNode we are trying to get to.
        /// </summary>
        private GraphNode mEnd;

        /// <summary>
        /// Has this Planner finished solving the path.
        /// </summary>
        private Boolean mSolved;

        private Boolean mPathInvalidated;

        public Planner()
        {
            // mUnusedNodes is a static used by all instances of the behavior.  We only want to allocate
            // it once.
            if (mUnusedNodes == null)
            {
                mUnusedNodes = new Stack<PathNode>(10000);
                for (Int32 i = 0; i < 100000; i++)
                {
                    PathNode temp = new PathNode();
                    mUnusedNodes.Push(temp);
                }
            }

            mPathInvalidated = false;

            mBestPathEnd = null;

            mSolved = false;
        }

        public Result PlanPath()
        {            
            // If there is no tile at the destination then there is no path finding to do.
            if (mEnd == null)
            {
                return Result.NotStarted;
            }

            // If we have a destination draw it. Even if there isn't a source yet.
            DebugShapeDisplay.pInstance.AddPoint(mEnd.pPosition, 2.0f, Color.Yellow);

            // If the destination is a solid tile then we will never be able to solve the path.
            if (null != mEnd && !mEnd.IsEmpty())
            {
                // We consider this a failure, similar to if a destination was surrounded by solid.
                return Result.Failed;
            }

            // If our source position is not on a tile then there is no path finding to do.
            if (mStart == null)
            {
                return Result.NotStarted;
            }

            // If our source position is not on a tile, or that tile is solid we cannot ever solve
            // this path, so abort right away.
            if (mStart != null && !mStart.IsEmpty())
            {
                // Trying to path find to a solid tile is considered a failure.
                return Result.Failed;
            }

            // If there is a source, draw it.
            DebugShapeDisplay.pInstance.AddPoint(mStart.pPosition, 2.0f, Color.Orange);

            // If the path hasn't already been invalidated this frame, we need to check that
            // the path didn't get blocked from something like the Player placing blocks.
            // TODO: This could be changed to only do this check when receiving specific events,
            //       such as the ObjectPlacement Behaviour telling it that a new block has been
            //       placed.
            if (!mPathInvalidated)
            {
                // Loop through the current path and check for any tiles that are not
                // empty. If they aren't empty this path is no longer valid as there is 
                // something now blocking it.
                //
                PathNode node = mBestPathEnd;

                while (null != node)
                {
                    if (!node.pGraphNode.IsEmpty())
                    {
                        // Setting this flag will force the path finder to start from 
                        // the begining.
                        mPathInvalidated = true;

                        // No need to loop any further. One blockade is enough.
                        break;
                    }

                    node = node.pPrevious;
                }
            }

            // If the path has become invalid, we need to restart the pathing algorithm.
            if (mPathInvalidated)
            {
                ClearNodeLists();

                // First thing we need to do is add the first node to the open list.
                PathNode p = mUnusedNodes.Pop();
                p.pGraphNode = mStart;

                // There is no cost because it is the starting node.
                p.pCostFromStart = 0;

                // For H we use the actual distance to the destination.  The Manhattan Heuristic method.
                p.pCostToEnd = 8.0f * System.Math.Max(System.Math.Abs(
                        p.pGraphNode.pPosition.X - mEnd.pPosition.X),
                        System.Math.Abs(p.pGraphNode.pPosition.Y - mEnd.pPosition.Y));

                // Add it to the list, and start the search!
                mOpenNodes.Add(p);

                // If the path was invalidated that assume that it is not longer solved.
                mSolved = false;

                // The path is no longer invalid.  It has begun.
                mPathInvalidated = false;
            }

            // Track how many times this planner has looped this time.
            Int32 count = 0;

            const Int32 maxLoops = 30;

            // Loop until all possibilities have been exhusted, the time slice is expired or the 
            // path is solved.
            while (mOpenNodes.Count > 0 && count < maxLoops && !mSolved)
            {
                count++;

                mBestPathEnd = mOpenNodes[0];

                for (Int32 i = 0; i < mOpenNodes.Count; i++)
                {
                    if (mOpenNodes[i].pFinalCost <= mBestPathEnd.pFinalCost)
                    {
                        mBestPathEnd = mOpenNodes[i];
                    }
                }

                mOpenNodes.Remove(mBestPathEnd);
                mClosedNodes.Add(mBestPathEnd);                
                
                // End the search once the destination node is added to the closed list.
                //
                if (mBestPathEnd.pGraphNode == mEnd)
                {
                    OnPathSolved();

                    mSolved = true;

                    break;
                }

                for (Int32 i = 0; i < mBestPathEnd.pGraphNode.pNeighbours.Count; i++)
                {
                    GraphNode.Neighbour nextNode = mBestPathEnd.pGraphNode.pNeighbours[i];

                    if (nextNode.mGraphNode.IsPassable(mBestPathEnd.pGraphNode))
                    {
                        Boolean found = false;
                        for (Int32 j = 0; j < mClosedNodes.Count; j++)
                        {
                            if (mClosedNodes[j].pGraphNode == nextNode.mGraphNode)
                            {
                                found = true;
                                break;
                            }
                        }

                        // This node is already in the closed list, so move on to the next node.
                        if (found)
                        {
                            continue;
                        }

                        // 3-b. If it isn’t on the open list, add it to the open list. 
                        // Make the current square the parent of this square. Record the F, G, and H costs of the square. 

                        // TODO: Get a better way to know if something is in the opened list already.
                        //
                        PathNode foundNode = null;

                        for (Int32 j = 0; j < mOpenNodes.Count; j++)
                        {
                            if (mOpenNodes[j].pGraphNode == nextNode.mGraphNode)
                            {
                                foundNode = mOpenNodes[j];
                                break;
                            }
                        }

                        // Calculate the cost of moving to this node.  This is the distance between the two nodes.
                        // This will be needed in both the case where the node is in the open list already,
                        // and the case where it is not.
                        // The cost is the cost of the previous node plus the distance cost to this node.
                        Single costFromCurrentBest = mBestPathEnd.pCostFromStart + nextNode.mCostToTravel;

                        // If the node was not found it needs to be added to the open list.
                        if (foundNode == null)
                        {
                            // Create a new node and add it to the open list so it can been considered for pathing
                            // in the updates to follow.
                            PathNode p = mUnusedNodes.Pop(); 
                            p.pGraphNode = nextNode.mGraphNode;

                            // For now it points back to the current node.  This can be overwritten if another node
                            // leads here with a lower cost (see else statement below).
                            p.pPrevious = mBestPathEnd;

                            // The cost to get to this node (G) is calculated above.
                            p.pCostFromStart = costFromCurrentBest;

                            // Combo
                            /*
                            Vector2 source = p.pGraphNode.pPosition;

                            Single h_diagonal = System.Math.Min(System.Math.Abs(source.X - mEnd.pPosition.X), System.Math.Abs(source.Y - mEnd.pPosition.Y));
                            Single h_straight = System.Math.Abs(source.X - mEnd.pPosition.X) + System.Math.Abs(source.Y - mEnd.pPosition.Y);
                            p.pCostToEnd = (11.314f) * h_diagonal + 8.0f * (h_straight - 2 * h_diagonal);
                            //p.mCostToEnd *= (10.0f + (1.0f/1000.0f));
                            */

                            p.pCostToEnd = 8.0f * System.Math.Max(System.Math.Abs(
                                                    p.pGraphNode.pPosition.X - mEnd.pPosition.X),
                                                    System.Math.Abs(p.pGraphNode.pPosition.Y - mEnd.pPosition.Y));

                            mOpenNodes.Add(p);

                            // Ending the search now will alomost always result in the best path
                            // but it is possible for it to fail.
                            if (p.pGraphNode == mEnd)
                            {
                                // Since the path is now solved, update mCurBest so that it is used for
                                // tracing back through the path from now on.
                                mBestPathEnd = p;
                                
                                OnPathSolved();
                                
                                break;
                            }
                        }
                        else
                        {
                            // If it is on the open list already, check to see if this path to that square is better, 
                            // using G cost as the measure. A lower G cost means that this is a better path. If so, 
                            // change the parent of the square to the current square, and recalculate the G and F 
                            // scores of the square. If you are keeping your open list sorted by F score, you may need
                            // to resort the list to account for the change.
                            if (foundNode.pCostFromStart > costFromCurrentBest)
                            {
                                foundNode.pPrevious = mBestPathEnd;
                                foundNode.pCostFromStart = costFromCurrentBest;
                            }
                        }
                    }
                }
            }

            // Draw the path.
            if (mBestPathEnd != null)
            {
                DebugDraw();
            }

            return (mSolved ? Result.Solved : Result.Failed);
        }

        /// <summary>
        /// Renders the path using debug shapes.  Includes open and closed list display.
        /// </summary>
        public void DebugDraw()
        {
            // Draw all the closed nodes.
            for (Int32 i = 0; i < mClosedNodes.Count; i++)
            {
                DebugShapeDisplay.pInstance.AddAABB(mClosedNodes[i].pGraphNode.pPosition, 4.0f, 4.0f, Color.Red);

                // If it has a previous node, draw a line from this to that, to show the relationship.
                if (mClosedNodes[i].pPrevious != null)
                {
                    DebugShapeDisplay.pInstance.AddSegment(
                        mClosedNodes[i].pGraphNode.pPosition,
                        mClosedNodes[i].pPrevious.pGraphNode.pPosition,
                        Color.Red);
                }
            }

            // Do the same for the open nodes.
            for (Int32 i = 0; i < mOpenNodes.Count; i++)
            {
                DebugShapeDisplay.pInstance.AddAABB(mOpenNodes[i].pGraphNode.pPosition, 4.0f, 4.0f, Color.Purple);
                if (mOpenNodes[i].pPrevious != null)
                {
                    DebugShapeDisplay.pInstance.AddSegment(
                        mOpenNodes[i].pGraphNode.pPosition,
                        mOpenNodes[i].pPrevious.pGraphNode.pPosition,
                        Color.Purple);
                }
            }

            if (mBestPathEnd != null)
            {
                // Draw the current solution.  May or may not be complete yet.
                // Does this by starting at the current best node and walking up the
                // tree through previous nodes until it hits the starting point.
                PathNode p = mBestPathEnd;
                while (p.pPrevious != null)
                {
                    DebugShapeDisplay.pInstance.AddSegment(
                        p.pGraphNode.pPosition,
                        p.pPrevious.pGraphNode.pPosition,
                        Color.SteelBlue);

                    p = p.pPrevious;
                }
            }
        }

        /// <summary>
        /// Called when the path has been solved.
        /// </summary>
        private void OnPathSolved()
        {
            // Since solving the path triggers and early return in the main update loop,
            // the code that usually draws the path will not be executed.  We need to do
            // it here once to avoid a flicker.
            DebugDraw();

            // The path is now solved.  This will tell the main update loop to stop trying to
            // solve it until it becomes invalidated again.
            mSolved = true;

            mBestPathEnd.pPathSolved = true;
        }

        /// <summary>
        /// Cleans up all the open and closed nodes.
        /// </summary>
        private void ClearNodeLists()
        {
            // Clear all the open nodes, and remember to return them to the unused pool!
            for (Int32 i = 0; i < mOpenNodes.Count; i++)
            {
                mOpenNodes[i].Reset();
                mUnusedNodes.Push(mOpenNodes[i]);
            }
            mOpenNodes.Clear();

            // Clear all the closed nodes and remember to return them to the unused pool!
            for (Int32 i = 0; i < mClosedNodes.Count; i++)
            {
                mClosedNodes[i].Reset();
                mUnusedNodes.Push(mClosedNodes[i]);
            }
            mClosedNodes.Clear();

            // The nodes are gone, so it doesn't make sense that we would
            // hold on to a reference to once of them.
            mBestPathEnd = null;
        }

        /// <summary>
        /// Update the location that the Planner is attempting to reach. The position will be used
        /// to look up the tile at that position in the world.
        /// </summary>
        /// <param name="destination"></param>
        public void SetDestination(GraphNode destination)
        {
            if (mEnd != destination)
            {
                mEnd = destination;
                mPathInvalidated = true;
                mSolved = false;
            }
        }

        /// <summary>
        /// Update where this Planner is travelling from. The position is used to look up a tile at that
        /// position in the world.
        /// </summary>
        /// <param name="source">The position in the world that the planner will start at.</param>
        public void SetSource(GraphNode source)
        {
            if (mStart != source)
            {
                mStart = source;
                mPathInvalidated = true;
                mSolved = false;
            }
        }

        /// <summary>
        /// Clear out the destination that the planner is trying to reach. This doubles as a way
        /// to stop the planner from trying to advance.
        /// </summary>
        public void ClearDestination()
        {
            mEnd = null;
            mSolved = false;

            // Release the Nodes. We don't need them anymore. If a new destination
            // is set, we will need to start from scratch anyway.
            ClearNodeLists();
        }

        /// <summary>
        /// The node at the end of the current best path. It may or may not actually be at the
        /// destination. Using mPrev clients can step back through the path eventually arriving
        /// at the tile found at mSource.
        /// </summary>
        public PathNode pCurrentBest
        {
            get
            {
                return mBestPathEnd;
            }
        }

        /// <summary>
        /// The number of nodes in the static list of unused nodes.
        /// </summary>
        static public Int32 pNumUnusedNodes
        {
            get
            {
                return mUnusedNodes.Count;
            }
        }
    }
}
