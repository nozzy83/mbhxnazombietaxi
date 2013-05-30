using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace MBHEngine.Math
{
    /// <summary>
    /// Anination helper for moving between 2 values over time.
    /// </summary>
    public struct Tween
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="watch">The watch used for tweening over time. Set it up yourself.</param>
        /// <param name="startVal">The low value.</param>
        /// <param name="finalVal">The high value.</param>
        public Tween(StopWatch watch, Single startVal, Single finalVal)
        {
            mWatch = watch;

            mStartValue = startVal;
            mFinalValue = finalVal;

            mCurrentValue = mStartValue;

            mDirection = true;
        }

        /// <summary>
        /// Call this on a regular basis.
        /// </summary>
        public void Update()
        {
            if (mWatch.IsExpired())
            {
                mWatch.Restart();

                mDirection ^= true;
            }

            Single lerpVal = mWatch.pPercentRemaining;

            // Change direction.
            if (mDirection)
            {
                // Lerp goes from 0 - 1 bur percent goes from 1 - 0 so correct that.
                lerpVal = 1.0f - lerpVal;
            }

            mCurrentValue = MathHelper.Lerp(mStartValue, mFinalValue, lerpVal);
        }

        /// <summary>
        /// The watch used for timing the tween over time.
        /// </summary>
        public StopWatch mWatch;

        /// <summary>
        /// The value to start at.
        /// </summary>
        public Single mStartValue;

        /// <summary>
        /// The value we will have at the end of the time.
        /// </summary>
        public Single mFinalValue;

        /// <summary>
        /// Used for tracking the current velocity of the tween when looping.
        /// </summary>
        public Boolean mDirection;

        /// <summary>
        /// A value between mStartValue and mEndValue based on the current time.
        /// </summary>
        public Single mCurrentValue;
    }
}
