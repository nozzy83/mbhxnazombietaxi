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
            /// <summary>
            /// The currently set sprite effect.
            /// </summary>
            public SpriteEffects mSpriteEffects_Out;

            /// <summary>
            /// Call this to put a message back to its default state.
            /// </summary>
            public override void Reset()
            {
                mSpriteEffects_Out = SpriteEffects.None;
            }
        };

        /// <summary>
        /// Overrides the current sprite effect being applied to this sprite.
        /// </summary>
        public class SetSpriteEffectsMessage : BehaviourMessage
        {
            /// <summary>
            /// The desired sprite effect.
            /// </summary>
            public SpriteEffects mSpriteEffects_In;

            /// <summary>
            /// Call this to put a message back to its default state.
            /// </summary>
            public override void Reset()
            {
                mSpriteEffects_In = SpriteEffects.None;
            }
        };

        /// <summary>
        /// Retrives the currently playing animation.
        /// </summary>
        public class GetActiveAnimationMessage : BehaviourMessage
        {
            /// <summary>
            /// The name of the animation currently playing.
            /// </summary>
            public String mAnimationSetName_Out;

            /// <summary>
            /// Call this to put a message back to its default state.
            /// </summary>
            public override void Reset()
            {
                mAnimationSetName_Out = String.Empty;
            }
        }

        /// <summary>
        /// Sets the currently playing animation by name.  See XML for this sprite for corisponding names.
        /// </summary>
        public class SetActiveAnimationMessage : BehaviourMessage
        {
            /// <summary>
            /// The animation to start playing.
            /// </summary>
            public String mAnimationSetName_In;

            /// <summary>
            /// Call this to put a message back to its default state.
            /// </summary>
            public override void Reset()
            {
                mAnimationSetName_In = String.Empty;
            }
        }

        /// <summary>
        /// An event that gets sent when not looping animations complete.
        /// </summary>
        public class OnAnimationCompleteMessage : BehaviourMessage
        {
            /// <summary>
            /// The name of the animation that completed.
            /// </summary>
            public String mAnimationSetName_In;

            /// <summary>
            /// Call this to put a message back to its default state.
            /// </summary>
            public override void Reset()
            {
                mAnimationSetName_In = String.Empty;
            }
        }

        /// <summary>
        /// Message used for retriving a named attachment point from a sprite.
        /// </summary>
        public class GetAttachmentPointMessage : BehaviourMessage
        {
            /// <summary>
            /// Set this to the name of the attachment point you wish to get.
            /// </summary>
            public String mName_In;

            /// <summary>
            /// The attachment position gets stored here.  Note that it is
            /// in world space.
            /// </summary>
            public Vector2 mPoisitionInWorld_Out;

            /// <summary>
            /// Call this to put a message back to its default state.
            /// </summary>
            public override void Reset()
            {
                mName_In = String.Empty;
                mPoisitionInWorld_Out = Vector2.Zero;
            }
        }

        /// <summary>
        /// Update the color that the sprite is multiplied with when rendering.
        /// </summary>
        public class SetColorMessage : BehaviourMessage
        {
            /// <summary>
            /// The color to change to.
            /// </summary>
            public Color mColor_In;

            /// <summary>
            /// Call this to put a message back to its default state.
            /// </summary>
            public override void Reset()
            {
                mColor_In = Color.Black;
            }
        }

        /// <summary>
        /// Update the color that the sprite it tinted with when rendering.
        /// </summary>
        public class SetTintMessage : BehaviourMessage
        {
            /// <summary>
            /// The color to tint the sprite with.
            /// </summary>
            public Color mColor_In;

            /// <summary>
            /// Call this to put a message back to its default state.
            /// </summary>
            public override void Reset()
            {
                mColor_In = Color.Black;
            }
        }

        /// <summary>
        /// Retrieve a reference to the Texture2D used to render this Sprite.
        /// </summary>
        public class GetTexture2DMessage : BehaviourMessage
        {
            /// <summary>
            /// The texture used to render this Sprite.
            /// </summary>
            public Texture2D mTexture_Out;

            /// <summary>
            /// Call this to put a message back to its default state.
            /// </summary>
            public override void Reset()
            {
                mTexture_Out = null;
            }
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
            /// When this animation completes, should the parent GameObject be removed from the GameObjectManager.
            /// This is useful for things like death animations.
            /// </summary>
            public Boolean mRemoveGameObjectOnComplete;

            /// <summary>
            /// Has a non-looping animation completed?
            /// </summary>
            public Boolean mAnimationComplete;

            /// <summary>
            /// Allows induvidual frames to have settings different from the rest of the animation.
            /// This is just to simplify things, so the client doesn't need to specify data for
            /// every frame since it will be the same 99% of the time.
            /// </summary>
            public Dictionary<Int32, SpriteRenderDefinition.FrameOverrides> mFrameOverrides;

            /// <summary>
            /// Reset the message to it's initial state.
            /// </summary>
            public void Reset()
            {
                mAnimationComplete = false;
            }
        };

        /// <summary>
        /// The texture used to render the sprite.
        /// </summary>
        private Texture2D mTexture;

        /// <summary>
        /// The name of the texture file used by this sprite.
        /// </summary>
        private String mSpriteFileName;

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
        /// The current frame of animation being displayed. This is relative to the current 
        /// Animation Set (mActiveAnimation).
        /// </summary>
        private Int32 mCurrentAnimationFrame;

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
        /// A list of position offsets (offset from the motion root) indexed by name.
        /// Used for attaching things to objects without having hard coded values.
        /// </summary>
        private Dictionary<String, SpriteRenderDefinition.AtachmentPoint> mAttachmentPoints;

        /// <summary>
        /// Preallocated messages to avoid garbage collection during gameplay.
        /// </summary>
        private OnAnimationCompleteMessage mOnAnimationCompleteMsg;

        /// <summary>
        /// Color multiplied by the color of the texture.  White means render as it appears
        /// outside the game.
        /// </summary>
        private Color mColor;

        /// <summary>
        /// Tints the sprite a certain color. Different than mColor in that this is additive, so
        /// you can actually add and remove color. The alpha channel controls strength.
        /// </summary>
        private Color mTint;

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

            mSpriteFileName = def.mSpriteFileName;
            mTexture = GameObjectManager.pInstance.pContentManager.Load<Texture2D>(mSpriteFileName);
            mAttachmentPoints = new Dictionary<string, SpriteRenderDefinition.AtachmentPoint>();
            if (def.mAttachmentPoints != null)
            {
                for (Int32 i = 0; i < def.mAttachmentPoints.Count; i++)
                {
                    mAttachmentPoints.Add(def.mAttachmentPoints[i].mName, def.mAttachmentPoints[i]);
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
                mIsAnimated = true;

                if (def.mFrameHeight == 0)
                {
                    System.Diagnostics.Debug.Assert(false, "Sprite has animations but a frame height of 0.  FrameHeight must be > 0.");

                    def.mFrameHeight = 1;
                }

                mFrameHeight = def.mFrameHeight;

                mAnimations = new List<AnimationSet>();

                for (int i = 0; i < def.mAnimationSets.Count; i++)
                {
                    AnimationSet temp = new AnimationSet();
                    temp.Reset();
                    temp.mNumFrames = def.mAnimationSets[i].mNumFrames;
                    temp.mTicksPerFrame = def.mAnimationSets[i].mTicksPerFrame;
                    temp.mName = def.mAnimationSets[i].mName;
                    temp.mStartingFrame = def.mAnimationSets[i].mStartingFrame;
                    temp.mLooping = def.mAnimationSets[i].mLooping;
                    temp.mRemoveGameObjectOnComplete = def.mAnimationSets[i].mRemoveGameObjectOnComplete;
                    temp.mAnimationComplete = false;
                    temp.mFrameOverrides = def.mAnimationSets[i].mFrameOverrides;
                    mAnimations.Add(temp);
                }
            }
            else
            {
                mIsAnimated     = false;
            }

            Reset();

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

                // By default use the animation set frame data, but there is a chance this was overwritten
                // on a per frame basis.
                Int32 framesToCheck = mAnimations[mActiveAnimation].mTicksPerFrame;

                // Is this frame special? Did it supply some specific timing?
                if (mAnimations[mActiveAnimation].mFrameOverrides != null &&
                    mAnimations[mActiveAnimation].mFrameOverrides.ContainsKey(mCurrentAnimationFrame))
                {
                    framesToCheck = mAnimations[mActiveAnimation].mFrameOverrides[mCurrentAnimationFrame].mTicksPerFrame;
                }

                // Is it time to advance to the next frame of animation.
                if (mFrameCounter > framesToCheck)
                {
                    mCurrentAnimationFrame += 1;

                    if (mCurrentAnimationFrame >= mAnimations[mActiveAnimation].mNumFrames)
                    {
                        // Handle the case where the animation does not loop.
                        if (!mAnimations[mActiveAnimation].mLooping)
                        {
                            // The current frame was already incremented above, so we need to reverse that so that
                            // we don't render the frame after this animation.
                            mCurrentAnimationFrame -= 1;

                            // Avoid the animation getting updates now since there is not point; it will just sit on the 
                            // last frame until it is reset or a new animation is played.
                            mAnimations[mActiveAnimation].mAnimationComplete = true;

                            // Set up and send the animation complete message so that people can react to it.
                            mOnAnimationCompleteMsg.mAnimationSetName_In = mAnimations[mActiveAnimation].mName;
                            mParentGOH.OnMessage(mOnAnimationCompleteMsg);
                        }
                        else
                        {
                            // In the case of a looping animation we simple go back to the first frame.
                            mCurrentAnimationFrame = 0;
                        }

                        if (mAnimations[mActiveAnimation].mRemoveGameObjectOnComplete)
                        {
                            GameObjectManager.pInstance.Remove(mParentGOH);
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
        /// <param name="effect">The currently set shader.</param>
        public override void Render(SpriteBatch batch, Effect effect)
        {
            effect.Parameters["tint"].SetValue(mTint.ToVector4());

            if (mIsAnimated)
            {
                Int32 baseIndex = (mAnimations[mActiveAnimation].mStartingFrame + mCurrentAnimationFrame) * mFrameHeight;
                Rectangle rect = new Rectangle(0, baseIndex, mTexture.Width, mFrameHeight);
                batch.Draw(mTexture,
                           mParentGOH.pPosition,
                           rect,
                           mColor,
                           mParentGOH.pRotation,
                           mParentGOH.pMotionRoot,
                           mParentGOH.pScale,
                           mSpriteEffects,
                           0);

                if (mHasShadow)
                {
                    // By default, just flip from the motion root.  This works when we want the 
                    // shadow to line up with the button of the animation frame.  
                    Vector2 shadowAttachmentPoint = new Vector2(0, (mFrameHeight - mParentGOH.pMotionRoot.Y) * 2.0f);

                    // Having the shadow line up to the buttom of the frame is great most of
                    // the time, but for cases where we want the shadow slightly embedded into the 
                    // frame, or spaced out, we allow the user to override the anchor point with a 
                    // special attachment point called "shadow".
                    if (mAttachmentPoints.ContainsKey("Shadow"))
                    {
                        // Note: The attachment point is stored as an offset from the motion root, 
                        //       it gets added to the achor which is currently storing the motion 
                        //       root.
                        shadowAttachmentPoint += mAttachmentPoints["Shadow"].mOffset;
                    }

                    batch.Draw(mTexture,
                               mParentGOH.pPosition + shadowAttachmentPoint,
                               rect,
                               new Color(0, 0, 0, 128),
                               -mParentGOH.pRotation + MathHelper.ToRadians(180),
                               mParentGOH.pMotionRoot,
                               mParentGOH.pScale,
                               mSpriteEffects ^ SpriteEffects.FlipHorizontally,
                               0);
                }
            }
            else
            {
                batch.Draw(mTexture,
                           mParentGOH.pPosition,
                           null,
                           mColor,
                           mParentGOH.pRotation,
                           mParentGOH.pMotionRoot,
                           mParentGOH.pScale,
                           mSpriteEffects,
                           0);

                if (mHasShadow)
                {
                    // By default, just flip from the motion root.  This works when we want the 
                    // shadow to line up with the button of the animation frame.  
                    Vector2 shadowAttachmentPoint = new Vector2(0, (mTexture.Height - mParentGOH.pMotionRoot.Y) * 2.0f);

                    // Having the shadow line up to the buttom of the frame is great most of
                    // the time, but for cases where we want the shadow slightly embedded into the 
                    // frame, or spaced out, we allow the user to override the anchor point with a 
                    // special attachment point called "shadow".
                    if (mAttachmentPoints.ContainsKey("Shadow"))
                    {
                        // Note: The attachment point is stored as an offset from the motion root, 
                        //       it gets added to the achor which is currently storing the motion 
                        //       root.
                        shadowAttachmentPoint += mAttachmentPoints["Shadow"].mOffset;
                    }

                    batch.Draw(mTexture,
                               mParentGOH.pPosition + shadowAttachmentPoint,
                               null,
                               new Color(0, 0, 0, 128),
                               -mParentGOH.pRotation + MathHelper.ToRadians(180),
                               mParentGOH.pMotionRoot,
                               mParentGOH.pScale,
                               mSpriteEffects ^ SpriteEffects.FlipHorizontally,
                               0);
                }
            }

#if ALLOW_GARBAGE
            Vector2 pos = new Vector2();
            foreach (KeyValuePair<String, SpriteRenderDefinition.AtachmentPoint> pair in mAttachmentPoints)
            {
                FindAttachmentPointInWorldSpace(pair.Key, ref pos);
                //DebugShapeDisplay.pInstance.AddTransform(mParentGOH.pPosition + pair.Value);
                DebugShapeDisplay.pInstance.AddPoint(pos, 1.0f, Color.Purple);
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
                temp.mSpriteEffects_Out = mSpriteEffects;
                msg = temp;
            }
            else if (msg is SpriteRender.SetSpriteEffectsMessage)
            {
                SpriteRender.SetSpriteEffectsMessage temp = (SpriteRender.SetSpriteEffectsMessage)msg;
                mSpriteEffects = temp.mSpriteEffects_In;
            }
            else if (msg is GetActiveAnimationMessage)
            {
                SpriteRender.GetActiveAnimationMessage temp = (SpriteRender.GetActiveAnimationMessage)msg;
                temp.mAnimationSetName_Out = mAnimations[mActiveAnimation].mName;
                msg = temp;
            }
            else if (msg is SetActiveAnimationMessage)
            {
                SpriteRender.SetActiveAnimationMessage temp = (SpriteRender.SetActiveAnimationMessage)msg;

                // If the animation is not currently playing we need to find it.
                if (mAnimations[mActiveAnimation].mName != temp.mAnimationSetName_In)
                {
                    Boolean animationFound = false;

                    for (int i = 0; i < mAnimations.Count; i++)
                    {
                        if (mAnimations[i].mName == temp.mAnimationSetName_In)
                        {
                            mActiveAnimation = i;
                            mCurrentAnimationFrame = 0;
                            mAnimations[mActiveAnimation].mAnimationComplete = false;
                            animationFound = true;
                            break;
                        }
                    }

                    System.Diagnostics.Debug.Assert(animationFound, "Attempting to set unknown Animation: " + temp.mAnimationSetName_In);
                }
                // In the case where it is a non-looping animation which has completed, we need to reset the 
                // animation to the beginning.
                // If it is a looping animation we don't do anything and assume that they just wanted to continue
                // the animation.
                else if (mAnimations[mActiveAnimation].mAnimationComplete)
                {
                    mCurrentAnimationFrame = 0;
                    mAnimations[mActiveAnimation].mAnimationComplete = false;

                }
            }
            else if (msg is GetAttachmentPointMessage)
            {
                GetAttachmentPointMessage temp = (GetAttachmentPointMessage)msg;

                FindAttachmentPointInWorldSpace(temp.mName_In, ref temp.mPoisitionInWorld_Out);
            }
            else if (msg is SetColorMessage)
            {
                SetColorMessage temp = (SetColorMessage)msg;
                mColor = temp.mColor_In;
            }
            else if (msg is SetTintMessage)
            {
                SetTintMessage temp = (SetTintMessage)msg;
                mTint = temp.mColor_In;
            }
            else if (msg is GetTexture2DMessage)
            {
                GetTexture2DMessage temp = (GetTexture2DMessage)msg;
                temp.mTexture_Out = mTexture;
            }
        }

        /// <summary>
        /// Finds a named attachment point, and returns that point in world space. Accounts for
        /// the position of game object, as well as potentially offseting to match a flipped 
        /// Sprite (if requested through mMoveWithSpriteFacing flag).
        /// </summary>
        /// <param name="pointName">The name of the attachment point.</param>
        /// <returns>Position of attachment point in world space.</returns>
        private void FindAttachmentPointInWorldSpace(String pointName, ref Vector2 pos)
        {
            if (mAttachmentPoints.ContainsKey(pointName))
            {
                pos = mAttachmentPoints[pointName].mOffset;

                // If requested, adjust the attachment point position based on the current facing of 
                // the Sprite.
                if (mAttachmentPoints[pointName].mMoveWithSpriteFacing)
                {
                    if ((mSpriteEffects & SpriteEffects.FlipHorizontally) == SpriteEffects.FlipHorizontally)
                    {
                        pos.X *= -1;
                    }

                    if ((mSpriteEffects & SpriteEffects.FlipVertically) == SpriteEffects.FlipVertically)
                    {
                        pos.Y *= -1;
                    }
                }

                pos += mParentGOH.pPosition;
            }
        }

        /// <summary>
        /// Reset the Behaviour to a default state so that it can be reused.
        /// </summary>
        public override void Reset()
        {
            mSpriteEffects = SpriteEffects.None;

            mFrameCounter = 0;
            mCurrentAnimationFrame = 0;
            mActiveAnimation = 0;
            mColor = Color.White;
            mTint = new Color(1.0f, 1.0f, 1.0f, 0.0f);

            // If this Sprite is animated, reset the animation data, like frame counters.
            for (int i = 0; null != mAnimations && i < mAnimations.Count; i++)
            {
                mAnimations[i].Reset();
            }
        }

#if ALLOW_GARBAGE
        /// <summary>
        /// Returns a bunch of information about the behaviour which can be dumped to
        /// a debug display for debugging at runtime.
        /// </summary>
        /// <returns>A formatted string of debug information.</returns>
        public override String [] GetDebugInfo()
        {
            String [] temp = new String[2];

            temp[0] = "File: " + mSpriteFileName;
            temp[1] = "Animated: " + mIsAnimated;
            return temp;
        }
#endif // ALLOW_GARBAGE

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
