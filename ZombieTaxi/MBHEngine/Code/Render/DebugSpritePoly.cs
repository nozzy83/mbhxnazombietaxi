using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace MBHEngine.Render 
{
    /// <summary>
    /// Used for rendering polys using sprite batches.
    /// </summary>
    public class DebugSpritePoly
    {
        /// <summary>
        /// This gets used to draw the polys.  Essential it is a tiny blank square that
        /// gets stretch to meet out needs.
        /// </summary>
        Texture2D mBlankTexture;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="device">Used for creating a new blank texture.</param>
        public DebugSpritePoly(GraphicsDevice device)
        {
            mBlankTexture = new Texture2D(device, 1, 1, false, SurfaceFormat.Color);
            mBlankTexture.SetData(new[] { Color.White });
        }

        /// <summary>
        /// Draws a single line of a specified with length and colour.
        /// </summary>
        /// <remarks>
        /// The line is not centered width wise, so having a thickness greater than 1 will reseult in
        /// an off-center display.
        /// </remarks>
        /// <param name="batch">The sprite batch to render to.</param>
        /// <param name="thinkness">Essentially the height of the line before it is rotated.</param>
        /// <param name="point1">Starting point for the line in world space.</param>
        /// <param name="point2">Ending point for the line in world space.</param>
        /// <param name="color">The line will be tinted with this colour.</param>
        public void DrawLine(SpriteBatch batch, Single thinkness, Vector2 point1, Vector2 point2, Color color)
        {
            Single angle = (Single)System.Math.Atan2(point2.Y - point1.Y, point2.X - point1.X);
            Single length = Vector2.Distance(point1, point2);

            // TODO: Can we scale the thichness by the inverse of the game camera to maintain a constant
            //       thickness in screen space?

            batch.Draw(
                mBlankTexture, 
                point1, 
                null, 
                color,                       
                angle, 
                Vector2.Zero, 
                new Vector2(length, thinkness),
                SpriteEffects.None, 
                0);
        }

        /// <summary>
        /// Draws the frame of a circle with no fill.
        /// </summary>
        /// <param name="batch">The sprite batch to render to.</param>
        /// <param name="centre">The poisiton of the circle in world space.</param>
        /// <param name="radius">The distance from the centre position to the endge of the circle.</param>
        /// <param name="color">The line will be tinted with this colour.</param>
        public void DrawCircle(SpriteBatch batch, Vector2 centre, Single radius, Color color)
        {
            DrawPoly(batch, centre, radius, 20, color);
        }

        /// <summary>
        /// Draws a polygon of any convex shape.
        /// </summary>
        /// <param name="batch">The sprite batch to render to.</param>
        /// <param name="centre">The poisiton of the shape in world space.</param>
        /// <param name="radius">The distance from the centre position to the endge of the circle.</param>
        /// <param name="tessilation">The number of points on the polygon.</param>
        /// <param name="color">The line will be tinted with this colour.</param>
        public void DrawPoly(SpriteBatch batch, Vector2 centre, Single radius, int tessilation, Color color)
        {
            //szize of angle between each vertex
            Single Increment = (Single)System.Math.PI * 2 / tessilation;
            Vector2[] Vertices = new Vector2[tessilation];
            //compute the locations of all the vertices
            for (Int32 i = 0; i < tessilation; i++)
            {
                Vertices[i].X = (Single)System.Math.Cos(Increment * i);
                Vertices[i].Y = (Single)System.Math.Sin(Increment * i);
            }
            //Now draw all the lines
            for (Int32 i = 0; i < tessilation - 1; i++)
            {
                DrawLine(batch, 1.0f, centre + Vertices[i] * radius, centre + Vertices[i + 1] * radius, color);
            }
            DrawLine(batch, 1.0f, centre + radius * Vertices[tessilation - 1], centre + radius * Vertices[0], color);
        }
    }
}
