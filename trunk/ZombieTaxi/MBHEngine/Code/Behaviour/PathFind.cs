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
        /// The planner used to find a path.
        /// </summary>
        public MBHEngine.PathFind.GenericAStar.Planner mPlanner;

        /// <summary>
        /// Preallocated to avoid garbage at runtime.
        /// </summary>
        private OnPathFindFailedMessage mOnPathFindFailedMsg;
        private MBHEngine.Behaviour.Level.GetTileAtPositionMessage mGetTileAtPositionMsg;

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

            mPlanner = new MBHEngine.PathFind.GenericAStar.Planner();
            
            // Preallocate messages to avoid GC during gameplay.
            //
            mOnPathFindFailedMsg = new OnPathFindFailedMessage();
            mGetTileAtPositionMsg = new Level.GetTileAtPositionMessage();
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
                mGetTileAtPositionMsg.mPosition_In = mParentGOH.pPosition + mParentGOH.pCollisionRoot;
                WorldManager.pInstance.pCurrentLevel.OnMessage(mGetTileAtPositionMsg);

                // Update the source incase the GO has moved since the last update.
                mPlanner.SetSource(mGetTileAtPositionMsg.mTile_Out.mGraphNode);
            }

            MBHEngine.PathFind.GenericAStar.Planner.Result res = mPlanner.PlanPath();

            // If the planner failed to find the destination tell the other behaviours.
            if (res == MBHEngine.PathFind.GenericAStar.Planner.Result.Failed)
            {
                mParentGOH.OnMessage(mOnPathFindFailedMsg);
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

                mGetTileAtPositionMsg.mPosition_In = tmp.mDestination_In;
                WorldManager.pInstance.pCurrentLevel.OnMessage(mGetTileAtPositionMsg);

                mPlanner.SetDestination(mGetTileAtPositionMsg.mTile_Out.mGraphNode);
            }
            else if (msg is ClearDestinationMessage)
            {
                mPlanner.ClearDestination();
            }
            else if (msg is SetSourceMessage)
            {
                SetSourceMessage tmp = (SetSourceMessage)msg;

                mGetTileAtPositionMsg.mPosition_In = tmp.mSource_In;
                WorldManager.pInstance.pCurrentLevel.OnMessage(mGetTileAtPositionMsg);

                // Update the source incase the GO has moved since the last update.
                mPlanner.SetSource(mGetTileAtPositionMsg.mTile_Out.mGraphNode);
            }
            else if (msg is GetCurrentBestNodeMessage)
            {
                GetCurrentBestNodeMessage tmp = (GetCurrentBestNodeMessage)msg;
                tmp.mBest_Out = mPlanner.pCurrentBest;
            }
        }
    }
}
