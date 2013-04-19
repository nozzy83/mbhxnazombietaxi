using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MBHEngine.GameObject;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MBHEngine.Behaviour
{
    /// <summary>
    /// Very basic path following behaviour. If given a Target every time it reaches a 
    /// node in the path, it will redo the search. The behaviour requires that there
    /// also be a PathFind Behaviour on mParentGOH.
    /// </summary>
    public class PathFollow : MBHEngine.Behaviour.Behaviour
    {
        /// <summary>
        /// Sent when this object has reached the final node in the path.
        /// </summary>
        public class OnReachedPathEndMessage : BehaviourMessage
        {
            /// <summary>
            /// Call this to put a message back to its default state.
            /// </summary>
            public override void Reset() { }
        }

        /// <summary>
        /// Set an optional Target for the Path Follower. With this set, every time mParentGOH
        /// reaches a new node in the path, it with do an updated path search to mTarget. This
        /// is to handle following a moving target.
        /// </summary>
        public class SetTargetObjectMessage : BehaviourMessage
        {
            /// <summary>
            /// A GameObject which the PathFollow will attempt to keep an up to date Path to, even 
            /// if that object is moving. In fact, this should not be set unless the object can be
            /// moving.
            /// </summary>
            public GameObject.GameObject mTarget_In;

            /// <summary>
            /// Call this to put a message back to its default state.
            /// </summary>
            public override void Reset()
            {
                mTarget_In = null;
            }
        }

        /// <summary>
        /// A GameObject that we are trying to reach. Only needed if the target can move.
        /// </summary>
        private GameObject.GameObject mTarget;

        /// <summary>
        /// Preallocate messages to avoid GC.
        /// </summary>
        private SpriteRender.SetActiveAnimationMessage mSetActiveAnimationMsg;
        private PathFind.SetDestinationMessage mSetDestinationMsg;
        private PathFind.SetSourceMessage mSetSourceMsg;
        private PathFind.GetCurrentBestNodeMessage mGetCurrentBestNodeMsg;
        private PathFind.ClearDestinationMessage mClearDestinationMsg;
        private OnReachedPathEndMessage mOnReachedPathEndMsg;

        /// <summary>
        /// Constructor which also handles the process of loading in the Behaviour
        /// Definition information.
        /// </summary>
        /// <param name="parentGOH">The game object that this behaviour is attached to.</param>
        /// <param name="fileName">The file defining this behaviour.</param>
        public PathFollow(GameObject.GameObject parentGOH, String fileName)
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

            //ExampleDefinition def = GameObjectManager.pInstance.pContentManager.Load<ExampleDefinition>(fileName);

            // By default we have no target.
            mTarget = null;

            mSetActiveAnimationMsg = new SpriteRender.SetActiveAnimationMessage(); ;
            mSetDestinationMsg = new PathFind.SetDestinationMessage();
            mSetSourceMsg = new PathFind.SetSourceMessage();
            mGetCurrentBestNodeMsg = new PathFind.GetCurrentBestNodeMessage();
            mClearDestinationMsg = new PathFind.ClearDestinationMessage();
            mOnReachedPathEndMsg = new OnReachedPathEndMessage();
        }

        /// <summary>
        /// Called when the Behaviour goes from being disabled to enabled.
        /// This will NOT be called if the behaviour initialially starts enabled.
        /// </summary>
        public override void OnEnable()
        {
            // If we don't have a destination set yet, set it up now.
            mSetSourceMsg.mSource_In = mParentGOH.pPosition + mParentGOH.pCollisionRoot;
            mParentGOH.OnMessage(mSetSourceMsg);

            if (mTarget != null)
            {
                mSetDestinationMsg.mDestination_In = mTarget.pPosition + mParentGOH.pCollisionRoot;
                mParentGOH.OnMessage(mSetDestinationMsg);
            }
        }

        /// <summary>
        /// Called once per frame by the game object.
        /// </summary>
        /// <param name="gameTime">The amount of time that has passed this frame.</param>
        public override void Update(GameTime gameTime)
        {
            // Get the curent path to the player. It may not be complete at this point, but should include enough
            // information to start moving.
            mParentGOH.OnMessage(mGetCurrentBestNodeMsg);

            // If we have a best node chosen (again maybe not a complete path, but the best so far), start
            // moving towards the next point on the path.
            if (mGetCurrentBestNodeMsg.mBest_Out != null && mGetCurrentBestNodeMsg.mBest_Out.pPathSolved)
            {
                // This is the node closest to the destination that we have found.
                MBHEngine.PathFind.GenericAStar.PathNode p = mGetCurrentBestNodeMsg.mBest_Out;

                // Traverse back towards the source node until the previous one has already been reached.
                // That means the current one is the next one that has not been reached yet.
                // We also want to make sure we don't try to get to the starting node since we should be 
                // standing on top of it already (hence the check for prev.prev).
                while (p.pPrevious != null && p.pPrevious.pPrevious != null && !p.pPrevious.pReached)
                {
                    p = p.pPrevious;
                }

                // If the current node is flagged as being reached, that means that every node
                // in the path has been reached.
                if (p.pReached)
                {
                    mParentGOH.OnMessage(mOnReachedPathEndMsg);

                    // If we don't have a dynamically moving target there is nothing left to do here.
                    if (null != mTarget)
                    {
                        mParentGOH.pDirection.mForward = Vector2.Zero;
                        return;
                    }
                }

                // The distance to check agaist is based on the move speed, since that is the amount
                // we will move this frame, and we want to avoid trying to hit the center point directly, since
                // that will only happen if moving in 1 pixel increments.
                // Also, we check double move speed because we are going to move this frame no matter what,
                // so what we are really checking is, are we going to be ther NEXT update.
                Single minDist = mParentGOH.pDirection.mSpeed * 2.0f;

                // Once we are within one unit of the target consider it reached.
                if (null != p.pGraphNode && Vector2.Distance(p.pGraphNode.pPosition + new Vector2(0.0f, 4.0f), mParentGOH.pPosition) <= minDist)
                {
                    // This node has been reached, so next update it will start moving towards the next node.
                    p.pReached = true;

                    // Recalculate the path every time we reach a node in the path.  This accounts for things like
                    // the target moving.
                    //DebugMessageDisplay.pInstance.AddConstantMessage("Reached target.  Setting new destination.");

                    // If we have an mTarget, then we need to update the PathFind behaviour in case that
                    // that object has moved since we original found this path.
                    if (null != mTarget)
                    {
                        mParentGOH.OnMessage(mClearDestinationMsg);
                        mSetSourceMsg.mSource_In = mParentGOH.pPosition + mParentGOH.pCollisionRoot;
                        mParentGOH.OnMessage(mSetSourceMsg);
                        mSetDestinationMsg.mDestination_In = mTarget.pPosition + mParentGOH.pCollisionRoot;
                        mParentGOH.OnMessage(mSetDestinationMsg);
                    }
                }

                //DebugMessageDisplay.pInstance.AddConstantMessage("Moving towards target.");

                if (p.pGraphNode != null && p.pGraphNode.pData != null)
                {
                    // Move towards the nodes center point.
                    Vector2 d = p.pGraphNode.pPosition - mParentGOH.pPosition + new Vector2(0.0f, 4.0f);
                    if (d.Length() != 0.0f)
                    {
                        d = Vector2.Normalize(d);
                        mParentGOH.pDirection.mForward = d;
                    }
                }
            }
            else if(null != mTarget)
            {

                mParentGOH.pDirection.mForward = Vector2.Zero;
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
            if (msg is SetTargetObjectMessage)
            {
                SetTargetObjectMessage temp = (SetTargetObjectMessage)msg;

                mTarget = temp.mTarget_In;

                if (null != mTarget)
                {
                    mSetDestinationMsg.mDestination_In = mTarget.pPosition + mParentGOH.pCollisionRoot;
                    mParentGOH.OnMessage(mSetDestinationMsg);
                }
            }
            else if (msg is PathFind.OnPathFindFailedMessage)
            {
                PathFind.OnPathFindFailedMessage temp = (PathFind.OnPathFindFailedMessage)msg;

                // Handle the case were the user places a tile right on top of the destination.
                if (null != mTarget && temp.mReason == PathFind.OnPathFindFailedMessage.Reason.InvalidLocation)
                {
                    mSetDestinationMsg.mDestination_In = mTarget.pPosition + mParentGOH.pCollisionRoot;
                    mParentGOH.OnMessage(mSetDestinationMsg);
                }
            }
        }
    }
}
