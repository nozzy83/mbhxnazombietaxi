using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MBHEngine.Behaviour;
using Microsoft.Xna.Framework;
using MBHEngine.GameObject;
using ZombieTaxiContentDefs;
using Microsoft.Xna.Framework.Graphics;

namespace ZombieTaxi.Behaviours
{
    /// <summary>
    /// The civilian type of stranded.
    /// </summary>
    class Civilian : MBHEngine.Behaviour.Behaviour
    {
        /// <summary>
        /// The different main state of this behaviour.
        /// </summary>
        private enum States
        {
            COWER = 0,
            FOLLOW_TARGET,
            STAY,

            INITIAL_STATE = COWER,
        };

        /// <summary>
        /// Each state goes through the same 3 sub states.  Enter and Exit which enter for 1 frame
        /// surround the Update state.
        /// </summary>
        private enum SubStates
        {
            ENTER = 0,
            UPDATE,
            EXIT,

            INITIAL_STATE = ENTER,
        };

        /// <summary>
        /// The state currently running.
        /// </summary>
        private States mCurrentState;

        /// <summary>
        /// The state we want to transition to.
        /// </summary>
        private States mNextState;

        /// <summary>
        /// The substate we are in right now.  There is no "NextSubState" because this just
        /// goes through in a linear order, so there is need to delay the next one.
        /// </summary>
        private SubStates mCurrentSubState;

        /// <summary>
        /// Preallocate messages to avoid GC.
        /// </summary>
        private SpriteRender.SetActiveAnimationMessage mSetActiveAnimationMsg;
        private PathFind.SetDestinationMessage mSetDestinationMsg;
        private PathFind.SetSourceMessage mSetSourceMsg;
        private PathFind.GetCurrentBestNode mGetCurrentBestNodeMsg;
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

            mCurrentState = mNextState = States.INITIAL_STATE;
            mCurrentSubState = SubStates.INITIAL_STATE;

            mParentGOH.pDirection.mSpeed = 0.5f;

            mSetActiveAnimationMsg = new SpriteRender.SetActiveAnimationMessage();
            mSetDestinationMsg = new PathFind.SetDestinationMessage();
            mSetSourceMsg = new PathFind.SetSourceMessage();
            mGetCurrentBestNodeMsg = new PathFind.GetCurrentBestNode();
            mSetSpriteFxMsg = new SpriteRender.SetSpriteEffectsMessage();
        }

        /// <summary>
        /// Called once per frame by the game object.
        /// </summary>
        /// <param name="gameTime">The amount of time that has passed this frame.</param>
        public override void Update(GameTime gameTime)
        {
            // If we have a new state requested and we have finished exiting the previous state, then
            // move on to the next state.
            if (mCurrentState != mNextState &&
                mCurrentSubState != SubStates.EXIT)
            {
                mCurrentState = mNextState;
            }

            switch (mCurrentState)
            {
                case States.COWER:
                    {
                        switch (mCurrentSubState)
                        {
                            case SubStates.ENTER:
                                {
                                    mSetActiveAnimationMsg.mAnimationSetName = "Hide";
                                    mParentGOH.OnMessage(mSetActiveAnimationMsg);

                                    mCurrentSubState = SubStates.UPDATE;

                                    break;
                                }
                            case SubStates.UPDATE:
                                {
                                    if (mParentGOH.pCollisionRect.Intersects(GameObjectManager.pInstance.pPlayer.pCollisionRect))
                                    {
                                        mNextState = States.FOLLOW_TARGET;
                                        mCurrentSubState = SubStates.EXIT;
                                    }

                                    break;
                                }
                            case SubStates.EXIT:
                                {
                                    mCurrentSubState = SubStates.ENTER;

                                    break;
                                }
                        }

                        break;
                    }
                case States.FOLLOW_TARGET:
                    {
                        switch (mCurrentSubState)
                        {
                            case SubStates.ENTER:
                                {
                                    mSetActiveAnimationMsg.mAnimationSetName = "Walk";
                                    mParentGOH.OnMessage(mSetActiveAnimationMsg);

                                    mSetSourceMsg.mSource = mParentGOH.pOrientation.mPosition;
                                    mParentGOH.OnMessage(mSetSourceMsg);
                                    mSetDestinationMsg.mDestination = GameObjectManager.pInstance.pPlayer.pOrientation.mPosition;
                                    mParentGOH.OnMessage(mSetDestinationMsg);

                                    mCurrentSubState = SubStates.UPDATE;

                                    break;
                                }
                            case SubStates.UPDATE:
                                {
                                    Follow();

                                    if (Vector2.DistanceSquared(GameObjectManager.pInstance.pPlayer.pOrientation.mPosition, mParentGOH.pOrientation.mPosition) > 64 * 64)
                                    {
                                        mNextState = States.COWER;
                                        mCurrentSubState = SubStates.EXIT;
                                    }
                                    else if (Vector2.DistanceSquared(GameObjectManager.pInstance.pPlayer.pOrientation.mPosition, mParentGOH.pOrientation.mPosition) < 16 * 16)
                                    {
                                        mNextState = States.STAY;
                                        mCurrentSubState = SubStates.EXIT;
                                    }

                                    break;
                                }
                            case SubStates.EXIT:
                                {
                                    mParentGOH.pDirection.mForward = Vector2.Zero;

                                    mCurrentSubState = SubStates.ENTER;

                                    break;
                                }
                        }

                        break;
                    }
                case States.STAY:
                    {
                        switch (mCurrentSubState)
                        {
                            case SubStates.ENTER:
                                {
                                    mSetActiveAnimationMsg.mAnimationSetName = "Idle";
                                    mParentGOH.OnMessage(mSetActiveAnimationMsg);

                                    mCurrentSubState = SubStates.UPDATE;

                                    break;
                                }
                            case SubStates.UPDATE:
                                {
                                    if (Vector2.DistanceSquared(GameObjectManager.pInstance.pPlayer.pOrientation.mPosition, mParentGOH.pOrientation.mPosition) > 24 * 24)
                                    {
                                        mNextState = States.FOLLOW_TARGET;
                                        mCurrentSubState = SubStates.EXIT;
                                    }

                                    break;
                                }
                            case SubStates.EXIT:
                                {
                                    mCurrentSubState = SubStates.ENTER;

                                    break;
                                }
                        }

                        break;
                    }
                default:
                    {
                        throw new Exception("Unhandled state.");
                    }
            }

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
        /// Resets a behaviour to its initial state.
        /// </summary>
        public override void Reset()
        {
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
            mParentGOH.OnMessage(mGetCurrentBestNodeMsg);

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
                Single minDist = mParentGOH.pDirection.mSpeed * 2.0f;

                // Once we are within one unit of the target consider it reached.
                if (Vector2.Distance(p.mTile.mCollisionRect.pCenterPoint, mParentGOH.pOrientation.mPosition) <= minDist)
                {
                    // This node has been reached, so next update it will start moving towards the next node.
                    p.mReached = true;

                    // Recalculate the path every time we reach a node in the path.  This accounts for things like
                    // the target moving.
                    //DebugMessageDisplay.pInstance.AddConstantMessage("Reached target.  Setting new destination.");

                    mSetSourceMsg.mSource = mParentGOH.pOrientation.mPosition;
                    mParentGOH.OnMessage(mSetSourceMsg);
                    mSetDestinationMsg.mDestination = player.pOrientation.mPosition;
                    mParentGOH.OnMessage(mSetDestinationMsg);
                }

                //DebugMessageDisplay.pInstance.AddConstantMessage("Moving towards target.");

                // Move towards the nodes center point.
                Vector2 d = p.mTile.mCollisionRect.pCenterPoint - mParentGOH.pOrientation.mPosition;
                if (d.Length() != 0.0f)
                {
                    d = Vector2.Normalize(d);
                    mParentGOH.pDirection.mForward = d;
                }
            }
            else
            {
                //DebugMessageDisplay.pInstance.AddConstantMessage("Setting first path destination.");

                // If we don't have a destination set yet, set it up now.
                mSetSourceMsg.mSource = mParentGOH.pOrientation.mPosition;
                mParentGOH.OnMessage(mSetSourceMsg);
                mSetDestinationMsg.mDestination = player.pOrientation.mPosition;
                mParentGOH.OnMessage(mSetDestinationMsg);
            }
        }
    }
}
