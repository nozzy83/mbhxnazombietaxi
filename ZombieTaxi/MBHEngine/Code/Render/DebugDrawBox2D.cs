/*
* Box2D.XNA port of Box2D:
* Copyright (c) 2009 Brandon Furtwangler, Nathan Furtwangler
*
* Original source Box2D:
* Copyright (c) 2006-2009 Erin Catto http://www.gphysics.com 
* 
* This software is provided 'as-is', without any express or implied 
* warranty.  In no event will the authors be held liable for any damages 
* arising from the use of this software. 
* Permission is granted to anyone to use this software for any purpose, 
* including commercial applications, and to alter it and redistribute it 
* freely, subject to the following restrictions: 
* 1. The origin of this software must not be misrepresented; you must not 
* claim that you wrote the original software. If you use this software 
* in a product, an acknowledgment in the product documentation would be 
* appreciated but is not required. 
* 2. Altered source versions must be plainly marked as such, and must not be 
* misrepresented as being the original software. 
* 3. This notice may not be removed or altered from any source distribution. 
*/

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Box2D.XNA;
using MBHEngine.Math;

namespace MBHEngine.Render
{
    public class DebugDrawBox2D : Box2D.XNA.DebugDraw
    {
        /// <summary>
        /// Used for rendering polygons using a sprite batch instead of the traditional 
        /// vertex buffers.
        /// </summary>
        private DebugSpritePoly mPolyRender;

        /// <summary>
        /// This is where the polys will be rendered to when using the poly sprite debug renderer.
        /// </summary>
        public SpriteBatch mBatch;

        GraphicsDevice mDevice;

        public DebugDrawBox2D(GraphicsDevice device, SpriteBatch batch)
        {
            _stringData = new List<StringData>();

            mBatch = batch;
            mPolyRender = new DebugSpritePoly(device);
            mDevice = device;
        }

        public override void DrawPolygon(ref FixedArray8<Vector2> vertices, int count, Color color)
        {
            for (int i = 0; i < count - 1; i ++)
            {
                _vertsLines[_lineCount * 2].Position = new Vector3(PhysicsManager.pInstance.PhysicalWorldToScreen(vertices[i]), 0.0f);
                _vertsLines[_lineCount * 2].Color = color;
                _vertsLines[_lineCount * 2 + 1].Position = new Vector3(PhysicsManager.pInstance.PhysicalWorldToScreen(vertices[i + 1]), 0.0f);
                _vertsLines[_lineCount * 2 + 1].Color = color;
                _lineCount++;
            }

            _vertsLines[_lineCount * 2].Position = new Vector3(PhysicsManager.pInstance.PhysicalWorldToScreen(vertices[count - 1]), 0.0f);
            _vertsLines[_lineCount * 2].Color = color;
            _vertsLines[_lineCount * 2 + 1].Position = new Vector3(PhysicsManager.pInstance.PhysicalWorldToScreen(vertices[0]), 0.0f);
            _vertsLines[_lineCount * 2 + 1].Color = color;
            _lineCount++;
        }

        public override void DrawSolidPolygon(ref FixedArray8<Vector2> vertices, int count, Color color)
        {
            DrawSolidPolygon(ref vertices, count, color, true);
        }

        private void DrawSolidPolygon(ref FixedArray8<Vector2> vertices, int count, Color color, bool outline)
        {
            if (count == 2)
            {
                DrawPolygon(ref vertices, count, color);
                return;
            }

            Color colorFill = color * (outline ? 0.5f : 1.0f);

            for (int i = 1; i < count - 1; i++)
            {
                _vertsFill[_fillCount * 3].Position = new Vector3(PhysicsManager.pInstance.PhysicalWorldToScreen(vertices[0]), 0.0f);
                _vertsFill[_fillCount * 3].Color = colorFill;

                _vertsFill[_fillCount * 3 + 1].Position = new Vector3(PhysicsManager.pInstance.PhysicalWorldToScreen(vertices[i]), 0.0f);
                _vertsFill[_fillCount * 3 + 1].Color = colorFill;

                _vertsFill[_fillCount * 3 + 2].Position = new Vector3(PhysicsManager.pInstance.PhysicalWorldToScreen(vertices[i+1]), 0.0f);
                _vertsFill[_fillCount * 3 + 2].Color = colorFill;

                _fillCount++;
            }

            if (outline)
            {
                DrawPolygon(ref vertices, count, color);
            }
        }

        public override void DrawCircle(Vector2 center, float radius, Color color)
        {
            int segments = 16;
            double increment = System.Math.PI * 2.0 / (double)segments;
            double theta = 0.0;

            for (int i = 0; i < segments; i++)
            {
                Vector2 v1 = center + radius * new Vector2((float)System.Math.Cos(theta), (float)System.Math.Sin(theta));
                Vector2 v2 = center + radius * new Vector2((float)System.Math.Cos(theta + increment), (float)System.Math.Sin(theta + increment));

                _vertsLines[_lineCount * 2].Position = PhysicsManager.pInstance.PhysicalWorldToScreen(new Vector3(v1, 0.0f));
                _vertsLines[_lineCount * 2].Color = color;
                _vertsLines[_lineCount * 2 + 1].Position = PhysicsManager.pInstance.PhysicalWorldToScreen(new Vector3(v2, 0.0f));
                _vertsLines[_lineCount * 2 + 1].Color = color;
                _lineCount++;

                theta += increment;
            }
        }

        public override void DrawSolidCircle(Vector2 center, float radius, Vector2 axis, Color color)
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

                _vertsFill[_fillCount * 3].Position = PhysicsManager.pInstance.PhysicalWorldToScreen(new Vector3(v0, 0.0f));
                _vertsFill[_fillCount * 3].Color = colorFill;

                _vertsFill[_fillCount * 3 + 1].Position = PhysicsManager.pInstance.PhysicalWorldToScreen(new Vector3(v1, 0.0f));
                _vertsFill[_fillCount * 3 + 1].Color = colorFill;

                _vertsFill[_fillCount * 3 + 2].Position = PhysicsManager.pInstance.PhysicalWorldToScreen(new Vector3(v2, 0.0f));
                _vertsFill[_fillCount * 3 + 2].Color = colorFill;

                _fillCount++;

                theta += increment;
            }
            DrawCircle(center, radius, color);

            DrawSegment(center, center + axis * radius, color);
        }

        public override void DrawSegment(Vector2 p1, Vector2 p2, Color color)
        {
            _vertsLines[_lineCount * 2].Position = PhysicsManager.pInstance.PhysicalWorldToScreen(new Vector3(p1, 0.0f));
            _vertsLines[_lineCount * 2 + 1].Position = PhysicsManager.pInstance.PhysicalWorldToScreen(new Vector3(p2, 0.0f));
            _vertsLines[_lineCount * 2].Color = _vertsLines[_lineCount * 2 + 1].Color = color;
            _lineCount++;
        }

        public override void DrawTransform(ref Transform xf)
        {
            float axisScale = 0.4f;
            Vector2 p1 = xf.Position;
            
            Vector2 p2 = p1 + axisScale * xf.R.col1;
            DrawSegment(p1, p2, Color.Red);

            p2 = p1 + axisScale * xf.R.col2;
            DrawSegment(p1, p2, Color.Green);
        }

        public void DrawPoint(Vector2 p, float size, Color color)
        {
            FixedArray8<Vector2> verts = new FixedArray8<Vector2>();
            float hs = size / 2.0f;
            verts[0] = p + new Vector2(-hs, -hs);
            verts[1] = p + new Vector2( hs, -hs);
            verts[2] = p + new Vector2( hs,  hs);
            verts[3] = p + new Vector2(-hs,  hs);

            DrawSolidPolygon(ref verts, 4, color, true);
        }

        public void DrawString(int x, int y, string s)
        {
            _stringData.Add(new StringData(x, y, s, null));
        }

        public void DrawString(int x, int y, string s, params object[] args)
        {
            _stringData.Add(new StringData(x, y, s, args));
        }

        public void FinishDrawShapes()
        {
            mDevice.RasterizerState = RasterizerState.CullNone;
            mDevice.BlendState = BlendState.AlphaBlend;
            
            if (_fillCount > 0)
                mDevice.DrawUserPrimitives<VertexPositionColor>(PrimitiveType.TriangleList, _vertsFill, 0, _fillCount);
            
            if (_lineCount > 0)
                mDevice.DrawUserPrimitives<VertexPositionColor>(PrimitiveType.LineList, _vertsLines, 0, _lineCount);

            _lineCount = _fillCount = 0;
        }

        public void FinishDrawString()
        {
            for (int i = 0; i < _stringData.Count; i++)
            {
                var text = _stringData[i].args == null ? _stringData[i].s : string.Format(_stringData[i].s, _stringData[i].args);
                mBatch.DrawString(_font, text, new Vector2(_stringData[i].x, _stringData[i].y), new Color(0.9f, 0.6f, 0.6f));
            }

            _stringData.Clear();
        }

        public void DrawAABB(ref AABB aabb, Color color)
        {
            FixedArray8<Vector2> verts = new FixedArray8<Vector2>();
            verts[0] = new Vector2(aabb.lowerBound.X, aabb.lowerBound.Y);
            verts[1] = new Vector2(aabb.upperBound.X, aabb.lowerBound.Y);
            verts[2] = new Vector2(aabb.upperBound.X, aabb.upperBound.Y);
            verts[3] = new Vector2(aabb.lowerBound.X, aabb.upperBound.Y);

            DrawPolygon(ref verts, 4, color);
        }

        public static VertexPositionColor[] _vertsLines = new VertexPositionColor[100000];
        public static VertexPositionColor[] _vertsFill = new VertexPositionColor[100000];
        public static int _lineCount;
        public static int _fillCount;
        public static SpriteFont _font;

        private List<StringData> _stringData;
        struct StringData
        {
            public StringData(int x, int y, string s, object[] args)
            {
                this.x = x;
                this.y = y;
                this.s = s;
                this.args = args;
            }

            public int x, y;
            public string s;
            public object[] args;
        }
    }
}
