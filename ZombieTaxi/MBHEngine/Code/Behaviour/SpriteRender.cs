using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MBHEngineContentDefs;
using MBHEngine.GameObject;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MBHEngine.Input;

namespace MBHEngine.Behaviour
{
    /// <summary>
    /// Behaviour for rendering simple 2D sprites to the screen.
    /// </summary>
    public class SpriteRender : MBHEngine.Behaviour.Behaviour
    {
        /// <summary>
        /// Retrieves the current sprite effect being applied to this sprite.
        /// </summary>
        public class GetSpriteEffectsMessage : BehaviourMessage
        {
            public SpriteEffects mSpriteEffects;
        };

        /// <summary>
        /// Overrides the current sprite effect being applied to this sprite.
        /// </summary>
        public class SetSpriteEffectsMessage : BehaviourMessage
        {
            public SpriteEffects mSpriteEffects;
        };

        /// <summary>
        /// The texture used to render the sprite.
        /// </summary>
        private Texture2D mTexture;

        /// <summary>
        /// Describes some simple effects that can be applied to the sprite, such as flipping.
        /// </summary>
        private SpriteEffects mSpriteEffects;

        /// <summary>
        /// True if this sprite contains animation.
        /// </summary>
        public Boolean mIsAnimated;

        /// <summary>
        /// The number of frames of animation.
        /// </summary>
        public Int32 mNumFrames;

        /// <summary>
        /// The height, in pixels, of a single frame of animation.
        /// </summary>
        public Int32 mFrameHeight;

        /// <summary>
        /// How many update passes to hold on a single frame of animation.
        /// </summary>
        public Int32 mTicksPerFrame;

        /// <summary>
        /// Used for tracking how many frames have passed since the last animation change.
        /// </summary>
        private Int32 mFrameCounter;

        /// <summary>
        /// The current frame of animation being displayed.
        /// </summary>
        private Int32 mCurrentFrame;

        /// <summary>
        /// Constructor which also handles the process of loading in the Behaviour
        /// Definition information.
        /// </summary>
        /// <param name="parentGOH">The game object that this behaviour is attached to.</param>
        /// <param name="fileName">The file defining this behaviour.</param>
        public SpriteRender(GameObject.GameObject parentGOH, String fileName)
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

            SpriteRenderDefinition def = GameObjectManager.pInstance.pContentManager.Load<SpriteRenderDefinition>(fileName);

            mTexture = GameObjectManager.pInstance.pContentManager.Load<Texture2D>(def.mSpriteFileName);

            if( def.mIsAnimated )
            {
                mIsAnimated     = def.mIsAnimated;
                mNumFrames      = def.mNumFrames;
                mFrameHeight    = def.mFrameHeight;
                mTicksPerFrame  = def.mTicksPerFrame;
            }

            mSpriteEffects = SpriteEffects.None;

            mFrameCounter = 0;
            mCurrentFrame = 0;
        }

        /// <summary>
        /// Called once per frame by the game object.
        /// </summary>
        /// <param name="gameTime">The amount of time that has passed this frame.</param>
        public override void Update(GameTime gameTime)
        {
            if (mIsAnimated)
            {
                mFrameCounter += 1;

                if (mFrameCounter > mTicksPerFrame)
                {
                    mCurrentFrame += 1;
                    if (mCurrentFrame >= mNumFrames) mCurrentFrame = 0;
                    mFrameCounter = 0;
                }
            }
        }

        /// <summary>
        /// Called once render cycle by the game object manager.
        /// </summary>
        /// <param name="batch">The sprite batch to render to.</param>
        public override void Render(SpriteBatch batch)
        {
            if (mIsAnimated)
            {
                Int32 baseIndex = mCurrentFrame * mFrameHeight;
                Rectangle rect = new Rectangle(0, baseIndex, mFrameHeight, mTexture.Width);
                batch.Draw(mTexture,
                           mParentGOH.pOrientation.mPosition,
                           rect,
                           Color.White,
                           mParentGOH.pOrientation.mRotation,
                           new Vector2(mTexture.Width * 0.5f, mFrameHeight * 0.5f),
                           mParentGOH.pOrientation.mScale,
                           mSpriteEffects,
                           0);
            }
            else
            {
                batch.Draw(mTexture,
                           mParentGOH.pOrientation.mPosition,
                           null,
                           Color.White,
                           mParentGOH.pOrientation.mRotation,
                           new Vector2(mTexture.Width * 0.5f, mTexture.Height * 0.5f),
                           mParentGOH.pOrientation.mScale,
                           mSpriteEffects,
                           0);
            }
        }

        /// <summary>
        /// The main interface for communicating between behaviours.  Using polymorphism, we
        /// define a bunch of different messages deriving from BehaviourMessage.  Each behaviour
        /// can then check for particular upcasted messahe types, and either grab some data 
        /// from it (set message) or store some data in it (get message).
        /// </summary>
        /// <param name="msg">The message being communicated to the behaviour.</param>
        /// <returns>The resulting message.  If not null, the message was handled.</returns>
        public override BehaviourMessage OnMessage(BehaviourMessage msg)
        {
            // Which type of message was sent to us?
            if (msg is SpriteRender.GetSpriteEffectsMessage)
            {
                SpriteRender.GetSpriteEffectsMessage temp = (SpriteRender.GetSpriteEffectsMessage)msg;
                temp.mSpriteEffects = mSpriteEffects;
                msg = temp;
            }
            else if (msg is SpriteRender.SetSpriteEffectsMessage)
            {
                SpriteRender.SetSpriteEffectsMessage temp = (SpriteRender.SetSpriteEffectsMessage)msg;
                mSpriteEffects = temp.mSpriteEffects;
            }
            else
            {
                // This is not a message we know how to handle.
                msg = null;
            }

            return msg;
        }
    }
}
