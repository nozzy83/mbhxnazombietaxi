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
        /// Get the collision boundaries for the current animation.
        /// </summary>
        public class GetCollisionRectangleMessage : BehaviourMessage
        {
            public MBHEngine.Math.Rectangle mBounds;
        }

        public class OnTileCollisionMessage : BehaviourMessage
        {
        }

        /// <summary>
        /// The collision boundary of this animation set.
        /// </summary>
        private MBHEngine.Math.Rectangle mCollisionRectangle;

        /// <summary>
        /// Store the previous position so that in the event of a collision we can go back.
        /// </summary>
        private Vector2 mPreviousPos;

        /// <summary>
        /// Messages.  Preallocated to avoid triggering the garbage collector.
        /// </summary>
        private Level.CheckForCollisionMessage mLevelCollisionMsg;
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

            mCollisionRectangle = new Math.Rectangle(def.mCollisionBoxDimensions);

            // Start the previous position at the current position.  It will get overwritten in th update anyway.
            mPreviousPos = mParentGOH.pOrientation.mPosition;

            // Preallocate messages to avoid garbage collection.
            mLevelCollisionMsg = new Level.CheckForCollisionMessage();
            mOnTileCollisionMsg = new OnTileCollisionMessage();

            mLevelCollisionMsg.mDesiredRect = new Math.Rectangle();
            mLevelCollisionMsg.mOriginalRect = new Math.Rectangle();
        }

        /// <summary>
        /// Called once per frame before the update function. Is called for ALL gameobjects, prior 
        /// to calling Update.
        /// </summary>
        /// <param name="gameTime">The amount of time that has passed this frame.</param>
        public override void PreUpdate(GameTime gameTime)
        {
            // Store the previous position before ay other behaviours have a chance to start moving it.
            mPreviousPos = mParentGOH.pOrientation.mPosition;
        }

        /// <summary>
        /// Called once per frame by the game object.
        /// </summary>
        /// <param name="gameTime">The amount of time that has passed this frame.</param>
        public override void PostUpdate(GameTime gameTime)
        {
            // Copy the current data into the message used for checking collision against the level.
            mLevelCollisionMsg.mDesiredRect.Copy(mCollisionRectangle);
            mLevelCollisionMsg.mDesiredRect.pCenterPoint = mParentGOH.pOrientation.mPosition;
            mLevelCollisionMsg.mOriginalRect.Copy(mCollisionRectangle);

            // Check for collision against the current level.
            WorldManager.pInstance.pCurrentLevel.OnMessage(mLevelCollisionMsg);

            // Once we detect a collision, we need to repsond to it.
            if (mLevelCollisionMsg.mCollisionDetected)
            {
#if ALLOW_GARBAGE
                //DebugMessageDisplay.pInstance.AddDynamicMessage("Collision Detected");
#endif

                // Seperate X and Y collision so that they can react seperatly.
                if (mLevelCollisionMsg.mCollisionDetectedX)
                {
#if ALLOW_GARBAGE
                    //DebugMessageDisplay.pInstance.AddDynamicMessage("Collision Detected X");
#endif

                    // If we collided along the x-axis, but the object directly against that collision point.
                    if (mParentGOH.pOrientation.mPosition.X > mPreviousPos.X)
                    {
                        mParentGOH.pOrientation.mPosition.X = mLevelCollisionMsg.mCollisionPointX - mCollisionRectangle.pDimensionsHalved.X;// mPreviousPos.X;
                    }
                    else if (mParentGOH.pOrientation.mPosition.X < mPreviousPos.X)
                    {
                        mParentGOH.pOrientation.mPosition.X = mLevelCollisionMsg.mCollisionPointX + mCollisionRectangle.pDimensionsHalved.X;// mPreviousPos.X;
                    }
                }
                if (mLevelCollisionMsg.mCollisionDetectedY)
                {
#if ALLOW_GARBAGE
                    //DebugMessageDisplay.pInstance.AddDynamicMessage("Collision Detected Y");
#endif

                    if (mParentGOH.pOrientation.mPosition.Y > mPreviousPos.Y)
                    {
                        mParentGOH.pOrientation.mPosition.Y = mLevelCollisionMsg.mCollisionPointY - mCollisionRectangle.pDimensionsHalved.Y;// mPreviousPos.X;
                    }
                    else if (mParentGOH.pOrientation.mPosition.Y < mPreviousPos.Y)
                    {
                        mParentGOH.pOrientation.mPosition.Y = mLevelCollisionMsg.mCollisionPointY + mCollisionRectangle.pDimensionsHalved.Y;// mPreviousPos.X;
                    }
                }

                mParentGOH.OnMessage(mOnTileCollisionMsg);
            }

            // Update the collision rectangle's position based on the final position of the parent game object.
            mCollisionRectangle.pCenterPoint = mParentGOH.pOrientation.mPosition;

            DebugShapeDisplay.pInstance.AddAABB(mCollisionRectangle, Color.Green);
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
            if (msg is GetCollisionRectangleMessage)
            {
                GetCollisionRectangleMessage temp = (GetCollisionRectangleMessage)msg;
                temp.mBounds = mCollisionRectangle;
                msg = temp;
            }
        }
    }
}
