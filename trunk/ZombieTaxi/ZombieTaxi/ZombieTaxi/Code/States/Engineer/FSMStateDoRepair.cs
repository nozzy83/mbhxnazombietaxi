using System;
using MBHEngine.StateMachine;
using MBHEngine.Behaviour;
using MBHEngine.GameObject;
using Microsoft.Xna.Framework;
using MBHEngine.Math;
using ZombieTaxi.Behaviours;

namespace ZombieTaxi.States.Engineer
{
    /// <summary>
    /// State where the Game Object stands in place waiting for the target to get far enough away
    /// to trigger a transition back to the follow state.
    /// </summary>
    class FSMStateDoRepair : FSMState
    {
        /// <summary>
        /// Animated sprite used to indicate that the tile is being repaired.
        /// </summary>
        private GameObject mDust;

        /// <summary>
        /// The number of HP a damaged tile will regen per frame when being repaired.
        /// </summary>
        private Single mHpRepairRate;

        /// <summary>
        /// Preallocate messages to avoid GC.
        /// </summary>
        private SpriteRender.SetActiveAnimationMessage mSetActiveAnimationMsg;
        private FSMEngineer.GetTileToRepairMessage mGetTileToRepairMsg;
        private Health.IncrementHealthMessage mIncrementHealthMsg;
        private Health.GetHealthMessage mGetHealthMsg;

        /// <summary>
        /// Constructor.
        /// </summary>
        public FSMStateDoRepair()
        {
            mHpRepairRate = 0.01f;

            mSetActiveAnimationMsg = new SpriteRender.SetActiveAnimationMessage();
            mGetTileToRepairMsg = new FSMEngineer.GetTileToRepairMessage();
            mIncrementHealthMsg = new Health.IncrementHealthMessage();
            mGetHealthMsg = new Health.GetHealthMessage();
        }

        /// <summary>
        /// Called once when the state starts.
        /// </summary>
        public override void OnBegin()
        {
            mSetActiveAnimationMsg.mAnimationSetName_In = "Idle";
            pParentGOH.OnMessage(mSetActiveAnimationMsg);

            pParentGOH.OnMessage(mGetTileToRepairMsg);

            if (mGetTileToRepairMsg.mTile_Out != null)
            {
                // Spawn some smoke to be more ninja like.
                mDust = GameObjectFactory.pInstance.GetTemplate("GameObjects\\Effects\\Dust\\Dust");

                // Put the smoke at the right position relative to the Scout.
                mDust.pPosition = mGetTileToRepairMsg.mTile_Out.pPosition;

                mSetActiveAnimationMsg.mAnimationSetName_In = "Repair";
                mDust.OnMessage(mSetActiveAnimationMsg);

                // The Smoke gets pushed onto the GameObjectManager and will delete itself when
                // it finishes the animation.
                GameObjectManager.pInstance.Add(mDust);
            }
        }

        /// <summary>
        /// Call repeatedly until it returns a valid new state to transition to.
        /// </summary>
        /// <returns>Identifier of a state to transition to.</returns>
        public override String OnUpdate()
        {
            GameObject tile = mGetTileToRepairMsg.mTile_Out;
            
            if (null != tile)
            {
                tile.OnMessage(mGetHealthMsg);

                if (mGetHealthMsg.mCurrentHealth_Out < mGetHealthMsg.mMaxHealth_Out)
                {
                    mIncrementHealthMsg.mIncrementAmount_In = mHpRepairRate;
                    tile.OnMessage(mIncrementHealthMsg);
                }
                else
                {
                    return "Repair";
                }
            }

            return null;
        }

        /// <summary>
        /// Called once when leaving this state.  Called the frame after the Update which returned
        /// a valid state to transition to.  This is a chance to do any clean up needed.
        /// </summary>
        public override void OnEnd()
        {
            if (null != mDust)
            {
                GameObjectManager.pInstance.Remove(mDust);
                mDust = null;
            }
        }
    }
}
