using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MBHEngine.GameObject;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MBHEngine.Debug;
using MBHEngine.Behaviour;
using MBHEngineContentDefs;

namespace MBHEngine.Behaviour
{
    /// <summary>
    /// Manages health for an object.
    /// </summary>
    public class Health : MBHEngine.Behaviour.Behaviour
    {
        /// <summary>
        /// Applies a specified amount of damage to the current health.
        /// </summary>
        public class ApplyDamageMessage : BehaviourMessage
        {
            /// <summary>
            /// The amount of damage to subtract from the current health.
            /// </summary>
            public Single mDamageAmount_In;

            /// <summary>
            /// Call this to put a message back to its default state.
            /// </summary>
            public override void Reset()
            {
                mDamageAmount_In = 0.0f;
            }
        }

        /// <summary>
        /// Gives a specified amount of health back to the target.
        /// </summary>
        public class IncrementHealthMessage : BehaviourMessage
        {
            /// <summary>
            /// The amount of health to add to the current health.
            /// </summary>
            public Single mIncrementAmount_In;

            /// <summary>
            /// Call this to put a message back to its default state.
            /// </summary>
            public override void Reset()
            {
                mIncrementAmount_In = 0.0f;
            }
        }

        /// <summary>
        /// Sent when the health reaches zero.
        /// </summary>
        public class OnZeroHealthMessage : BehaviourMessage
        {
            /// <summary>
            /// Call this to put a message back to its default state.
            /// </summary>
            public override void Reset() { }
        }

        /// <summary>
        /// Retrives data about this Health behaviour.  Things like the min and max health.
        /// </summary>
        public class GetHealthMessage : BehaviourMessage
        {
            /// <summary>
            /// The amount of health the GO currently has.
            /// </summary>
            public Single mCurrentHealth_Out;

            /// <summary>
            /// The max amount of health this GO can have.
            /// </summary>
            public Single mMaxHealth_Out;

            /// <summary>
            /// Call this to put a message back to its default state.
            /// </summary>
            public override void Reset()
            {
                mCurrentHealth_Out = mMaxHealth_Out = 0.0f;
            }
        }

        /// <summary>
        /// Set the maximum health that this object can have.
        /// </summary>
        public class SetMaxHealthMessage : BehaviourMessage
        {
            /// <summary>
            /// The next value for max health.
            /// </summary>
            public Single mMaxHealth_In;

            /// <summary>
            /// Call this to put a message back to its default state.
            /// </summary>
            public override void Reset()
            {
                mMaxHealth_In = 0.0f;
            }

        }

        /// <summary>
        /// Set the maximum health that this object can have.
        /// </summary>
        public class IncrementMaxHealthMessage : BehaviourMessage
        {
            /// <summary>
            /// The amount to increase max health by.
            /// </summary>
            public Single mIncrementAmount_In;

            /// <summary>
            /// Call this to put a message back to its default state.
            /// </summary>
            public override void Reset()
            {
                mIncrementAmount_In = 0.0f;
            }

        }

        /// <summary>
        /// The current amount of health.
        /// </summary>
        private Single mCurrentHealth;

        /// <summary>
        /// The maxium amount of health this game object can store.
        /// </summary>
        private Single mMaxHealth;

        /// <summary>
        /// True if this object should be deleted when it reaches Zero Health.
        /// </summary>
        private Boolean mRemoveOnDeath;

        /// <summary>
        /// Preallocated messages so that we don't trigger garbage collection during gameplay.
        /// </summary>
        private OnZeroHealthMessage mOnZeroHealthMsg;

        /// <summary>
        /// Constructor which also handles the process of loading in the Behaviour
        /// Definition information.
        /// </summary>
        /// <param name="parentGOH">The game object that this behaviour is attached to.</param>
        /// <param name="fileName">The file defining this behaviour.</param>
        public Health(GameObject.GameObject parentGOH, String fileName)
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

            if( def.mMaxHealth < def.mCurrentHealth )
            {
                System.Diagnostics.Debug.Assert(false, "Max health must not be less than current health.");

                def.mMaxHealth = def.mCurrentHealth;
            }

            mMaxHealth = def.mMaxHealth;
            mCurrentHealth = def.mCurrentHealth;
            mRemoveOnDeath = def.mRemoveOnDeath;

            mOnZeroHealthMsg = new OnZeroHealthMessage();
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
            if (msg is ApplyDamageMessage)
            {
                ApplyDamageMessage temp = (ApplyDamageMessage)msg;

                ApplyDamage(temp.mDamageAmount_In);
            }
            else if (msg is IncrementHealthMessage)
            {
                IncrementHealthMessage temp = (IncrementHealthMessage)msg;

                mCurrentHealth = System.Math.Min(mMaxHealth, mCurrentHealth + temp.mIncrementAmount_In);
            }
            else if (msg is GetHealthMessage)
            {
                GetHealthMessage temp = (GetHealthMessage)msg;

                temp.mCurrentHealth_Out = mCurrentHealth;
                temp.mMaxHealth_Out = mMaxHealth;
            }
            else if (msg is SetMaxHealthMessage)
            {
                SetMaxHealthMessage temp = (SetMaxHealthMessage)msg;

                Single percent = mCurrentHealth / mMaxHealth;

                mMaxHealth = temp.mMaxHealth_In;

                mCurrentHealth = percent * mMaxHealth;
            }
            else if (msg is IncrementMaxHealthMessage)
            {
                IncrementMaxHealthMessage temp = (IncrementMaxHealthMessage)msg;

                Single percent = mCurrentHealth / mMaxHealth;

                mMaxHealth += temp.mIncrementAmount_In;

                mCurrentHealth = percent * mMaxHealth;
            }
        }

        /// <summary>
        /// Resets a behaviour to its initial state.
        /// </summary>
        public override void Reset()
        {
            mCurrentHealth = mMaxHealth;
        }

#if ALLOW_GARBAGE
        /// <summary>
        /// Returns a bunch of information about the behaviour which can be dumped to
        /// a debug display for debugging at runtime.
        /// </summary>
        /// <returns>A formatted string of debug information.</returns>
        public override String[] GetDebugInfo()
        {
            String [] info = new String[1];

            info[0] = "Health: " + mCurrentHealth + "/" + mMaxHealth;

            return info;
        }
#endif // ALLOW_GARBAGE

        /// <summary>
        /// Subtracts the amount from the current health amount.
        /// </summary>
        /// <param name="amount">The amount to subtract from the current health.</param>
        private void ApplyDamage(Single amount)
        {
            if (amount < 0)
            {
                System.Diagnostics.Debug.Assert(false, "Attempting to apply negative damage");
                amount = 0;
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

                    if (mRemoveOnDeath)
                    {
                        GameObjectManager.pInstance.Remove(mParentGOH);
                    }
                }
            }
        }

    }
}
