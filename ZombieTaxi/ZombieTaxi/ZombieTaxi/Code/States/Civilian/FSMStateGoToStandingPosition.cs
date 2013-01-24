using System;
using System.Collections.Generic;
using MBHEngine.StateMachine;
using MBHEngine.Behaviour;
using MBHEngine.GameObject;
using MBHEngineContentDefs;
using Microsoft.Xna.Framework;
using MBHEngine.Math;
using MBHEngine.World;

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
        private Level.GetTileAtObjectMessage mGetTileAtObjectMsg;

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
            mGetTileAtObjectMsg = new Level.GetTileAtObjectMessage();
        }

        /// <summary>
        /// Called once when the state starts.
        /// </summary>
        public override void OnBegin()
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
                    safeHouses.RemoveAt(i);

                    continue;
                }

                // If the tile is occupied we should not try to go there.
                if (mGetTileAtObjectMsg.mTile_Out.HasAttribute(Level.Tile.Attribute.Occupied))
                {
                    safeHouses.RemoveAt(i);

                    continue;
                }
            }

            // There is a chance that all safeHouse tiles are occupied.
            if (safeHouses.Count <= 0)
            {
                /// <todo>
                /// Go to a new state; maybe one where the dude just wanders a bit.
                /// </todo>
                /// 

                // There are no spaces left in the SafeHouse so don't try to find a spot.
                return;
            }

            // Show the player walking towards his destination.
            mSetActiveAnimationMsg.mAnimationSetName_In = "Run";
            pParentGOH.OnMessage(mSetActiveAnimationMsg);

            // Use the collision center point since the position root is at the
            // bottom of the image and can result in off by 1 errors when finding
            // which tile this guy is standing in.
            mSetSourceMsg.mSource_In = pParentGOH.pCollisionRect.pCenterPoint;
            pParentGOH.OnMessage(mSetSourceMsg);

            // Pick a random safeHouse location to move to.
            Int32 index =  RandomManager.pInstance.RandomNumber() % safeHouses.Count;

            mSetDestinationMsg.mDestination_In = safeHouses[index].pPosition;
            pParentGOH.OnMessage(mSetDestinationMsg);

            // Grab the tile at that location and update its Attributes to now be
            // Occupied.
            mGetTileAtObjectMsg.Reset();
            mGetTileAtObjectMsg.mObject_In = safeHouses[index];
            WorldManager.pInstance.pCurrentLevel.OnMessage(mGetTileAtObjectMsg);

            if (null != mGetTileAtObjectMsg.mTile_Out)
            {
                mGetTileAtObjectMsg.mTile_Out.SetAttribute(Level.Tile.Attribute.Occupied);
            }

            // With the destination and source set on the PathFind Behaviour, turning on the PathFollow
            // Behaviour will cause him to walk to the destination.
            pParentGOH.SetBehaviourEnabled<PathFollow>(true);

            // The PathFollow style should not dynamically update the destination.
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
