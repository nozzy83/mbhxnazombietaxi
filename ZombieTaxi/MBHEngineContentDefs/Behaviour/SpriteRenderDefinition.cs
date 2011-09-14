using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MBHEngineContentDefs
{
    public class SpriteRenderDefinition : BehaviourDefinition
    {
        /// <summary>
        /// The name of the file which contains the sprite image.
        /// </summary>
        public String mSpriteFileName;

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
    }
}
