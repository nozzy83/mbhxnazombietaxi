using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MBHEngine.GameObject;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MBHEngine.Input;
using Microsoft.Xna.Framework.Input;
using MBHEngine.Debug;
using MBHEngine.Math;

namespace ZombieTaxi.Behaviour
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
            mMoveSpeed = 5.0f;
            mCurrentBullet = 0;

            base.LoadContent(fileName);

            mGun = new GameObject("Gun\\Gun");
            GameObjectManager.pInstance.Add(mGun);

            mBullets = new GameObject[100];

            for (Int16 i = 0; i < 100; i++)
            {
                mBullets[i] = new GameObject("Bullet\\Bullet");
                mBullets[i].pDirection.mSpeed = 30.0f;
                GameObjectManager.pInstance.Add(mBullets[i]);
            }
        }

        /// <summary>
        /// Called once per frame by the game object.
        /// </summary>
        /// <param name="gameTime">The amount of time that has passed this frame.</param>
        public override void Update(GameTime gameTime)
        {            
            GamePadState g = InputManager.pInstance.pActiveGamePadState;

            Vector2 dir1 = new Vector2(mMoveSpeed, -mMoveSpeed);
            dir1 *= g.ThumbSticks.Left;
            mParentGOH.pOrientation.mPosition += dir1;

            Double angle = Math.Atan2(-g.ThumbSticks.Right.Y, g.ThumbSticks.Right.X);
            if (angle < 0)
            {
                angle += 2 * Math.PI;
            }

            Single deg = MathHelper.ToDegrees((Single)angle);

#if ALLOW_GARBAGE
            DebugMessageDisplay.pInstance.AddDynamicMessage("Angle: " + deg);
            DebugMessageDisplay.pInstance.AddDynamicMessage("X: " + g.ThumbSticks.Right.X);
            DebugMessageDisplay.pInstance.AddDynamicMessage("Y: " + g.ThumbSticks.Right.Y);
#endif

            mGun.pOrientation.mPosition = mParentGOH.pOrientation.mPosition;
            Vector2 dir = new Vector2(32.0f, -32.0f);
            dir *= Vector2.Normalize(g.ThumbSticks.Right);
            mGun.pOrientation.mPosition += dir;
            mGun.pOrientation.mRotation = (Single)angle;

            //if (InputManager.pInstance.CheckAction(InputManager.InputActions.R2, true))
            if (dir.X != 0 && dir.Y != 0)
            {
                Single spread = 0.1f;
                if (InputManager.pInstance.CheckAction(InputManager.InputActions.R2, false))
                {
                    spread = 0.5f;
                }
                mBullets[mCurrentBullet].pOrientation.mPosition = mGun.pOrientation.mPosition;
                mBullets[mCurrentBullet].pOrientation.mRotation = mGun.pOrientation.mRotation;

                Single offset = ((Single)RandomManager.pInstance.RandomPercent() * spread) - (spread * 0.5f);
                angle += offset;
                Vector2 finalDir = new Vector2((Single)Math.Cos(angle), (Single)Math.Sin(angle));
                finalDir.Y *= -1;

                mBullets[mCurrentBullet].pDirection.mForward = finalDir;

                // The screen's y direction is opposite the controller.
                mBullets[mCurrentBullet].pDirection.mForward.Y *= -1;

                mCurrentBullet++;

                if (mCurrentBullet >= mBullets.Length)
                {
                    mCurrentBullet = 0;
                }
            }
        }

        /// <summary>
        /// Called once render cycle by the game object manager.
        /// </summary>
        /// <param name="batch">The sprite batch to render to.</param>
        public override void Render(SpriteBatch batch)
        {
        }
    }
}
