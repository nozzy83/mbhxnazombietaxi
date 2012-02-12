using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MBHEngine.Behaviour;
using Microsoft.Xna.Framework;
using MBHEngine.GameObject;

namespace ZombieTaxi.Behaviours
{
    /// <summary>
    /// An object that explodes under some conditions.
    /// </summary>
    class Explosive : MBHEngine.Behaviour.Behaviour
    {
        /// <summary>
        /// When an explosive explodes there is an explosion effect that needs to be shown.  This is it.
        /// </summary>
        GameObject mExplosionEffect;

        /// <summary>
        /// Keep track of whether or not this explosive has actually exploded yet.
        /// </summary>
        Boolean mExploded;

        /// <summary>
        /// Preallocate our messages so that we don't trigger the garbage collector later.
        /// </summary>
        SpriteRender.SetActiveAnimationMessage mSetActiveAnimationMessage;

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

            //HealthDefinition def = GameObjectManager.pInstance.pContentManager.Load<HealthDefinition>(fileName);

            mExplosionEffect = new GameObject("GameObjects\\Effects\\Explosion\\Explosion");
            GameObjectManager.pInstance.Add(mExplosionEffect);

            mExploded = false;

            mSetActiveAnimationMessage = new SpriteRender.SetActiveAnimationMessage();
        }

        /// <summary>
        /// Called once per frame by the game object.
        /// </summary>
        /// <param name="gameTime">The amount of time that has passed this frame.</param>
        public override void Update(GameTime gameTime)
        {
        }

        /// <summary>
        /// Resets a behaviour to its initial state.
        /// </summary>
        public override void Reset()
        {
            mExplosionEffect.ResetBehaviours();
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
            // Which type of message was sent to us?
            if ((!mExploded) &&
                (msg is MBHEngine.Behaviour.TileCollision.OnTileCollisionMessage || 
                msg is MBHEngine.Behaviour.Timer.OnTimerCompleteMessage))
            {
                // TODO: Play explosion here.
                mSetActiveAnimationMessage.mAnimationSetName = "Explode";
                mSetActiveAnimationMessage.mReset = true;

                mExplosionEffect.pOrientation.mPosition = mParentGOH.pOrientation.mPosition;
                mExplosionEffect.OnMessage(mSetActiveAnimationMessage);
                mExplosionEffect.pDoRender = mExplosionEffect.pDoUpdate = true;

                mParentGOH.pDoRender = false;
                mParentGOH.pDirection.mForward = Vector2.Zero;

                mExploded = true;
            }
        }
    }
}
