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
            public Boolean mReset;
        }

        /// <summary>
        /// An event that gets sent when not looping animations complete.
        /// </summary>
        public class OnAnimationCompleteMessage : BehaviourMessage
        {
            public String mAnimationSetName;
        }

        /// <summary>
        /// Message used for retriving a named attachment point from a sprite.
        /// </summary>
        public class GetAttachmentPointMessage : BehaviourMessage
        {
            /// <summary>
            /// Set this to the name of the attachment point you wish to get.
            /// </summary>
            public String mName;

            /// <summary>
            /// The attachment position gets stored here.  Note that it is
            /// in world space.
            /// </summary>
            public Vector2 mPoisitionInWorld;
        }

        /// <summary>
        /// Update the color that the sprite is tinted with when rendering.
        /// </summary>
        public class SetColorMessage : BehaviourMessage
        {
            public Color mColor;
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
            /// Does this animation loop?
            /// </summary>
            public Boolean mLooping;

            /// <summary>
            /// Has a non-looping animation completed?
            /// </summary>
            public Boolean mAnimationComplete;
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
        /// A list of position offsets (offset from the motion root) indexed by name.
        /// Used for attaching things to objects without having hard coded values.
        /// </summary>
        private Dictionary<String, Vector2> mAttachmentPoints;

        /// <summary>
        /// Preallocated messages to avoid garbage collection during gameplay.
        /// </summary>
        private OnAnimationCompleteMessage mOnAnimationCompleteMsg;

        /// <summary>
        /// Color used to tint the sprite when rendering.  White means render as it appears
        /// outside the game.
        /// </summary>
        private Color mColor;

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
            mAttachmentPoints = new Dictionary<string, Vector2>();
            if (def.mAttachmentPoints != null)
            {
                for (Int32 i = 0; i < def.mAttachmentPoints.Count; i++)
                {
                    mAttachmentPoints.Add(def.mAttachmentPoints[i].mName, def.mAttachmentPoints[i].mOffset);
                }
            }
            mHasShadow = def.mHasShadow;
            Single colHeight = mTexture.Height;
            if (def.mFrameHeight > 0)
            {
                colHeight = def.mFrameHeight;
            }

            if (def.mAnimationSets != null)
            {
                mIsAnimated     = true;
                mFrameHeight    = def.mFrameHeight;

                mAnimations = new List<AnimationSet>();

                for (int i = 0; i < def.mAnimationSets.Count; i++)
                {
                    AnimationSet temp = new AnimationSet();
                    temp.mNumFrames = def.mAnimationSets[i].mNumFrames;
                    temp.mTicksPerFrame = def.mAnimationSets[i].mTicksPerFrame;
                    temp.mName = def.mAnimationSets[i].mName;
                    temp.mStartingFrame = def.mAnimationSets[i].mStartingFrame;
                    temp.mLooping = def.mAnimationSets[i].mLooping;
                    temp.mAnimationComplete = false;
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
            mActiveAnimation = 0;
            mColor = Color.White;

            mOnAnimationCompleteMsg = new OnAnimationCompleteMessage();
        }

        /// <summary>
        /// Called once per frame by the game object.
        /// </summary>
        /// <param name="gameTime">The amount of time that has passed this frame.</param>
        public override void Update(GameTime gameTime)
        {
            if (mIsAnimated && !mAnimations[mActiveAnimation].mAnimationComplete)
            {
                mFrameCounter += 1;

                if (mFrameCounter > mAnimations[mActiveAnimation].mTicksPerFrame)
                {
                    mCurrentFrame += 1;

                    if (mCurrentFrame >= mAnimations[mActiveAnimation].mNumFrames)
                    {
                        // Handle the case where the animation does not loop.
                        if (!mAnimations[mActiveAnimation].mLooping)
                        {
                            // The current frame was already incremented above, so we need to reverse that so that
                            // we don't render the frame after this animation.
                            mCurrentFrame -= 1;

                            // Avoid the animation getting updates now since there is not point; it will just sit on the 
                            // last frame until it is reset or a new animation is played.
                            mAnimations[mActiveAnimation].mAnimationComplete = true;

                            // Set up and send the animation complete message so that people can react to it.
                            mOnAnimationCompleteMsg.mAnimationSetName = mAnimations[mActiveAnimation].mName;
                            mParentGOH.OnMessage(mOnAnimationCompleteMsg);
                        }
                        else
                        {
                            // In the case of a looping animation we simple go back to the first frame.
                            mCurrentFrame = 0;
                        }
                    }
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
                           mColor,
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
                           mColor,
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
                               new Color(0, 0, 0, 128),
                               mParentGOH.pOrientation.mRotation,
                               mMotionRoot,
                               mParentGOH.pOrientation.mScale,
                               mSpriteEffects | SpriteEffects.FlipVertically,
                               0);
                }
            }

#if ALLOW_GARBAGE
            foreach (KeyValuePair<String, Vector2> pair in mAttachmentPoints)
            {
                //DebugShapeDisplay.pInstance.AddTransform(mParentGOH.pOrientation.mPosition + pair.Value);
                DebugShapeDisplay.pInstance.AddPoint(mParentGOH.pOrientation.mPosition + pair.Value, 1.0f, Color.Purple);
            }
#endif
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

                // If the animation is not currently playing we need to find it.
                if (mAnimations[mActiveAnimation].mName != temp.mAnimationSetName)
                {
                    for (int i = 0; i < mAnimations.Count; i++)
                    {
                        if (mAnimations[i].mName == temp.mAnimationSetName)
                        {
                            mActiveAnimation = i;
                            mCurrentFrame = 0;
                            mAnimations[mActiveAnimation].mAnimationComplete = false;
                        }
                    }
                }
                // In the case where it is a non-looping animation which has completed, we need to reset the 
                // animation to the beginning.
                // If it is a looping animation we don't do anything and assume that they just wanted to continue
                // the animation.
                else if (mAnimations[mActiveAnimation].mAnimationComplete)
                {
                    mCurrentFrame = 0;
                    mAnimations[mActiveAnimation].mAnimationComplete = false;

                }
                
            }
            else if (msg is GetAttachmentPointMessage)
            {
                GetAttachmentPointMessage temp = (GetAttachmentPointMessage)msg;

                if (mAttachmentPoints.ContainsKey(temp.mName))
                {
                    temp.mPoisitionInWorld = mAttachmentPoints[temp.mName] + mParentGOH.pOrientation.mPosition;
                }
            }
            else if (msg is SetColorMessage)
            {
                SetColorMessage temp = (SetColorMessage)msg;
                mColor = temp.mColor;
            }
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
