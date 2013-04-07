using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MBHEngine.Debug;
using MBHEngine.World;
using MBHEngineContentDefs;
using MBHEngine.GameObject;

namespace MBHEngine.Behaviour
{
    public class TileCollision : MBHEngine.Behaviour.Behaviour
    {
        /// <summary>
        /// Sent out when mParentGOH collides with a tile.
        /// </summary>
        public class OnTileCollisionMessage : BehaviourMessage
        {
            /// <summary>
            /// Call this to put a message back to its default state.
            /// </summary>
            public override void Reset() { }
        }

        /// <summary>
        /// Store the previous position so that in the event of a collision we can go back.
        /// </summary>
        private Vector2 mPreviousPos;

        /// <summary>
        /// Messages.  Preallocated to avoid triggering the garbage collector.
        /// </summary>
        private Level.GetCollisionInfoMessage mLevelCollisionMsg;
        private OnTileCollisionMessage mOnTileCollisionMsg;

        /// <summary>
        /// Constructor which also handles the process of loading in the Behaviour
        /// Definition information.
        /// </summary>
        /// <param name="parentGOH">The game object that this behaviour is attached to.</param>
        /// <param name="fileName">The file defining this behaviour.</param>
        public TileCollision(GameObject.GameObject parentGOH, String fileName)
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

            TileCollisionDefinition def = GameObjectManager.pInstance.pContentManager.Load<TileCollisionDefinition>(fileName);

            //mTexture = GameObjectManager.pInstance.pContentManager.Load<Texture2D>(def.mSpriteFileName);
            
            // Start the previous position at the current position.  It will get overwritten in th update anyway.
            mPreviousPos = mParentGOH.pPosition;

            // Preallocate messages to avoid garbage collection.
            mLevelCollisionMsg = new Level.GetCollisionInfoMessage();
            mOnTileCollisionMsg = new OnTileCollisionMessage();

            mLevelCollisionMsg.mDesiredRect_In = new Math.Rectangle();
            mLevelCollisionMsg.mOriginalRect_In = new Math.Rectangle();
        }

        /// <summary>
        /// Called once per frame before the update function. Is called for ALL gameobjects, prior 
        /// to calling Update.
        /// </summary>
        /// <param name="gameTime">The amount of time that has passed this frame.</param>
        public override void PreUpdate(GameTime gameTime)
        {
            // Store the previous position before ay other behaviours have a chance to start moving it.
            mPreviousPos = mParentGOH.pPosition;
        }

        /// <summary>
        /// Called once per frame by the game object.
        /// </summary>
        /// <param name="gameTime">The amount of time that has passed this frame.</param>
        public override void PostUpdate(GameTime gameTime)
        {
            // Copy the current data into the message used for checking collision against the level.
            mLevelCollisionMsg.mDesiredRect_In.Copy(mParentGOH.pCollisionRect);
            mLevelCollisionMsg.mOriginalRect_In.Copy(mParentGOH.pCollisionRect);
            mLevelCollisionMsg.mOriginalRect_In.pCenterPoint = mParentGOH.pPrevPos + mParentGOH.pCollisionRoot;

            // Check for collision against the current level.
            WorldManager.pInstance.pCurrentLevel.OnMessage(mLevelCollisionMsg);

            // Once we detect a collision, we need to repsond to it.
            if (mLevelCollisionMsg.mCollisionDetected_Out)
            {
                //DebugMessageDisplay.pInstance.AddDynamicMessage("Collision Detected");

                // Seperate X and Y collision so that they can react seperatly.
                if (mLevelCollisionMsg.mCollisionDetectedX_Out)
                {
                    //DebugMessageDisplay.pInstance.AddDynamicMessage("Collision Detected X");

                    // If we collided along the x-axis, but the object directly against that collision point.
                    if (mParentGOH.pPosition.X > mPreviousPos.X)
                    {
                        mParentGOH.pPosX = mLevelCollisionMsg.mCollisionPointX_Out - mParentGOH.pCollisionRect.pDimensionsHalved.X - mParentGOH.pCollisionRoot.X;// mPreviousPos.X;
                    }
                    else if (mParentGOH.pPosition.X < mPreviousPos.X)
                    {
                        mParentGOH.pPosX = mLevelCollisionMsg.mCollisionPointX_Out + mParentGOH.pCollisionRect.pDimensionsHalved.X - mParentGOH.pCollisionRoot.X;// mPreviousPos.X;
                    }
                }
                if (mLevelCollisionMsg.mCollisionDetectedY_Out)
                {
                    //DebugMessageDisplay.pInstance.AddDynamicMessage("Collision Detected Y");

                    if (mParentGOH.pPosY > mPreviousPos.Y)
                    {
                        mParentGOH.pPosY = mLevelCollisionMsg.mCollisionPointY_Out - mParentGOH.pCollisionRect.pDimensionsHalved.Y - mParentGOH.pCollisionRoot.Y;// mPreviousPos.X;
                    }
                    else if (mParentGOH.pPosition.Y < mPreviousPos.Y)
                    {
                        mParentGOH.pPosY = mLevelCollisionMsg.mCollisionPointY_Out + mParentGOH.pCollisionRect.pDimensionsHalved.Y - mParentGOH.pCollisionRoot.Y;// mPreviousPos.X;
                    }
                }

                mParentGOH.OnMessage(mOnTileCollisionMsg);
            }
        }
    }
}
