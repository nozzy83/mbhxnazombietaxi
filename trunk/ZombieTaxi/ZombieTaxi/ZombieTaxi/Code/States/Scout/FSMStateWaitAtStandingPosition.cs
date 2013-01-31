using MBHEngine.StateMachine;
using MBHEngine.Behaviour;
using MBHEngine.World;
using MBHEngine.Input;
using ZombieTaxi.Behaviours;
using MBHEngine.GameObject;
using ZombieTaxi.StatBoost.Behaviours;

namespace ZombieTaxi.States.Scout
{
    /// <summary>
    /// State where the Game Object stands and waits for the Player to stand close enough that
    /// they will bring up a prompt asking which state to transition to.
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

        /// <summary>
        /// Constructor.
        /// </summary>
        public FSMStateWaitAtStandingPosition()
        {
            mSetActiveAnimationMsg = new SpriteRender.SetActiveAnimationMessage();
        }

        /// <summary>
        /// Called once when the state starts.
        /// </summary>
        public override void OnBegin()
        {
            mSetActiveAnimationMsg.mAnimationSetName_In = "Idle";
            pParentGOH.OnMessage(mSetActiveAnimationMsg);

            mButtonHint = null;
        }

        /// <summary>
        /// Call repeatedly until it returns a valid new state to transition to.
        /// </summary>
        /// <returns>Identifier of a state to transition to.</returns>
        public override string OnUpdate()
        {
            // When the player is standing close to the GameObject, show a button hint and check for input.
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

                if (InputManager.pInstance.CheckAction(InputManager.InputActions.X, true))
                {
                    // Start looking for a Stranded to detect.
                    return "BeginSearch";
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
            if (null != mButtonHint)
            {
                GameObjectManager.pInstance.Remove(mButtonHint);
                mButtonHint = null;
            }
        }
    }
}
