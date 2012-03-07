using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MBHEngine.GameObject;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MBHEngine.Debug;
using MBHEngine.Behaviour;
using ZombieTaxi.Behaviours;
using ZombieTaxiContentDefs;

namespace ZombieTaxi.Behaviours
{
    /// <summary>
    /// Manages health for an object.
    /// </summary>
    class Health : MBHEngine.Behaviour.Behaviour
    {
        /// <summary>
        /// Applies a specified amount of damage to the current health.
        /// </summary>
        public class OnApplyDamage : BehaviourMessage
        {
            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="damageAmount">The amount of damage to subtract from the current health.</param>
            public OnApplyDamage(Single damageAmount)
            {
                mDamagaAmount = damageAmount;
            }

            /// <summary>
            /// The amount of damage to subtract from the current health.
            /// </summary>
            public Single mDamagaAmount;
        }

        /// <summary>
        /// Sent when the health reaches zero.
        /// </summary>
        public class OnZeroHealth : BehaviourMessage
        {
        }

        /// <summary>
        /// The current amount of health.
        /// </summary>
        private Single mCurrentHealth;

        /// <summary>
        /// The maxium amount of health this game object can store.
        /// </summary>
        private Single mMaxHealth;

        private OnZeroHealth mOnZeroHealthMsg;

        /// <summary>
        /// Constructor which also handles the process of loading in the Behaviour
        /// Definition information.
        /// </summary>
        /// <param name="parentGOH">The game object that this behaviour is attached to.</param>
        /// <param name="fileName">The file defining this behaviour.</param>
        public Health(GameObject parentGOH, String fileName)
            : base(parentGOH, fileName)
        {
        }

        /// <summary>
        /// Call this to initialize a Behaviour with data supplied in a file.
        /// </summary>
        /// <param name="fileName">The file to load from.</param>
        public override void LoadContent(String fileName)
        {
            base.LoadContent(fileName);

            HealthDefinition def = GameObjectManager.pInstance.pContentManager.Load<HealthDefinition>(fileName);

            mMaxHealth = def.mMaxHealth;
            mCurrentHealth = def.mCurrentHealth;

            mOnZeroHealthMsg = new OnZeroHealth();
        }

        /// <summary>
        /// Called once per frame by the game object.
        /// </summary>
        /// <param name="gameTime">The amount of time that has passed this frame.</param>
        public override void Update(GameTime gameTime)
        {
#if ALLOW_GARBAGE
            //DebugMessageDisplay.pInstance.AddDynamicMessage("Health: " + mCurrentHealth + "/" + mMaxHealth);
#endif        
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
            // Which type of message was sent to us?
            if (msg is OnApplyDamage)
            {
                OnApplyDamage temp = (OnApplyDamage)msg;

                ApplyDamage(temp.mDamagaAmount);
            }
        }

        /// <summary>
        /// Subtracts the amount from the current health amount.
        /// </summary>
        /// <param name="amount">The amount to subtract from the current health.</param>
        private void ApplyDamage(Single amount)
        {
            if (amount < 0)
            {
                throw new Exception("Attempting to apply negative damage");
            }

            // Don't reduce the health if we are already dead.
            if (mCurrentHealth > 0)
            {
                mCurrentHealth -= amount;

                if (mCurrentHealth <= 0)
                {
                    mCurrentHealth = 0;

                    // Let anyone interested know that we have died.
                    mParentGOH.OnMessage(mOnZeroHealthMsg);
                }
            }
        }

    }
}
