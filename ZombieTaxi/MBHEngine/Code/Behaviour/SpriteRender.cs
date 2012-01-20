using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MBHEngineContentDefs;
using MBHEngine.GameObject;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MBHEngine.Input;
using MBHEngine.Debug;

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
        /// Retrives the currently playing animation.
        /// </summary>
        public class GetActiveAnimationMessage : BehaviourMessage
        {
            public String mAnimationSetName;
        }

        /// <summary>
        /// Sets the currently playing animation by name.  See XML for this sprite for corisponding names.
        /// </summary>
        public class SetActiveAnimationMessage : BehaviourMessage
        {
            public String mAnimationSetName;
        }

        /// <summary>
        /// Get the collision boundaries for the current animation.
        /// </summary>
        public class GetCollisionRectangleMessage : BehaviourMessage
        {
            public MBHEngine.Math.Rectangle mBounds;
        }

        /// <summary>
        /// Defines a single set of animation.  A sprite sheet will usually contain a number of animation
        /// sets.
        /// </summary>
        public class AnimationSet
        {
            /// <summary>
            /// How many update passes to hold on a single frame of animation.
            /// </summary>
            public Int32 mTicksPerFrame;

            /// <summary>
            /// The name that this animation will be referenced as in code.
            /// </summary>
            public String mName;

            /// <summary>
            /// Which frame of the image does this particular animation start on.
            /// </summary>
            public Int32 mStartingFrame;

            /// <summary>
            /// The number of frames of animation.
            /// </summary>
            public Int32 mNumFrames;

            /// <summary>
            /// The collision boundary of this animation set.
            /// </summary>
            public MBHEngine.Math.Rectangle mCollisionRectangle;
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
        /// The height, in pixels, of a single frame of animation.
        /// </summary>
        public Int32 mFrameHeight;

        /// <summary>
        /// Used for tracking how many frames have passed since the last animation change.
        /// </summary>
        private Int32 mFrameCounter;

        /// <summary>
        /// The current frame of animation being displayed.
        /// </summary>
        private Int32 mCurrentFrame;

        /// <summary>
        /// A list of all the animations this sprite contains.
        /// </summary>
        private List<AnimationSet> mAnimations;

        /// <summary>
        /// Which index in mAnimations is currently playing.
        /// </summary>
        private Int32 mActiveAnimation;

        /// <summary>
        /// Draw second sprite as shadow.
        /// </summary>
        private Boolean mHasShadow;

        /// <summary>
        /// The offset from 0,0 that this sprite should be rendered at.
        /// </summary>
        private Vector2 mMotionRoot;

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
            mMotionRoot = def.mMotionRoot;

            if (def.mAnimationSets != null)
            {
                mIsAnimated     = true;
                mFrameHeight    = def.mFrameHeight;
                mHasShadow      = def.mHasShadow;

                mAnimations = new List<AnimationSet>();

                for (int i = 0; i < def.mAnimationSets.Count; i++)
                {
                    AnimationSet temp = new AnimationSet();
                    temp.mNumFrames = def.mAnimationSets[i].mNumFrames;
                    temp.mTicksPerFrame = def.mAnimationSets[i].mTicksPerFrame;
                    temp.mName = def.mAnimationSets[i].mName;
                    temp.mStartingFrame = def.mAnimationSets[i].mStartingFrame;
                    mAnimations.Add(temp);
                }
            }
            else
            {
                mIsAnimated     = false;
            }

            mSpriteEffects = SpriteEffects.None;

            mFrameCounter = 0;
            mCurrentFrame = 0;
            mActiveAnimation = 1;
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

                if (mFrameCounter > mAnimations[mActiveAnimation].mTicksPerFrame)
                {
                    mCurrentFrame += 1;
                    if (mCurrentFrame >= mAnimations[mActiveAnimation].mNumFrames) mCurrentFrame = 0;
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
                Int32 baseIndex = (mAnimations[mActiveAnimation].mStartingFrame + mCurrentFrame) * mFrameHeight;
                Rectangle rect = new Rectangle(0, baseIndex, mFrameHeight, mTexture.Width);
                batch.Draw(mTexture,
                           mParentGOH.pOrientation.mPosition,
                           rect,
                           Color.White,
                           mParentGOH.pOrientation.mRotation,
                           mMotionRoot,
                           mParentGOH.pOrientation.mScale,
                           mSpriteEffects,
                           0);

                if (mHasShadow)
                {
                    batch.Draw(mTexture,
                               mParentGOH.pOrientation.mPosition + new Vector2(0, mFrameHeight),
                               rect,
                               new Color(0, 0, 0, 128),
                               mParentGOH.pOrientation.mRotation,
                               mMotionRoot,
                               mParentGOH.pOrientation.mScale,
                               mSpriteEffects | SpriteEffects.FlipVertically,
                               0);
                }
            }
            else
            {
                batch.Draw(mTexture,
                           mParentGOH.pOrientation.mPosition,
                           null,
                           Color.White,
                           mParentGOH.pOrientation.mRotation,
                           mMotionRoot,
                           mParentGOH.pOrientation.mScale,
                           mSpriteEffects,
                           0);

                if (mHasShadow)
                {
                    batch.Draw(mTexture,
                               mParentGOH.pOrientation.mPosition + new Vector2(0, mTexture.Height),
                               null,
                               Color.White,
                               mParentGOH.pOrientation.mRotation,
                               mMotionRoot,
                               mParentGOH.pOrientation.mScale,
                               mSpriteEffects,
                               0);
                }
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
            else if (msg is GetActiveAnimationMessage)
            {
                SpriteRender.GetActiveAnimationMessage temp = (SpriteRender.GetActiveAnimationMessage)msg;
                temp.mAnimationSetName = mAnimations[mActiveAnimation].mName;
                msg = temp;
            }
            else if (msg is SetActiveAnimationMessage)
            {
                SpriteRender.SetActiveAnimationMessage temp = (SpriteRender.SetActiveAnimationMessage)msg;

                if (mAnimations[mActiveAnimation].mName != temp.mAnimationSetName)
                {
                    for (int i = 0; i < mAnimations.Count; i++)
                    {
                        if (mAnimations[i].mName == temp.mAnimationSetName)
                        {
                            mActiveAnimation = i;
                            mCurrentFrame = 0;
                        }
                    }
                }
            }
            else if (msg is GetCollisionRectangleMessage)
            {
                GetCollisionRectangleMessage temp = (GetCollisionRectangleMessage)msg;
                temp.mBounds = mAnimations[mActiveAnimation].mCollisionRectangle;
                msg = temp;
            }
            else
            {
                // This is not a message we know how to handle.
                // TODO:
                // This seems wrong.  Won't this overwrite any set messages that might need to be passed on to other
                // behaviours?
                msg = null;
            }

            return msg;
        }

        /// <summary>
        /// The currently playing animation.  See XML for this sprite for corisponding names.
        /// </summary>
        public String pCurrentAnimation
        {
            get
            {
                return mAnimations[mActiveAnimation].mName;
            }
            set
            {
                for (int i = 0; i < mAnimations.Count; i++)
                {
                    if (mAnimations[i].mName == value)
                    {
                        mActiveAnimation = i;
                    }
                }
            }
        }
    }
}
