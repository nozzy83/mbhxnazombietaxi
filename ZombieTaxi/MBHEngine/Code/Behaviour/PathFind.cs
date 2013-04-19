using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using MBHEngine.Debug;
using MBHEngine.GameObject;
using MBHEngine.Input;
using System.Diagnostics;
using MBHEngine.World;
using MBHEngine.PathFind.GenericAStar;
using MBHEngine.PathFind.HPAStar;
using MBHEngineContentDefs;

namespace MBHEngine.Behaviour
{
    /// <summary>
    /// Behaviour which finds a path from one point to another using A*.
    /// </summary>
    public class PathFind : Behaviour
    {
        /// <summary>
        /// Updates the curent destination of the path finder.  This will reset any path finding
        /// data solved up to this point.
        /// </summary>
        public class SetDestinationMessage : BehaviourMessage
        {
            /// <summary>
            /// The location we want to get to.
            /// </summary>
            public Vector2 mDestination_In;

            /// <summary>
            /// Call this to put a message back to its default state.
            /// </summary>
            public override void Reset()
            {
                mDestination_In = Vector2.Zero;
            }
        }

        /// <summary>
        /// Clears the currently set destination, effectivly stopping the path finding algorithm.
        /// </summary>
        public class ClearDestinationMessage : BehaviourMessage
        {
            /// <summary>
            /// Call this to put a message back to its default state.
            /// </summary>
            public override void Reset() { }
        }

        /// <summary>
        /// Update the source position of the path finder.  This will reset any path finding 
        /// data solved up to this point.
        /// </summary>
        public class SetSourceMessage : BehaviourMessage
        {
            /// <summary>
            /// The new position to start the search from.
            /// </summary>
            public Vector2 mSource_In;

            /// <summary>
            /// Call this to put a message back to its default state.
            /// </summary>
            public override void Reset()
            {
                mSource_In = Vector2.Zero;
            }
        }

        /// <summary>
        /// Retrives the current best node in the path.  By traversing from here back to the source
        /// using mPrev, you can deterime the whole path.
        /// </summary>
        public class GetCurrentBestNodeMessage : BehaviourMessage
        {
            /// <summary>
            /// The current best node. Use pPrev to trace your way back to mSource.
            /// </summary>
            public MBHEngine.PathFind.GenericAStar.PathNode mBest_Out;

            /// <summary>
            /// Call this to put a message back to its default state.
            /// </summary>
            public override void Reset()
            {
                mBest_Out = null;
            }
        }

        /// <summary>
        /// Sent when the path finder fails to find the target each frame.
        /// </summary>
        public class OnPathFindFailedMessage : BehaviourMessage
        {
            /// <summary>
            /// The different ways that a path find search can fail.
            /// </summary>
            public enum Reason
            {
                Undefined = -1,
                Failed = 0,         // Exhusted all posibilities without reaching destination. 
                InvalidLocation,    // Either the source or destination are on a wall.
                Timeout,            // Allotted search time exceeded.
            };

            /// <summary>
            /// The reason that the search failed.
            /// </summary>
            public Reason mReason;

            /// <summary>
            /// Call this to put a message back to its default state.
            /// </summary>
            public override void Reset() 
            {
                mReason = Reason.Undefined;
            }
        }

        /// <summary>
        /// Allows for the possibility to automatically update the source position based on the parent 
        /// Game Object.
        /// </summary>
        private Boolean mUpdateSourceAutomatically;

        /// <summary>
        /// The planner used to find a path at a high level.
        /// </summary>
        public Planner mPlannerNavMesh;

        /// <summary>
        /// The planner used to find path at a low level. Used to find path between PathNode objects
        /// in <see cref="mPlannerNavMesh"/>.
        /// </summary>
        public Planner mPlannerTileMap;

        /// <summary>
        /// The current best at the mPlannerTileMap level. We don't use mPlannerTileMap.pCurBest directly
        /// because we will sometimes continue to search deeper into the high level path while this node
        /// is used to start moving.
        /// </summary>
        private PathNode mLowLevelBest;

        /// <summary>
        /// Tracks the deepest node in the nav mesh that has been pathed to with mPlannerTileMap in the 
        /// current search problem.
        /// </summary>
        private PathNode mLastHighLevelSearched;

        /// <summary>
        /// How many passes can the path planner make before this search is considered a failure.
        /// This prevent searches going on for an absurdly long time.
        /// </summary>
        private Int32 mSearchPassLimit;

        /// <summary>
        /// The current number of a passes a given search has gone on.
        /// </summary>
        private Int32 mSearchPassCount;

        /// <summary>
        /// Preallocated to avoid garbage at runtime.
        /// </summary>
        private OnPathFindFailedMessage mOnPathFindFailedMsg;
        private MBHEngine.Behaviour.Level.GetTileAtPositionMessage mGetTileAtPositionMsg;
        private Level.GetNavMeshMessage mGetNavMeshMsg;

        /// <summary>
        /// Constructor which also handles the process of loading in the Behaviour
        /// Definition information.
        /// </summary>
        /// <param name="parentGOH">The game object that this behaviour is attached to.</param>
        /// <param name="fileName">The file defining this behaviour.</param>
        public PathFind(GameObject.GameObject parentGOH, String fileName)
            : base(parentGOH, fileName)
        {
        }

        /// <summary>
        /// When this behaviour is removed we need to clean up some of the temp nodes that may have been
        /// created.
        /// </summary>
        public override void OnRemove()
        {
            WorldManager.pInstance.pCurrentLevel.OnMessage(mGetNavMeshMsg);
            if (mPlannerNavMesh.pStart != null)
            {
                mGetNavMeshMsg.mNavMesh_Out.RemoveTempNode(mPlannerNavMesh.pStart);
                mPlannerNavMesh.SetSource(null);
            }
            ClearDestination();
        }

        /// <summary>
        /// Call this to initialize a Behaviour with data supplied in a file.
        /// </summary>
        /// <param name="fileName">The file to load from.</param>
        public override void LoadContent(String fileName)
        {
            base.LoadContent(fileName);

            PathFindDefinition def = GameObjectManager.pInstance.pContentManager.Load<PathFindDefinition>(fileName);

            // TODO: This should be read in from the xml.
            mUpdateSourceAutomatically = false;

            mSearchPassLimit = def.mSearchPassLimit;
            mSearchPassCount = 0;

            mPlannerNavMesh = new Planner();
            mPlannerTileMap = new Planner();
            
            // Preallocate messages to avoid GC during gameplay.
            //
            mOnPathFindFailedMsg = new OnPathFindFailedMessage();
            mGetTileAtPositionMsg = new Level.GetTileAtPositionMessage();
            mGetNavMeshMsg = new Level.GetNavMeshMessage();
        }

        /// <summary>
        /// Called once per frame by the game object.
        /// </summary>
        /// <param name="gameTime">The amount of time that has passed this frame.</param>
        public override void Update(GameTime gameTime)
        {
            // Does this instance of the behaviour just want to automatically update the source
            // based on our parents position?
            if (mUpdateSourceAutomatically)
            {
                SetSource(mParentGOH.pPosition);
            }

            if (mGetNavMeshMsg.mNavMesh_Out != null)
            {
                //mGetNavMeshMsg.mNavMesh_Out.DebugCheckNodes();
            }

            // Plan the path at a high level.
            MBHEngine.PathFind.GenericAStar.Planner.Result res = mPlannerNavMesh.PlanPath();

            // If the search is anything but InProgress it is safe to research the pass counter.
            if (res == Planner.Result.InProgress)
            {
                mSearchPassCount++;
            }
            else
            {
                mSearchPassCount = 0;
            }

            // If the planner failed to find the destination tell the other behaviours.
            if (res == MBHEngine.PathFind.GenericAStar.Planner.Result.Failed ||
                res == Planner.Result.InvalidLocation ||
                mSearchPassCount > mSearchPassLimit)
            {
                if (res == Planner.Result.Failed)
                {
                    mOnPathFindFailedMsg.mReason = OnPathFindFailedMessage.Reason.Failed;
                }
                else if (res == Planner.Result.InvalidLocation)
                {
                    mOnPathFindFailedMsg.mReason = OnPathFindFailedMessage.Reason.InvalidLocation;
                }
                else if (mSearchPassCount > mSearchPassLimit)
                {
                    mOnPathFindFailedMsg.mReason = OnPathFindFailedMessage.Reason.Timeout;
                }

                mParentGOH.OnMessage(mOnPathFindFailedMsg);

                mSearchPassCount = 0;
            }
            else if (res == Planner.Result.Solved)// && InputManager.pInstance.CheckAction(InputManager.InputActions.B))
            {
                // When the high level path finding is solved, start path finding at a lower
                // level betwen PathNodes within the higher level.
                //

                PathNode node = mPlannerNavMesh.pCurrentBest;

                // We need to do more than just walk the list until we hit the mLastHighLevelSearched. There
                // are cases (such as 2 nodes at the same position) where we don't want to use a Node.
                // This tracks which was the last VALID node.
                PathNode lastValid = node;

                // Loop all the way back to one after the starting point avoiding the starting node, as
                // well as previously searched nodes.
                // We don't want the starting node because that is where we are likely already standing.
                while (
                    node != mLastHighLevelSearched && // Was the first node actually the one we looked at last Update?
                    node.pPrevious != mLastHighLevelSearched && // Is the next one the Node looked at last Update?
                    node.pPrevious.pGraphNode.pPosition != mPlannerTileMap.pStart.pPosition && // Is this Node at the same position as the starting position? We are already there so trying to get there again would cause issues.
                    node.pPrevious.pPrevious != null)
                {
                    node = node.pPrevious;

                    // Only choose nodes that are not at the same position as the last valid node. Trying to path find between
                    // 2 nodes at the TileMap level causes problems. It will solve the path fine, but the issue is when it goes
                    // back to the HPA level, since it hasn't moved the starting position, HPA will find the exact same path.
                    // This cycle repeats forever.
                    if (lastValid.pGraphNode.pPosition != node.pGraphNode.pPosition)
                    {
                        lastValid = node;
                    }
                }

                node = lastValid;

                // The lower level search uses the Level.Tile Graph, not the NavMesh, so we need to 
                // use the node in NavMesh to find a node in the main tile map.
                mGetTileAtPositionMsg.Reset();
                mGetTileAtPositionMsg.mPosition_In = node.pGraphNode.pPosition;
                WorldManager.pInstance.pCurrentLevel.OnMessage(mGetTileAtPositionMsg);

                // If this is the first time we are searching at the low level (for this particular 
                // search) we want to SET the destination. If that isn't the case, we want to extend
                // the search to continue to a new destination.
                if (null == mLowLevelBest)
                {
                    mPlannerTileMap.SetDestination(mGetTileAtPositionMsg.mTile_Out.mGraphNode);
                }
                else
                {
                    mPlannerTileMap.ExtendDestination(mGetTileAtPositionMsg.mTile_Out.mGraphNode);
                }

                // Start/Continue planning the path.
                Planner.Result tileRes = mPlannerTileMap.PlanPath();

                // Once the path has been solved, store out that this node in the high level search has 
                // been completed, so the next time through this function, the next node in the path w
                // will be the target.
                if (tileRes == Planner.Result.Solved)
                {
                    mLowLevelBest = mPlannerTileMap.pCurrentBest;

                    mLastHighLevelSearched = node;
                }
            }
        }


        /// <summary>
        /// The main interface for communicating between behaviours.  Using polymorphism, we
        /// define a bunch of different messages deriving from BehaviourMessage.  Each behaviour
        /// can then check for particular upcasted messahe types, and either grab some data 
        /// from it (set message) or store some data in it (get message).
        /// </summary>
        /// <param name="msg">The message being communicated to the behaviour.</param>
        public override void OnMessage(ref BehaviourMessage msg)
        {
            // Which type of message was sent to us?
            if (msg is SetDestinationMessage)
            {
                SetDestinationMessage tmp = (SetDestinationMessage)msg;

                SetDestination(tmp.mDestination_In);
            }
            else if (msg is ClearDestinationMessage)
            {
                ClearDestination();
            }
            else if (msg is SetSourceMessage)
            {
                SetSourceMessage tmp = (SetSourceMessage)msg;

                SetSource(tmp.mSource_In);
            }
            else if (msg is GetCurrentBestNodeMessage)
            {
                GetCurrentBestNodeMessage tmp = (GetCurrentBestNodeMessage)msg;

                // If the low level search hasn't started yet, return the high level one, so that
                // movement can start as soon as possible.
                tmp.mBest_Out = mLowLevelBest; // mLowLevelBest != null ? mLowLevelBest : mPlannerNavMesh.pCurrentBest;
            }
            else if (msg is Level.OnNavMeshInvalidatedMessage)
            {
                mPlannerNavMesh.InvalidateCurrentPath();
                mPlannerTileMap.InvalidateCurrentPath();

                SetSource(mParentGOH.pPosition);
            }
        }

        /// <summary>
        /// Clears the destination and all the the associated data.
        /// </summary>
        private void ClearDestination()
        {
            //MBHEngine.PathFind.HPAStar.NavMesh.DebugCheckNode(mPlannerNavMesh.pStart);
            
			WorldManager.pInstance.pCurrentLevel.OnMessage(mGetNavMeshMsg);
            
			// If mPlannerNavMesh currently has a destination, that means that we added a temp
            // node to the nav mesh as that destination, and it needs to be removed now.
            if (mPlannerNavMesh.pEnd != null)
            {
                System.Diagnostics.Debug.Assert(mPlannerNavMesh.pEnd != mPlannerNavMesh.pStart, "End and start are the same.");

                mGetNavMeshMsg.mNavMesh_Out.RemoveTempNode(mPlannerNavMesh.pEnd);
            }

            // Members used to coordinate the low level search need to be reset since the 
            // search had been invalidated.
            mLowLevelBest = null;
            mLastHighLevelSearched = null;

            mPlannerNavMesh.ClearDestination();
            mPlannerTileMap.ClearDestination();

            //mGetNavMeshMsg.mNavMesh_Out.DebugCheckNodes();

            //MBHEngine.PathFind.HPAStar.NavMesh.DebugCheckNode(mPlannerNavMesh.pStart);
        }

        /// <summary>
        /// Sets the current location that we want to path search to.
        /// </summary>
        /// <param name="pos">The position to try and reach.</param>
        private void SetDestination(Vector2 pos)
        {
            mGetTileAtPositionMsg.Reset();
            mGetTileAtPositionMsg.mPosition_In = pos;
            WorldManager.pInstance.pCurrentLevel.OnMessage(mGetTileAtPositionMsg);

            GraphNode end = mPlannerNavMesh.pEnd;

            // Only set the new destination if it is different than the current one (or the current
            // one does not exist).
            if (end == null || mGetTileAtPositionMsg.mTile_Out != end.pData)
            {
                WorldManager.pInstance.pCurrentLevel.OnMessage(mGetNavMeshMsg);

                // If the GraphNode was already created, remove it before adding a new one.
                if (end != null)
                {
                    //mGetNavMeshMsg.mNavMesh_Out.UnlinkGraphNodes(end, mPlannerNavMesh.pStart);
                    mGetNavMeshMsg.mNavMesh_Out.RemoveTempNode(end);
                }

                GraphNode node = mGetNavMeshMsg.mNavMesh_Out.InsertTempNode(pos);

                // Attempt to set a new destination. Returns true in the case where the destination was 
                // different (and thus changed).
                if (mPlannerNavMesh.SetDestination(node))
                {
                    mLowLevelBest = null;
                    mLastHighLevelSearched = null;

                    // If the destination changes, it means the low level search is no longer valid, and
                    // needs to wait for the high level search to complete first.
                    mPlannerTileMap.ClearDestination();
                }
            }

            //mGetNavMeshMsg.mNavMesh_Out.DebugCheckNodes();

            //MBHEngine.PathFind.HPAStar.NavMesh.DebugCheckNode(mPlannerNavMesh.pEnd);

            //MBHEngine.PathFind.HPAStar.NavMesh.DebugCheckNode(mPlannerNavMesh.pStart);
        }

        /// <summary>
        /// Helper function for doing the work needed to go from a position to a GraphNode, and then finally
        /// sending that GraphNode to the Planner.
        /// </summary>
        /// <param name="pos">The position at which we wish to travel from.</param>
        private void SetSource(Vector2 pos)
        {
            mGetTileAtPositionMsg.Reset();
            mGetTileAtPositionMsg.mPosition_In = pos;
            WorldManager.pInstance.pCurrentLevel.OnMessage(mGetTileAtPositionMsg);

            GraphNode start = mPlannerNavMesh.pStart;

            // Only set the new destination if it is different than the current one (or the current
            // one does not exist).
            if (start == null || mGetTileAtPositionMsg.mTile_Out != start.pData)
            {
                WorldManager.pInstance.pCurrentLevel.OnMessage(mGetNavMeshMsg);

                // If the GraphNode was already created, remove it before adding a new one.
                if (start != null)
                {
                    //mGetNavMeshMsg.mNavMesh_Out.UnlinkGraphNodes(end, mPlannerNavMesh.pStart);
                    mGetNavMeshMsg.mNavMesh_Out.RemoveTempNode(start);
                }

                GraphNode node = mGetNavMeshMsg.mNavMesh_Out.InsertTempNode(pos);

                // Attempt to set a new destination. Returns true in the case where the destination was 
                // different (and thus changed).
                if (mPlannerNavMesh.SetSource(node))
                {
                    mLowLevelBest = null;
                    mLastHighLevelSearched = null;
                    // Find the TileGraphNode that maps to the location of node.
                    mGetTileAtPositionMsg.Reset();
                    mGetTileAtPositionMsg.mPosition_In = pos;
                    WorldManager.pInstance.pCurrentLevel.OnMessage(mGetTileAtPositionMsg);

                    mPlannerTileMap.SetSource(mGetTileAtPositionMsg.mTile_Out.mGraphNode);
                }
            }

            //mGetNavMeshMsg.mNavMesh_Out.DebugCheckNodes();

            //MBHEngine.PathFind.HPAStar.NavMesh.DebugCheckNode(mPlannerNavMesh.pEnd);

            //MBHEngine.PathFind.HPAStar.NavMesh.DebugCheckNode(mPlannerNavMesh.pStart);            
        }
    }
}
