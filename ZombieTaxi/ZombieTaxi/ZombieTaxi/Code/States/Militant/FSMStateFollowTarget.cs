using System;
using System.Collections.Generic;
using MBHEngine.StateMachine;
using MBHEngine.Behaviour;
using MBHEngine.GameObject;
using MBHEngineContentDefs;
using MBHEngine.Debug;
using Microsoft.Xna.Framework;
using ZombieTaxi.Behaviours.HUD;
using MBHEngine.World;

namespace ZombieTaxi.States.Militant
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
        /// The Militant can go to the SafeHouse but then return to this state later by User request.
        /// When that happens we need to ensure that the Militant does not try to return to the SafeHouse
        /// until he leaves it.
        /// </summary>
        private Boolean mMustLeaveSafeHouse;

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
        private Level.GetTileAtObjectMessage mGetTileAtObjectMsg;
        private ZombieTaxi.Behaviours.Civilian.GetSafeHouseScoreMessage mGetSafeHouseScoreMessage;

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
            mSetTargetObjectMsg = new PathFollow.SetTargetObjectMessage();
            mGetTileAtObjectMsg = new Level.GetTileAtObjectMessage();
            mGetSafeHouseScoreMessage = new Behaviours.Civilian.GetSafeHouseScoreMessage();
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

            mSetTargetObjectMsg.mTarget_In = GameObjectManager.pInstance.pPlayer;
            pParentGOH.OnMessage(mSetTargetObjectMsg);

            pParentGOH.SetBehaviourEnabled<PathFollow>(true);

            pParentGOH.OnMessage(mGetSafeHouseScoreMessage);
            mIncrementScoreMsg.mAmount_In = mGetSafeHouseScoreMessage.mSafeHouseScore_Out;

            // If he starts in the SafeHouse he must leave it before trying to return again.
            mSafeHouseInRange.Clear();
            GameObjectManager.pInstance.GetGameObjectsInRange(pParentGOH, ref mSafeHouseInRange, mSafeHouseClassifications);
            if (mSafeHouseInRange.Count != 0)
            {
                mMustLeaveSafeHouse = true;
            }
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
                // Don't even attempt to enter the SafeHouse unless there are spots available.
                // This prevents getting points, and then not actually staying in the Safe House.
                // Also make sure that we aren't currently trying to leave the Safe House.
                if (!mMustLeaveSafeHouse && CheckForValidSafeHouseTiles())
                {
                    DebugMessageDisplay.pInstance.AddConstantMessage("Reached SafeHouse.");

                    // For every civilian we save, increment the score a little.
                    GameObjectManager.pInstance.BroadcastMessage(mIncrementScoreMsg, pParentGOH);

                    // Now just stand around for a little bit.
                    return "GoToStandingPosition";
                }
            }
            else
            {
                mMustLeaveSafeHouse = false;
            }
            
            // Are we close enough that we should just stand still until the player starts moving again.
            if (Vector2.DistanceSquared(GameObjectManager.pInstance.pPlayer.pPosition, pParentGOH.pPosition) < 16 * 16)
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

        /// <summary>
        /// Checks if there are currently any available SafeHouse tiles.
        /// </summary>
        /// <returns>True if there are 1 or more tiles available.</returns>
        private Boolean CheckForValidSafeHouseTiles()
        {
            // Grab a list of all the SafeHouse objects, so that we can pick one at 
            // random to walk to.
            List<GameObject> safeHouses = GameObjectManager.pInstance.GetGameObjectsOfClassification(GameObjectDefinition.Classifications.SAFE_HOUSE);
            
            // We want to avoid standing on a tile that is already occupied by another Stranded.
            // To do that, we just loop through the list of safeHouses and remove any that are
            // already occupied.
            for (Int32 i = safeHouses.Count - 1; i >= 0 ; i--)
            {
                // Get the tile at the position of this SafeHouse.
                mGetTileAtObjectMsg.Reset();
                mGetTileAtObjectMsg.mObject_In = safeHouses[i];
                WorldManager.pInstance.pCurrentLevel.OnMessage(mGetTileAtObjectMsg);

                // If there is no Tile, there we should not be trying to go here at all.
                if (null == mGetTileAtObjectMsg.mTile_Out)
                {
                    continue;
                }

                // If the tile is occupied we should not try to go there.
                if (!mGetTileAtObjectMsg.mTile_Out.HasAttribute(Level.Tile.Attribute.Occupied))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
