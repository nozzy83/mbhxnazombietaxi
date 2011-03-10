﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace MBHEngine.Render
{
    /// <summary>
    /// For now we just have the camera work wrapped up into a signleton so that is can be accessed and manipulated
    /// easily from across the game.
    /// 
    /// Eventually this might need to be broken up into behaviours and attached to game objects to allow for more varying
    /// types of cameras.
    /// </summary>
    public class CameraManager
    {
        /// <summary>
        /// Static instance of the class; this is a singleton class.
        /// </summary>
        private static CameraManager mInstance = null;

        /// <summary>
        /// The current transform of the camera.
        /// </summary>
        private Matrix mTransform;

        /// <summary>
        /// The center of the screen, used for offsetting the matrix.
        /// </summary>
        private Matrix mScreenCenter;

        /// <summary>
        /// The position which the camera is trying to get to.
        /// </summary>
        private Vector2 mTargetPosition;

        /// <summary>
        /// The last target position successfully reached.
        /// </summary>
        private Vector2 mLastPosition;

        /// <summary>
        /// The amount of frames it takes to get from the last position to the target position.
        /// </summary>
        private Single mBlendFrames;

        /// <summary>
        /// The current number of frames that have passed while blending between the last and target transform.
        /// </summary>
        private Single mCurBlendFrames;

        /// <summary>
        /// Constructor.
        /// </summary>
        private CameraManager()
        {
            mTransform = Matrix.Identity;
            mTargetPosition = new Vector2(640, 360);
            mLastPosition = new Vector2();
            mCurBlendFrames = 0;

            mBlendFrames = 10;
            mScreenCenter = Matrix.CreateTranslation(640, 360, 0);

            mTransform =
                Matrix.CreateTranslation(-new Vector3(0f, 0f, 0.0f)) *
                //Matrix.CreateRotationZ(Rotation) *
                Matrix.CreateScale(new Vector3(1.0f, 1.0f, 1.0f)) *
                mScreenCenter;
        }

        /// <summary>
        /// Call this once per frame to keep the camera up to date.
        /// </summary>
        /// <param name="gameTime">The amount of time that has passed this frame.</param>
        public void Update(GameTime gameTime)
        {
            mCurBlendFrames += 1;

            // Calculate the percent of the tween that has been completed.
            Single percent = (Single)mCurBlendFrames / (Single)mBlendFrames;

            Vector2 curPos = Vector2.SmoothStep(mLastPosition, mTargetPosition, percent);

            mTransform =
                Matrix.CreateTranslation(-new Vector3(mTargetPosition, 0.0f)) * // change this to curPos to bring back blend
                //Matrix.CreateRotationZ(Rotation) *
                Matrix.CreateScale(new Vector3(1.0f, 1.0f, 1.0f)) *
                mScreenCenter;
        }

        /// <summary>
        /// Access to the single instance of the class.
        /// </summary>
        public static CameraManager pInstance
        {
            get
            {
                if(mInstance == null)
                {
                    mInstance = new CameraManager();
                }

                return mInstance;
            }
        }

        /// <summary>
        /// Returns the current transform of the camera.
        /// </summary>
        public Matrix pFinalTransform
        {
            get
            {
                return mTransform;
            }
        }

        public Vector2 pTargetPosition
        {
            get
            {
                return mTargetPosition;
            }
            set
            {
                if (value.X != mTargetPosition.X || value.Y != mTargetPosition.Y)
                {
                    // Calculate the percent of the tween that has been completed.
                    Single percent = (Single)mCurBlendFrames / (Single)mBlendFrames;
                    Vector2 curPos = Vector2.SmoothStep(mLastPosition, mTargetPosition, percent);

                    mLastPosition = curPos;
                    mTargetPosition = value;
                    mCurBlendFrames = 0;
                }
            }
        }
    }
}
