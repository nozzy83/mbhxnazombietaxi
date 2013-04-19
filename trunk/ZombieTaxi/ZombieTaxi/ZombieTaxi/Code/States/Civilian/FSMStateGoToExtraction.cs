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
        private PathFind.ClearDestinationMessage mClearDestinationMsg;
        private PathFollow.SetTargetObjectMessage mSetTargetObjectMsg;
        private FiniteStateMachine.SetStateMessage mSetStateMsg;

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
            mClearDestinationMsg = new PathFind.ClearDestinationMessage();
            mSetTargetObjectMsg = new PathFollow.SetTargetObjectMessage();
            mSetStateMsg = new FiniteStateMachine.SetStateMessage();
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

            pParentGOH.SetBehaviourEnabled<PathFollow>(true);

            mSetTargetObjectMsg.mTarget_In = null;
            pParentGOH.OnMessage(mSetTargetObjectMsg);

        }

        /// <summary>
        /// Call repeatedly until it returns a valid new state to transition to.
        /// </summary>
        /// <returns>Identifier of a state to transition to.</returns>
        public override String OnUpdate()
        {
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

            pParentGOH.OnMessage(mClearDestinationMsg);
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
            if (msg is PathFollow.OnReachedPathEndMessage)
            {
                // The player gets a bunch of points for rescuing people!
                GameObjectManager.pInstance.BroadcastMessage(mIncrementScoreMsg, pParentGOH);

                GameObjectManager.pInstance.Remove(pParentGOH);
            }
            else if (msg is PathFind.OnPathFindFailedMessage)
            {
                // Once we reach our destination, sit and wait for a spell.
                mSetStateMsg.mNextState_In = "Follow";
                pParentGOH.OnMessage(mSetStateMsg);
            }
        }
    }
}
