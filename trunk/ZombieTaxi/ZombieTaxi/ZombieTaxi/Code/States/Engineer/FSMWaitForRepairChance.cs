using System;
using MBHEngine.StateMachine;
using MBHEngine.Behaviour;
using MBHEngine.GameObject;
using Microsoft.Xna.Framework;
using MBHEngine.Math;
using ZombieTaxi.Behaviours;

namespace ZombieTaxi.States.Engineer
{
    /// <summary>
    /// When there are no objects to repair we go to this state and wait for something to get 
    /// damaged.
    /// </summary>
    class FSMStateWaitForRepairChance : FSMState
    {
        /// <summary>
        /// Tracks how long we have been waiting at the Patrol Point.
        /// </summary>
        private StopWatch mWait;

        /// <summary>
        /// Preallocated messages.
        /// </summary>
        private SpriteRender.SetActiveAnimationMessage mSetActiveAnimationMsg;

        /// <summary>
        /// Constructor.
        /// </summary>
        public FSMStateWaitForRepairChance()
        {
            mSetActiveAnimationMsg = new SpriteRender.SetActiveAnimationMessage();
        }

        /// <summary>
        /// Called once when the state starts.
        /// </summary>
        public override void OnBegin()
        {
            mWait = StopWatchManager.pInstance.GetNewStopWatch();
            mWait.pLifeTime = 60 * 5; // Assuming 60fps.

            mSetActiveAnimationMsg.mAnimationSetName_In = "Idle";
            pParentGOH.OnMessage(mSetActiveAnimationMsg);
        }

        /// <summary>
        /// Call repeatedly until it returns a valid new state to transition to.
        /// </summary>
        /// <returns>Identifier of a state to transition to.</returns>
        public override String OnUpdate()
        {
            // Have we been waiting long enough yet?
            if (mWait.IsExpired())
            {
                return "Repair";
            }

            return null;
        }

        /// <summary>
        /// Called once when leaving this state.  Called the frame after the Update which returned
        /// a valid state to transition to.  This is a chance to do any clean up needed.
        /// </summary>
        public override void OnEnd()
        {
            StopWatchManager.pInstance.RecycleStopWatch(mWait);
        }
    }
}
