using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace MBHEngine.Math
{
    /// <summary>
    /// The Rectangle class included with XNA works on Ints, but for things like collision
    /// detection we want to work in floating point percision/
    /// </summary>
    public class Rectangle
    {
        /// <summary>
        /// The width from left to right.
        /// </summary>
        private Single mWidth;

        /// <summary>
        /// The height from top to bottom.
        /// </summary>
        private Single mHeight;

        /// <summary>
        /// The width from center point to the edge.
        /// </summary>
        private Single mHalfWidth;

        /// <summary>
        /// The height from the center point to the edge.
        /// </summary>
        private Single mHalfHeight;

        /// <summary>
        /// The center point of this rectangle.
        /// </summary>
        private Vector2 mCenterPoint;
        
        /// <summary>
        /// Constructor.
        /// </summary>
        public Rectangle()
        {
            mWidth = 0;
            mHeight = 0;
            mHalfWidth = 0;
            mHalfHeight = 0;
            mCenterPoint = new Vector2();
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="width">The width from left edge to right edge.</param>
        /// <param name="height">The height from top edge to bottom edge.</param>
        /// <param name="center">The position of this rectangle.</param>
        public Rectangle(Single width, Single height, Vector2 center = new Vector2())
        {
            pDimensions = new Vector2(width, height);
            pCenterPoint = center;
        }

        /// <summary>
        /// Constrcutor.
        /// </summary>
        /// <param name="dimension">The width and height.</param>
        /// <param name="center">The position of the rectangle.</param>
        public Rectangle(Vector2 dimension, Vector2 center = new Vector2())
        {
            pDimensions = dimension;
            pCenterPoint = center;
        }

        /// <summary>
        /// Checks if a point in space is inside this Rectangle.
        /// </summary>
        /// <param name="other">A point to check against.</param>
        /// <returns>True if the rectangle intersect.</returns>
        public Boolean Intersects(Vector2 other)
        {
            // We will determine intersection by seeing if the distance between the rectangle and point
            // is less than the rectangle's half-widths.
            //

            // Start by getting the distance between the two rectangles.
            Vector2 seperation = mCenterPoint - other;

            // Now get the minimum amont of seperation required to avoid a collision.
            Single safeSeperationX = mHalfWidth;
            Single safeSeperationY = mHalfHeight;

            // Is the distance between the two greater than the minumum safe distance?
            if (System.Math.Abs(seperation.X) >= safeSeperationX)
            {
                return false;
            }

            if (System.Math.Abs(seperation.Y) >= safeSeperationY)
            {
                return false;
            }

            // If we make it to this point we have at least one collision.
            return true;
        }

        /// <summary>
        /// Checks if this Rectangle intersects with another Rectangle.
        /// </summary>
        /// <param name="other">The other rectangle to check against.</param>
        /// <returns>True if the rectangle intersect.</returns>
        public Boolean Intersects(Rectangle other)
        {
            // We will determine intersection by seeing if the distance between the two rectangles
            // is less than the two rectangles half-widths.
            //

            // Start by getting the distance between the two rectangles.
            Vector2 seperation = mCenterPoint - other.pCenterPoint;

            // Now get the minimum amont of seperation required to avoid a collision.
            Single safeSeperationX = mHalfWidth + other.pDimensionsHalved.X;
            Single safeSeperationY = mHalfHeight + other.pDimensionsHalved.Y;

            // Is the distance between the two greater than the minumum safe distance?
            if (System.Math.Abs(seperation.X) >= safeSeperationX)
            {
                return false;
            }

            if (System.Math.Abs(seperation.Y) >= safeSeperationY)
            {
                return false;
            }

            // If we make it to this point we have at least one collision.
            return true;
        }

        /// <summary>
        /// Deep copy of Rectangles.
        /// </summary>
        /// <param name="other">The rectangle to copy.</param>
        public void Copy(Rectangle other)
        {
            pCenterPoint = other.pCenterPoint;
            pDimensions = other.pDimensions;
        }

        /// <summary>
        /// Convert the rectangle to whole numbers.  Rounds all numbers instead of straight truncation.
        /// </summary>
        public void ToWholeNumber()
        {
            pDimensions = new Vector2((Single)System.Math.Round(pDimensions.X), (Single)System.Math.Round(pDimensions.Y));
            pCenterPoint = new Vector2((Single)System.Math.Round(pCenterPoint.X), (Single)System.Math.Round(pCenterPoint.Y));
        }

        /// <summary>
        /// Get a line defining a particular edge of the rectangle.
        /// </summary>
        /// <param name="edge">A preallocated edge (to avoid garbage).</param>
        public void GetTopEdge(ref LineSegment edge)
        {
            edge.pPointA = pTopLeft;
            edge.pPointB = pTopRight;
        }

        /// <summary>
        /// Get a line defining a particular edge of the rectangle.
        /// </summary>
        /// <param name="edge">A preallocated edge (to avoid garbage).</param>
        public void GetRightEdge(ref LineSegment edge)
        {
            edge.pPointA = pTopRight;
            edge.pPointB = pBottomRight;
        }

        /// <summary>
        /// Get a line defining a particular edge of the rectangle.
        /// </summary>
        /// <param name="edge">A preallocated edge (to avoid garbage).</param>
        public void GetBottomEdge(ref LineSegment edge)
        {
            edge.pPointA = pBottomLeft;
            edge.pPointB = pBottomRight;
        }

        /// <summary>
        /// Get a line defining a particular edge of the rectangle.
        /// </summary>
        /// <param name="edge">A preallocated edge (to avoid garbage).</param>
        public void GetLeftEdge(ref LineSegment edge)
        {
            edge.pPointA = pTopLeft;
            edge.pPointB = pBottomLeft;
        }

        /// <summary>
        /// The top of the rectangle in world space.
        /// </summary>
        public Single pTop
        {
            get
            {
                return mCenterPoint.Y - mHalfHeight;
            }
        }

        /// <summary>
        /// The bottom of the rectangle in world space.
        /// </summary>
        public Single pBottom
        {
            get
            {
                return mCenterPoint.Y + mHalfHeight;
            }
        }

        /// <summary>
        /// The left of the rectangle in world space.
        /// </summary>
        public Single pLeft
        {
            get
            {
                return mCenterPoint.X - mHalfWidth;
            }
        }

        /// <summary>
        /// The right of the rectangle in world space.
        /// </summary>
        public Single pRight
        {
            get
            {
                return mCenterPoint.X + mHalfWidth;
            }
        }

        /// <summary>
        /// The point in world space.
        /// </summary>
        public Vector2 pTopLeft
        {
            set
            {
                pCenterPoint = new Vector2(value.X + mHalfWidth, value.Y + mHalfHeight);
            }
            get
            {
                return new Vector2(pLeft, pTop);
            }
        }

        /// <summary>
        /// The point in world space.
        /// </summary>
        public Vector2 pTopRight
        {
            get
            {
                return new Vector2(pRight, pTop);
            }
        }

        /// <summary>
        /// The point in world space.
        /// </summary>
        public Vector2 pBottomRight
        {
            get
            {
                return new Vector2(pRight, pBottom);
            }
        }

        /// <summary>
        /// The point in world space.
        /// </summary>
        public Vector2 pBottomLeft
        {
            get
            {
                return new Vector2(pLeft, pBottom);
            }
        }
        
        /// <summary>
        /// The center of the rectangle.
        /// </summary>
        public Vector2 pCenterPoint
        {
            get
            {
                return mCenterPoint;
            }
            set
            {
                mCenterPoint = value;
            }
        }

        /// <summary>
        /// The point in world space.
        /// </summary>
        public Vector2 pCenterBottom
        {
            get
            {
                return new Vector2(pCenterPoint.X, pBottom);
            }
        }

        /// <summary>
        /// The point in world space.
        /// </summary>
        public Vector2 pCenterTop
        {
            get
            {
                return new Vector2(pCenterPoint.X, pTop);
            }
        }

        /// <summary>
        /// The point in world space.
        /// </summary>
        public Vector2 pCenterLeft
        {
            get
            {
                return new Vector2(pLeft, pCenterPoint.Y);
            }
        }

        /// <summary>
        /// The point in world space.
        /// </summary>
        public Vector2 pCenterRight
        {
            get
            {
                return new Vector2(pRight, pCenterPoint.Y);
            }
        }

        /// <summary>
        /// The width and height of the rectangle.
        /// </summary>
        public Vector2 pDimensions
        {
            get
            {
                return new Vector2(mWidth, mHeight);
            }
            set
            {
                mWidth = value.X;
                mHalfWidth = value.X * 0.5f;
                mHeight = value.Y;
                mHalfHeight = value.Y * 0.5f;
            }
        }

        /// <summary>
        /// The width and height of the rectnagle cut in half.
        /// The distance from the center to the edge.
        /// </summary>
        public Vector2 pDimensionsHalved
        {
            get
            {
                return new Vector2(mHalfWidth, mHalfHeight);
            }
        }
    }
}
