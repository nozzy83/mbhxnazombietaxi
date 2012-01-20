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
        private Vector2 mTopLeft;
        private Vector2 mBottomRight;

        public Rectangle()
        {
            mTopLeft = new Vector2();
            mBottomRight = new Vector2();
        }

        public Rectangle(Vector2 topLeft, Vector2 bottomRight)
        {
            mTopLeft = topLeft;
            mBottomRight = bottomRight;
        }
    }
}
