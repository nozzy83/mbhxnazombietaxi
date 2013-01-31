using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework;

namespace MBHEngineContentDefs
{
    public class SpriteRenderDefinition : BehaviourDefinition
    {
        /// <summary>
        /// Data that can be overwritten on a frame by frame basis (rather than the data for
        /// the animation as a set).
        /// </summary>
        public struct FrameOverrides
        {
            /// <summary>
            /// How many update passes to hold on a single frame of animation.
            /// </summary>
            public Int32 mTicksPerFrame;
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
            [ContentSerializer(Optional = true)]
            public Boolean mRemoveGameObjectOnComplete;

            /// <summary>
            /// Allows induvidual frames to have settings different from the rest of the animation.
            /// This is just to simplify things, so the client doesn't need to specify data for
            /// every frame since it will be the same 99% of the time.
            /// </summary>
            [ContentSerializer(Optional = true)]
            public Dictionary<Int32, FrameOverrides> mFrameOverrides;
        };

        /// <summary>
        /// A point on the sprite where things can be attached to.
        /// </summary>
        public class AtachmentPoint
        {
            /// <summary>
            /// The name of this attachment point.  Used for looking it up at runtime.
            /// </summary>
            public String mName;

            /// <summary>
            /// The offset from the motion root.
            /// </summary>
            public Vector2 mOffset;

            /// <summary>
            /// True if the attach point should move with the Sprite facing. Meaning, if
            /// the sprite is flipped horizontally, the attachment point will move the the
            /// opposite side of the sprite on the X axis.
            /// </summary>
            public Boolean mMoveWithSpriteFacing;
        };

        /// <summary>
        /// The name of the file which contains the sprite image.
        /// </summary>
        public String mSpriteFileName;

        /// <summary>
        /// The height, in pixels, of a single frame of animation.
        /// </summary>
        [ContentSerializer(Optional = true)]
        public Int32 mFrameHeight;

        /// <summary>
        /// Draw second sprite as shadow.
        /// </summary>
        [ContentSerializer(Optional = true)]
        public Boolean mHasShadow;

        /// <summary>
        /// A list of attachment points, relative to the motion root.
        /// </summary>
        [ContentSerializer(Optional = true)]
        public List<AtachmentPoint> mAttachmentPoints;

        /// <summary>
        /// A list of all the animations contained in this sprite set.
        /// </summary>
        [ContentSerializer(Optional = true)]
        public List<AnimationSet> mAnimationSets;
    }
}
