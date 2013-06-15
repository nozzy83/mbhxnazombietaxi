using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MBHEngine.GameObject;
using MBHEngine.Render;
using MBHEngine.Debug;

namespace MBHEngine.Behaviour
{
    public class InfiniteBG : Behaviour
    {
        /// <summary>
        /// A 1x1 solid texture used for filling the tile.
        /// </summary>
        private Texture2D mFill;

        /// <summary>
        /// The rectangle which will define the tile and its position for rendering.
        /// It will be used over and over in a single pass to create a checkerboard.
        /// </summary>
        private Rectangle mBlock;

        /// <summary>
        /// The size of a single tile, both width and height.
        /// </summary>
        private Int32 mTileSize;

        /// <summary>
        /// How many times need to be rendered in each direction to cover the screen.
        /// </summary>
        private Int32 mNumWide;
        private Int32 mNumHigh;

        /// <summary>
        /// Half those numbers; convienient for translating from the center of the screen.
        /// </summary>
        private Int32 mNumWideHalf;
        private Int32 mNumHighHalf;

        /// <summary>
        /// Constructor which also handles the process of loading in the Behaviour
        /// Definition information.
        /// </summary>
        /// <param name="parentGOH">The game object that this behaviour is attached to.</param>
        /// <param name="fileName">The file defining this behaviour.</param>
        public InfiniteBG(GameObject.GameObject parentGOH, String fileName)
            : base(parentGOH, fileName)
        {
        }

        /// <summary>
        /// Call this to initialize a Behaviour with data supplied in a file.
        /// </summary>
        /// <param name="fileName">The file to load from.</param>
        public override void LoadContent(String fileName)
        {
            base.LoadContent(fileName);

            mTileSize = 8;

            mNumWide = 24;
            mNumHigh = 16;

            mNumWideHalf = mNumWide / 2;
            mNumHighHalf = mNumHigh / 2;

            mFill = new Texture2D(GameObjectManager.pInstance.pGraphicsDevice, 1, 1);
            mFill.SetData(new Color[] { Color.White });
            mBlock = new Rectangle(0, 0, mTileSize, mTileSize);
        }

        /// <summary>
        /// Called once per frame by the game object.
        /// </summary>
        /// <param name="gameTime">The amount of time that has passed this frame.</param>
        public override void Update(GameTime gameTime)
        {
            // TODO:
            // See if most of the render calculations can be moved to Update pass.
        }

        /// <summary>
        /// Called once render cycle by the game object manager.
        /// </summary>
        /// <param name="batch">The sprite batch to render to.</param>
        /// <param name="effect">The currently set shader.</param>
        public override void Render(SpriteBatch batch, Effect effect)
        {
            // This is slightly odd because the camera can support tweening to target positions, but it is
            // currently disabled, so this works.  We really want to use the actual position of the camera,
            // but it is only stored in the final matrix which include scale data which messes up the translation
            // data.
            Vector2 pos = CameraManager.pInstance.pTargetPosition;

            // The top left of the tile closest to the center of the screen.
            Int32 centerLeft = (Int32)(pos.X - pos.X % mTileSize);
            Int32 centerTop = (Int32)(pos.Y - pos.Y % mTileSize);

            // Based on that center tiles top left coordinate, calculate where to top-left tile of the
            // whole BG should be placed.
            Int32 left = centerLeft - (mNumWideHalf * mTileSize);
            Int32 top = centerTop - (mNumHighHalf * mTileSize);

            // We want a checkboard effect.  The tricky part is that the checkboard needs to appear static as
            // you move around and the tiles get recycled.  We calculate what color the tile should be based
            // on world space, so that it never changes.
            Color col = Color.LightGray;
            if (centerTop / mTileSize % 2 == 0) // Even
            {
                if (centerLeft / mTileSize % 2 == 0)
                {
                    col = Color.Gray;
                }
            }
            else // Odd
            {
                if (centerLeft / mTileSize % 2 != 0)
                {
                    col = Color.Gray;
                }
            }

            // Loop through all the tiles going from top left across, and down to bottom right.
            for (int y = 0; y < mNumHigh; y++)
            {
                for (int x = 0; x < mNumWide; x++)
                {
                    // Reposition the rectangle.
                    mBlock.X = left + (x * mTileSize);
                    mBlock.Y = top + (y * mTileSize);

                    // Make the rectangle semi-transparent simiply because it looks kind of nice.
                    col.A = 125;

                    // Add this tile to the batch render.
                    batch.Draw(mFill, mBlock, col);
                    //batch.Draw(mFill, mBlock, null, col, 0, Vector2.Zero, SpriteEffects.None, 0);

                    // Reset the alpha so that the compare below works.
                    col.A = 255;

                    // Flip between light and dark gray.
                    if (col == Color.Gray)
                    {
                        col = Color.LightGray;
                    }
                    else
                    {
                        col = Color.Gray;
                    }
                }

                // Flip between colors for the height as well.
                if (col == Color.Gray)
                {
                    col = Color.LightGray;
                }
                else
                {
                    col = Color.Gray;
                }
            }
        }
    }
}
