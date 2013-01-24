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
            mIncrementScoreMsg.mAmount_In = 100;
            mSetTargetObjectMsg = new PathFollow.SetTargetObjectMessage();
        }

        /// <summary>
        /// Called once when the state starts.
        /// </summary>
        public override void OnBegin()
        {
            mSetActiveAnimationMsg.mAnimationSetName_In = "Run";
            pParentGOH.OnMessage(mSetActiveAnimationMsg);

            mSetSourceMsg.mSource_In = pParentGOH.pCollisionRect.pCenterPoint;
            pParentGOH.OnMessage(mSetSourceMsg);
            mSetDestinationMsg.mDestination_In = GameObjectManager.pInstance.pPlayer.pPosition;
            pParentGOH.OnMessage(mSetDestinationMsg);

            pParentGOH.SetBehaviourEnabled<PathFollow>(true);

            mSetTargetObjectMsg.mTarget_In = GameObjectManager.pInstance.pPlayer;
            pParentGOH.OnMessage(mSetTargetObjectMsg);
        }

        /// <summary>
        /// Call repeatedly until it returns a valid new state to transition to.
        /// </summary>
        /// <returns>Identifier of a state to transition to.</returns>
        public override String OnUpdate()
        {
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
    }
}
