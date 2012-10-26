using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MBHEngine.Behaviour;
using Microsoft.Xna.Framework;
using MBHEngine.GameObject;
using ZombieTaxiContentDefs;
using MBHEngine.Math;
using MBHEngineContentDefs;

namespace ZombieTaxi.Behaviours
{
    /// <summary>
    /// An object that explodes under some conditions.
    /// </summary>
    class Explosive : MBHEngine.Behaviour.Behaviour
    {
        /// <summary>
        /// Tells the Explosive to detonate right now.
        /// </summary>
        public class DetonateMessage : BehaviourMessage
        {
        }

        /// <summary>
        /// When an explosive explodes there is an explosion effect that needs to be shown.  This is it.
        /// </summary>
        private String mExplosionEffect;

        /// <summary>
        /// Keep track of whether or not this explosive has actually exploded yet.
        /// </summary>
        private Boolean mExploded;

        /// <summary>
        /// To make explosions look a little better they can possibly play somewhat random animations
        /// adding variety when they happen very close to each other.
        /// </summary>
        private List<String> mExplosionAnimationNames;

        /// <summary>
        /// Keeps track of whether this explosion is manually triggered using the DetonateMessage.
        /// </summary>
        private Boolean mManualExplosion;

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
        /// A list of the types of objects that this does damage to when exploding.
        /// </summary>
        private List<GameObjectDefinition.Classifications> mDamageAppliedTo;

        /// <summary>
        /// Preallocate our messages so that we don't trigger the garbage collector later.
        /// </summary>
        private SpriteRender.SetActiveAnimationMessage mSetActiveAnimationMessage;
        private Health.OnApplyDamage mOnApplyDamageMsg;
        private SpriteRender.GetAttachmentPointMessage mGetAttachmentPointMsg;

        /// <summary>
        /// Constructor which also handles the process of loading in the Behaviour
        /// Definition information.
        /// </summary>
        /// <param name="parentGOH">The game object that this behaviour is attached to.</param>
        /// <param name="fileName">The file defining this behaviour.</param>
        public Explosive(GameObject parentGOH, String fileName)
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

            ExplosiveDefinition def = GameObjectManager.pInstance.pContentManager.Load<ExplosiveDefinition>(fileName);

            mExplosionEffect = def.mEffectFileName;

            mExplosionAnimationNames = def.mAnimationsToPlay;

            mManualExplosion = def.mManualExplosion;

            mDamagedCaused = def.mDamageCaused;

            mDamageAppliedTo = new List<GameObjectDefinition.Classifications>();

            for (Int32 i = 0; i < def.mDamageAppliedTo.Count; i++)
            {
                mDamageAppliedTo.Add(def.mDamageAppliedTo[i]);
            }

            mObjectsInRange = new List<GameObject>(16);

            mSetActiveAnimationMessage = new SpriteRender.SetActiveAnimationMessage();
            mOnApplyDamageMsg = new Health.OnApplyDamage(mDamagedCaused);
            mGetAttachmentPointMsg = new SpriteRender.GetAttachmentPointMessage();

            Reset();
        }

        /// <summary>
        /// Called once per frame by the game object.
        /// </summary>
        /// <param name="gameTime">The amount of time that has passed this frame.</param>
        public override void Update(GameTime gameTime)
        {
            if (!mManualExplosion && mDamageAppliedTo.Count > 0)
            {
                // Find all the objects near by and apply some damage to them.
                mObjectsInRange.Clear();
                GameObjectManager.pInstance.GetGameObjectsInRange(mParentGOH, ref mObjectsInRange, mDamageAppliedTo);
                if (mObjectsInRange.Count > 0)
                {
                    Detonate();
                }
            }
        }

        /// <summary>
        /// Resets a behaviour to its initial state.
        /// </summary>
        public override void Reset()
        {
            mExploded = false;
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
            if (!mExploded)
            {
                // Which type of message was sent to us?
                if (!mManualExplosion &&
                    (msg is MBHEngine.Behaviour.TileCollision.OnTileCollisionMessage || msg is MBHEngine.Behaviour.Timer.OnTimerCompleteMessage))
                {
                    Detonate();
                }
                else if (msg is DetonateMessage || msg is Health.OnZeroHealth)
                {
                    Detonate();
                }
            }
        }

        /// <summary>
        /// Trigger the explosive to detonate.
        /// </summary>
        private void Detonate()
        {
            // Do this now so that if another event is sent from within this function it will not trigger
            // a second detonation.
            mExploded = true;

            // Pick a random animation to play.
            Int32 index = RandomManager.pInstance.RandomNumber() % mExplosionAnimationNames.Count;
            mSetActiveAnimationMessage.mAnimationSetName = mExplosionAnimationNames[index];
            mSetActiveAnimationMessage.mReset = true;

            GameObject fx = GameObjectFactory.pInstance.GetTemplate(mExplosionEffect);
            mGetAttachmentPointMsg.mName = "Ground";
            mGetAttachmentPointMsg.mPoisitionInWorld = mParentGOH.pPosition; // Set a default incase it doesn't have a Ground attachment point.
            mParentGOH.OnMessage(mGetAttachmentPointMsg);
            fx.pPosition = mGetAttachmentPointMsg.mPoisitionInWorld;
            fx.OnMessage(mSetActiveAnimationMessage);
            GameObjectManager.pInstance.Add(fx);

            // Find all the objects near by and apply some damage to them.
            mObjectsInRange.Clear();
            GameObjectManager.pInstance.GetGameObjectsInRange(mParentGOH, ref mObjectsInRange, mDamageAppliedTo);
            for (Int32 i = 0; i < mObjectsInRange.Count; i++)
            {
                mObjectsInRange[i].OnMessage(mOnApplyDamageMsg);
            }

            // This guy is exploded so he should be cleaned up.
            GameObjectManager.pInstance.Remove(mParentGOH);
        }
    }
}
