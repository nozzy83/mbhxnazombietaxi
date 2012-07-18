using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MBHEngine.GameObject;
using MBHEngine.Debug;

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
        /// The camera transform as it should be used for UI.  Essentially just the scale,
        /// without any translation or centering.
        /// </summary>
        private Matrix mTransformUI;

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
        /// The amount of zoom to scale the camera shot by.  1 means default zoom, higher numbers mean more zoomed in.
        /// </summary>
        private Single mZoomAmount;

        /// <summary>
        /// Keeps track of the viewable world space.
        /// </summary>
        private MBHEngine.Math.Rectangle mViewRectangle;

        /// <summary>
        /// Initialize the singleton.  Call before first use.
        /// </summary>
        /// <param name="device">The initialized graphics device.  Used to calculate screen position.</param>
        public void Initialize(GraphicsDevice device)
        {
            mTransform = Matrix.Identity;
            mTargetPosition = new Vector2(device.Viewport.Width * 0.5f, device.Viewport.Height * 0.5f);
            mLastPosition = new Vector2();
            mCurBlendFrames = 0;

            mBlendFrames = 10;
            mScreenCenter = Matrix.CreateTranslation(device.Viewport.Width * 0.5f, device.Viewport.Height * 0.5f, 0);
#if SMALL_WINDOW
            mZoomAmount = 4.0f;
#else
            mZoomAmount = 8.0f;
#endif

            mTransform =
                Matrix.CreateTranslation(-new Vector3(0f, 0f, 0.0f)) *
                //Matrix.CreateRotationZ(Rotation) *
                Matrix.CreateScale(new Vector3(mZoomAmount)) *
                mScreenCenter;

            mTransformUI = Matrix.CreateScale(new Vector3(mZoomAmount));

            mViewRectangle = new Math.Rectangle();
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
                Matrix.CreateScale(new Vector3(mZoomAmount)) *
                //Matrix.CreateScale(new Vector3(1.0f, 1.0f, 1.0f)) *
                mScreenCenter;

            // Ideally this would only happen when the zoom property changes, but I am worried I will forget and
            // set it directly from within this class.
            mTransformUI = Matrix.CreateScale(new Vector3(mZoomAmount));

            // Update the view area.
            mViewRectangle.pCenterPoint = pTargetPosition;
            mViewRectangle.pDimensions = new Vector2(
                (GameObjectManager.pInstance.pGraphicsDevice.Viewport.Width) / mZoomAmount,
                (GameObjectManager.pInstance.pGraphicsDevice.Viewport.Height) / mZoomAmount);

            //DebugShapeDisplay.pInstance.AddAABB(mViewRectangle, Color.Orange);
        }

        /// <summary>
        /// Checks if a Rectangle is on screen at all.
        /// </summary>
        /// <param name="rect">The rectangle to check.</param>
        /// <returns></returns>
        public Boolean IsOnCamera(MBHEngine.Math.Rectangle rect)
        {
            return mViewRectangle.Intersects(rect);
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

        /// <summary>
        /// Returns the current transform of the camera for use with rendering UI.  This transform
        /// will not include things like translation and screen centering.  It is pretty much just 
        /// scale.
        /// </summary>
        public Matrix pFinalTransformUI
        {
            get
            {
                return mTransformUI;
            }
        }

        /// <summary>
        /// Where the camera is trying to get to. We quickly blend between the current position
        /// to this position.
        /// </summary>
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

        /// <summary>
        /// The position in the world that is currently at the center of the screen.
        /// </summary>
        public Vector2 pScreenCenter
        {
            get
            {
                return new Vector2(mScreenCenter.Translation.X, mScreenCenter.Translation.Y);
            }
        }

        /// <summary>
        /// The amount that the camera is currently zoomed in by. This is a scalar, so 1.0 
        /// means no zoom.
        /// </summary>
        public Single pZoomScale
        {
            get
            {
                return mZoomAmount;
            }
            set
            {
                mZoomAmount = value;
            }
        }

        /// <summary>
        /// A rectangle defining the viewable area of the world.
        /// </summary>
        public MBHEngine.Math.Rectangle pViewRect
        {
            get
            {
                return mViewRectangle;
            }
        }
    }
}
