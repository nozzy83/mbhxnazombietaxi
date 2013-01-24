using System;
using System.Collections.Generic;
using MBHEngine.StateMachine;
using MBHEngine.Behaviour;
using MBHEngine.GameObject;
using MBHEngineContentDefs;
using Microsoft.Xna.Framework;
using MBHEngine.Math;

namespace ZombieTaxi.States.Civilian
{
    /// <summary>
    /// State where the Game Object stands in place waiting for the target to get far enough away
    /// to trigger a transition back to the follow state.
    /// </summary>
    class FSMStateGoToStandingPosition : FSMState
    {
        /// <summary>
        /// Preallocate messages to avoid GC.
        /// </summary>
        private SpriteRender.SetActiveAnimationMessage mSetActiveAnimationMsg;
        private PathFind.SetDestinationMessage mSetDestinationMsg;
        private PathFind.SetSourceMessage mSetSourceMsg;
        private PathFind.GetCurrentBestNodeMessage mGetCurrentBestNodeMsg;
        private PathFind.ClearDestinationMessage mClearDestinationMsg;
        private PathFollow.SetTargetObjectMessage mSetTargetObjectMsg;
        private FiniteStateMachine.SetStateMessage mSetStateMsg;

        /// <summary>
        /// Constructor.
        /// </summary>
        public FSMStateGoToStandingPosition()
        {
            mSetActiveAnimationMsg = new SpriteRender.SetActiveAnimationMessage();
            mSetDestinationMsg = new PathFind.SetDestinationMessage();
            mSetSourceMsg = new PathFind.SetSourceMessage();
            mGetCurrentBestNodeMsg = new PathFind.GetCurrentBestNodeMessage();
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

            // Use the collision center point since the position root is at the
            // bottom of the image and can result in off by 1 errors when finding
            // which tile this guy is standing in.
            mSetSourceMsg.mSource_In = pParentGOH.pCollisionRect.pCenterPoint;
            pParentGOH.OnMessage(mSetSourceMsg);

            List<GameObject> safeHouses = GameObjectManager.pInstance.GetGameObjectsOfClassification(GameObjectDefinition.Classifications.SAFE_HOUSE);

            Int32 index = RandomManager.pInstance.RandomNumber() % safeHouses.Count;

            mSetDestinationMsg.mDestination_In = safeHouses[index].pPosition;
            pParentGOH.OnMessage(mSetDestinationMsg);

            pParentGOH.SetBehaviourEnabled<PathFollow>(true);

            mSetTargetObjectMsg.mTarget_In = null;
            pParentGOH.OnMessage(mSetTargetObjectMsg);
        }

        /// <summary>
        /// Call repeatedly until it returns a valid new state to transition to.
        /// </summary>
        /// <returns>Identifier of a state to transition to.</returns>
        public override string OnUpdate()
        {
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
                mSetStateMsg.mNextState_In = "WaitAtStandingPosition";
                pParentGOH.OnMessage(mSetStateMsg);
            }
            else if (msg is PathFind.OnPathFindFailedMessage)
            {
                mSetStateMsg.mNextState_In = "GoToStandingPosition";
                pParentGOH.OnMessage(mSetStateMsg);
            }
        }
    }
}
