using System;
using System.Collections.Generic;
using MBHEngine.StateMachine;
using MBHEngine.Behaviour;
using MBHEngine.GameObject;
using MBHEngineContentDefs;
using MBHEngine.Debug;
using Microsoft.Xna.Framework;
using ZombieTaxi.Behaviours.HUD;

namespace ZombieTaxi.States.Civilian
{
    /// <summary>
    /// State where the game object follows its target.
    /// </summary>
    class FSMStateFollowTarget : FSMState
    {
        /// <summary>
        /// Used to store any safe houses in range.
        /// </summary>
        private List<GameObject> mSafeHouseInRange;

        /// <summary>
        /// A list of the types of objects that this does damage to when exploding.
        /// </summary>
        private List<GameObjectDefinition.Classifications> mSafeHouseClassifications;

        /// <summary>
        /// Preallocate messages to avoid GC.
        /// </summary>
        private SpriteRender.SetActiveAnimationMessage mSetActiveAnimationMsg;
        private PathFind.SetDestinationMessage mSetDestinationMsg;
        private PathFind.SetSourceMessage mSetSourceMsg;
        private PathFind.GetCurrentBestNodeMessage mGetCurrentBestNodeMsg;
        private PathFind.ClearDestinationMessage mClearDestinationMsg;
        private PlayerScore.IncrementScoreMessage mIncrementScoreMsg;
        private PathFollow.SetTargetObjectMessage mSetTargetObjectMsg;

        /// <summary>
        /// Constructor.
        /// </summary>
        public FSMStateFollowTarget()
        {
            // We need to detect when the GameObject reaches a safe house and to do so we need
            // to do a collision check against all objects of a particular classification, in this
            // case "SAFE_HOUSE".  We preallocate the two lists needed to do the check to avoid
            // triggering the GC.
            mSafeHouseInRange = new List<GameObject>(16);
            mSafeHouseClassifications = new List<GameObjectDefinition.Classifications>();
            mSafeHouseClassifications.Add(GameObjectDefinition.Classifications.SAFE_HOUSE);

            mSetActiveAnimationMsg = new SpriteRender.SetActiveAnimationMessage();
            mSetDestinationMsg = new PathFind.SetDestinationMessage();
            mSetSourceMsg = new PathFind.SetSourceMessage();
            mGetCurrentBestNodeMsg = new PathFind.GetCurrentBestNodeMessage();
            mClearDestinationMsg = new PathFind.ClearDestinationMessage();
            mIncrementScoreMsg = new PlayerScore.IncrementScoreMessage();
            mIncrementScoreMsg.mAmount = 100;
            mSetTargetObjectMsg = new PathFollow.SetTargetObjectMessage();
        }

        /// <summary>
        /// Called once when the state starts.
        /// </summary>
        public override void OnBegin()
        {
            mSetActiveAnimationMsg.mAnimationSetName = "Run";
            pParentGOH.OnMessage(mSetActiveAnimationMsg);

            mSetSourceMsg.mSource = pParentGOH.pCollisionRect.pCenterPoint;
            pParentGOH.OnMessage(mSetSourceMsg);
            mSetDestinationMsg.mDestination = GameObjectManager.pInstance.pPlayer.pPosition;
            pParentGOH.OnMessage(mSetDestinationMsg);

            pParentGOH.SetBehaviourEnabled<PathFollow>(true);

            mSetTargetObjectMsg.mTarget = GameObjectManager.pInstance.pPlayer;
            pParentGOH.OnMessage(mSetTargetObjectMsg);
        }

        /// <summary>
        /// Call repeatedly until it returns a valid new state to transition to.
        /// </summary>
        /// <returns>Identifier of a state to transition to.</returns>
        public override String OnUpdate()
        {
            //Follow();

            // After moving this frame, check if we are within rangle of a SafeHouse.
            mSafeHouseInRange.Clear();
            GameObjectManager.pInstance.GetGameObjectsInRange(pParentGOH, ref mSafeHouseInRange, mSafeHouseClassifications);

            // Are we intersecting with any safehouses?
            if (mSafeHouseInRange.Count != 0)
            {
#if ALLOW_GARBAGE
                DebugMessageDisplay.pInstance.AddConstantMessage("Reached SafeHouse.");
#endif
                // For every civilian we save, increment the score a little.
                GameObjectManager.pInstance.BroadcastMessage(mIncrementScoreMsg, pParentGOH);

                // Now just stand around for a little bit.
                return "GoToStandingPosition";
            }
            // Has the Player run too far away causing us to get scared?
            else if (Vector2.DistanceSquared(GameObjectManager.pInstance.pPlayer.pPosition, pParentGOH.pPosition) > 64 * 64)
            {
                return "Cower";
            }
            // Are we close enough that we should just stand still until the player starts moving again.
            else if (Vector2.DistanceSquared(GameObjectManager.pInstance.pPlayer.pPosition, pParentGOH.pPosition) < 16 * 16)
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
            pParentGOH.OnMessage(mClearDestinationMsg);

            // Clear the forward direction of this object so that it doesn't keep moving.
            pParentGOH.pDirection.mForward = Vector2.Zero;

            pParentGOH.SetBehaviourEnabled<PathFollow>(false);
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
                if (Vector2.Distance(p.mTile.mCollisionRect.pCenterBottom, pParentGOH.pPosition) <= minDist)
                {
                    // This node has been reached, so next update it will start moving towards the next node.
                    p.mReached = true;

                    // Recalculate the path every time we reach a node in the path.  This accounts for things like
                    // the target moving.
                    //DebugMessageDisplay.pInstance.AddConstantMessage("Reached target.  Setting new destination.");

                    mSetSourceMsg.mSource = pParentGOH.pPosition + pParentGOH.pCollisionRoot;
                    pParentGOH.OnMessage(mSetSourceMsg);
                    mSetDestinationMsg.mDestination = player.pPosition + pParentGOH.pCollisionRoot;
                    pParentGOH.OnMessage(mSetDestinationMsg);
                }

                //DebugMessageDisplay.pInstance.AddConstantMessage("Moving towards target.");

                // Move towards the nodes center point.
                Vector2 d = p.mTile.mCollisionRect.pCenterBottom - pParentGOH.pPosition;
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
                mSetSourceMsg.mSource = pParentGOH.pPosition + pParentGOH.pCollisionRoot;
                pParentGOH.OnMessage(mSetSourceMsg);
                mSetDestinationMsg.mDestination = player.pPosition + pParentGOH.pCollisionRoot;
                pParentGOH.OnMessage(mSetDestinationMsg);
            }
        }
    }
}
