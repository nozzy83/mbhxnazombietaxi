using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MBHEngine.Behaviour;
using Microsoft.Xna.Framework;
using MBHEngine.GameObject;
using MBHEngine.StateMachine;
using ZombieTaxiContentDefs;
using Microsoft.Xna.Framework.Graphics;

namespace ZombieTaxi.Behaviours
{
    /// <summary>
    /// The civilian type of stranded.
    /// </summary>
    class Civilian : MBHEngine.Behaviour.FiniteStateMachine
    {

        #region FSMStates

        /// <summary>
        /// State where the Game Object sits in a cowering pose.
        /// </summary>
        private class FSMStateCower : FSMState
        {
            /// <summary>
            /// Preallocate messages to avoid GC.
            /// </summary>
            private SpriteRender.SetActiveAnimationMessage mSetActiveAnimationMsg;

            /// <summary>
            /// Constructor.
            /// </summary>
            public FSMStateCower()
            {
                mSetActiveAnimationMsg = new SpriteRender.SetActiveAnimationMessage();
            }

            /// <summary>
            /// Called once when the state starts.
            /// </summary>
            public override void OnBegin()
            {
                mSetActiveAnimationMsg.mAnimationSetName = "Hide";
                pParentGOH.OnMessage(mSetActiveAnimationMsg);
            }

            /// <summary>
            /// Call repeatedly until it returns a valid new state to transition to.
            /// </summary>
            /// <returns>Identifier of a state to transition to.</returns>
            public override String OnUpdate()
            {
                if (pParentGOH.pCollisionRect.Intersects(GameObjectManager.pInstance.pPlayer.pCollisionRect))
                {
                    return "Follow";
                }

                return null;
            }
        }

        /// <summary>
        /// State where the game object follows its target.
        /// </summary>
        private class FSMStateFollowTarget : FSMState
        {
            /// <summary>
            /// Preallocate messages to avoid GC.
            /// </summary>
            private SpriteRender.SetActiveAnimationMessage mSetActiveAnimationMsg;
            private PathFind.SetDestinationMessage mSetDestinationMsg;
            private PathFind.SetSourceMessage mSetSourceMsg;
            private PathFind.GetCurrentBestNode mGetCurrentBestNodeMsg;

            /// <summary>
            /// Constructor.
            /// </summary>
            public FSMStateFollowTarget()
            {
                mSetActiveAnimationMsg = new SpriteRender.SetActiveAnimationMessage();
                mSetDestinationMsg = new PathFind.SetDestinationMessage();
                mSetSourceMsg = new PathFind.SetSourceMessage();
                mGetCurrentBestNodeMsg = new PathFind.GetCurrentBestNode();
            }

            /// <summary>
            /// Called once when the state starts.
            /// </summary>
            public override void OnBegin()
            {
                mSetActiveAnimationMsg.mAnimationSetName = "Walk";
                pParentGOH.OnMessage(mSetActiveAnimationMsg);

                mSetSourceMsg.mSource = pParentGOH.pOrientation.mPosition;
                pParentGOH.OnMessage(mSetSourceMsg);
                mSetDestinationMsg.mDestination = GameObjectManager.pInstance.pPlayer.pOrientation.mPosition;
                pParentGOH.OnMessage(mSetDestinationMsg);
            }

            /// <summary>
            /// Call repeatedly until it returns a valid new state to transition to.
            /// </summary>
            /// <returns>Identifier of a state to transition to.</returns>
            public override String OnUpdate()
            {
                Follow();

                if (Vector2.DistanceSquared(GameObjectManager.pInstance.pPlayer.pOrientation.mPosition, pParentGOH.pOrientation.mPosition) > 64 * 64)
                {
                    return "Cower";
                }
                else if (Vector2.DistanceSquared(GameObjectManager.pInstance.pPlayer.pOrientation.mPosition, pParentGOH.pOrientation.mPosition) < 16 * 16)
                {
                    return "Stay";
                }

                return null;
            }

            /// <summary>
            /// Called once when leaving this state.  Called the frame after the Update which returned
            /// a valid state to transition to.
            /// </summary>
            public override void OnEnd()
            {
                pParentGOH.pDirection.mForward = Vector2.Zero;
            }

            /// <summary>
            /// Logic for basic follow behaviour.  Uses path find behaviour to follow the player.
            /// </summary>
            /// <remarks>
            /// This is almost identicle to what is found in the Kamikaze Behaviour.  They should be combined.
            /// </remarks>
            private void Follow()
            {
                GameObject player = GameObjectManager.pInstance.pPlayer;

                // Get the curent path to the player. It may not be complete at this point, but should include enough
                // information to start moving.
                pParentGOH.OnMessage(mGetCurrentBestNodeMsg);

                // If we have a best node chosen (again maybe not a complete path, but the best so far), start
                // moving towards the next point on the path.
                if (mGetCurrentBestNodeMsg.mBest != null)
                {
                    // This is the node closest to the destination that we have found.
                    PathFind.PathNode p = mGetCurrentBestNodeMsg.mBest;

                    // Traverse back towards the source node until the previous one has already been reached.
                    // That means the current one is the next one that has not been reached yet.
                    // We also want to make sure we don't try to get to the starting node since we should be 
                    // standing on top of it already (hence the check for prev.prev).
                    while (p.mPrev != null && p.mPrev.mPrev != null && !p.mPrev.mReached)
                    {
                        p = p.mPrev;
                    }

                    // The distance to check agaist is based on the move speed, since that is the amount
                    // we will move this frame, and we want to avoid trying to hit the center point directly, since
                    // that will only happen if moving in 1 pixel increments.
                    // Also, we check double move speed because we are going to move this frame no matter what,
                    // so what we are really checking is, are we going to be ther NEXT update.
                    Single minDist = pParentGOH.pDirection.mSpeed * 2.0f;

                    // Once we are within one unit of the target consider it reached.
                    if (Vector2.Distance(p.mTile.mCollisionRect.pCenterPoint, pParentGOH.pOrientation.mPosition) <= minDist)
                    {
                        // This node has been reached, so next update it will start moving towards the next node.
                        p.mReached = true;

                        // Recalculate the path every time we reach a node in the path.  This accounts for things like
                        // the target moving.
                        //DebugMessageDisplay.pInstance.AddConstantMessage("Reached target.  Setting new destination.");

                        mSetSourceMsg.mSource = pParentGOH.pOrientation.mPosition;
                        pParentGOH.OnMessage(mSetSourceMsg);
                        mSetDestinationMsg.mDestination = player.pOrientation.mPosition;
                        pParentGOH.OnMessage(mSetDestinationMsg);
                    }

                    //DebugMessageDisplay.pInstance.AddConstantMessage("Moving towards target.");

                    // Move towards the nodes center point.
                    Vector2 d = p.mTile.mCollisionRect.pCenterPoint - pParentGOH.pOrientation.mPosition;
                    if (d.Length() != 0.0f)
                    {
                        d = Vector2.Normalize(d);
                        pParentGOH.pDirection.mForward = d;
                    }
                }
                else
                {
                    //DebugMessageDisplay.pInstance.AddConstantMessage("Setting first path destination.");

                    // If we don't have a destination set yet, set it up now.
                    mSetSourceMsg.mSource = pParentGOH.pOrientation.mPosition;
                    pParentGOH.OnMessage(mSetSourceMsg);
                    mSetDestinationMsg.mDestination = player.pOrientation.mPosition;
                    pParentGOH.OnMessage(mSetDestinationMsg);
                }
            }
        }

        /// <summary>
        /// State where the Game Object stands in place waiting for the target to get far enough away
        /// to trigger a transition back to the follow state.
        /// </summary>
        private class FSMStateStay : FSMState
        {
            /// <summary>
            /// Preallocate messages to avoid GC.
            /// </summary>
            private SpriteRender.SetActiveAnimationMessage mSetActiveAnimationMsg;

            /// <summary>
            /// Constructor.
            /// </summary>
            public FSMStateStay()
            {
                mSetActiveAnimationMsg = new SpriteRender.SetActiveAnimationMessage();
            }

            /// <summary>
            /// Called once when the state starts.
            /// </summary>
            public override void OnBegin()
            {
                mSetActiveAnimationMsg.mAnimationSetName = "Idle";
                pParentGOH.OnMessage(mSetActiveAnimationMsg);
            }

            /// <summary>
            /// Call repeatedly until it returns a valid new state to transition to.
            /// </summary>
            /// <returns>Identifier of a state to transition to.</returns>
            public override String OnUpdate()
            {
                if (Vector2.DistanceSquared(GameObjectManager.pInstance.pPlayer.pOrientation.mPosition, pParentGOH.pOrientation.mPosition) > 24 * 24)
                {
                    return "Follow";
                }

                return null;
            }
        }

        #endregion // FSMStates

        /// <summary>
        /// Preallocate messages to avoid GC.
        /// </summary>
        private SpriteRender.SetSpriteEffectsMessage mSetSpriteFxMsg;

        /// <summary>
        /// Constructor which also handles the process of loading in the Behaviour
        /// Definition information.
        /// </summary>
        /// <param name="parentGOH">The game object that this behaviour is attached to.</param>
        /// <param name="fileName">The file defining this behaviour.</param>
        public Civilian(GameObject parentGOH, String fileName)
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

            CivilianDefinition def = GameObjectManager.pInstance.pContentManager.Load<CivilianDefinition>(fileName);

            //mFSM = new MBHEngine.StateMachine.FiniteStateMachine(mParentGOH);
            AddState(new FSMStateCower(), "Cower");
            AddState(new FSMStateFollowTarget(), "Follow");
            AddState(new FSMStateStay(), "Stay");

            mParentGOH.pDirection.mSpeed = 0.5f;

            mSetSpriteFxMsg = new SpriteRender.SetSpriteEffectsMessage();
        }

        /// <summary>
        /// Called once per frame by the game object.
        /// </summary>
        /// <param name="gameTime">The amount of time that has passed this frame.</param>
        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (mParentGOH.pDirection.mForward.X < 0)
            {
                mSetSpriteFxMsg.mSpriteEffects = SpriteEffects.FlipHorizontally;
                mParentGOH.OnMessage(mSetSpriteFxMsg);
            }
            else if (mParentGOH.pDirection.mForward.X > 0)
            {
                mSetSpriteFxMsg.mSpriteEffects = SpriteEffects.None;
                mParentGOH.OnMessage(mSetSpriteFxMsg);
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
            base.OnMessage(ref msg);
        }
    }
}
