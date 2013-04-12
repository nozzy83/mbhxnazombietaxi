using System;
using MBHEngine.StateMachine;
using MBHEngine.Behaviour;
using MBHEngine.GameObject;
using ZombieTaxi.Behaviours;

namespace ZombieTaxi.States.Militant
{
    /// <summary>
    /// State where the Game Object sits in a cowering pose.
    /// </summary>
    class FSMStateCower : FSMState
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
            mSetActiveAnimationMsg.mAnimationSetName_In = "Idle";
            pParentGOH.OnMessage(mSetActiveAnimationMsg);

            pParentGOH.SetBehaviourEnabled<PointAndShoot>(false);
        }

        /// <summary>
        /// Call repeatedly until it returns a valid new state to transition to.
        /// </summary>
        /// <returns>Identifier of a state to transition to.</returns>
        public override String OnUpdate()
        {
            if (pParentGOH.pCollisionRect.Intersects(GameObjectManager.pInstance.pPlayer.pCollisionRect))
            {
                // Don't do this OnEnd because we might be leaving here due to dying at which point 
                // the behaviour would have also been intentionally disabled.
                pParentGOH.SetBehaviourEnabled<PointAndShoot>(true);

                return "Follow";
            }

            return null;
        }

        /// <summary>
        /// Called once when leaving this state.  Called the frame after the Update which returned
        /// a valid state to transition to.  This is a chance to do any clean up needed.
        /// </summary>
        public override void OnEnd()
        {
        }
    }
}
