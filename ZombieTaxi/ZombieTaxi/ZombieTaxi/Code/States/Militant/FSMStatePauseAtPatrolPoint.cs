using System;
using MBHEngine.StateMachine;
using MBHEngine.Behaviour;
using MBHEngine.GameObject;
using Microsoft.Xna.Framework;
using MBHEngine.Math;

namespace ZombieTaxi.States.Militant
{
    /// <summary>
    /// State where the Game Object stands in place waiting for the target to get far enough away
    /// to trigger a transition back to the follow state.
    /// </summary>
    class FSMStatePauseAtPatrolPoint : FSMState
    {
        /// <summary>
        /// Tracks how long we have been waiting at the Patrol Point.
        /// </summary>
        private StopWatch mWait;

        /// <summary>
        /// Preallocate messages to avoid GC.
        /// </summary>
        private SpriteRender.SetActiveAnimationMessage mSetActiveAnimationMsg;

        /// <summary>
        /// Constructor.
        /// </summary>
        public FSMStatePauseAtPatrolPoint()
        {
            mSetActiveAnimationMsg = new SpriteRender.SetActiveAnimationMessage();
        }

        /// <summary>
        /// Called once when the state starts.
        /// </summary>
        public override void OnBegin()
        {
            mSetActiveAnimationMsg.mAnimationSetName_In = "Idle";
            pParentGOH.OnMessage(mSetActiveAnimationMsg);

            mWait = StopWatchManager.pInstance.GetNewStopWatch();
            mWait.pLifeTime = 60 * 4; // 4 seconds (assuming 60fps)
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
                return "Patrol";
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
