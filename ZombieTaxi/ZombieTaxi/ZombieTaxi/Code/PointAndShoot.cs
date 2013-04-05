using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MBHEngine.Behaviour;
using MBHEngine.GameObject;
using MBHEngine.Debug;
using MBHEngine.Math;
using ZombieTaxiContentDefs;

namespace ZombieTaxi.Behaviours
{
    class PointAndShoot : MBHEngine.Behaviour.Behaviour
    {
        /// <summary>
        /// The gun used for firing.
        /// </summary>
        private GameObject mGun;

        /// <summary>
        /// The amount of time that needs to pass between shots of the gun.
        /// </summary>
        private StopWatch mGunCooldown;

        /// <summary>
        /// The distance at which this Behaviour will start trying to target an object.
        /// </summary>
        public Single mFiringRange;

        /// <summary>
        /// The speed at which bullets will move when fired.
        /// </summary>
        public Single mBulletSpeed;

        /// <summary>
        /// The name of the GameObject script to use for the Bullet fired by the gun.
        /// </summary>
        public String mBulletScriptName;

        /// <summary>
        /// Preallocated list of the types of targets we will be trying to shoot.
        /// </summary>
        private List<MBHEngineContentDefs.GameObjectDefinition.Classifications> mTargetClassifications;

        /// <summary>
        /// Preallocated list to store potential firing targets.
        /// </summary>
        private List<GameObject> mPotentialTargets;

        /// <summary>
        /// Preallocated messages to avoid GC.
        /// </summary>
        private FaceForward.SetLookTargetMessage mSetLookTargetMsg;
        private SpriteRender.SetSpriteEffectsMessage mSetSpriteEffectsMsg;
        private SpriteRender.GetAttachmentPointMessage mGetAttachmentPointMsg;

        /// <summary>
        /// Constructor which also handles the process of loading in the Behaviour
        /// Definition information.
        /// </summary>
        /// <param name="parentGOH">The game object that this behaviour is attached to.</param>
        /// <param name="fileName">The file defining this behaviour.</param>
        public PointAndShoot(GameObject parentGOH, String fileName)
            : base(parentGOH, fileName)
        {
        }

        /// <summary>
        /// Destructor.
        /// </summary>
        ~PointAndShoot()
        {
            StopWatchManager.pInstance.RecycleStopWatch(mGunCooldown);
        }

        /// <summary>
        /// Call this to initialize a Behaviour with data supplied in a file.
        /// </summary>
        /// <param name="fileName">The file to load from.</param>
        public override void LoadContent(String fileName)
        {
            base.LoadContent(fileName);

            PointAndShootDefinition def = GameObjectManager.pInstance.pContentManager.Load<PointAndShootDefinition>(fileName);

            mGun = new GameObject(def.mGunScriptName);
            GameObjectManager.pInstance.Add(mGun);
            mGun.pPosition = mParentGOH.pPosition;
            mGun.pPosX = mGun.pPosX + 1.0f;
            
            mGunCooldown = StopWatchManager.pInstance.GetNewStopWatch();
            mGunCooldown.pLifeTime = def.mFiringDelay; // 1 bullet fired every mFiringDelay frames.

            mFiringRange = def.mFiringRange;
            mBulletSpeed = def.mBulletSpeed;
            mTargetClassifications = def.mTargetClassifications;
            mBulletScriptName = def.mBulletScriptName;

            mPotentialTargets = new List<GameObject>(16);

            mSetLookTargetMsg = new FaceForward.SetLookTargetMessage();
            mSetSpriteEffectsMsg = new SpriteRender.SetSpriteEffectsMessage();
            mGetAttachmentPointMsg = new SpriteRender.GetAttachmentPointMessage();
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
            if (null != mGun)
            {
                GameObjectManager.pInstance.Remove(mGun);
                mGun = null;
            }
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
            mPotentialTargets.Clear();

            // Find all the possible targets in our firing range.
            GameObjectManager.pInstance.GetGameObjectsInRange(
                mParentGOH.pPosition,
                mFiringRange,
                ref mPotentialTargets,
                mTargetClassifications);

            // Most calculations are based on the attachment point of the Gun, rather than the position of
            // the parent GameObject.
            mGetAttachmentPointMsg.mName_In = "Gun";
            mParentGOH.OnMessage(mGetAttachmentPointMsg);

            // Position the gun at the attachment point every frame since we will be moving.
            mGun.pPosition = mGetAttachmentPointMsg.mPoisitionInWorld_Out;

            // If no targets were found there is still a bit of work to do.
            if (mPotentialTargets.Count <= 0)
            {
                // With no targets active, use the forward facing direction.
                Single dir = mParentGOH.pDirection.mForward.X;
                if (dir < 0)
                {
                    mSetSpriteEffectsMsg.mSpriteEffects_In = SpriteEffects.FlipVertically;
                    mGun.OnMessage(mSetSpriteEffectsMsg);
                    mGun.pRotation = MathHelper.ToRadians(180.0f);
                }
                else if (dir > 0)
                {
                    mSetSpriteEffectsMsg.mSpriteEffects_In = SpriteEffects.None;
                    mGun.OnMessage(mSetSpriteEffectsMsg);
                    mGun.pRotation = MathHelper.ToRadians(0.0f);
                }

                // Clear the look target.
                mSetLookTargetMsg.mTarget_In = null;
                mParentGOH.OnMessage(mSetLookTargetMsg);

                return;
            }

            // Just pick the first target. This might be improved with some logic to pick targets 
            Vector2 target = mPotentialTargets[0].pPosition;

            mSetLookTargetMsg.mTarget_In = mPotentialTargets[0];
            mParentGOH.OnMessage(mSetLookTargetMsg);

            Vector2 toTarg = target - mGetAttachmentPointMsg.mPoisitionInWorld_Out;
            toTarg.Normalize();

            //DebugShapeDisplay.pInstance.AddSegment(mGetAttachmentPointMsg.mPoisitionInWorld_Out, mGetAttachmentPointMsg.mPoisitionInWorld_Out + (toTarg * 8.0f), Color.Red);

            // Convert the direction into an angle so that it can be used to set the rotation of
            // the sprite.
            Double angle = Math.Atan2(toTarg.Y, toTarg.X);
            if (angle < 0)
            {
                angle += 2 * Math.PI;
            }

            mGun.pRotation = (Single)angle;

            if (toTarg.X < 0)
            {
                mSetSpriteEffectsMsg.mSpriteEffects_In = SpriteEffects.FlipVertically;
                mGun.OnMessage(mSetSpriteEffectsMsg);
            }
            else if(toTarg.X > 0)
            {
                mSetSpriteEffectsMsg.mSpriteEffects_In = SpriteEffects.None;
                mGun.OnMessage(mSetSpriteEffectsMsg);
            }

            // We want some slight randomness to the bullets fired.  This is the randomness in radians.
            Single spread = 0.1f;

            // Offset by a random amount within the spread range.
            Single offset = ((Single)RandomManager.pInstance.RandomPercent() * spread) - (spread * 0.5f);
            angle += offset;

            // Convert the angle back into a vector so that it can be used to move the bullet.
            Vector2 finalDir = toTarg;
            finalDir.Y *= -1;

            Vector2 finalUp = new Vector2(-finalDir.Y, -finalDir.X);
            if (finalDir.X < 0) finalUp *= -1;

            if (mGunCooldown.IsExpired())
            {
                mGunCooldown.Restart();

                GameObject bullet = GameObjectFactory.pInstance.GetTemplate(mBulletScriptName);

                if (bullet != null)
                {
                    // Store the direction locally so as to not alter it and screw things
                    // up for the grenade afterwards.
                    Vector2 bulletDir = finalDir;

                    bullet.pDirection.mSpeed = mBulletSpeed;

                    // Update the game object with all the new data.
                    bullet.pPosition = mGun.pPosition;
                    bullet.pRotation = (Single)angle;
                    bullet.pDirection.mForward = bulletDir;

                    bulletDir.Y *= -1;
                    bullet.pPosition += finalUp * 1.0f;
                    bullet.pPosition += bulletDir * 3.5f;

                    // The screen's y direction is opposite the controller.
                    bullet.pDirection.mForward.Y *= -1;

                    GameObjectManager.pInstance.Add(bullet);
                }
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
            if (msg is Health.OnZeroHealthMessage)
            {
                // When this object dies, disable this behaviour.
                mParentGOH.SetBehaviourEnabled<PointAndShoot>(false);

                if (null != mGun)
                {
                    GameObjectManager.pInstance.Remove(mGun);
                    mGun = null;
                }
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
