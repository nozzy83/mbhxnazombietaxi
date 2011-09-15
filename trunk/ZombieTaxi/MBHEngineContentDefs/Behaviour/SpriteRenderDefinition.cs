using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Content;

namespace MBHEngineContentDefs
{
    public class SpriteRenderDefinition : BehaviourDefinition
    {
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
        /// A list of all the animations contained in this sprite set.
        /// </summary>
        [ContentSerializer(Optional = true)]
        public List<AnimationSet> mAnimationSets;
    }
}
