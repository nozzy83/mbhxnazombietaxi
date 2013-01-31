using MBHEngine.StateMachine;
using MBHEngine.Behaviour;
using MBHEngine.World;
using MBHEngine.Input;
using ZombieTaxi.Behaviours;
using MBHEngine.GameObject;
using ZombieTaxi.StatBoost.Behaviours;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System;

namespace ZombieTaxi.States.Scout
{
    /// <summary>
    /// The Stranded has been found and the Scout is standing near him. He now just sits and waits
    /// for the Player to arrive, just so that the Player makes the connection that the Scout found
    /// this guy.
    /// </summary>
    class FSMStateWaitAtTarget : FSMState
    {
        /// <summary>
        /// To avoid having to add another state, this flag tracks whether the animation that 
        /// the scout plays before vanishing has started playing yet.
        /// </summary>
        private Boolean mAnimationStarted;

        /// <summary>
        /// The distance the Player must be (squared) to trigger the Scout to vanish.
        /// </summary>
        private Int32 mMinDist2;

        /// <summary>
        /// Preallocated messages to avoid triggering GC.
        /// </summary>
        private SpriteRender.GetAttachmentPointMessage mGetAttachmentPointMsg;
        private SpriteRender.SetActiveAnimationMessage mSetActiveAnimationMsg;
        private FiniteStateMachine.SetStateMessage mSetStateMsg;

        /// <summary>
        /// Constructor.
        /// </summary>
        public FSMStateWaitAtTarget()
        {
            mGetAttachmentPointMsg = new SpriteRender.GetAttachmentPointMessage();
            mSetActiveAnimationMsg = new SpriteRender.SetActiveAnimationMessage();
            mSetStateMsg = new FiniteStateMachine.SetStateMessage();
        }

        /// <summary>
        /// Called once when the state starts.
        /// </summary>
        public override void OnBegin()
        {
            mSetActiveAnimationMsg.mAnimationSetName_In = "Idle";
            pParentGOH.OnMessage(mSetActiveAnimationMsg);

            mAnimationStarted = false;

            // 4 tiles squared.
            mMinDist2 = (8 * 4) * (8 * 4);
        }

        /// <summary>
        /// Call repeatedly until it returns a valid new state to transition to.
        /// </summary>
        /// <returns>Identifier of a state to transition to.</returns>
        public override string OnUpdate()
        {
            if (!mAnimationStarted)
            {
                // Once the player is in range, just play a quick animation and vanish.
                if (Vector2.DistanceSquared(pParentGOH.pPosition, GameObjectManager.pInstance.pPlayer.pPosition) < mMinDist2)
                {
                    mSetActiveAnimationMsg.mAnimationSetName_In = "RaiseArm";
                    pParentGOH.OnMessage(mSetActiveAnimationMsg);

                    mAnimationStarted = true;
                }
            }

            return null;
        }

        /// <summary>
        /// Called once when leaving this state.  Called the frame after the Update which returned
        /// a valid state to transition to.
        /// </summary>
        public override void OnEnd()
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
            if (msg is SpriteRender.OnAnimationCompleteMessage)
            {
                SpriteRender.OnAnimationCompleteMessage temp = (SpriteRender.OnAnimationCompleteMessage)msg;

                if (temp.mAnimationSetName_In == "RaiseArm")
                {
                    // By default just spawn the object where this object is.
                    GameObject go = GameObjectFactory.pInstance.GetTemplate("GameObjects\\Effects\\Dust\\Dust");

                    // Grab that attachment point and position the new object there.
                    mGetAttachmentPointMsg.mName_In = "Smoke";
                    pParentGOH.OnMessage(mGetAttachmentPointMsg);
                    Vector2 spawnPos = mGetAttachmentPointMsg.mPoisitionInWorld_Out;

                    go.pPosition = spawnPos;

                    GameObjectManager.pInstance.Add(go);

                    GameObjectManager.pInstance.Remove(pParentGOH);
                }
            }
        }
    }
}
