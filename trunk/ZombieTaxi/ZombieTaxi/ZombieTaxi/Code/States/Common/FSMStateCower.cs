using System;
using MBHEngine.StateMachine;
using MBHEngine.Behaviour;
using MBHEngine.GameObject;
using ZombieTaxi.Behaviours;

namespace ZombieTaxi.States.Common
{
    /// <summary>
    /// State where the Game Object sits in a cowering pose.
    /// </summary>
    class FSMStateCower : FSMState
    {
        private String mAnimationName;

        /// <summary>
        /// Preallocate messages to avoid GC.
        /// </summary>
        private SpriteRender.SetActiveAnimationMessage mSetActiveAnimationMsg;

        /// <summary>
        /// Constructor.
        /// </summary>
        public FSMStateCower(String animName)
        {
            mAnimationName = animName;

            mSetActiveAnimationMsg = new SpriteRender.SetActiveAnimationMessage();
        }

        /// <summary>
        /// Called once when the state starts.
        /// </summary>
        public override void OnBegin()
        {
            mSetActiveAnimationMsg.mAnimationSetName_In = mAnimationName;
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
    }
}
