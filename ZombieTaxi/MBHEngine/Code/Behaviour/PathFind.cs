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
            /// Call this to put a message back to its default state.
            /// </summary>
            public override void Reset() { }
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
        /// Call this to initialize a Behaviour with data supplied in a file.
        /// </summary>
        /// <param name="fileName">The file to load from.</param>
        public override void LoadContent(String fileName)
        {
            base.LoadContent(fileName);

            //TimerDefinition def = GameObjectManager.pInstance.pContentManager.Load<TimerDefinition>(fileName);

            // TODO: This should be read in from the xml.
            mUpdateSourceAutomatically = false;

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

            // Plan the path at a high level.
            MBHEngine.PathFind.GenericAStar.Planner.Result res = mPlannerNavMesh.PlanPath();

            // If the planner failed to find the destination tell the other behaviours.
            if (res == MBHEngine.PathFind.GenericAStar.Planner.Result.Failed)
            {
                mParentGOH.OnMessage(mOnPathFindFailedMsg);
            }
            else if (res == Planner.Result.Solved)
            {
                // When the high level path finding is solved, start path finding at a lower
                // level betwen PathNodes within the higher level.
                PathNode node = mPlannerNavMesh.pCurrentBest;

                // Loop all the way back to one after the starting point. We don't want the starting
                // node because that is where we are likely already standing.
                while (node != null && node.pPrevious != null && node.pPrevious.pPrevious != null)
                {
                    node = node.pPrevious;
                }

                // The lower level search uses the Level.Tile Graph, not the NavMesh, so we need to 
                // use the node in NavMesh to find a node in the main tile map.
                mGetTileAtPositionMsg.Reset();
                mGetTileAtPositionMsg.mPosition_In = node.pGraphNode.pPosition;
                WorldManager.pInstance.pCurrentLevel.OnMessage(mGetTileAtPositionMsg);

                // Each tile stores a reference to the GraphNode that stores it.
                mPlannerTileMap.SetDestination(mGetTileAtPositionMsg.mTile_Out.mGraphNode);

                // Start planning the path.
                Planner.Result tileRes = mPlannerTileMap.PlanPath();
                
                // Keep going until it is solved. Should be very quick since this is such a
                // small problem maze to solve.
                while (tileRes == Planner.Result.InProgress)
                {
                    tileRes = mPlannerTileMap.PlanPath();
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

                WorldManager.pInstance.pCurrentLevel.OnMessage(mGetNavMeshMsg);

                // Find the GraphNode in the NavMesh closest to the new destination.
                /// <todo>
                /// This should be accurate to the Level.Tile, not just the closest.
                /// </todo>
                GraphNode node = mGetNavMeshMsg.mNavMesh_Out.GetClostestNode(tmp.mDestination_In); //mGetNavMeshMsg.mNavMesh_Out.AddSearchNode(tmp.mDestination_In);

                // Attempt to set a new destination. Returns true in the case where the destination was 
                // different (and thus changed).
                if (mPlannerNavMesh.SetDestination(node))
                {
                    // If the destination changes, it means the low level search is no longer valid, and
                    // needs to wait for the high level search to complete first.
                    mPlannerTileMap.ClearDestination();
                }
            }
            else if (msg is ClearDestinationMessage)
            {
                mPlannerNavMesh.ClearDestination();
                mPlannerTileMap.ClearDestination();
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
                tmp.mBest_Out = mPlannerTileMap.pCurrentBest != null ? mPlannerTileMap.pCurrentBest : mPlannerNavMesh.pCurrentBest;
            }
        }

        /// <summary>
        /// Helper function for doing the work needed to go from a position to a GraphNode, and then finally
        /// sending that GraphNode to the Planner.
        /// </summary>
        /// <param name="pos">The position at which we wish to travel from.</param>
        private void SetSource(Vector2 pos)
        {
            WorldManager.pInstance.pCurrentLevel.OnMessage(mGetNavMeshMsg);

            // First, check if we are standing on an existing GraphNode. This is best case, 
            // as it means we don't need to create a new GraphNode (and do the associated
            // A* search to get cost to all other GraphNode objects in Graph).
            GraphNode node = mGetNavMeshMsg.mNavMesh_Out.FindNodeAt(pos);

            // If we don't find a GraphNode at the target position, we need to create a new
            // node to use in ou search.
            if (null == node)
            {
                // Create a node, but only do the links from this node to neighbours (not back
                // again).
                node = mGetNavMeshMsg.mNavMesh_Out.CreateOneWayGraphNode(pos); //mGetNavMeshMsg.mNavMesh_Out.GetClostestNode(tmp.mSource_In);//mGetNavMeshMsg.mNavMesh_Out.AddSearchNode(tmp.mSource_In);
            }

            // If we got a node to target, pass it on to the Planner.
            if (null != node)
            {
                mPlannerNavMesh.SetSource(node);

                // Find the TileGraphNode that maps to the location of node.
                mGetTileAtPositionMsg.Reset();
                mGetTileAtPositionMsg.mPosition_In = pos;
                WorldManager.pInstance.pCurrentLevel.OnMessage(mGetTileAtPositionMsg);

                mPlannerTileMap.SetSource(mGetTileAtPositionMsg.mTile_Out.mGraphNode);
            }
        }
    }
}
