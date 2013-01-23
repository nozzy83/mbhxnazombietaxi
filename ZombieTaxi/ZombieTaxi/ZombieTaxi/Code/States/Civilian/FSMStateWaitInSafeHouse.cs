using System;
using MBHEngine.StateMachine;
using MBHEngine.Behaviour;
using MBHEngine.Math;

namespace ZombieTaxi.States.Civilian
{
    /// <summary>
    /// State where the Game Object has reached the safehouse and should now wait there for
    /// extraction.
    /// </summary>
    class FSMStateWaitInSafeHouse : FSMState
    {
        /// <summary>
        /// Waiting in the safe house cycles between this state and FSMStateWanderInSafeHouse.  In this state
        /// we stand around for a set period of time.  This is that time.
        /// </summary>
        private StopWatch mWatch;

        /// <summary>
        /// Preallocate messages to avoid GC.
        /// </summary>
        private SpriteRender.SetActiveAnimationMessage mSetActiveAnimationMsg;

        /// <summary>
        /// Constructor.
        /// </summary>
        public FSMStateWaitInSafeHouse()
        {
            mWatch = StopWatchManager.pInstance.GetNewStopWatch();
            mWatch.pLifeTime = 90;

            mSetActiveAnimationMsg = new SpriteRender.SetActiveAnimationMessage();
        }

        /// <summary>
        /// Destrucor.
        /// </summary>
        ~FSMStateWaitInSafeHouse()
        {
            StopWatchManager.pInstance.RecycleStopWatch(mWatch);
        }

        /// <summary>
        /// Called once when the state starts.
        /// </summary>
        public override void OnBegin()
        {
            mSetActiveAnimationMsg.mAnimationSetName = "Idle";
            pParentGOH.OnMessage(mSetActiveAnimationMsg);

            mWatch.Restart();
        }

        /// <summary>
        /// Call repeatedly until it returns a valid new state to transition to.
        /// </summary>
        /// <returns>Identifier of a state to transition to.</returns>
        public override String OnUpdate()
        {
            // Has enough time passed that we should try moving to a new space?
            if (mWatch.IsExpired())
            {
                return "WanderInSafeHouse";
            }

            return null;
        }
    }
}
