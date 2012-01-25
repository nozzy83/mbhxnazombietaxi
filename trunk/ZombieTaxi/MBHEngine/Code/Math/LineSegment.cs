using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace MBHEngine.Math
{
    /// <summary>
    /// Basic line class.
    /// </summary>
    public class LineSegment
    {
        /// <summary>
        /// One end point on the line.
        /// </summary>
        private Vector2 mPointA;

        /// <summary>
        /// The other end point on the line.
        /// </summary>
        private Vector2 mPointB;

        /// <summary>
        /// Constructor.
        /// </summary>
        public LineSegment()
        {
            mPointA = new Vector2();
            mPointB = new Vector2();
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="pA">Point A</param>
        /// <param name="pB">Point B</param>
        public LineSegment(Vector2 pA, Vector2 pB)
        {
            mPointA = pA;
            mPointB = pB;
        }

        /// <summary>
        /// Checks if two lines intersect, and if they do, gives the point at which they intersect.
        /// </summary>
        /// <param name="lineB">The line to check against.</param>
        /// <param name="outIntersectPoint">The point on the line where they intersect (if they do).</param>
        /// <returns>True if the lines intersect.</returns>
        /// <remarks>
        /// See: http://paulbourke.net/geometry/lineline2d/Helpers.cs 
        /// </remarks>
        public Boolean Intersects(LineSegment lineB, ref Vector2 outIntersectPoint)
        {
            // Denominator for ua and ub are the same, so store this calculation
            Single d =
               (lineB.mPointB.Y - lineB.mPointA.Y) * (mPointB.X - mPointA.X)
               -
               (lineB.mPointB.X - lineB.mPointA.X) * (mPointB.Y - mPointA.Y);

            //n_a and n_b are calculated as seperate values for readability
            Single n_a =
               (lineB.mPointB.X - lineB.mPointA.X) * (mPointA.Y - lineB.mPointA.Y)
               -
               (lineB.mPointB.Y - lineB.mPointA.Y) * (mPointA.X - lineB.mPointA.X);

            Single n_b =
               (mPointB.X - mPointA.X) * (mPointA.Y - lineB.mPointA.Y)
               -
               (mPointB.Y - mPointA.Y) * (mPointA.X - lineB.mPointA.X);

            // Make sure there is not a division by zero - this also indicates that
            // the lines are parallel.  
            // If n_a and n_b were both equal to zero the lines would be on top of each 
            // other (coincidental).  This check is not done because it is not 
            // necessary for this implementation (the parallel check accounts for this).
            if (d == 0)
                return false;

            // Calculate the intermediate fractional point that the lines potentially intersect.
            Single ua = n_a / d;
            Single ub = n_b / d;

            // The fractional point will be between 0 and 1 inclusive if the lines
            // intersect.  If the fractional calculation is larger than 1 or smaller
            // than 0 the lines would need to be longer to intersect.
            if (ua >= 0d && ua <= 1d && ub >= 0d && ub <= 1d)
            {
                outIntersectPoint.X = mPointA.X + (ua * (mPointB.X - mPointA.X));
                outIntersectPoint.Y = mPointA.Y + (ua * (mPointB.Y - mPointA.Y));
                return true;
            }
            return false;
        }

        /// <summary>
        /// One end of the point.
        /// </summary>
        public Vector2 pPointA
        {
            get
            {
                return mPointA;
            }
            set
            {
                mPointA = value;
            }
        }

        /// <summary>
        /// One end of the point.
        /// </summary>
        public Vector2 pPointB
        {
            get
            {
                return mPointB;
            }
            set
            {
                mPointB = value;
            }
        }
    }
}
