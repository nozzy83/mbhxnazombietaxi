using MBHEngine.StateMachine;
using MBHEngine.Behaviour;
using MBHEngine.World;
using MBHEngine.Input;
using ZombieTaxi.Behaviours;
using MBHEngine.GameObject;
using ZombieTaxi.StatBoost.Behaviours;

namespace ZombieTaxi.States.Civilian
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
        /// Preallocate messages to avoid GC.
        /// </summary>
        private SpriteRender.SetActiveAnimationMessage mSetActiveAnimationMsg;
        private Level.GetTileAtObjectMessage mGetTileAtObjectMsg;
        private StatBoostResearch.GetLevelsRemainingMessage mGetLevelsRemainingMsg;
        private SpriteRender.GetAttachmentPointMessage mGetAttachmentPointMsg;

        /// <summary>
        /// Constructor.
        /// </summary>
        public FSMStateWaitAtStandingPosition()
        {
            mSetActiveAnimationMsg = new SpriteRender.SetActiveAnimationMessage();
            mGetTileAtObjectMsg = new Level.GetTileAtObjectMessage();
            mGetLevelsRemainingMsg = new StatBoostResearch.GetLevelsRemainingMessage();
            mGetAttachmentPointMsg = new SpriteRender.GetAttachmentPointMessage();
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
        }

        /// <summary>
        /// Call repeatedly until it returns a valid new state to transition to.
        /// </summary>
        /// <returns>Identifier of a state to transition to.</returns>
        public override string OnUpdate()
        {
            pParentGOH.OnMessage(mGetLevelsRemainingMsg);

            if (mGetLevelsRemainingMsg.mLevelsRemaining > 0 && pParentGOH.pCollisionRect.Intersects(GameObjectManager.pInstance.pPlayer.pCollisionRect))
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

                if (InputManager.pInstance.CheckAction(InputManager.InputActions.X, true))
                {
                    return "ResearchStatBoost";
                }
                else if (InputManager.pInstance.CheckAction(InputManager.InputActions.B, true))
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

                    return null;
                }
            }
            else
            {
                if (null != mButtonHint)
                {
                    mButtonHint.pDoRender = false;
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
                // to 
                mGetTileAtObjectMsg.mTile_Out.ClearAttribute(Level.Tile.Attribute.Occupied);
            }

            if (null != mButtonHint)
            {
                GameObjectManager.pInstance.Remove(mButtonHint);
                mButtonHint = null;
            }
        }
    }
}
