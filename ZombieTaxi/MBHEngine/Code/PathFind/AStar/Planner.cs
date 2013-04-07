using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MBHEngine.Behaviour;
using Microsoft.Xna.Framework;
using System.Diagnostics;
using MBHEngine.Debug;
using MBHEngine.Input;

namespace MBHEngine.PathFind.AStar
{
    /// <summary>
    /// Standard A* path planner.
    /// </summary>
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
        /// Represents a node in the path.  Basically a wrapper for a tile, but with some additional
        /// information needed to track its place in the search algorithm.
        /// </summary>
        public class PathNode
        {
            /// <summary>
            /// The tile in the level this node represents.
            /// </summary>
            public Level.Tile mTile;

            /// <summary>
            /// The node that lead to this node.  By tracing down through mPrev we see the actual path.
            /// </summary>
            public PathNode mPrev;

            /// <summary>
            /// Often refered to as G in most A* algorithms.  This is the "cost" to get to this tile from the starting
            /// point.
            /// </summary>
            public Single mCostFromStart;

            /// <summary>
            /// Often refered to as H in most A* algorithms.  This is the "cost" to get from this tile 
            /// to the destination. This is just an estimate since we won't know the actual cost until 
            /// the path is calculated.
            /// </summary>
            public Single mCostToEnd;

            /// <summary>
            /// Used by the client to keep track of whether or not the object using this path has 
            /// reached this node yet.
            /// </summary>
            public Boolean mReached;

            /// <summary>
            /// If this is the start of the current best path, than this will be true or false, to
            /// indicate whether or not a complete path to the destination has been found.
            /// </summary>
            public Boolean mPathSolved;

            /// <summary>
            /// The tallied cost of this node taking into account G and H.  Used to determine which 
            /// nodes are better/worse than others.
            /// </summary>
            public Single pFinalCost
            {
                get
                {
                    return mCostFromStart + mCostToEnd;
                }
            }

            /// <summary>
            /// These nodes get used over and over again, so we need to make sure they 
            /// get completely reset between uses.
            /// </summary>
            public void Reset()
            {
                mTile = null;
                mPrev = null;
                mCostFromStart = 0;
                mCostToEnd = 0;
                mReached = false;
                mPathSolved = false;
            }
        }

        /// <summary>
        /// The position we are path finding FROM.
        /// </summary>
        private Vector2 mSource;

        /// <summary>
        /// The position we are path finding TO.
        /// </summary>
        private Vector2 mDestination;

        /// <summary>
        /// In the A* algorithm, this list represents all nodes which are possible path points, but have
        /// not yet been evaluated.
        /// </summary>
        private List<PathNode> mOpenNodes;

        /// <summary>
        /// In the A* algorithm, this list represents all the nodes that have already been evaluated for
        /// path possibilites.
        /// </summary>
        private List<PathNode> mClosedNodes;

        /// <summary>
        /// A static stack of nodes shared by all PathFind Behaviour instances.  This is to avoid
        /// having to allocate new path nodes during gameplay and potentially triggering a GC.
        /// It is very important that when nodes are removed from the closed and open lists, they
        /// get Reset and pushed back onto this stack.
        /// </summary>
        private static Stack<PathNode> mUnusedNodes;

        /// <summary>
        /// This is only needed for debug display of the path.  Once the path is solved, this would
        /// no longer be updated (as it is set inside the main while loop of the solver) so to render
        /// the solved path after the fact, we keep a handle of this in the class.
        /// </summary>
        private PathNode mCurBest;

        /// <summary>
        /// Has the path been solved yet?  If it has then we can avoid continously trying to
        /// solve it over and over again.
        /// </summary>
        private Boolean mSolved;

        /// <summary>
        /// The tile where this path finding starts.
        /// </summary>
        private Level.Tile mSourceTile;

        /// <summary>
        /// The tile at the location we are trying to get to.
        /// </summary>
        private Level.Tile mDestinationTile;

        /// <summary>
        /// There are a couple things that can cause our current pathing solution to be considered
        /// invalid and force us to start over (eg. source moved).
        /// </summary>
        Boolean mPathInvalidated;

        /// <summary>
        /// Preallocated to avoid garbage at runtime.
        /// </summary>
        private MBHEngine.Behaviour.Level.GetTileAtPositionMessage mGetTileAtPositionMsg;

        /// <summary>
        /// Constructor.
        /// </summary>
        public Planner()
        {
            // Create the open and closed lists with a default capacity.  Every time that capacity is
            // exceeded, the capacity is doubled and we make allocations on the Heap.  This has the potential
            // to trigger a garbage collection, which we never want to happen during gameplay.
            // So make sure this default capacity is big enough!
            mOpenNodes = new List<PathNode>(500);
            mClosedNodes = new List<PathNode>(500);

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

            // Assume the path is not solved.
            mSolved = false;
            mPathInvalidated = false;

            // Preallocate messages to avoid GC during gameplay.
            //
            mGetTileAtPositionMsg = new Level.GetTileAtPositionMessage();
            //mOnPathFindFailedMsg = new OnPathFindFailedMessage();
        }

        /// <summary>
        /// Called once per frame by the game object.
        /// </summary>
        /// <param name="gameTime">The amount of time that has passed this frame.</param>
        public Result Update(GameTime gameTime)
        {
            //DebugMessageDisplay.pInstance.AddDynamicMessage("Path Find - Open: " + mOpenNodes.Count);
            //DebugMessageDisplay.pInstance.AddDynamicMessage("Path Find - Closed: " + mClosedNodes.Count);
            //DebugMessageDisplay.pInstance.AddDynamicMessage("Path Find - Unused: " + mUnusedNodes.Count);

            // Store the current level for easy access throughout algorithm.
            GameObject.GameObject curLvl = World.WorldManager.pInstance.pCurrentLevel;

            // If there is no tile at the destination then there is no path finding to do.
            if( mDestinationTile == null)
            {
                return Result.NotStarted;
            }

            // If we have a destination draw it. Even if there isn't a source yet.
            DebugShapeDisplay.pInstance.AddPoint(mDestination, 2.0f, Color.Yellow);

            // If the destination is a solid tile then we will never be able to solve the path.
            if (mDestinationTile != null && mDestinationTile.mType != Level.Tile.TileTypes.Empty)
            {
                // We consider this a failure, similar to if a destination was surrounded by solid.
                return Result.Failed;
            }

            // If our source position is not on a tile then there is no path finding to do.
            if (mSourceTile == null)
            {
                return Result.NotStarted;
            }

            // If our source position is not on a tile, or that tile is solid we cannot ever solve
            // this path, so abort right away.
            if (mSourceTile != null && mSourceTile.mType != Level.Tile.TileTypes.Empty)
            {
                // Trying to path find to a solid tile is considered a failure.
                return Result.Failed;
            }

            // If there is a source, draw it.
            DebugShapeDisplay.pInstance.AddPoint(mSource, 2.0f, Color.Orange);

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
                PathNode node = mCurBest;
                while (null != node)
                {
                    if (node.mTile.mType != Level.Tile.TileTypes.Empty)
                    {
                        // Setting this flag will force the path finder to start from 
                        // the begining.
                        mPathInvalidated = true;

                        // No need to loop any further. One blockade is enough.
                        break;
                    }

                    node = node.mPrev;
                }
            }

            // If the path has become invalid, we need to restart the pathing algorithm.
            if (mPathInvalidated)
            {
                ClearNodeLists();

                // First thing we need to do is add the first node to the open list.
                PathNode p = mUnusedNodes.Pop();

                // There is no cost because it is the starting node.
                p.mCostFromStart = 0;

                // For H we use the actual distance to the destination.  The Manhattan Heuristic method.
                p.mCostToEnd = Vector2.Distance(mSource, mDestination);

                // Store the tile itself.  No need to store the previous tile, as there is none in this case.
                p.mTile = mSourceTile;

                // Add it to the list, and start the search!
                mOpenNodes.Add(p);

                // If the path was invalidated that assume that it is not longer solved.
                mSolved = false;

                // The path is no longer invalid.  It has begun.
                mPathInvalidated = false;
            }

            // This path finding is very expensive over long distances.  To avoid slowing down the game,
            // the solver is time sliced; it will only execute a small chunk of the algorithm per frame.
            Int32 timeSliceCount = 0;
			
			// TODO: Configure this from XML script.
            Int32 timeSliceCap = 30;
            
            // Continue searching until we hit the destination or run out of open nodes to check against.
            while (!mSolved && mOpenNodes.Count > 0 && timeSliceCount < timeSliceCap)// && InputManager.pInstance.CheckAction(InputManager.InputActions.B, true))
            {
                timeSliceCount++;

                // 1. Look for the lowest F cost square on the open list. We refer to this as the current square.
                mCurBest = mOpenNodes[0];
                for (Int32 i = 1; i < mOpenNodes.Count; i++)
                {
                    if (mOpenNodes[i].pFinalCost <= mCurBest.pFinalCost)
                    {
                        mCurBest = mOpenNodes[i];
                    }
                }

                // 2. Switch it to the closed list.
                mOpenNodes.Remove(mCurBest);
                mClosedNodes.Add(mCurBest);

                // End the search once the destination node is added to the closed list.
                //
                if (mCurBest.mTile == mDestinationTile)
                {
                    OnPathSolved(mCurBest);

                    break;
                }

                // 3.  For each of the 8 squares adjacent to this current square...
                for (Int32 i = 0; i < mCurBest.mTile.mAdjecentTiles.Length; i++)
                {
                    // 3-a. If it is not walkable or if it is on the closed list, ignore it. Otherwise...
                    if (mCurBest.mTile.mAdjecentTiles[i] != null && mCurBest.mTile.mAdjecentTiles[i].mType == Level.Tile.TileTypes.Empty)
                    {
                        // Avoid pathing that cut diagonally across solid tiles.  This is ok for the line of
                        // 0 width, but for actual GO, they should not be able to fit through that tight
                        // spot.
                        //      /
                        //  [+]/
                        //    /[+]
                        //   /
                        if (Level.IsAttemptingInvalidDiagonalMove((Level.Tile.AdjacentTileDir)i, mCurBest.mTile))
                        {
                            continue;
                        }

                        // TODO: Get a better way to know if something is in the closed list already.
                        //
                        Boolean found = false;
                        for (Int32 j = 0; j < mClosedNodes.Count; j++)
                        {
                            if (mClosedNodes[j].mTile == mCurBest.mTile.mAdjecentTiles[i])
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
                            if (mOpenNodes[j].mTile == mCurBest.mTile.mAdjecentTiles[i])
                            {
                                foundNode = mOpenNodes[j];
                                break;
                            }
                        }

                        // Diagonal movement cost more than lateral movement, so figure out where this node is
                        // in relation to the current node.
                        Boolean isDiag = 
                            ( i == (Int32)Level.Tile.AdjacentTileDir.LEFT_DOWN ) ||
                            ( i == (Int32)Level.Tile.AdjacentTileDir.LEFT_UP ) ||
                            ( i == (Int32)Level.Tile.AdjacentTileDir.RIGHT_DOWN ) ||
                            ( i == (Int32)Level.Tile.AdjacentTileDir.RIGHT_UP );

                        // Calculate the cost of moving to this node.  This is the distance between the two nodes.
                        // This will be needed in both the case where the node is in the open list already,
                        // and the case where it is not.
                        // The cost is the cost of the previous node plus the distance cost to this node.
                        Single costFromCurrentBest = mCurBest.mCostFromStart + (isDiag ? 11.314f : 8);

                        // If the node was not found it needs to be added to the open list.
                        if (foundNode == null)
                        {
                            // Create a new node and add it to the open list so it can been considered for pathing
                            // in the updates to follow.
                            PathNode p = mUnusedNodes.Pop();

                            // For now it points back to the current node.  This can be overwritten if another node
                            // leads here with a lower cost (see else statement below).
                            p.mPrev = mCurBest;

                            // Store the actual tile.
                            p.mTile = mCurBest.mTile.mAdjecentTiles[i];

                            // The cost to get to this node (G) is calculated above.
                            p.mCostFromStart = costFromCurrentBest;

                            // The cost to end (H) is just a straight distance calculation.
                            // Manhattan.
                            /*
                            p.mCostToEnd = Vector2.Distance(
                                p.mTile.mCollisionRect.pCenterPoint, 
                                mDestination);
                            */

                            // Diagonal.
                            /*
                            p.mCostToEnd = 8.0f * System.Math.Max(
                                System.Math.Abs(
                                p.mTile.mCollisionRect.pCenterPoint.X - mDestination.X),
                                System.Math.Abs(p.mTile.mCollisionRect.pCenterPoint.Y - mDestination.Y));
                            */

                            // Combo
                            Vector2 source = p.mTile.mCollisionRect.pCenterPoint;

                            Single h_diagonal = System.Math.Min(System.Math.Abs(source.X - mDestination.X), System.Math.Abs(source.Y - mDestination.Y));
                            Single h_straight = System.Math.Abs(source.X - mDestination.X) + System.Math.Abs(source.Y - mDestination.Y);
                            p.mCostToEnd = (11.314f) * h_diagonal + 8.0f * (h_straight - 2 * h_diagonal);
                            //p.mCostToEnd *= (10.0f + (1.0f/1000.0f));

                            mOpenNodes.Add(p);

                            /*
                            Boolean inserted = false;

                            for (Int32 k = 0; k < mOpenNodes.Count; k++)
                            {
                                if (mOpenNodes[k].mCostToEnd > p.mCostToEnd)
                                {
                                    mOpenNodes.Insert(k, p);

                                    inserted = true;

                                    break;
                                }
                            }

                            if (!inserted)
                            {
                                mOpenNodes.Add(p);
                            }
                            */

                            // Ending the search now will alomost always result in the best path
                            // but it is possible for it to fail.
                            if (p.mTile == mDestinationTile)
                            {
                                // Since the path is now solved, update mCurBest so that it is used for
                                // tracing back through the path from now on.
                                mCurBest = p;
                                OnPathSolved(mCurBest);
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
                            if (foundNode.mCostFromStart > costFromCurrentBest)
                            {
                                foundNode.mPrev = mCurBest;
                                foundNode.mCostFromStart = costFromCurrentBest;
                            }
                        }
                    }
                }
            }

            // Draw the path.
            if (mCurBest != null)
            {
                DrawPath(mCurBest);
            }

            return (mSolved ? Result.Solved : Result.Failed);
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
            mCurBest = null;
        }

        /// <summary>
        /// Renders the path using debug shapes.  Includes open and closed list display.
        /// </summary>
        /// <param name="endPoint">The node to start at, and trace back to start.</param>
        private void DrawPath(PathNode endPoint)
        {
            // Draw all the closed nodes.
            for (Int32 i = 0; i < mClosedNodes.Count; i++)
            {
                DebugShapeDisplay.pInstance.AddAABB(mClosedNodes[i].mTile.mCollisionRect, Color.Red);

                // If it has a previous node, draw a line from this to that, to show the relationship.
                if (mClosedNodes[i].mPrev != null)
                {
                    DebugShapeDisplay.pInstance.AddSegment(
                        mClosedNodes[i].mTile.mCollisionRect.pCenterPoint,
                        mClosedNodes[i].mPrev.mTile.mCollisionRect.pCenterPoint,
                        Color.Red);
                }
            }

            // Do the same for the open nodes.
            for (Int32 i = 0; i < mOpenNodes.Count; i++)
            {
                DebugShapeDisplay.pInstance.AddAABB(mOpenNodes[i].mTile.mCollisionRect, Color.Green);
                if (mOpenNodes[i].mPrev != null)
                {
                    DebugShapeDisplay.pInstance.AddSegment(
                        mOpenNodes[i].mTile.mCollisionRect.pCenterPoint,
                        mOpenNodes[i].mPrev.mTile.mCollisionRect.pCenterPoint,
                        Color.Green);
                }
            }

            // Draw the current solution.  May or may not be complete yet.
            // Does this by starting at the current best node and walking up the
            // tree through previous nodes until it hits the starting point.
            PathNode p = endPoint;
            while (p.mPrev != null)
            {
                DebugShapeDisplay.pInstance.AddSegment(
                    p.mTile.mCollisionRect.pCenterPoint,
                    p.mPrev.mTile.mCollisionRect.pCenterPoint,
                    Color.SteelBlue);

                p = p.mPrev;
            }
        }

        /// <summary>
        /// Called when the path has been solved.
        /// </summary>
        /// <param name="endNode">The final node in the solution.</param>
        private void OnPathSolved(PathNode endNode)
        {
            // Since solving the path triggers and early return in the main update loop,
            // the code that usually draws the path will not be executed.  We need to do
            // it here once to avoid a flicker.
            DrawPath(endNode);

            // The path is now solved.  This will tell the main update loop to stop trying to
            // solve it until it becomes invalidated again.
            mSolved = true;

            endNode.mPathSolved = true;
        }

        /// <summary>
        /// Update the location that the Planner is attempting to reach. The position will be used
        /// to look up the tile at that position in the world.
        /// </summary>
        /// <param name="destination"></param>
        public void SetDestination(Vector2 destination)
        {
            mGetTileAtPositionMsg.mPosition_In = destination;
            World.WorldManager.pInstance.pCurrentLevel.OnMessage(mGetTileAtPositionMsg);

            if (mDestinationTile != mGetTileAtPositionMsg.mTile_Out)
            {
                mDestination = destination;
                mPathInvalidated = true;
                mSolved = false;

                // We will need to do a couple checks to see if we have found the destination tile, so cache that
                // now.
                mDestinationTile = mGetTileAtPositionMsg.mTile_Out;
            }
        }

        /// <summary>
        /// Update where this Planner is travelling from. The position is used to look up a tile at that
        /// position in the world.
        /// </summary>
        /// <param name="source">The position in the world that the planner will start at.</param>
        public void SetSource(Vector2 source)
        {                
            // Grab the tile at the source position.
            mGetTileAtPositionMsg.mPosition_In = source;
            World.WorldManager.pInstance.pCurrentLevel.OnMessage(mGetTileAtPositionMsg);

            // We only care if they moved enough to make it onto a new tile.
            if (mSourceTile != mGetTileAtPositionMsg.mTile_Out)
            {
                // Update the source incase the GO has moved since the last update.
                mSource = source;

                // Update the source tile with the tile the GO has moved to.
                mSourceTile = mGetTileAtPositionMsg.mTile_Out;

                // Let the algorithm know that it needs to recalculate.
                // TODO: With only moving one tile at a time, could this be optimized to
                //       check if this tile is already in the path, or append it to the start
                //       of the path?
                mPathInvalidated = true;

                // Since the source has changed, this path can no longer be considered solved.
                mSolved = false;
            }
        }

        /// <summary>
        /// Clear out the destination that the planner is trying to reach. This doubles as a way
        /// to stop the planner from trying to advance.
        /// </summary>
        public void ClearDestination()
        {
            mDestinationTile = null;
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
                return mCurBest;
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
