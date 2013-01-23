using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MBHEngine.Behaviour;
using Microsoft.Xna.Framework;
using MBHEngine.GameObject;
using ZombieTaxiContentDefs;
using MBHEngineContentDefs;

namespace ZombieTaxi.Behaviours
{
    /// <summary>
    /// Simply applies damage to any objects touching this object.  Think fire!
    /// </summary>
    class DamageOnContact : MBHEngine.Behaviour.Behaviour
    {
        /// <summary>
        /// The amount of damage this explosive causes when exploding.
        /// </summary>
        private Single mDamagedCaused;

        /// <summary>
        /// Used to store all the objects in range of this explosion so that it can
        /// apply damage to them.
        /// Preallocated to avoid GC.
        /// </summary>
        private List<GameObject> mObjectsInRange;

        /// <summary>
        /// Keeps track of all the game objects which have already been damaged by this
        /// behaviour, to avoid to from continually damaging the same object.
        /// </summary>
        private List<GameObject> mObjectsDamaged;

        /// <summary>
        /// A list of the types of objects that this does damage to when exploding.
        /// </summary>
        private List<GameObjectDefinition.Classifications> mDamageAppliedTo;

        /// <summary>
        /// Preallocate our messages so that we don't trigger the garbage collector later.
        /// </summary>
        private Health.ApplyDamageMessage mApplyDamageMsg;

        /// <summary>
        /// Constructor which also handles the process of loading in the Behaviour
        /// Definition information.
        /// </summary>
        /// <param name="parentGOH">The game object that this behaviour is attached to.</param>
        /// <param name="fileName">The file defining this behaviour.</param>
        public DamageOnContact(GameObject parentGOH, String fileName)
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

            DamageOnContactDefinition def = GameObjectManager.pInstance.pContentManager.Load<DamageOnContactDefinition>(fileName);

            mDamagedCaused = def.mDamageCaused;

            mDamageAppliedTo = new List<GameObjectDefinition.Classifications>();

            for (Int32 i = 0; i < def.mDamageAppliedTo.Count; i++)
            {
                mDamageAppliedTo.Add(def.mDamageAppliedTo[i]);
            }

            mObjectsInRange = new List<GameObject>(16);
            mObjectsDamaged = new List<GameObject>(64);

            mApplyDamageMsg = new Health.ApplyDamageMessage();
            mApplyDamageMsg.mDamageAmount_In = mDamagedCaused;

            Reset();
        }

        /// <summary>
        /// Called once per frame by the game object.
        /// </summary>
        /// <param name="gameTime">The amount of time that has passed this frame.</param>
        public override void Update(GameTime gameTime)
        {
            // Find all the objects near by and apply some damage to them.
            mObjectsInRange.Clear();
            GameObjectManager.pInstance.GetGameObjectsInRange(mParentGOH, ref mObjectsInRange, mDamageAppliedTo);
            for (Int32 i = 0; i < mObjectsInRange.Count; i++)
            {
                if( !mObjectsDamaged.Contains( mObjectsInRange[i] ) )
                {
                    mObjectsDamaged.Add(mObjectsInRange[i]);
                    mObjectsInRange[i].OnMessage(mApplyDamageMsg);
                }
            }
        }

        /// <summary>
        /// The main interface for communicating between behaviours.  Using polymorphism, we
        /// define a bunch of different messages deriving from BehaviourMessage.  Each behaviour
        /// can then check for particular upcasted message types, and either grab some data 
        /// from it (set message) or store some data in it (get message).
        /// </summary>
        /// <param name="msg">The message being communicated to the behaviour.</param>
        public override void OnMessage(ref BehaviourMessage msg)
        {
        }

        /// <summary>
        /// Resets a behaviour to its initial state.
        /// </summary>
        public override void Reset()
        {
            mObjectsDamaged.Clear();
        }
    }
}
