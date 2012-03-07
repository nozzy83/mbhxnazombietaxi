using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MBHEngine.GameObject;
using MBHEngine.Render;
using MBHEngine.Math;

namespace MBHEngine.Debug
{
    /// <summary>
    /// Used to draw shapes to the screen for debug purposes.  The complexity comes from the fact
    /// that this is a 2D engine, but these shapes are displayed using polygons.
    /// </summary>
    public class DebugShapeDisplay
    {
        /// <summary>
        /// Static instance of itself makes this a singleton.
        /// </summary>
        static private DebugShapeDisplay mInstance = null;

        /// <summary>
        /// Preallocated verticies for rendering solid lines.
        /// </summary>
        private VertexPositionColor[] mVertsLines = new VertexPositionColor[1000000];

        /// <summary>
        /// Preallocated verticies used for rendering the semi-transparent fill on some shapes.
        /// </summary>
        private VertexPositionColor[] mVertsFill = new VertexPositionColor[1000000];

        /// <summary>
        /// Keep track of the currently used verticies.
        /// </summary>
        private int mLineCount = 0;
        private int mFillCount = 0;

        /// <summary>
        /// A simple effect needed to render our polys.
        /// </summary>
        private BasicEffect mSimpleColorEffect;

        /// <summary>
        /// Constructor.  Called automatically when the pInstance is accessed the first time.
        /// </summary>
        private DebugShapeDisplay()
        {
            mSimpleColorEffect = new BasicEffect(GameObjectManager.pInstance.pGraphicsDevice);
            mSimpleColorEffect.VertexColorEnabled = true;
        }

        /// <summary>
        /// Draw a complex shape's outline.
        /// </summary>
        /// <param name="vertices">A collection of points defining the shape.</param>
        /// <param name="count">How many verts are in verticies.</param>
        /// <param name="color">The colour of the shape.</param>
        public void AddPolygon(Vector2[] vertices, Int32 count, Color color)
        {
            for (int i = 0; i < count - 1; i ++)
            {
                mVertsLines[mLineCount * 2].Position = new Vector3(vertices[i], 0.0f);
                mVertsLines[mLineCount * 2].Color = color;
                mVertsLines[mLineCount * 2 + 1].Position = new Vector3(vertices[i + 1], 0.0f);
                mVertsLines[mLineCount * 2 + 1].Color = color;
                mLineCount++;
            }

            mVertsLines[mLineCount * 2].Position = new Vector3(vertices[count - 1], 0.0f);
            mVertsLines[mLineCount * 2].Color = color;
            mVertsLines[mLineCount * 2 + 1].Position = new Vector3(vertices[0], 0.0f);
            mVertsLines[mLineCount * 2 + 1].Color = color;
            mLineCount++;
        }

        /// <summary>
        /// Draw a complex shape with a solid fill.
        /// </summary>
        /// <param name="vertices">A collection of points defining the shape.</param>
        /// <param name="count">How many verts are in verticies.</param>
        /// <param name="color">The colour of the shape.</param>
        public void AddSolidPolygon(Vector2[] vertices, Int32 count, Color color)
        {
            AddSolidPolygon(vertices, count, color, true);
        }

        /// <summary>
        /// Internal draw function for polygons.
        /// </summary>
        /// <param name="vertices">A collection of points defining the shape.</param>
        /// <param name="count">How many verts are in verticies.</param>
        /// <param name="color">The colour of the shape.</param>
        /// <param name="outline">Whether or not to render the shape with an ouline.</param>
        private void AddSolidPolygon(Vector2[] vertices, int count, Color color, bool outline)
        {
            if (count == 2)
            {
                AddPolygon(vertices, count, color);
                return;
            }

            Color colorFill = color * (outline ? 0.5f : 1.0f);

            for (int i = 1; i < count - 1; i++)
            {
                mVertsFill[mFillCount * 3].Position = new Vector3(vertices[0], 0.0f);
                mVertsFill[mFillCount * 3].Color = colorFill;

                mVertsFill[mFillCount * 3 + 1].Position = new Vector3(vertices[i], 0.0f);
                mVertsFill[mFillCount * 3 + 1].Color = colorFill;

                mVertsFill[mFillCount * 3 + 2].Position = new Vector3(vertices[i+1], 0.0f);
                mVertsFill[mFillCount * 3 + 2].Color = colorFill;

                mFillCount++;
            }

            if (outline)
            {
                AddPolygon(vertices, count, color);
            }
        }

        /// <summary>
        /// Draws a circle outline.
        /// </summary>
        /// <param name="center">The position of the circle.</param>
        /// <param name="radius">The size of the circle.</param>
        /// <param name="color">Color of the shape.</param>
        public void AddCircle(Vector2 center, float radius, Color color)
        {
            int segments = 16;
            double increment = System.Math.PI * 2.0 / (double)segments;
            double theta = 0.0;

            for (int i = 0; i < segments; i++)
            {
                Vector2 v1 = center + radius * new Vector2((float)System.Math.Cos(theta), (float)System.Math.Sin(theta));
                Vector2 v2 = center + radius * new Vector2((float)System.Math.Cos(theta + increment), (float)System.Math.Sin(theta + increment));

                mVertsLines[mLineCount * 2].Position = new Vector3(v1, 0.0f);
                mVertsLines[mLineCount * 2].Color = color;
                mVertsLines[mLineCount * 2 + 1].Position = new Vector3(v2, 0.0f);
                mVertsLines[mLineCount * 2 + 1].Color = color;
                mLineCount++;

                theta += increment;
            }
        }

        /// <summary>
        /// Draws a circle with a semitransparent solid fil.
        /// </summary>
        /// <param name="center">The position of the shape.</param>
        /// <param name="radius">The size of the shape.</param>
        /// <param name="axis">Normalized vector defining the orientation of the shape.</param>
        /// <param name="color">The colour of the shape.</param>
        public void AddSolidCircle(Vector2 center, float radius, Vector2 axis, Color color)
        {
            int segments = 16;
            double increment = System.Math.PI * 2.0 / (double)segments;
            double theta = 0.0;

            Color colorFill = color * 0.5f;

            Vector2 v0 = center + radius * new Vector2((float)System.Math.Cos(theta), (float)System.Math.Sin(theta));
            theta += increment;

            for (int i = 1; i < segments - 1; i++)
            {
                Vector2 v1 = center + radius * new Vector2((float)System.Math.Cos(theta), (float)System.Math.Sin(theta));
                Vector2 v2 = center + radius * new Vector2((float)System.Math.Cos(theta + increment), (float)System.Math.Sin(theta + increment));

                mVertsFill[mFillCount * 3].Position = new Vector3(v0, 0.0f);
                mVertsFill[mFillCount * 3].Color = colorFill;

                mVertsFill[mFillCount * 3 + 1].Position = new Vector3(v1, 0.0f);
                mVertsFill[mFillCount * 3 + 1].Color = colorFill;

                mVertsFill[mFillCount * 3 + 2].Position = new Vector3(v2, 0.0f);
                mVertsFill[mFillCount * 3 + 2].Color = colorFill;

                mFillCount++;

                theta += increment;
            }
            AddCircle(center, radius, color);

            AddSegment(center, center + axis * radius, color);
        }

        /// <summary>
        /// Draws a line between 2 points.
        /// </summary>
        /// <param name="p1">The starting position.</param>
        /// <param name="p2">The end of point of the line.</param>
        /// <param name="color">The colour of the line.</param>
        public void AddSegment(Vector2 p1, Vector2 p2, Color color)
        {
            mVertsLines[mLineCount * 2].Position = new Vector3(p1, 0.0f);
            mVertsLines[mLineCount * 2 + 1].Position = new Vector3(p2, 0.0f);
            mVertsLines[mLineCount * 2].Color = mVertsLines[mLineCount * 2 + 1].Color = color;
            mLineCount++;
        }

        /// <summary>
        /// Draws a line between 2 points.
        /// </summary>
        /// <param name="line">The line to draw.</param>
        /// <param name="color">The color of the line.</param>
        public void AddSegment(LineSegment line, Color color)
        {
            AddSegment(line.pPointA, line.pPointB, color);
        }

        /// <summary>
        /// Draws a simple cross to represent a transform in the world.
        /// </summary>
        /// <param name="rootPosition">Position of the Object.</param>
        public void AddTransform(Vector2 rootPosition)
        {
            Single length = 2.0f;
            Single offsetX = rootPosition.X + length;
            Single offsetY = rootPosition.Y - length;
            AddSegment(rootPosition, new Vector2(offsetX, rootPosition.Y), Color.Red);
            AddSegment(rootPosition, new Vector2(rootPosition.X, offsetY), Color.Blue);
        }

        /// <summary>
        /// Draws a dot.
        /// </summary>
        /// <param name="p">The center point of the dot.</param>
        /// <param name="size">The radius of the dot.</param>
        /// <param name="color">The color of the dot.</param>
        public void AddPoint(Vector2 p, float size, Color color)
        {
            Vector2[] verts = new Vector2[8];
            float hs = size / 2.0f;
            verts[0] = p + new Vector2(-hs, -hs);
            verts[1] = p + new Vector2( hs, -hs);
            verts[2] = p + new Vector2( hs,  hs);
            verts[3] = p + new Vector2(-hs,  hs);

            AddSolidPolygon(verts, 4, color, true);
        }

        /// <summary>
        /// Draws an axis-align bounding box.
        /// </summary>
        /// <param name="position">Center point of the box.</param>
        /// <param name="width">The full width of the box from far left to far right.</param>
        /// <param name="height">The full height of the box from the bottom to the top.</param>
        /// <param name="color">The color to use when rendering the box.</param>
        public void AddAABB(Vector2 position, Single width, Single height, Color color)
        {
            Single widthHalf = width * 0.5f;
            Single heightHalf = height * 0.5f;
            Single left = position.X - widthHalf;
            Single right = position.X + widthHalf;
            Single top = position.Y - heightHalf;
            Single bottom = position.Y + heightHalf;

            Vector2[] verts = new Vector2[8];
            verts[0] = new Vector2(left, bottom);
            verts[1] = new Vector2(left, top);
            verts[2] = new Vector2(right, top);
            verts[3] = new Vector2(right, bottom);

            AddSolidPolygon(verts, 4, color);
        }

        /// <summary>
        /// Draws an axis-aligned rectangle.
        /// </summary>
        /// <param name="box">Rectangle to render.</param>
        /// <param name="color">The color to render it as.</param>
        public void AddAABB(Math.Rectangle box, Color color)
        {
            Vector2[] verts = new Vector2[8];
            verts[0] = new Vector2(box.pLeft, box.pBottom);
            verts[1] = new Vector2(box.pLeft, box.pTop);
            verts[2] = new Vector2(box.pRight, box.pTop);
            verts[3] = new Vector2(box.pRight, box.pBottom);

            AddSolidPolygon(verts, 4, color);
        }

        /// <summary>
        /// Call this once per frame.
        /// </summary>
        /// <remarks>This needs to be called before anything is added this frame.</remarks>
        public void Update()
        {
            // Reset the used verticies for the next frame.
            mLineCount = mFillCount = 0;
        }

        /// <summary>
        /// Call to render all the currently pending debug shapes.
        /// </summary>
        public void Render()
        {
            GraphicsDevice device = GameObjectManager.pInstance.pGraphicsDevice;

            Single viewLeft = CameraManager.pInstance.pTargetPosition.X - (device.Viewport.Width * 0.5f);
            Single viewRight = CameraManager.pInstance.pTargetPosition.X + (device.Viewport.Width * 0.5f);
            Single viewTop = CameraManager.pInstance.pTargetPosition.Y - (device.Viewport.Height * 0.5f);
            Single viewBottom = CameraManager.pInstance.pTargetPosition.Y + (device.Viewport.Height * 0.5f);

            Matrix projectionMatrix = Matrix.CreateOrthographicOffCenter(
                viewLeft,
                viewRight,
                viewBottom,
                viewTop,
                -1.0f, 10.0f);

            projectionMatrix *= Matrix.CreateScale(new Vector3(CameraManager.pInstance.pZoomScale));

            mSimpleColorEffect.Projection = projectionMatrix;

            mSimpleColorEffect.Techniques[0].Passes[0].Apply();

            device.RasterizerState = RasterizerState.CullNone;
            device.BlendState = BlendState.AlphaBlend;
            
            if (mFillCount > 0)
                device.DrawUserPrimitives<VertexPositionColor>(PrimitiveType.TriangleList, mVertsFill, 0, mFillCount);
            
            if (mLineCount > 0)
                device.DrawUserPrimitives<VertexPositionColor>(PrimitiveType.LineList, mVertsLines, 0, mLineCount);
        }

        /// <summary>
        /// Access to the single instance of the class.
        /// </summary>
        public static DebugShapeDisplay pInstance
        {
            get
            {
                if (mInstance == null)
                {
                    mInstance = new DebugShapeDisplay();
                }

                return mInstance;
            }
        }
    }
}
