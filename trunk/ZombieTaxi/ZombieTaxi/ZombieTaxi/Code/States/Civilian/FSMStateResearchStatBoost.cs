using System;
using MBHEngine.StateMachine;
using MBHEngine.Behaviour;
using MBHEngine.Math;
using ZombieTaxi.Behaviours;
using MBHEngine.GameObject;
using MBHEngine.World;
using ZombieTaxi.StatBoost.Behaviours;

namespace ZombieTaxi.States.Civilian
{
    /// <summary>
    /// State where the object is researching a stat boost for the player.
    /// </summary>
    class FSMStateResearchStatBoost : FSMState
    {
        /// <summary>
        /// Preallocate messages to avoid GC.
        /// </summary>
        private StatBoostResearch.SetTargetMessage mSetTargetMsg;
        private Level.GetTileAtObjectMessage mGetTileAtObjectMsg;
        private FiniteStateMachine.SetStateMessage mSetStateMsg;

        /// <summary>
        /// Constructor.
        /// </summary>
        public FSMStateResearchStatBoost()
        {
            mSetTargetMsg = new StatBoostResearch.SetTargetMessage();
            mGetTileAtObjectMsg = new Level.GetTileAtObjectMessage();
            mSetStateMsg = new FiniteStateMachine.SetStateMessage();
        }

        /// <summary>
        /// Called once when the state starts.
        /// </summary>
        public override void OnBegin()
        {
            // Start the StatBoostResearch since we are now researching.
            pParentGOH.SetBehaviourEnabled<StatBoostResearch>(true);

            // The stat boost needs to be given to the player.
            mSetTargetMsg.mTarget_In = GameObjectManager.pInstance.pPlayer;
            pParentGOH.OnMessage(mSetTargetMsg);            
            
            // Grab the tile at that location and update its Attributes to now be
            // Occupied.
            mGetTileAtObjectMsg.Reset();
            mGetTileAtObjectMsg.mObject_In = pParentGOH;
            WorldManager.pInstance.pCurrentLevel.OnMessage(mGetTileAtObjectMsg);

            if (null != mGetTileAtObjectMsg.mTile_Out)
            {
                mGetTileAtObjectMsg.mTile_Out.SetAttribute(Level.Tile.Attribute.Occupied);
            }
        }

        /// <summary>
        /// Called once when leaving this state.  Called the frame after the Update which returned
        /// a valid state to transition to.
        /// </summary>
        public override void OnEnd()
        {
            if (null != mGetTileAtObjectMsg.mTile_Out)
            {
                // This would have been set in OnBegin. It will be set again if we are transitioning
                // to 
                mGetTileAtObjectMsg.mTile_Out.ClearAttribute(Level.Tile.Attribute.Occupied);
            }

            pParentGOH.SetBehaviourEnabled<StatBoostResearch>(false);
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
            if (msg is StatBoostResearch.OnResearchComplete)
            {
                mSetStateMsg.mNextState_In = "WaitAtStandingPosition";
                pParentGOH.OnMessage(mSetStateMsg);
            }
        }
    }
}
