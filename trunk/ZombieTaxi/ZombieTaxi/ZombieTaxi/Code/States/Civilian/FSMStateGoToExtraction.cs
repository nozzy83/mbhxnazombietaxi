using System;
using MBHEngine.StateMachine;
using MBHEngine.Behaviour;
using MBHEngine.GameObject;
using Microsoft.Xna.Framework;
using ZombieTaxi.Behaviours.HUD;

namespace ZombieTaxi.States.Civilian
{
    /// <summary>
    /// State where the game object follows its target.
    /// </summary>
    class FSMStateGoToExtraction : FSMState
    {
        /// <summary>
        /// Preallocate messages to avoid GC.
        /// </summary>
        private SpriteRender.SetActiveAnimationMessage mSetActiveAnimationMsg;
        private PathFind.SetDestinationMessage mSetDestinationMsg;
        private PathFind.SetSourceMessage mSetSourceMsg;
        private PathFind.GetCurrentBestNodeMessage mGetCurrentBestNodeMsg;
        private ZombieTaxi.Behaviours.Civilian.GetExtractionPointMessage mGetExtractionPointMsg;
        private PlayerScore.IncrementScoreMessage mIncrementScoreMsg;

        /// <summary>
        /// Constructor.
        /// </summary>
        public FSMStateGoToExtraction()
        {
            mSetActiveAnimationMsg = new SpriteRender.SetActiveAnimationMessage();
            mSetDestinationMsg = new PathFind.SetDestinationMessage();
            mSetSourceMsg = new PathFind.SetSourceMessage();
            mGetCurrentBestNodeMsg = new PathFind.GetCurrentBestNodeMessage();
            mGetExtractionPointMsg = new ZombieTaxi.Behaviours.Civilian.GetExtractionPointMessage();
            mIncrementScoreMsg = new PlayerScore.IncrementScoreMessage();
            mIncrementScoreMsg.mAmount_In = 500;
        }

        /// <summary>
        /// Called once when the state starts.
        /// </summary>
        public override void OnBegin()
        {
            mSetActiveAnimationMsg.mAnimationSetName_In = "Run";
            pParentGOH.OnMessage(mSetActiveAnimationMsg);

            pParentGOH.OnMessage(mGetExtractionPointMsg);

            mSetSourceMsg.mSource_In = pParentGOH.pPosition + pParentGOH.pCollisionRoot;
            pParentGOH.OnMessage(mSetSourceMsg);
            mSetDestinationMsg.mDestination_In = mGetExtractionPointMsg.mExtractionPoint_Out.pPosition + pParentGOH.pCollisionRoot;
            pParentGOH.OnMessage(mSetDestinationMsg);

        }

        /// <summary>
        /// Call repeatedly until it returns a valid new state to transition to.
        /// </summary>
        /// <returns>Identifier of a state to transition to.</returns>
        public override String OnUpdate()
        {
            if (Follow())
            {
                // The player gets a bunch of points for rescuing people!
                GameObjectManager.pInstance.BroadcastMessage(mIncrementScoreMsg, pParentGOH);

                GameObjectManager.pInstance.Remove(pParentGOH);
            }

            return null;
        }

        /// <summary>
        /// Called once when leaving this state.  Called the frame after the Update which returned
        /// a valid state to transition to.
        /// </summary>
        public override void OnEnd()
        {
            // Clear the forward direction of this object so that it doesn't keep moving.
            pParentGOH.pDirection.mForward = Vector2.Zero;
        }

        /// <summary>
        /// Logic for basic follow behaviour.
        /// </summary>
        /// <returns>True if the destination has been reached.</returns>
        /// <remarks>
        /// This is almost identicle to what is found in the Kamikaze Behaviour.  They should be combined.
        /// </remarks>
        private Boolean Follow()
        {
            GameObject player = GameObjectManager.pInstance.pPlayer;

            // Get the curent path to the player. It may not be complete at this point, but should include enough
            // information to start moving.
            pParentGOH.OnMessage(mGetCurrentBestNodeMsg);

            // If we have a best node chosen (again maybe not a complete path, but the best so far), start
            // moving towards the next point on the path.
            if (mGetCurrentBestNodeMsg.mBest_Out != null)
            {
                // This is the node closest to the destination that we have found.
                PathFind.PathNode p = mGetCurrentBestNodeMsg.mBest_Out;

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

                    if (p == mGetCurrentBestNodeMsg.mBest_Out)
                    {
                        return true;
                    }
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

            return false;
        }
    }
}
