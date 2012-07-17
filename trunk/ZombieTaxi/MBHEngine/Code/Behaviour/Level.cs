using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework;
using MBHEngine.GameObject;
using MBHEngineContentDefs;
using MBHEngine.Debug;
using MBHEngine.Math;
using MBHEngine.Render;

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
            /// <summary>
            /// Was a collision detected at all.
            /// </summary>
            public Boolean mCollisionDetected;

            /// <summary>
            /// Was a collision detected n the X direction specifically.
            /// </summary>
            public Boolean mCollisionDetectedX;

            /// <summary>
            /// Was a collision detect in the Y direction specifically.
            /// </summary>
            public Boolean mCollisionDetectedY;

            /// <summary>
            /// At what point did the collision occur in the X (if one did occur).
            /// </summary>
            public Single mCollisionPointX;

            /// <summary>
            /// At what point did the collision occur in the Y (if one did occur).
            /// </summary>
            public Single mCollisionPointY;

            /// <summary>
            /// The collision volume representing when the object started at the begining of this movement.
            /// </summary>
            public MBHEngine.Math.Rectangle mOriginalRect;

            /// <summary>
            /// The collision volume representing where the object is trying to reach.
            /// </summary>
            public MBHEngine.Math.Rectangle mDesiredRect;
        };

        /// <summary>
        /// Finds the tile at the specified position.
        /// </summary>
        public class GetTileAtPositionMessage : BehaviourMessage
        {
            /// <summary>
            /// A position in world space to check against.
            /// </summary>
            public Vector2 mPosition;

            /// <summary>
            /// The tile which the position intersects, or null if there is not one.
            /// </summary>
            public Tile mTile;
        }

        /// <summary>
        /// Data about a single tile.
        /// </summary>
        public class Tile
        {
            /// <summary>
            /// The different types of walls that a tile can have.  Basically the different sides.
            /// </summary>
            public enum WallTypes
            {
                None = 0,
                Top = 1,
                Right = 2,
                Bottom = 4,
                Left = 8,
            };

            /// <summary>
            /// The different types of tiles. 
            /// </summary>
            public enum TileTypes
            {
                Empty = 0,
                Solid = 1,
                Collision = 2,
                CollisionChecked = 4,
            };

            /// <summary>
            /// The type of tile this is.
            /// </summary>
            /// <remarks>Right now this is just 0 (not solid), 1 (solid), 2 (temp - solid with collision)</remarks>
            public TileTypes mType = TileTypes.Empty;

            /// <summary>
            /// Bits representing while walls this tile can have.  & it with a WallType to determine
            /// if it has a particular wall.
            /// </summary>
            public WallTypes mActiveWalls = WallTypes.None;

            /// <summary>
            /// Rather than calculating the rectangle defining the bounds of this tile.
            /// </summary>
            public MBHEngine.Math.Rectangle mCollisionRect;

            /// <summary>
            /// References to the tiles surrounding this one.
            /// </summary>
            public Tile[] mAdjecentTiles;

            /// <summary>
            /// Uses these enums to look up the adjecent tiles in mAdjecentTiles.
            /// </summary>
            public enum AdjectTileDir
            {
                LEFT = 0,
                LEFT_UP,
                UP,
                RIGHT_UP,
                RIGHT,
                RIGHT_DOWN,
                DOWN,
                LEFT_DOWN,

                NUM_DIRECTIONS,
            }
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
        private Texture2D mDebugTexture;

        /// <summary>
        /// These are needed to do our wall collision and we want to avoid allocating them over and
        /// over again, so instead we just reuse the same ones.
        /// </summary>
        private LineSegment mCollisionWall;
        private LineSegment mCollisionRectMovement;

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

            // TODO: This should be loaded in from the def.
            mMapWidth = 200;
            mMapHeight = 200;
            mTileWidth = mTileHeight = 8;

            // Start by creating all the tiles for this level.
            mCollisionGrid = new Tile[mMapWidth, mMapHeight];
            for (Int32 y = 0; y < mMapHeight; y++)
            {
                for (Int32 x = 0; x < mMapWidth; x++)
                {
                    mCollisionGrid[x,y] = new Tile();

                    // Precalculate the adjecent tiles to this one.
                    //

                    // Allocate space for the Array itself.
                    mCollisionGrid[x, y].mAdjecentTiles = new Tile[(Int32)Tile.AdjectTileDir.NUM_DIRECTIONS];

                    // Start with the tiles left of this one, but avoid looking outside the map.
                    if (x > 0)
                    {
                        // Store a reference to the tile to the left.
                        mCollisionGrid[x, y].mAdjecentTiles[(Int32)Tile.AdjectTileDir.LEFT] = mCollisionGrid[x - 1, y];

                        // Since the tile to the left was created before this one, it needs to be updated to point to this
                        // once as the tile on it right.
                        mCollisionGrid[x - 1, y].mAdjecentTiles[(Int32)Tile.AdjectTileDir.RIGHT] = mCollisionGrid[x, y];

                        // Check up and to the left if that is not outside the map.
                        if (y > 0)
                        {
                            // Again, set ourselves and then the adjecent one which was created prior to us being
                            // created.
                            mCollisionGrid[x, y].mAdjecentTiles[(Int32)Tile.AdjectTileDir.LEFT_UP] = mCollisionGrid[x - 1, y - 1];
                            mCollisionGrid[x - 1, y - 1].mAdjecentTiles[(Int32)Tile.AdjectTileDir.RIGHT_DOWN] = mCollisionGrid[x, y];
                        }
                    }

                    // For tiles above.
                    if (y > 0)
                    {
                        // Set our up, their down.
                        mCollisionGrid[x, y].mAdjecentTiles[(Int32)Tile.AdjectTileDir.UP] = mCollisionGrid[x, y - 1];
                        mCollisionGrid[x, y - 1].mAdjecentTiles[(Int32)Tile.AdjectTileDir.DOWN] = mCollisionGrid[x, y];

                        // All that is left is the RIGHT_UP/LEFT_DOWN relationship.
                        if (x < mMapWidth - 1)
                        {
                            mCollisionGrid[x, y].mAdjecentTiles[(Int32)Tile.AdjectTileDir.RIGHT_UP] = mCollisionGrid[x + 1, y - 1];
                            mCollisionGrid[x + 1, y - 1].mAdjecentTiles[(Int32)Tile.AdjectTileDir.LEFT_DOWN] = mCollisionGrid[x, y];
                        }
                    }

                    // Calculate the center point of the tile.
                    Vector2 cent = new Vector2((x * mTileWidth) + (mTileWidth * 0.5f), (y * mTileHeight) + (mTileHeight * 0.5f));

                    // Create a rectangle to represent the tile.
                    mCollisionGrid[x,y].mCollisionRect = new MBHEngine.Math.Rectangle(mTileWidth, mTileHeight, cent);

                    // Give it a random chance to be solid.
                    if (RandomManager.pInstance.RandomPercent() <= 0.05f)
                    {
                        mCollisionGrid[x,y].mType = Level.Tile.TileTypes.Solid;
                    }
                }
            }

            // TODO: The map data should be read in from the level def.
            /*
            mCollisionGrid[0, 1].mType = 1;
            mCollisionGrid[1, 1].mType = 1;
            mCollisionGrid[1, 0].mType = 1;
            mCollisionGrid[2, 0].mType = 1;

            mCollisionGrid[4, 5].mType = 1;
            mCollisionGrid[4, 7].mType = 1;
            mCollisionGrid[5, 5].mType = 1;
            mCollisionGrid[6, 7].mType = 1;

            mCollisionGrid[4, 10].mType = 1;
            mCollisionGrid[6, 10].mType = 1;
            mCollisionGrid[4, 11].mType = 1;
            mCollisionGrid[6, 11].mType = 1;
            mCollisionGrid[5, 12].mType = 1;

            mCollisionGrid[0, 2].mType = 1;
            mCollisionGrid[0, 3].mType = 1;
            mCollisionGrid[0, 4].mType = 1;
            mCollisionGrid[0, 5].mType = 1;
            mCollisionGrid[0, 6].mType = 1;
            mCollisionGrid[0, 7].mType = 1;
            mCollisionGrid[0, 8].mType = 1;
            mCollisionGrid[0, 9].mType = 1;
            mCollisionGrid[0, 10].mType = 1;
            mCollisionGrid[0, 11].mType = 1;
            mCollisionGrid[0, 12].mType = 1;
            mCollisionGrid[0, 13].mType = 1;
            mCollisionGrid[0, 14].mType = 1;
            */
            
            // Loop through all the tiles and calculate which sides need to have collision checks done on it.
            // For example if a tile has another tile directly above it, it does not need to check collision 
            // on the top side, because it is not reachable.
            for (Int32 y = 0; y < mMapHeight; y++)
            {
                for (Int32 x = 0; x < mMapWidth; x++)
                {
                    if ((mCollisionGrid[x, y].mType & Level.Tile.TileTypes.Solid) == Tile.TileTypes.Solid)
                    {
                        if (y == 0 || (mCollisionGrid[x, y - 1].mType & Level.Tile.TileTypes.Solid) != Tile.TileTypes.Solid)
                            mCollisionGrid[x, y].mActiveWalls |= Tile.WallTypes.Top;
                        if (x == mMapWidth - 1 || (mCollisionGrid[x + 1, y].mType & Level.Tile.TileTypes.Solid) != Tile.TileTypes.Solid)
                            mCollisionGrid[x, y].mActiveWalls |= Tile.WallTypes.Right;
                        if (y == mMapHeight - 1 || (mCollisionGrid[x, y + 1].mType & Level.Tile.TileTypes.Solid) != Tile.TileTypes.Solid)
                            mCollisionGrid[x, y].mActiveWalls |= Tile.WallTypes.Bottom;
                        if (x == 0 || (mCollisionGrid[x - 1, y].mType & Level.Tile.TileTypes.Solid) != Tile.TileTypes.Solid)
                            mCollisionGrid[x, y].mActiveWalls |= Tile.WallTypes.Left;
                    }
                }
            }

            // Allocate these once and use them over and over again.
            mCollisionWall = new LineSegment();
            mCollisionRectMovement = new LineSegment();

            // A debug texture used for rendering to blocks.
            mDebugTexture = new Texture2D(GameObjectManager.pInstance.pGraphicsDevice, 1, 1);
            mDebugTexture.SetData(new Color[] { Color.White });

            base.LoadContent(fileName);
        }

        /// <summary>
        /// Called once render cycle by the game object manager.
        /// </summary>
        /// <param name="batch">The sprite batch to render to.</param>
        public override void Render(SpriteBatch batch)
        {
            //Vector2 playerPos = GameObjectManager.pInstance.pPlayer.pOrientation.mPosition;
            //Tile playerTile = GetTileAtPosition(playerPos.X, playerPos.Y);
            //if (playerTile != null)
            //{
            //    for (Int32 i = 0; i < playerTile.mAdjecentTiles.Length; i++)
            //    {
            //        if (playerTile.mAdjecentTiles[i] != null)
            //        {
            //            DebugShapeDisplay.pInstance.AddSegment(
            //                playerTile.mCollisionRect.pCenterPoint,
            //                playerTile.mAdjecentTiles[i].mCollisionRect.pCenterPoint,
            //                Color.Purple);
            //        }
            //    }
            //}
            for (Int32 y = 0; y < mMapHeight; y++)
            {
                for (Int32 x = 0; x < mMapWidth; x++)
                {
                    // Is this tile solid?
                    if (mCollisionGrid[x, y].mType != Level.Tile.TileTypes.Empty)
                    {
                        // By default render it black.
                        Color c = Color.Black;

                        // But if a collision was detected on it, render it red.
                        if((mCollisionGrid[x, y].mType & Level.Tile.TileTypes.Collision) == Tile.TileTypes.Collision)
                            c = Color.Red;
                        // If a collision was even checked for, render it Orange.
                        else if((mCollisionGrid[x, y].mType & Level.Tile.TileTypes.CollisionChecked) == Tile.TileTypes.CollisionChecked)
                            c = Color.OrangeRed;


                        // Clear the temp bits used for rendering collision info.
                        mCollisionGrid[x, y].mType &= ~(Tile.TileTypes.Collision | Tile.TileTypes.CollisionChecked);

                        if (!CameraManager.pInstance.IsOnCamera(mCollisionGrid[x, y].mCollisionRect))
                        {
                            continue;
                        }

                        // Draw the collison volume.
                        batch.Draw(mDebugTexture, new Microsoft.Xna.Framework.Rectangle(x * mTileWidth, y * mTileHeight, mTileWidth, mTileHeight), c);

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
        public override void OnMessage(ref BehaviourMessage msg)
        {
            // Which type of message was sent to us?
            if (msg is Level.CheckForCollisionMessage)
            {
                Level.CheckForCollisionMessage temp = (Level.CheckForCollisionMessage)msg;
                temp.mCollisionDetected = CheckForCollision(temp.mOriginalRect,
                                                            temp.mDesiredRect,
                                                            out temp.mCollisionDetectedX,
                                                            out temp.mCollisionDetectedY,
                                                            out temp.mCollisionPointX,
                                                            out temp.mCollisionPointY);
                msg = temp;
            }
            else if (msg is GetTileAtPositionMessage)
            {
                GetTileAtPositionMessage temp = (GetTileAtPositionMessage)msg;

                temp.mTile = GetTileAtPosition(temp.mPosition.X, temp.mPosition.Y);

                msg = temp;
            }
        }

        /// <summary>
        /// Calculate which tile is at a particular position in world space.
        /// </summary>
        /// <param name="x">The x position in world space.</param>
        /// <param name="y">The y position in world space.</param>
        /// <returns>The tile at that position or null if none exists.</returns>
        private Tile GetTileAtPosition(Single x, Single y)
        {
            // We assume that the x and y are at the center point of the object so don't round these numbers
            // just truncate them down.
            Int32 xIndex = (Int32)(x / mTileWidth);
            Int32 yIndex = (Int32)(y / mTileHeight);

            if (xIndex < 0 || xIndex >= mMapWidth || yIndex < 0 || yIndex >= mMapHeight)
            {
                return null;
            }
            else
            {
                return mCollisionGrid[xIndex, yIndex];
            }
        }

        /// <summary>
        /// Does a collision check between every tile in the level and a specified rectangle.  Requires
        /// both the starting position and the desired position, so that it can figure out details about
        /// how it collided with different tiles.
        /// </summary>
        /// <param name="startRect">The location of this collision rect at the start of this movement.</param>
        /// <param name="endRect">The rectangle representing the final position that it is trying to get to.</param>
        /// <param name="collideX">Did this check yield a collision in the X direction?</param>
        /// <param name="collideY">Did this check yield a collision in the Y direction?</param>
        /// <param name="collidePointX">Where did the X collision happen (if it did)?</param>
        /// <param name="collidePointY">Where did the Y collision happen (if it did)?</param>
        /// <returns></returns>
        private Boolean CheckForCollision(
            MBHEngine.Math.Rectangle startRect, 
            MBHEngine.Math.Rectangle endRect, 
            out Boolean collideX, 
            out Boolean collideY,
            out Single collidePointX,
            out Single collidePointY)
        {
            // Which direction is this thing heading?
            // Needed to determine which walls to check against (eg. no need to check left wall if we are moving left).
            Vector2 dir = endRect.pCenterPoint - startRect.pCenterPoint;

            // Start by assuming no collisions occured.
            Boolean hit = false;
            collideX = false;
            collideY = false;
            collidePointX = collidePointY = 0;

            // We don't want to check against every tile in the world.  Instead attempt to narrow it down
            // based on the fact that the tiles can be mapped from their x,y position to their index
            // into the array.
            Int32 checkRange = 5;
            Single x2 = (endRect.pCenterPoint.X / mTileWidth) - ((Single)checkRange * 0.5f);
            Int32 startX = System.Math.Max((Int32)System.Math.Round(x2), 0);

            Single y2 = (endRect.pCenterPoint.Y / mTileHeight) - ((Single)checkRange * 0.5f);
            Int32 startY = System.Math.Max((Int32)System.Math.Round(y2), 0);

            // Loop through every time checking for collisions.
            for (Int32 y = startY; y < mMapHeight && y < startY + checkRange; y++)
            {
                for (Int32 x = startX; x < mMapWidth && x < startX + checkRange; x++)
                {

                    // Is this tile solid and does it have any active walls?
                    // It may be solid with no walls in the case of one completly surrounded.
                    if (mCollisionGrid[x, y].mType != Level.Tile.TileTypes.Empty && mCollisionGrid[x, y].mActiveWalls != Tile.WallTypes.None)
                    {
                        // This tile has been considered for a collision.  It will be changed to type 2 if there is a
                        // collision.
                        mCollisionGrid[x, y].mType |= Level.Tile.TileTypes.CollisionChecked;

                        // Calculate the center point of the tile.
                        Vector2 cent = new Vector2((x * mTileWidth) + (mTileWidth * 0.5f), (y * mTileHeight) + (mTileHeight * 0.5f));

                        // Create a rectangle to represent the tile.
                        //MBHEngine.Math.Rectangle tileRect = new MBHEngine.Math.Rectangle(mTileWidth, mTileHeight, cent);

                        // Does the place we are trying to move to intersect with the tile? 
                        if (mCollisionGrid[x, y].mCollisionRect.Intersects(endRect))
                        {
                            // If we are moving right, and we hit a tile with a left wall...
                            if (dir.X > 0 &&
                                (mCollisionGrid[x, y].mActiveWalls & Tile.WallTypes.Left) != Tile.WallTypes.None)
                            {
                                // Create a line from the center of our destination...
                                mCollisionRectMovement.pPointA = mCollisionGrid[x, y].mCollisionRect.pCenterPoint;
                                mCollisionRectMovement.pPointB = endRect.pCenterPoint;

                                // ...and a line representing the wall we are testing against.
                                mCollisionGrid[x, y].mCollisionRect.GetLeftEdge(ref mCollisionWall);

                                // If those two lines intersect, then we count this collision.
                                // We do this line check to avoid colliding with both top/bottom and
                                // left/right in the same movement.  That causes issues like getting stuck in
                                // tight corridors.
                                Vector2 intersect = new Vector2();
                                if (mCollisionWall.Intersects(mCollisionRectMovement, ref intersect))
                                {
                                    DebugShapeDisplay.pInstance.AddSegment(mCollisionRectMovement, Color.DarkRed);
                                    DebugShapeDisplay.pInstance.AddPoint(intersect, 1, Color.Orange);
                                    // We have collide along the x axis.
                                    collideX = true;
                                    collidePointX = x * mTileWidth;
                                }
                            }
                            // If we are moving left, and we hit a tile with a right wall...
                            else if (dir.X < 0 &&
                                (mCollisionGrid[x, y].mActiveWalls & Tile.WallTypes.Right) != Tile.WallTypes.None)
                            {
                                mCollisionRectMovement.pPointA = mCollisionGrid[x, y].mCollisionRect.pCenterPoint;
                                mCollisionRectMovement.pPointB = endRect.pCenterPoint;

                                mCollisionGrid[x, y].mCollisionRect.GetRightEdge(ref mCollisionWall);

                                Vector2 intersect = new Vector2();

                                if (mCollisionWall.Intersects(mCollisionRectMovement, ref intersect))
                                {
                                    DebugShapeDisplay.pInstance.AddSegment(mCollisionRectMovement, Color.DarkRed);
                                    DebugShapeDisplay.pInstance.AddPoint(intersect, 1, Color.Orange);
                                    // We have collide along the x axis.
                                    collideX = true;
                                    collidePointX = x * mTileWidth + mTileWidth;
                                }
                            }
                            if (dir.Y > 0 &&
                                (mCollisionGrid[x, y].mActiveWalls & Tile.WallTypes.Top) != Tile.WallTypes.None)
                            {
                                mCollisionRectMovement.pPointA = mCollisionGrid[x, y].mCollisionRect.pCenterPoint;
                                mCollisionRectMovement.pPointB = endRect.pCenterPoint;

                                mCollisionGrid[x, y].mCollisionRect.GetTopEdge(ref mCollisionWall);

                                Vector2 intersect = new Vector2();

                                if (mCollisionWall.Intersects(mCollisionRectMovement, ref intersect))
                                {
                                    DebugShapeDisplay.pInstance.AddSegment(mCollisionRectMovement, Color.DarkRed);
                                    DebugShapeDisplay.pInstance.AddPoint(intersect, 1, Color.Orange);
                                    // We have collide along the y axis.
                                    collideY = true;
                                    collidePointY = y * mTileHeight;
                                }
                            }
                            else if (dir.Y < 0 &&
                                (mCollisionGrid[x, y].mActiveWalls & Tile.WallTypes.Bottom) != Tile.WallTypes.None)
                            {
                                mCollisionRectMovement.pPointA = mCollisionGrid[x, y].mCollisionRect.pCenterPoint;
                                mCollisionRectMovement.pPointB = endRect.pCenterPoint;

                                mCollisionGrid[x, y].mCollisionRect.GetBottomEdge(ref mCollisionWall);

                                Vector2 intersect = new Vector2();

                                if (mCollisionWall.Intersects(mCollisionRectMovement, ref intersect))
                                {
                                    DebugShapeDisplay.pInstance.AddSegment(mCollisionRectMovement, Color.DarkRed);
                                    DebugShapeDisplay.pInstance.AddPoint(intersect, 1, Color.Orange);
                                    // We have collide along the y axis.
                                    collideY = true;
                                    collidePointY = y * mTileHeight + mTileHeight;
                                }
                            }

#if ALLOW_GARBAGE
                            //DebugMessageDisplay.pInstance.AddDynamicMessage("Collide Dir: " + dir);
#endif

                            // Set the collision type temporarily to 2, to signal that it collided.
                            mCollisionGrid[x, y].mType |= Level.Tile.TileTypes.Collision;

                            // If we make it to this point there was a collision of some type.
                            hit = true;
                        }
                    }
                }
            }

            return hit;
        }
    }
}
