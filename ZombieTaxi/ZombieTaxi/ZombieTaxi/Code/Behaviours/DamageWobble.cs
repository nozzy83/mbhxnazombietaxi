using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MBHEngine.Behaviour;
using MBHEngine.GameObject;
using MBHEngine.Math;
using ZombieTaxiContentDefs;

namespace ZombieTaxi.Behaviours
{
    class DamageWobble : MBHEngine.Behaviour.Behaviour
    {
        /// <summary>
        /// Defines how the scale of the object changes during the wobble. This is applied
        /// uniformly to both X and Y.
        /// </summary>
        private Tween mScaleTween;

        /// <summary>
        /// Defines how rotation of the object changes during the wobble.
        /// </summary>
        private Tween mRotationTween;

        /// <summary>
        /// The amount of time that needs to pass after taking damage for the effect to stop playing.
        /// If more damage is taken before this hits zero, the timer is reset, thus taking consistant 
        /// damage in succession will cause th effect to stay running.
        /// </summary>
        private StopWatch mDamageCooldown;

        /// <summary>
        /// Constructor which also handles the process of loading in the Behaviour
        /// Definition information.
        /// </summary>
        /// <param name="parentGOH">The game object that this behaviour is attached to.</param>
        /// <param name="fileName">The file defining this behaviour.</param>
        public DamageWobble(GameObject parentGOH, String fileName)
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

            DamageWobbleDefinition def = GameObjectManager.pInstance.pContentManager.Load<DamageWobbleDefinition>(fileName);

            StopWatch watch = StopWatchManager.pInstance.GetNewStopWatch();
            watch.pLifeTime = 10.0f;
            mScaleTween = new Tween(watch, 1.0f, 1.2f);
            
            watch = StopWatchManager.pInstance.GetNewStopWatch();
            watch.pLifeTime = 2.0f;
            mRotationTween = new Tween(watch, -5, 5);

            mDamageCooldown = StopWatchManager.pInstance.GetNewStopWatch();
            mDamageCooldown.pLifeTime = def.mFramesToReset;
            mDamageCooldown.ForceExpire();
        }

        /// <summary>
        /// Called at the end of the frame where mParentGOH was added to the GameObjectManager.
        /// </summary>
        public override void OnAdd()
        {
        }

        /// <summary>
        /// Called at the end of the frame on which this Behaviour's mParentGOH was removed from
        /// the GameObjectManager.
        /// </summary>
        public override void OnRemove()
        {
            StopWatchManager.pInstance.RecycleStopWatch(mScaleTween.mWatch);
            StopWatchManager.pInstance.RecycleStopWatch(mRotationTween.mWatch);
        }

        /// <summary>
        /// Called once per frame before the update function. Is called for ALL gameobjects, prior 
        /// to calling Update.
        /// </summary>
        /// <param name="gameTime">The amount of time that has passed this frame.</param>
        public override void PreUpdate(GameTime gameTime)
        {
        }

        /// <summary>
        /// Called once per frame by the game object.
        /// </summary>
        /// <param name="gameTime">The amount of time that has passed this frame.</param>
        public override void Update(GameTime gameTime)
        {
            mScaleTween.Update();
            mRotationTween.Update();

            if (!mDamageCooldown.IsExpired())
            {
                mParentGOH.pRotation = MathHelper.ToRadians(mRotationTween.mCurrentValue);
                mParentGOH.pScaleXY = mScaleTween.mCurrentValue;
            }
            else
            {
                // TODO: Don't assume these values.
                mParentGOH.pRotation = 0.0f;
                mParentGOH.pScaleXY = 1.0f;
            }
        }

        /// <summary>
        /// Called once per frame after the Update function. Is called after all objects have
        /// caled Update.
        /// </summary>
        /// <param name="gameTime">The amount of time that has passed this frame.</param>
        public override void PostUpdate(GameTime gameTime)
        {
        }

        /// <summary>
        /// Called once render cycle by the game object manager.
        /// </summary>
        /// <param name="batch">The sprite batch to render to.</param>
        /// <param name="effect">The effect being used to render this object.</param>
        public override void Render(SpriteBatch batch, Effect effect)
        {
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
            if (msg is Health.ApplyDamageMessage)
            {
                mDamageCooldown.Restart();
            }
        }

        /// <summary>
        /// Resets a behaviour to its initial state.
        /// </summary>
        public override void Reset()
        {
        }
    }
}
