using MBHEngine.StateMachine;
using MBHEngine.Behaviour;
using MBHEngine.World;
using MBHEngine.Input;
using ZombieTaxi.Behaviours;
using MBHEngine.GameObject;
using ZombieTaxi.StatBoost.Behaviours;
using MBHEngineContentDefs;
using ZombieTaxiContentDefs;
using MBHEngine.Debug;
using System;

namespace ZombieTaxi.States.Common
{
    /// <summary>
    /// State where the Game Object stands in place waiting for the target to get far enough away
    /// to trigger a transition back to the follow state.
    /// </summary>
    class FSMStateWaitAtStandingPosition : FSMState
    {
        /// <summary>
        /// Button hint that is shown to the player when standing near this GameObject.
        /// </summary>
        private GameObject mButtonHint;

        /// <summary>
        /// We need to hang onto the StrandedPopup so that we can detect messages broadcast from it.
        /// </summary>
        private GameObject mPopup;

        private String mPopupScript;

        /// <summary>
        /// Preallocate messages to avoid GC.
        /// </summary>
        private SpriteRender.SetActiveAnimationMessage mSetActiveAnimationMsg;
        private Level.GetTileAtObjectMessage mGetTileAtObjectMsg;
        private SpriteRender.GetAttachmentPointMessage mGetAttachmentPointMsg;
        private StrandedPopup.OnPopupClosedMessage mOnPopupCloseMsg;
        private FiniteStateMachine.SetStateMessage mSetStateMsg;

        /// <summary>
        /// Constructor.
        /// </summary>
        public FSMStateWaitAtStandingPosition(String popupScript)
        {
            mPopupScript = popupScript;

            mSetActiveAnimationMsg = new SpriteRender.SetActiveAnimationMessage();
            mGetTileAtObjectMsg = new Level.GetTileAtObjectMessage();
            mGetAttachmentPointMsg = new SpriteRender.GetAttachmentPointMessage();
            mOnPopupCloseMsg = new StrandedPopup.OnPopupClosedMessage();
            mSetStateMsg = new FiniteStateMachine.SetStateMessage();
        }

        /// <summary>
        /// Called once when the state starts.
        /// </summary>
        public override void OnBegin()
        {
            mSetActiveAnimationMsg.mAnimationSetName_In = "Idle";
            pParentGOH.OnMessage(mSetActiveAnimationMsg);

            // Grab the tile at that location and update its Attributes to now be
            // Occupied.
            mGetTileAtObjectMsg.Reset();
            mGetTileAtObjectMsg.mObject_In = pParentGOH;
            WorldManager.pInstance.pCurrentLevel.OnMessage(mGetTileAtObjectMsg);

            if (null != mGetTileAtObjectMsg.mTile_Out)
            {
                mGetTileAtObjectMsg.mTile_Out.SetAttribute(Level.Tile.Attribute.Occupied);
            }

            mButtonHint = null;
            mPopup = null;

            pParentGOH.SetBehaviourEnabled<HealNearby>(true);
        }

        /// <summary>
        /// Call repeatedly until it returns a valid new state to transition to.
        /// </summary>
        /// <returns>Identifier of a state to transition to.</returns>
        public override string OnUpdate()
        {
            if (pParentGOH.pCollisionRect.Intersects(GameObjectManager.pInstance.pPlayer.pCollisionRect))
            {
                if (null == mButtonHint)
                {
                    // Set up the button hint.
                    mButtonHint = GameObjectFactory.pInstance.GetTemplate("GameObjects\\Interface\\ButtonHint\\ButtonHint");
                    mButtonHint.pPosition = pParentGOH.pPosition;

                    mSetActiveAnimationMsg.mAnimationSetName_In = "X";
                    mButtonHint.OnMessage(mSetActiveAnimationMsg);

                    GameObjectManager.pInstance.Add(mButtonHint);
                }

                mButtonHint.pDoRender = true;

                // If the player stands on this Stranded, and presses the prompt button, bring up a popup
                // asking them what kind of task they wish to assign to this character.
                if (InputManager.pInstance.CheckAction(InputManager.InputActions.X, true))
                {
                    mPopup = GameObjectFactory.pInstance.GetTemplate(mPopupScript);
                    GameObjectManager.pInstance.Add(mPopup);

                    // Switch to a new update pass so that the game essentially pauses.
                    GameObjectManager.pInstance.pCurUpdatePass = BehaviourDefinition.Passes.POPUP;
                }
            }
            else
            {
                // Get rid of the Button Hint.
                if (null != mButtonHint)
                {
                    GameObjectManager.pInstance.Remove(mButtonHint);
                    mButtonHint = null;
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
            if (null != mGetTileAtObjectMsg.mTile_Out)
            {
                // This would have been set in OnBegin. It will be set again if we are transitioning
                // to another state where the tile should be occupied.
                mGetTileAtObjectMsg.mTile_Out.ClearAttribute(Level.Tile.Attribute.Occupied);
            }

            // If the button was active when the state ended it needs to be cleaned up.
            if (null != mButtonHint)
            {
                GameObjectManager.pInstance.Remove(mButtonHint);
                mButtonHint = null;
            }

            mPopup = null;

            pParentGOH.SetBehaviourEnabled<HealNearby>(false);
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
            base.OnMessage(ref msg);

            if (msg is StrandedPopup.OnPopupClosedMessage)
            {
                // This message is broadcast to all GameObjects, so make sure it is actually a popup we
                // opened before reacting to it.
                if (msg.pSender == mPopup)
                {
                    StrandedPopup.OnPopupClosedMessage temp = (StrandedPopup.OnPopupClosedMessage)msg;

                    if (temp.mSelection_In == StrandedPopupDefinition.ButtonTypes.GunUp)
                    {
                        mSetStateMsg.mNextState_In = "ResearchStatBoost";
                        pParentGOH.OnMessage(mSetStateMsg);
                    }
					else if (temp.mSelection_In == StrandedPopupDefinition.ButtonTypes.HpUp)
                    {
                        mSetStateMsg.mNextState_In = "ResearchStatBoost";
                        pParentGOH.OnMessage(mSetStateMsg);
                    }
                    else if (temp.mSelection_In == StrandedPopupDefinition.ButtonTypes.MilitantPatrol)
                    {
                        DebugMessageDisplay.pInstance.AddConstantMessage("Patrol");
                        mSetStateMsg.mNextState_In = "Patrol";
                        pParentGOH.OnMessage(mSetStateMsg);
                    }
                    else if (temp.mSelection_In == StrandedPopupDefinition.ButtonTypes.MilitantFollow)
                    {
                        DebugMessageDisplay.pInstance.AddConstantMessage("Follow");
                        mSetStateMsg.mNextState_In = "Follow";
                        pParentGOH.OnMessage(mSetStateMsg);
                    }
                    else if (temp.mSelection_In == StrandedPopupDefinition.ButtonTypes.MakeScout)
                    {
                        // Spawn some smoke to be more ninja like.
                        GameObject go = GameObjectFactory.pInstance.GetTemplate("GameObjects\\Effects\\Dust\\Dust");

                        // Grab that attachment point and position the new object there.
                        mGetAttachmentPointMsg.mName_In = "Smoke";
                        pParentGOH.OnMessage(mGetAttachmentPointMsg);

                        // Put the smoke at the right position relative to the Civilian.
                        go.pPosition = mGetAttachmentPointMsg.mPoisitionInWorld_Out;

                        // The Smoke gets pushed onto the GameObjectManager and will delete itself when
                        // it finishes the animation.
                        GameObjectManager.pInstance.Add(go);

                        // Spawn the Scout to replace this Civilian.
                        go = GameObjectFactory.pInstance.GetTemplate("GameObjects\\Characters\\Scout\\Scout");
                        go.pPosition = pParentGOH.pPosition;
                        GameObjectManager.pInstance.Add(go);

                        GameObjectManager.pInstance.Remove(pParentGOH);
                    }
					else if (temp.mSelection_In == StrandedPopupDefinition.ButtonTypes.ScoutSearch)
                    {
                        mSetStateMsg.mNextState_In = "BeginSearch";
                        pParentGOH.OnMessage(mSetStateMsg);
                    }
                    else if (temp.mSelection_In == StrandedPopupDefinition.ButtonTypes.EngineerRepair)
                    {
                        DebugMessageDisplay.pInstance.AddConstantMessage("Repair");
                        mSetStateMsg.mNextState_In = "Repair";
                        pParentGOH.OnMessage(mSetStateMsg);
                    }

                    // Popups get recycled so if ours closes, we need to make sure to clear our local 
                    // reference, else the next object to use it might send messages that we react to.
                    mPopup = null;
                }
            }
        }
    }
}
