using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MBHEngine.GameObject;
using Microsoft.Xna.Framework.Graphics;
using MBHEngine.Input;
using Microsoft.Xna.Framework.Input;
using MBHEngine.Debug;
using MBHEngine.Math;
using MBHEngine.Render;
using MBHEngine.Behaviour;
using MBHEngine.World;
using Microsoft.Xna.Framework;
using ZombieTaxiContentDefs;

namespace ZombieTaxi.Behaviours
{
    /// <summary>
    /// A class to handle the controls of a twin stick shooter.
    /// </summary>
    class TwinStick : MBHEngine.Behaviour.Behaviour
    {
        /// <summary>
        /// The speed at which the GOH is moved when the user presses the dpad.
        /// </summary>
        private Single mMoveSpeed;

        /// <summary>
        /// The gun used for firing.
        /// </summary>
        private GameObject mGun;

        /// <summary>
        /// The bullets fired from the gun.
        /// </summary>
        private GameObject[] mBullets;

        /// <summary>
        /// mBullets is used as a circular array, and this is the current index.
        /// </summary>
        private Int16 mCurrentBullet;

        private SpriteRender.SetSpriteEffectsMessage mSpriteFxMsg;
        private SpriteRender.GetSpriteEffectsMessage mGetSpriteFxMsg;
        private SpriteRender.SetActiveAnimationMessage mSpriteActiveAnimMsg;

        /// <summary>
        /// Constructor which also handles the process of loading in the Behaviour
        /// Definition information.
        /// </summary>
        /// <param name="parentGOH">The game object that this behaviour is attached to.</param>
        /// <param name="fileName">The file defining this behaviour.</param>
        public TwinStick(GameObject parentGOH, String fileName)
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

            TwinStickDefinition def = GameObjectManager.pInstance.pContentManager.Load<TwinStickDefinition>(fileName);

            mMoveSpeed = def.mMoveSpeed;
            mCurrentBullet = 0;

            mBullets = new GameObject[100];

            for (Int16 i = 0; i < 100; i++)
            {
                mBullets[i] = new GameObject("GameObjects\\Items\\Bullet\\Bullet");
                mBullets[i].pDirection.mSpeed = 1.75f;
                GameObjectManager.pInstance.Add(mBullets[i]);
            }

            mGun = new GameObject("GameObjects\\Items\\Gun\\Gun");
            GameObjectManager.pInstance.Add(mGun);
            mGun.pOrientation.mPosition = mParentGOH.pOrientation.mPosition;
            mGun.pOrientation.mPosition.X = mGun.pOrientation.mPosition.X + 1.0f;
            //mGun.pOrientation.mPosition.Y = mGun.pOrientation.mPosition.Y + 4.5f;

            mSpriteFxMsg = new SpriteRender.SetSpriteEffectsMessage();
            mGetSpriteFxMsg = new SpriteRender.GetSpriteEffectsMessage();
            mSpriteActiveAnimMsg = new SpriteRender.SetActiveAnimationMessage();
        }

        /// <summary>
        /// Called once per frame by the game object.
        /// </summary>
        /// <param name="gameTime">The amount of time that has passed this frame.</param>
        public override void Update(GameTime gameTime)
        {
            // Grab the current state of the gamepad.
            GamePadState g = InputManager.pInstance.pActiveGamePadState;

            // Store the original position prior to polling any input for use with collision reactions.
            Vector2 origPos = mParentGOH.pOrientation.mPosition;

            // The character will move at this rate in the direction of the Left Analog Stick.
            Vector2 dir1 = new Vector2(mMoveSpeed, -mMoveSpeed);
            dir1 *= g.ThumbSticks.Left;
            mParentGOH.pOrientation.mPosition += dir1;

            // If the player has moved at all this frame, start with the gun right behind him.  We don't do this if he didn't
            // move so that we don't need to figure out where to position it; we just use what was already there.
            if (dir1.X != 0.0f)
            {
                mGun.pOrientation.mPosition = mParentGOH.pOrientation.mPosition;
                //mGun.pOrientation.mPosition.Y = mGun.pOrientation.mPosition.Y + 4.5f;
            }

            // Flip the sprite to face the direction that we are moving.
            if (g.ThumbSticks.Left.X > 0)
            {
                mSpriteFxMsg.mSpriteEffects = SpriteEffects.None;
                mParentGOH.OnMessage(mSpriteFxMsg);
                mSpriteActiveAnimMsg.mAnimationSetName = "Walk";
                mParentGOH.OnMessage(mSpriteActiveAnimMsg);

                // Initially the gun is positioned assuming the R-Stick is not pressed.  Just point straight
                // in the direction the player is walking.
                mSpriteFxMsg.mSpriteEffects = SpriteEffects.None;
                mGun.OnMessage(mSpriteFxMsg);
                mGun.pOrientation.mRotation = 0.0f;
                mGun.pOrientation.mPosition.X = mGun.pOrientation.mPosition.X + 1.0f;
            }
            else if (g.ThumbSticks.Left.X < 0)
            {
                mSpriteFxMsg.mSpriteEffects = SpriteEffects.FlipHorizontally;
                mParentGOH.OnMessage(mSpriteFxMsg);
                mSpriteActiveAnimMsg.mAnimationSetName = "Walk";
                mParentGOH.OnMessage(mSpriteActiveAnimMsg);

                mSpriteFxMsg.mSpriteEffects = SpriteEffects.FlipVertically;
                mGun.OnMessage(mSpriteFxMsg);
                mGun.pOrientation.mRotation = MathHelper.ToRadians(180.0f);
                mGun.pOrientation.mPosition.X = mGun.pOrientation.mPosition.X - 1.0f;
            }
            else
            {
                mSpriteActiveAnimMsg.mAnimationSetName = "Idle";
                mParentGOH.OnMessage(mSpriteActiveAnimMsg);
            }

            // Convert the direction of the right analog stick into an angle so that it can be used to set the rotation of
            // the sprite.
            Double angle = Math.Atan2(-g.ThumbSticks.Right.Y, g.ThumbSticks.Right.X);
            if (angle < 0)
            {
                angle += 2 * Math.PI;
            }

#if ALLOW_GARBAGE
            Single deg = MathHelper.ToDegrees((Single)angle);
            DebugMessageDisplay.pInstance.AddDynamicMessage("Angle: " + deg);
            DebugMessageDisplay.pInstance.AddDynamicMessage("X: " + g.ThumbSticks.Right.X);
            DebugMessageDisplay.pInstance.AddDynamicMessage("Y: " + g.ThumbSticks.Right.Y);
#endif // ALLOW_GARBAGE

            // Determine the direction that right analog stick is pointing (if any).
            Vector2 dir = Vector2.Normalize(g.ThumbSticks.Right);

            // If the user is pressing the right analog stick, then they need to fire a bullet.
            if (!Single.IsNaN(dir.X) && !Single.IsNaN(dir.Y))
            {
                //mGun.pOrientation.mPosition += dir;
                mGun.pOrientation.mRotation = (Single)angle;

                // Use dir, not finalDir, so that the direction does not include the spread randomization.
                if (dir.X > 0)
                {
                    mSpriteFxMsg.mSpriteEffects = SpriteEffects.None;
                    mParentGOH.OnMessage(mSpriteFxMsg);

                    // To start the gun would be set to point in the direction we are walking.  We have turned to face the direction
                    // the player is shooting, so the gun needs to be updated as well.
                    mSpriteFxMsg.mSpriteEffects = SpriteEffects.None;
                    mGun.OnMessage(mSpriteFxMsg);
                    mGun.pOrientation.mPosition.X = mParentGOH.pOrientation.mPosition.X + 1.0f;
                }
                else if (dir.X < 0)
                {
                    mSpriteFxMsg.mSpriteEffects = SpriteEffects.FlipHorizontally;
                    mParentGOH.OnMessage(mSpriteFxMsg);

                    mSpriteFxMsg.mSpriteEffects = SpriteEffects.FlipVertically;
                    mGun.OnMessage(mSpriteFxMsg);
                    mGun.pOrientation.mPosition.X = mParentGOH.pOrientation.mPosition.X - 1.0f;
                }

                // We want some slight randomness to the bullets fired.  This is the randomness in radians.
                Single spread = 0.1f;

                // If they are holding R2, then the spread is even larger.
                if (InputManager.pInstance.CheckAction(InputManager.InputActions.R2, false))
                {
                    spread = 0.5f;
                }

                // Offset by a random amount within the spread range.
                Single offset = ((Single)RandomManager.pInstance.RandomPercent() * spread) - (spread * 0.5f);
                angle += offset;

                // Convert the angle back into a vector so that it can be used to move the bullet.
                Vector2 finalDir = new Vector2((Single)Math.Cos(angle), (Single)Math.Sin(angle));
                finalDir.Y *= -1;

                Vector2 finalUp = new Vector2(-finalDir.Y, -finalDir.X);
                if (finalDir.X < 0) finalUp *= -1;

                // Update the game object with all the new data.
                mBullets[mCurrentBullet].pOrientation.mPosition = mGun.pOrientation.mPosition;
                mBullets[mCurrentBullet].pOrientation.mRotation = (Single)angle;
                mBullets[mCurrentBullet].pDirection.mForward = finalDir;

                finalDir.Y *= -1;
                mBullets[mCurrentBullet].pOrientation.mPosition += finalUp * 1.0f;
                mBullets[mCurrentBullet].pOrientation.mPosition += finalDir * 3.5f;

                // The screen's y direction is opposite the controller.
                mBullets[mCurrentBullet].pDirection.mForward.Y *= -1;

                // By default the bullets have their renderer turned off.
                mBullets[mCurrentBullet].pDoRender = true;

                // Next time fire a new bullet.
                mCurrentBullet++;

                // Make sure the array gets looped around.
                if (mCurrentBullet >= mBullets.Length)
                {
                    mCurrentBullet = 0;
                }
            }

#if ALLOW_GARBAGE
            DebugMessageDisplay.pInstance.AddDynamicMessage("Player Pos: " + mParentGOH.pOrientation.mPosition);
#endif
            // DEBUG TESTING
            //
            if (InputManager.pInstance.CheckAction(InputManager.InputActions.A, true))
            {
                mParentGOH.OnMessage(new Health.OnApplyDamage(113));
            }

            CameraManager.pInstance.pTargetPosition = mParentGOH.pOrientation.mPosition;
        }

        /// <summary>
        /// Called once render cycle by the game object manager.
        /// </summary>
        /// <param name="batch">The sprite batch to render to.</param>
        public override void Render(SpriteBatch batch)
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
            if (msg is Health.OnZeroHealth)
            {
#if ALLOW_GARBAGE
                DebugMessageDisplay.pInstance.AddConstantMessage("Player Died");
#endif
            }
        }
    }
}
