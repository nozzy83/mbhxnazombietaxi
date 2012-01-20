using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OgmoXNA4;
using OgmoXNA4.Layers;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework;
using MBHEngine.GameObject;
using MBHEngineContentDefs;
using MBHEngine.Debug;

namespace MBHEngine.Behaviour
{
    /// <summary>
    /// The root of most content for the game.  Contains all the data about a particular area of the game.
    /// </summary>
    public class Level : MBHEngine.Behaviour.Behaviour
    {
        /// <summary>
        /// Overrides the current sprite effect being applied to this sprite.
        /// </summary>
        public class CheckForCollisionMessage : BehaviourMessage
        {
            public Boolean mCollisionDetected;
            public Boolean mCollisionDetectedX;
            public Boolean mCollisionDetectedY;
            public Rectangle mOriginalRect;
            public Vector2 mOriginalPos;
            public Rectangle mDesiredRect;
            public Vector2 mDesiredPos;
        };

        /// <summary>
        /// Data about a single tile.
        /// </summary>
        public class Tile
        {
            public enum WallTypes
            {
                None = 0,
                Top = 1,
                Right = 2,
                Bottom = 4,
                Left = 8,
            };
            public Int32 mType = 0;
            public WallTypes mActiveWalls = WallTypes.None;
        }

        /// <summary>
        /// Collision information for this level.
        /// </summary>
        private Tile[,] mCollisionGrid;

        /// <summary>
        /// The size of the map in tiles.
        /// </summary>
        private Int32 mMapWidth;

        /// <summary>
        /// The size of the map in tiles.
        /// </summary>
        private Int32 mMapHeight;

        /// <summary>
        /// The width of a single tile.
        /// </summary>
        private Int32 mTileWidth;

        /// <summary>
        /// The height of a single tile.
        /// </summary>
        private Int32 mTileHeight;

        /// <summary>
        /// Texture used for rendering the collision boxes.
        /// </summary>
        private Texture2D debugTexture;

        /// <summary>
        /// Constructor which also handles the process of loading in the Behaviour
        /// Definition information.
        /// </summary>
        /// <param name="parentGOH">The game object that this behaviour is attached to.</param>
        /// <param name="fileName">The file defining this behaviour.</param>
        public Level(GameObject.GameObject parentGOH, String fileName)
            : base(parentGOH, fileName)
        {
        }

        /// <summary>
        /// Call this to initialize a Behaviour with data supplied in a file.
        /// </summary>
        /// <param name="fileName">The file to load from.</param>
        public override void LoadContent(String fileName)
        {
            LevelDefinition def = GameObjectManager.pInstance.pContentManager.Load<LevelDefinition>(fileName);

            mMapWidth = 20;
            mMapHeight = 20;
            mTileWidth = mTileHeight = 8;

            mCollisionGrid = new Tile[mMapWidth, mMapHeight];
            for (Int32 y = 0; y < mMapHeight; y++)
            {
                for (Int32 x = 0; x < mMapWidth; x++)
                {
                    mCollisionGrid[x,y] = new Tile();
                }
            }

            //mCollisionGrid[0, 0] = 1;
            mCollisionGrid[0, 1].mType = 1;
            mCollisionGrid[1, 1].mType = 1;
            mCollisionGrid[1, 0].mType = 1;
            mCollisionGrid[2, 0].mType = 1;

            mCollisionGrid[4, 5].mType = 1;
            mCollisionGrid[4, 7].mType = 1;
            mCollisionGrid[5, 5].mType = 1;
            mCollisionGrid[5, 7].mType = 1;

            mCollisionGrid[4, 10].mType = 1;
            mCollisionGrid[6, 10].mType = 1;
            mCollisionGrid[4, 11].mType = 1;
            mCollisionGrid[6, 11].mType = 1;
            mCollisionGrid[5, 12].mType = 1;
            for (Int32 y = 0; y < mMapHeight; y++)
            {
                for (Int32 x = 0; x < mMapWidth; x++)
                {
                    if (mCollisionGrid[x, y].mType == 1)
                    {
                        if (y == 0 || mCollisionGrid[x, y - 1].mType != 1)
                            mCollisionGrid[x, y].mActiveWalls |= Tile.WallTypes.Top;
                        if (x == mMapWidth - 1 || mCollisionGrid[x + 1, y].mType != 1)
                            mCollisionGrid[x, y].mActiveWalls |= Tile.WallTypes.Right;
                        if (y == mMapHeight - 1 || mCollisionGrid[x, y + 1].mType != 1)
                            mCollisionGrid[x, y].mActiveWalls |= Tile.WallTypes.Bottom;
                        if (x == 0 || mCollisionGrid[x - 1, y].mType != 1)
                            mCollisionGrid[x, y].mActiveWalls |= Tile.WallTypes.Left;
                    }
                }
            }

            debugTexture = new Texture2D(GameObjectManager.pInstance.pGraphicsDevice, 1, 1);
            debugTexture.SetData(new Color[] { Color.White });

            base.LoadContent(fileName);
        }

        /// <summary>
        /// Called once render cycle by the game object manager.
        /// </summary>
        /// <param name="batch">The sprite batch to render to.</param>
        public override void Render(SpriteBatch batch)
        {
            for (Int32 y = 0; y < mMapHeight; y++)
            {
                for (Int32 x = 0; x < mMapWidth; x++)
                {
                    // Is this tile solid?
                    if (mCollisionGrid[x, y].mType != 0)
                    {
                        // By default render it black.
                        Color c = Color.Black;

                        // But if a collision was detected on it, render it red.
                        if (mCollisionGrid[x, y].mType == 2)
                            c = Color.Red;

                        // Reset the collision type (assuming that if it entered this if statement it should,
                        // be type 1.
                        mCollisionGrid[x, y].mType = 1;

                        // Draw the collison volume.
                        batch.Draw(debugTexture, new Rectangle(x * mTileWidth, y * mTileHeight, mTileWidth, mTileHeight), c);

                        // Draw the walls that have been determined to require collision checks.
                        //
                        if ((mCollisionGrid[x, y].mActiveWalls & Tile.WallTypes.Top) != Tile.WallTypes.None)
                        {
                            DebugShapeDisplay.pInstance.AddSegment(new Vector2(x * mTileWidth, y * mTileHeight),
                                                                   new Vector2(x * mTileWidth + mTileWidth, y * mTileHeight),
                                                                   Color.Red);         
                        }
                        if ((mCollisionGrid[x, y].mActiveWalls & Tile.WallTypes.Right) != Tile.WallTypes.None)
                        {
                            DebugShapeDisplay.pInstance.AddSegment(new Vector2(x * mTileWidth + mTileWidth, y * mTileHeight),
                                                                   new Vector2(x * mTileWidth + mTileWidth, y * mTileHeight + mTileHeight),
                                                                   Color.Red);
                        }
                        if ((mCollisionGrid[x, y].mActiveWalls & Tile.WallTypes.Bottom) != Tile.WallTypes.None)
                        {
                            DebugShapeDisplay.pInstance.AddSegment(new Vector2(x * mTileWidth, y * mTileHeight + mTileHeight),
                                                                   new Vector2(x * mTileWidth + mTileWidth, y * mTileHeight + mTileHeight),
                                                                   Color.Red);
                        }
                        if ((mCollisionGrid[x, y].mActiveWalls & Tile.WallTypes.Left) != Tile.WallTypes.None)
                        {
                            DebugShapeDisplay.pInstance.AddSegment(new Vector2(x * mTileWidth, y * mTileHeight),
                                                                   new Vector2(x * mTileWidth, y * mTileHeight + mTileHeight),
                                                                   Color.Red);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// The main interface for communicating between behaviours.  Using polymorphism, we
        /// define a bunch of different messages deriving from BehaviourMessage.  Each behaviour
        /// can then check for particular upcasted messahe types, and either grab some data 
        /// from it (set message) or store some data in it (get message).
        /// </summary>
        /// <param name="msg">The message being communicated to the behaviour.</param>
        /// <returns>The resulting message.  If not null, the message was handled.</returns>
        public override BehaviourMessage OnMessage(BehaviourMessage msg)
        {
            // Which type of message was sent to us?
            if (msg is Level.CheckForCollisionMessage)
            {
                Level.CheckForCollisionMessage temp = (Level.CheckForCollisionMessage)msg;
                temp.mCollisionDetected = CheckForCollision(temp.mOriginalRect,
                                                            temp.mDesiredRect,
                                                            temp.mOriginalPos,
                                                            temp.mDesiredPos,
                                                            out temp.mCollisionDetectedX,
                                                            out temp.mCollisionDetectedY);
                msg = temp;
            }
            else
            {
                // This is not a message we know how to handle.
                // TODO:
                // This seems wrong.  Won't this overwrite any set messages that might need to be passed on to other
                // behaviours?
                msg = null;
            }

            return msg;
        }

        private Boolean CheckForCollision(Rectangle startRect, Rectangle endRect, Vector2 start, Vector2 end, out Boolean collideX, out Boolean collideY)
        {
            Vector2 dir = end - start;

            Boolean hit = false;
            collideX = false;
            collideY = false;

            for (Int32 y = 0; y < mMapHeight; y++)
            {
                for (Int32 x = 0; x < mMapWidth; x++)
                {
                    if (mCollisionGrid[x, y].mType == 1 && mCollisionGrid[x,y].mActiveWalls != Tile.WallTypes.None)
                    {
                        Rectangle tileRect = new Rectangle(x * mTileWidth, y * mTileHeight, mTileWidth, mTileHeight);
                        if (tileRect.Intersects(endRect))
                        {
                            if (dir.X > 0 &&
                                (mCollisionGrid[x, y].mActiveWalls & Tile.WallTypes.Left) != Tile.WallTypes.None &&
                                startRect.Right <= x * mTileWidth)
                            {
                                collideX = true;
                            }
                            else if (dir.X < 0 &&
                                (mCollisionGrid[x, y].mActiveWalls & Tile.WallTypes.Right) != Tile.WallTypes.None &&
                                startRect.Left >= x * mTileWidth + mTileWidth)
                            {
                                collideX = true;
                            }
                            if (dir.Y > 0 &&
                                (mCollisionGrid[x, y].mActiveWalls & Tile.WallTypes.Top) != Tile.WallTypes.None &&
                                startRect.Bottom <= y * mTileHeight)
                            {
                                collideY = true;
                            }
                            else if (dir.Y < 0 &&
                                (mCollisionGrid[x, y].mActiveWalls & Tile.WallTypes.Bottom) != Tile.WallTypes.None &&
                                startRect.Top >= y * mTileHeight + mTileWidth)
                            {
                                collideY = true;
                            }
                            mCollisionGrid[x, y].mType = 2;
                            hit = true;
                        }
                    }
                }
            }

            return hit;
        }
    }
}
