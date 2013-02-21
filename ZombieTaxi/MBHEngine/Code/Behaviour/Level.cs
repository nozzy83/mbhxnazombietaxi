using System;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using MBHEngine.GameObject;
using MBHEngineContentDefs;
using MBHEngine.Debug;
using MBHEngine.Math;
using MBHEngine.Render;
using MBHEngine.PathFind.HPAStar;
using MBHEngine.PathFind.GenericAStar;

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
        public class GetCollisionInfoMessage : BehaviourMessage
        {
            /// <summary>
            /// Was a collision detected at all.
            /// </summary>
            public Boolean mCollisionDetected_Out;

            /// <summary>
            /// Was a collision detected n the X direction specifically.
            /// </summary>
            public Boolean mCollisionDetectedX_Out;

            /// <summary>
            /// Was a collision detect in the Y direction specifically.
            /// </summary>
            public Boolean mCollisionDetectedY_Out;

            /// <summary>
            /// At what point did the collision occur in the X (if one did occur).
            /// </summary>
            public Single mCollisionPointX_Out;

            /// <summary>
            /// At what point did the collision occur in the Y (if one did occur).
            /// </summary>
            public Single mCollisionPointY_Out;

            /// <summary>
            /// The collision volume representing when the object started at the begining of this movement.
            /// </summary>
            public MBHEngine.Math.Rectangle mOriginalRect_In;

            /// <summary>
            /// The collision volume representing where the object is trying to reach.
            /// </summary>
            public MBHEngine.Math.Rectangle mDesiredRect_In;

            /// <summary>
            /// Call this to put a message back to its default state.
            /// </summary>
            public override void Reset()
            {
                mCollisionDetected_Out = mCollisionDetectedX_Out = mCollisionDetectedY_Out = false;
                mCollisionPointX_Out = mCollisionPointY_Out = 0.0f;
                if (null != mOriginalRect_In)
                {
                    mOriginalRect_In.pCenterPoint = Vector2.Zero;
                }
                if (null != mDesiredRect_In)
                {
                    mDesiredRect_In.pCenterPoint = Vector2.Zero;
                }
            }
        };

        /// <summary>
        /// Finds the tile at the specified position.
        /// </summary>
        public class GetTileAtPositionMessage : BehaviourMessage
        {
            /// <summary>
            /// A position in world space to check against.
            /// </summary>
            public Vector2 mPosition_In;

            /// <summary>
            /// The tile which the position intersects, or null if there is not one.
            /// </summary>
            public Tile mTile_Out;

            /// <summary>
            /// Call this to put a message back to its default state.
            /// </summary>
            public override void Reset()
            {
                mPosition_In = Vector2.Zero;
                mTile_Out = null;
            }
        }

        /// <summary>
        /// Very similar to GetTileAtPositionMessage except that because this version takes a GameObject
        /// it can perform a more accurate search.
        /// </summary>
        public class GetTileAtObjectMessage : BehaviourMessage
        {
            /// <summary>
            /// The object which we want to know which tile it is standing it.
            /// </summary>
            public GameObject.GameObject mObject_In;

            /// <summary>
            /// The tile which mObject is standing it.
            /// </summary>
            public Tile mTile_Out;

            /// <summary>
            /// Call this to put a message back to its default state.
            /// </summary>
            public override void Reset()
            {
                mObject_In = null;
                mTile_Out = null;
            }
        }

        /// <summary>
        /// Set the type of a tile at a specific location. Updates surround tiles to make sure
        /// collision continues to work properly.
        /// </summary>
        public class SetTileTypeAtPositionMessage : BehaviourMessage
        {
            /// <summary>
            /// A position in world space to check against.
            /// </summary>
            public Vector2 mPosition_In;

            /// <summary>
            /// The type of tile to place here.
            /// </summary>
            public Tile.TileTypes mType_In;

            /// <summary>
            /// Upon return, this member will store the previous type of tile that was stored here.
            /// </summary>
            public Tile.TileTypes mPreviousType_Out;

            /// <summary>
            /// Call this to put a message back to its default state.
            /// </summary>
            public override void Reset()
            {
                mPosition_In = Vector2.Zero;
                mType_In = Tile.TileTypes.Empty;
                mPreviousType_Out = Tile.TileTypes.Empty;
            }
        }

        /// <summary>
        /// Retrives the struct containing a bunch of information about this level.
        /// </summary>
        public class GetMapInfoMessage : BehaviourMessage
        {
            /// <summary>
            /// Contains various pieces of information about this map.
            /// </summary>
            public MapInfo mInfo_Out;

            /// <summary>
            /// Call this to put a message back to its default state.
            /// </summary>
            public override void Reset()
            {
                mInfo_Out = null;
            }
        }

        /// <summary>
        /// Sent when the NavMesh managed by the Level becomes invalid.
        /// </summary>
        public class OnNavMeshInvalidatedMessage : BehaviourMessage
        {
            /// <summary>
            /// The location of change which resulted in the NavMesh being invalidated.
            /// eg. the location a tile was placed.
            /// </summary>
            public Vector2 mPosition_In;

            /// <summary>
            /// Call this to put a message back to its default state.
            /// </summary>
            public override void Reset()
            {
                mPosition_In = Vector2.Zero;
            }
        }

        /// <summary>
        /// Retrieves the NavMesh for this level.
        /// </summary>
        public class GetNavMeshMessage : BehaviourMessage
        {
            /// <summary>
            /// The NavMesh used on this Level.
            /// </summary>
            public NavMesh mNavMesh_Out;

            public override void Reset()
            {
            }
        }

        /// <summary>
        /// Data about a single tile.
        /// </summary>
        public class Tile
        {
            /// <summary>
            /// The different types of walls that a tile can have.  Basically the different sides.
            /// </summary>
            [Flags]
            public enum WallTypes
            {
                None    = 0,
                Top     = 1 << 0,
                Right   = 1 << 1,
                Bottom  = 1 << 2,
                Left    = 1 << 3,
            };

            /// <summary>
            /// The different types of tiles. 
            /// </summary>
            public enum TileTypes
            {
                Empty = 0,
                Solid = 1,
            };

            /// <summary>
            /// Attributes that this Tile has. Can be extended by engine.
            /// </summary>
            [Flags]
            public enum Attribute
            {
                None                = 0,
                Occupied            = 1 << 0,
                Collision           = 1 << 1,
                CollisionChecked    = 1 << 2,
            }

            /// <summary>
            /// Uses these enums to look up the adjecent tiles in mAdjecentTiles.
            /// </summary>
            public enum AdjacentTileDir
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

                START_HORZ = LEFT,
                START_DIAG = LEFT_UP,
            }

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
            /// The index into the tile map to use to render this tile.
            /// </summary>
            public Int32 mImageIndex;

            /// <summary>
            /// Bitfield discribing attributes for this tile.
            /// </summary>
            private Attribute mAttributes = Attribute.None;

            /// <summary>
            /// Tile and TileGraphNode objects have a 1:1 mapping.
            /// </summary>
            public GraphNode mGraphNode;

            /// <summary>
            /// Check if a particular Attribute is set.
            /// </summary>
            /// <param name="att">The attribute the check.</param>
            /// <returns>True if the attribute is set.</returns>
            public Boolean HasAttribute(Attribute att)
            {
                return (mAttributes & att) != 0;
            }

            /// <summary>
            /// Gives the Tile a particular Attribute.
            /// </summary>
            /// <param name="att">The Attribute to give this tile.</param>
            public void SetAttribute(Attribute att)
            {
                mAttributes |= att;
            }

            /// <summary>
            /// Removes a particular Attribute from this Tile. Ignored if not set.
            /// </summary>
            /// <param name="att"></param>
            public void ClearAttribute(Attribute att)
            {
                mAttributes &= ~(att);
            }
        }

        /// <summary>
        /// Stores a bunch of constant information about the level making it easier
        /// to send this information to other objects.
        /// </summary>
        public class MapInfo
        {
            /// <summary>
            /// The size of the map in tiles.
            /// </summary>
            public Int32 mMapWidth;

            /// <summary>
            /// The size of the map in tiles.
            /// </summary>
            public Int32 mMapHeight;

            /// <summary>
            /// The width of a single tile.
            /// </summary>
            public Int32 mTileWidth;

            /// <summary>
            /// The height of a single tile.
            /// </summary>
            public Int32 mTileHeight;

            /// <summary>
            /// Texture used for rendering the tile map.
            /// </summary>        
            public Texture2D mTileMap;
        }

        /// <summary>
        /// Collision information for this level.
        /// </summary>
        private Tile[,] mCollisionGrid;

        /// <summary>
        /// The defininition of this map.
        /// </summary>
        private MapInfo mMapInfo;

        /// <summary>
        /// Nav mesh used by HPAStar.
        /// </summary>
        private NavMesh mNavMesh;

        /// <summary>
        /// Search graph used by GenericAStar.
        /// </summary>
        private MBHEngine.PathFind.GenericAStar.Graph mGraph;

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
            base.LoadContent(fileName);

            LevelDefinition def = GameObjectManager.pInstance.pContentManager.Load<LevelDefinition>(fileName);

            mGraph = new MBHEngine.PathFind.GenericAStar.Graph();

            mMapInfo = new MapInfo();

            // TODO: This should be loaded in from the def.
            mMapInfo.mMapWidth = (Int32)def.mMapDimensions.X;
            mMapInfo.mMapHeight = (Int32)def.mMapDimensions.Y;
            mMapInfo.mTileWidth = (Int32)def.mTileDimensions.X;
            mMapInfo.mTileHeight = (Int32)def.mTileDimensions.Y;

            // Start by creating all the tiles for this level.
            mCollisionGrid = new Tile[mMapInfo.mMapWidth, mMapInfo.mMapHeight];
            for (Int32 y = 0; y < mMapInfo.mMapHeight; y++)
            {
                for (Int32 x = 0; x < mMapInfo.mMapWidth; x++)
                {
                    mCollisionGrid[x,y] = new Tile();

                    // Precalculate the adjecent tiles to this one.
                    //

                    // Allocate space for the Array itself.
                    mCollisionGrid[x, y].mAdjecentTiles = new Tile[(Int32)Tile.AdjacentTileDir.NUM_DIRECTIONS];

                    // Calculate the center point of the tile.
                    Vector2 cent = new Vector2((x * mMapInfo.mTileWidth) + (mMapInfo.mTileWidth * 0.5f), (y * mMapInfo.mTileHeight) + (mMapInfo.mTileHeight * 0.5f));

                    // Create a rectangle to represent the tile.
                    mCollisionGrid[x, y].mCollisionRect = new MBHEngine.Math.Rectangle(mMapInfo.mTileWidth, mMapInfo.mTileHeight, cent);

                    mCollisionGrid[x, y].mGraphNode = new TileGraphNode(mCollisionGrid[x, y]);

                    mGraph.AddNode(mCollisionGrid[x, y].mGraphNode);

                    // Start with the tiles left of this one, but avoid looking outside the map.
                    if (x > 0)
                    {
                        // Store a reference to the tile to the left.
                        mCollisionGrid[x, y].mAdjecentTiles[(Int32)Tile.AdjacentTileDir.LEFT] = mCollisionGrid[x - 1, y];

                        mCollisionGrid[x, y].mGraphNode.AddNeighbour(mCollisionGrid[x - 1, y].mGraphNode);

                        // Since the tile to the left was created before this one, it needs to be updated to point to this
                        // once as the tile on it right.
                        mCollisionGrid[x - 1, y].mAdjecentTiles[(Int32)Tile.AdjacentTileDir.RIGHT] = mCollisionGrid[x, y];

                        mCollisionGrid[x - 1, y].mGraphNode.AddNeighbour(mCollisionGrid[x, y].mGraphNode);

                        // Check up and to the left if that is not outside the map.
                        if (y > 0)
                        {
                            // Again, set ourselves and then the adjecent one which was created prior to us being
                            // created.
                            mCollisionGrid[x, y].mAdjecentTiles[(Int32)Tile.AdjacentTileDir.LEFT_UP] = mCollisionGrid[x - 1, y - 1];
                            mCollisionGrid[x, y].mGraphNode.AddNeighbour(mCollisionGrid[x - 1, y - 1].mGraphNode);
                            mCollisionGrid[x - 1, y - 1].mAdjecentTiles[(Int32)Tile.AdjacentTileDir.RIGHT_DOWN] = mCollisionGrid[x, y];
                            mCollisionGrid[x - 1, y - 1].mGraphNode.AddNeighbour(mCollisionGrid[x, y].mGraphNode);
                        }
                    }

                    // For tiles above.
                    if (y > 0)
                    {
                        // Set our up, their down.
                        mCollisionGrid[x, y].mAdjecentTiles[(Int32)Tile.AdjacentTileDir.UP] = mCollisionGrid[x, y - 1];
                        mCollisionGrid[x, y].mGraphNode.AddNeighbour(mCollisionGrid[x, y - 1].mGraphNode);
                        mCollisionGrid[x, y - 1].mAdjecentTiles[(Int32)Tile.AdjacentTileDir.DOWN] = mCollisionGrid[x, y];
                        mCollisionGrid[x, y - 1].mGraphNode.AddNeighbour(mCollisionGrid[x, y].mGraphNode);

                        // All that is left is the RIGHT_UP/LEFT_DOWN relationship.
                        if (x < mMapInfo.mMapWidth - 1)
                        {
                            mCollisionGrid[x, y].mAdjecentTiles[(Int32)Tile.AdjacentTileDir.RIGHT_UP] = mCollisionGrid[x + 1, y - 1];
                            mCollisionGrid[x, y].mGraphNode.AddNeighbour(mCollisionGrid[x + 1, y - 1].mGraphNode);
                            mCollisionGrid[x + 1, y - 1].mAdjecentTiles[(Int32)Tile.AdjacentTileDir.LEFT_DOWN] = mCollisionGrid[x, y];
                            mCollisionGrid[x + 1, y - 1].mGraphNode.AddNeighbour(mCollisionGrid[x, y].mGraphNode);
                        }
                    }

                    // Give it a random chance to be solid.
                    if (RandomManager.pInstance.RandomPercent() <= 0.05f)// && false)
                    {
                        mCollisionGrid[x, y].mType = Level.Tile.TileTypes.Solid;

                        GameObject.GameObject g;
                        
                        Single chance = (Single)RandomManager.pInstance.RandomPercent( );

                        if (chance < 0.2f)
                        {
                            g = GameObjectFactory.pInstance.GetTemplate("GameObjects\\Environments\\WallWood\\WallWood");
                        }
                        else if (chance < 0.4f)
                        {
                            g = GameObjectFactory.pInstance.GetTemplate("GameObjects\\Environments\\WallStone\\WallStone");
                        }
                        else if (chance < 0.6f)
                        {
                            g = GameObjectFactory.pInstance.GetTemplate("GameObjects\\Environments\\WallSteel\\WallSteel");
                        }
                        else if (chance < 0.8f)
                        {
                            g = GameObjectFactory.pInstance.GetTemplate("GameObjects\\Items\\GunTurret\\GunTurret");
                        }
                        else
                        {
                            g = GameObjectFactory.pInstance.GetTemplate("GameObjects\\Items\\Detector\\Detector");
                        }
                        g.pPosX =(x * mMapInfo.mTileWidth) + (mMapInfo.mTileWidth * 0.5f);
                        g.pPosY = (y * mMapInfo.mTileHeight) + (mMapInfo.mTileHeight * 0.5f);
                        GameObjectManager.pInstance.Add(g);
                    }

                    DetermineAndSetImage(mCollisionGrid[x, y]);
                }
            }
            
            // Loop through all the tiles and calculate which sides need to have collision checks done on it.
            // For example if a tile has another tile directly above it, it does not need to check collision 
            // on the top side, because it is not reachable.
            for (Int32 y = 0; y < mMapInfo.mMapHeight; y++)
            {
                for (Int32 x = 0; x < mMapInfo.mMapWidth; x++)
                {
                    if ((mCollisionGrid[x, y].mType & Level.Tile.TileTypes.Solid) == Tile.TileTypes.Solid)
                    {
                        if (y == 0 || (mCollisionGrid[x, y - 1].mType & Level.Tile.TileTypes.Solid) != Tile.TileTypes.Solid)
                            mCollisionGrid[x, y].mActiveWalls |= Tile.WallTypes.Top;
                        if (x == mMapInfo.mMapWidth - 1 || (mCollisionGrid[x + 1, y].mType & Level.Tile.TileTypes.Solid) != Tile.TileTypes.Solid)
                            mCollisionGrid[x, y].mActiveWalls |= Tile.WallTypes.Right;
                        if (y == mMapInfo.mMapHeight - 1 || (mCollisionGrid[x, y + 1].mType & Level.Tile.TileTypes.Solid) != Tile.TileTypes.Solid)
                            mCollisionGrid[x, y].mActiveWalls |= Tile.WallTypes.Bottom;
                        if (x == 0 || (mCollisionGrid[x - 1, y].mType & Level.Tile.TileTypes.Solid) != Tile.TileTypes.Solid)
                            mCollisionGrid[x, y].mActiveWalls |= Tile.WallTypes.Left;
                    }
                }
            }

            // Allocate these once and use them over and over again.
            mCollisionWall = new LineSegment();
            mCollisionRectMovement = new LineSegment();

            // Load an image to use for rendering the level.
            mMapInfo.mTileMap = GameObjectManager.pInstance.pContentManager.Load<Texture2D>
                (def.mTileMapImageName);

            // Instantiate the NavMesh, but wait to actually initialize it.
            mNavMesh = new NavMesh(5);

            // Let the GameObjectManager know that the level data has changed.
            GameObjectManager.pInstance.OnMapInfoChange(mMapInfo);
        }

        /// <summary>
        /// Called at the end of the frame where mParentGOH was added to the GameObjectManager.
        /// </summary>
        public override void OnAdd()
        {
            // Create the NavMesh after OnLoad because CreateNavMesh is going to try to call OnMessage
            // on this Level.
            mNavMesh.CreateNavMesh(mParentGOH);
        }

        /// <summary>
        /// Centralized place for logic in mapping a Tile state to a Tile image.
        /// </summary>
        /// <param name="tile">The tile to update.</param>
        private void DetermineAndSetImage(Tile tile)
        {
            // Give it a random chance to be solid.
            if (tile.mType == Tile.TileTypes.Solid)
            {
                // Solid tiles all use the same image.
                tile.mImageIndex = 1;
            }
            else
            {
                // We want empty tiles to mostly be one image with a low chance of being one of the
                // other tiles.
                if (RandomManager.pInstance.RandomPercent() > 0.9f)
                {
                    tile.mImageIndex = (RandomManager.pInstance.RandomNumber() % 2) + 3;
                }
                else
                {
                    tile.mImageIndex = 2; // Most will use this.
                }
            }
        }

        /// <summary>
        /// Helper function for UpdateEdgeCollisionData.
        /// </summary>
        /// <param name="t">The tile being updated.</param>
        /// <param name="dir">The direction to update the edge of.</param>
        /// <param name="type">The edge side (should match dir).</param>
        /// <returns>The tile that was adjacent which we checked.</returns>
        private Tile UpdateSingleEdge(Tile t, Tile.AdjacentTileDir dir, Tile.WallTypes type)
        {
            Tile adjTile = t.mAdjecentTiles[(Int32)dir];
            if (adjTile != null)
            {
                if (t.mType == Tile.TileTypes.Solid)
                {
                    if (adjTile.mType != Tile.TileTypes.Solid)
                    {
                        t.mActiveWalls |= type;
                    }
                }
            }

            return adjTile;
        }

        /// <summary>
        /// When a tile changes collision states, the edges need to be recalculated.
        /// </summary>
        /// <param name="t">The tile to update.</param>
        /// <param name="updateAdjacent">True if the adjacent tiles should be updated as well.</param>
        private void UpdateEdgeCollisionData(Tile t, Boolean updateAdjacent = true)
        {
            // Fail said to make recursive calls a little cleaner (don't need to check for a bunch 
            // of nulls).
            if (t == null)
            {
                return;
            }

            // Start with no edges,
            t.mActiveWalls = 0;

            // Update each edge based on the surrounding tiles.
            Tile left = UpdateSingleEdge(t, Tile.AdjacentTileDir.LEFT, Tile.WallTypes.Left);
            Tile up = UpdateSingleEdge(t, Tile.AdjacentTileDir.UP, Tile.WallTypes.Top);
            Tile down = UpdateSingleEdge(t, Tile.AdjacentTileDir.DOWN, Tile.WallTypes.Bottom);
            Tile right = UpdateSingleEdge(t, Tile.AdjacentTileDir.RIGHT, Tile.WallTypes.Right);

            if (true == updateAdjacent)
            {
                // If this was the tile that changed, the surrounding tiles likely need to
                // be updated because the edge touching this tile has changed. However,
                // the tiles adjacent to THEM do not need to be updated.
                UpdateEdgeCollisionData(left, false);
                UpdateEdgeCollisionData(up, false);
                UpdateEdgeCollisionData(down, false);
                UpdateEdgeCollisionData(right, false);
            }
        }

        /// <summary>
        /// Called once render cycle by the game object manager.
        /// </summary>
        /// <param name="batch">The sprite batch to render to.</param>
        /// <param name="effect">The currently set shader.</param>
        public override void Render(SpriteBatch batch, Effect effect)
        {
            //Vector2 playerPos = GameObjectManager.pInstance.pPlayer.pPosition;
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

            // Only render tiles that are within the view area to save significant rendering time.
            // Convert the camera's view rectangle into tile indexes (by deviding by tile size).
            // Add +1 for the outsides to account for the fact that this is based on the left size
            // of the tile.
            //
            MBHEngine.Math.Rectangle view = CameraManager.pInstance.pViewRect;
            Int32 startX = (Int32)MathHelper.Max((Int32)view.pLeft / mMapInfo.mTileWidth, 0);
            Int32 endX = (Int32)MathHelper.Min((Int32)view.pRight / mMapInfo.mTileWidth + 1, mMapInfo.mMapWidth);
            Int32 startY = (Int32)MathHelper.Max((Int32)view.pTop / mMapInfo.mTileHeight, 0);
            Int32 endY = (Int32)MathHelper.Min((Int32)view.pBottom / mMapInfo.mTileHeight + 1, mMapInfo.mMapHeight);

            // Loop through every tile on screen.
            for (Int32 y = startY; y < endY; y++)
            {
                for (Int32 x = startX; x < endX; x++)
                {
                    // Is this tile solid?
                    if (mCollisionGrid[x, y].mType != Level.Tile.TileTypes.Empty)
                    {
                        // If a collision was detected on it, render it red.
                        if(mCollisionGrid[x, y].HasAttribute(Tile.Attribute.Collision))
                            DebugShapeDisplay.pInstance.AddAABB(mCollisionGrid[x, y].mCollisionRect, Color.Red);
                        // If a collision was even checked for, render it Orange.
                        else if (mCollisionGrid[x, y].HasAttribute(Tile.Attribute.CollisionChecked))
                            DebugShapeDisplay.pInstance.AddAABB(mCollisionGrid[x, y].mCollisionRect, Color.OrangeRed);
                        else
                            DebugShapeDisplay.pInstance.AddAABB(mCollisionGrid[x, y].mCollisionRect, Color.Black);

                        // Clear the temp bits used for rendering collision info.
                        // TODO: This is not being cleared for tiles not on screen. Does that matter?
                        mCollisionGrid[x, y].ClearAttribute(Tile.Attribute.Collision | Tile.Attribute.CollisionChecked);

                        // Render the tile image.
                        batch.Draw(
                            mMapInfo.mTileMap,
                            new Microsoft.Xna.Framework.Rectangle(
                                x * mMapInfo.mTileWidth, y * mMapInfo.mTileHeight, mMapInfo.mTileWidth, mMapInfo.mTileHeight),
                            new Microsoft.Xna.Framework.Rectangle(
                                mCollisionGrid[x, y].mImageIndex * mMapInfo.mTileWidth, 0, mMapInfo.mTileWidth, mMapInfo.mTileHeight),
                            Color.White);

                        // Draw the walls that have been determined to require collision checks.
                        //
                        if ((mCollisionGrid[x, y].mActiveWalls & Tile.WallTypes.Top) != Tile.WallTypes.None)
                        {
                            DebugShapeDisplay.pInstance.AddSegment(new Vector2(x * mMapInfo.mTileWidth, y * mMapInfo.mTileHeight),
                                                                   new Vector2(x * mMapInfo.mTileWidth + mMapInfo.mTileWidth, y * mMapInfo.mTileHeight),
                                                                   Color.Red);
                        }
                        if ((mCollisionGrid[x, y].mActiveWalls & Tile.WallTypes.Right) != Tile.WallTypes.None)
                        {
                            DebugShapeDisplay.pInstance.AddSegment(new Vector2(x * mMapInfo.mTileWidth + mMapInfo.mTileWidth, y * mMapInfo.mTileHeight),
                                                                   new Vector2(x * mMapInfo.mTileWidth + mMapInfo.mTileWidth, y * mMapInfo.mTileHeight + mMapInfo.mTileHeight),
                                                                   Color.Red);
                        }
                        if ((mCollisionGrid[x, y].mActiveWalls & Tile.WallTypes.Bottom) != Tile.WallTypes.None)
                        {
                            DebugShapeDisplay.pInstance.AddSegment(new Vector2(x * mMapInfo.mTileWidth, y * mMapInfo.mTileHeight + mMapInfo.mTileHeight),
                                                                   new Vector2(x * mMapInfo.mTileWidth + mMapInfo.mTileWidth, y * mMapInfo.mTileHeight + mMapInfo.mTileHeight),
                                                                   Color.Red);
                        }
                        if ((mCollisionGrid[x, y].mActiveWalls & Tile.WallTypes.Left) != Tile.WallTypes.None)
                        {
                            DebugShapeDisplay.pInstance.AddSegment(new Vector2(x * mMapInfo.mTileWidth, y * mMapInfo.mTileHeight),
                                                                   new Vector2(x * mMapInfo.mTileWidth, y * mMapInfo.mTileHeight + mMapInfo.mTileHeight),
                                                                   Color.Red);
                        }
                    }
                    else
                    {
                        if (mCollisionGrid[x, y].HasAttribute(Tile.Attribute.Occupied))
                        {
                            DebugShapeDisplay.pInstance.AddAABB(mCollisionGrid[x, y].mCollisionRect, Color.Pink);
                        }

                        // Render the empty tile.
                        batch.Draw(
                            mMapInfo.mTileMap,
                            new Microsoft.Xna.Framework.Rectangle(
                                x * mMapInfo.mTileWidth, y * mMapInfo.mTileHeight, mMapInfo.mTileWidth, mMapInfo.mTileHeight),
                            new Microsoft.Xna.Framework.Rectangle(
                                mCollisionGrid[x, y].mImageIndex * mMapInfo.mTileWidth, 0, mMapInfo.mTileWidth, mMapInfo.mTileHeight),
                            Color.White);
                    }
                }
            }

            mNavMesh.DebugDraw(true);
            //mGraph.DebugDraw(false);
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
            if (msg is Level.GetCollisionInfoMessage)
            {
                Level.GetCollisionInfoMessage temp = (Level.GetCollisionInfoMessage)msg;
                temp.mCollisionDetected_Out = CheckForCollision(temp.mOriginalRect_In,
                                                            temp.mDesiredRect_In,
                                                            out temp.mCollisionDetectedX_Out,
                                                            out temp.mCollisionDetectedY_Out,
                                                            out temp.mCollisionPointX_Out,
                                                            out temp.mCollisionPointY_Out);
                msg = temp;
            }
            else if (msg is GetTileAtPositionMessage)
            {
                GetTileAtPositionMessage temp = (GetTileAtPositionMessage)msg;

                temp.mTile_Out = GetTileAtPosition(temp.mPosition_In.X, temp.mPosition_In.Y);

                msg = temp;
            }
            else if (msg is GetTileAtObjectMessage)
            {
                GetTileAtObjectMessage temp = (GetTileAtObjectMessage)msg;

                temp.mTile_Out = GetTileAtObject(temp.mObject_In);

                msg = temp;
            }
            else if (msg is SetTileTypeAtPositionMessage)
            {
                SetTileTypeAtPositionMessage temp = (SetTileTypeAtPositionMessage)msg;

                Tile t = GetTileAtPosition(temp.mPosition_In.X, temp.mPosition_In.Y);

                temp.mPreviousType_Out = t.mType;

                if (t.mType != temp.mType_In)
                {
                    t.mType = temp.mType_In;

                    DetermineAndSetImage(t);

                    UpdateEdgeCollisionData(t);

                    mNavMesh.RegenerateCluster(temp.mPosition_In);
                }
            }
            else if (msg is GetMapInfoMessage)
            {
                GetMapInfoMessage temp = (GetMapInfoMessage)msg;

                temp.mInfo_Out = mMapInfo;

                msg = temp;
            }
            else if (msg is OnNavMeshInvalidatedMessage)
            {
                OnNavMeshInvalidatedMessage temp = (OnNavMeshInvalidatedMessage)msg;
                mNavMesh.RegenerateCluster(temp.mPosition_In);
            }
            else if (msg is GetNavMeshMessage)
            {
                GetNavMeshMessage temp = (GetNavMeshMessage)msg;
                temp.mNavMesh_Out = mNavMesh;
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
            Int32 xIndex = (Int32)(x / mMapInfo.mTileWidth);
            Int32 yIndex = (Int32)(y / mMapInfo.mTileHeight);

            if (xIndex < 0 || xIndex >= mMapInfo.mMapWidth || yIndex < 0 || yIndex >= mMapInfo.mMapHeight)
            {
                return null;
            }
            else
            {
                return mCollisionGrid[xIndex, yIndex];
            }
        }

        /// <summary>
        /// Finds the tile that an Object is currently standing on. It starts by trying to use the
        /// position, which incorperates the motion root, but if that is directly on the edge of a
        /// tile, it could give unexpected results. So in that case, we fall back to the center point
        /// of the object's collision volume.
        /// </summary>
        /// <param name="obj">The GameObject who we want to know which Tile it is standing in.</param>
        /// <returns>The tile that obj is standing in/</returns>
        private Tile GetTileAtObject(GameObject.GameObject obj)
        {
            // By default just use the position values.
            float x = obj.pPosX;
            float y = obj.pPosY;

            // If the object is standing right on an edge the tile it chooses might be incorrect (it
            // will always shift up/left). So for instance, if the motion root of the object is right at
            // the bottom of the object, visually it will be standing in the upper tile, but our logic 
            // will place it in the bottom tile. In those cases, default to the collision volumes position.
            if ((int)x % mMapInfo.mTileWidth == 0)
            {
                x = obj.pCollisionRect.pCenterPoint.X;
            }
            if ((int)y % mMapInfo.mTileHeight == 0)
            {
                y = obj.pCollisionRect.pCenterPoint.Y;
            }

            System.Diagnostics.Debug.Assert((int)x % mMapInfo.mTileWidth != 0, "X position is still directly inbetween 2 tiles. Will choose tile to left by default even if not visually accurate.");
            System.Diagnostics.Debug.Assert((int)y % mMapInfo.mTileHeight != 0, "Y position is still directly inbetween 2 tiles. Will choose tile above by default even if not visually accurate.");
            
            return GetTileAtPosition(x, y);
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
            Single x2 = (endRect.pCenterPoint.X / mMapInfo.mTileWidth) - ((Single)checkRange * 0.5f);
            Int32 startX = System.Math.Max((Int32)System.Math.Round(x2), 0);

            Single y2 = (endRect.pCenterPoint.Y / mMapInfo.mTileHeight) - ((Single)checkRange * 0.5f);
            Int32 startY = System.Math.Max((Int32)System.Math.Round(y2), 0);

            // Loop through every time checking for collisions.
            for (Int32 y = startY; y < mMapInfo.mMapHeight && y < startY + checkRange; y++)
            {
                for (Int32 x = startX; x < mMapInfo.mMapWidth && x < startX + checkRange; x++)
                {

                    // Is this tile solid and does it have any active walls?
                    // It may be solid with no walls in the case of one completly surrounded.
                    if (mCollisionGrid[x, y].mType != Level.Tile.TileTypes.Empty && mCollisionGrid[x, y].mActiveWalls != Tile.WallTypes.None)
                    {
                        // This tile has been considered for a collision.  It will be changed to type 2 if there is a
                        // collision.
                        mCollisionGrid[x, y].SetAttribute(Tile.Attribute.CollisionChecked);

                        // Calculate the center point of the tile.
                        Vector2 cent = new Vector2((x * mMapInfo.mTileWidth) + (mMapInfo.mTileWidth * 0.5f), (y * mMapInfo.mTileHeight) + (mMapInfo.mTileHeight * 0.5f));

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
                                    collidePointX = x * mMapInfo.mTileWidth;
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
                                    collidePointX = x * mMapInfo.mTileWidth + mMapInfo.mTileWidth;
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
                                    collidePointY = y * mMapInfo.mTileHeight;
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
                                    collidePointY = y * mMapInfo.mTileHeight + mMapInfo.mTileHeight;
                                }
                            }

#if ALLOW_GARBAGE
                            //DebugMessageDisplay.pInstance.AddDynamicMessage("Collide Dir: " + dir);
#endif

                            // Set the collision type temporarily to 2, to signal that it collided.
                            mCollisionGrid[x, y].SetAttribute(Tile.Attribute.Collision);

                            // If we make it to this point there was a collision of some type.
                            hit = true;
                        }
                    }
                }
            }

            return hit;
        }

         /// <summary>
        /// Checks in an adjecent tile is solid.  Safely avoids cases where their is no adjecent tile.
        /// </summary>
        /// <param name="dir">The direction to check in.</param>
        /// <param name="rootTile">The tile to move from.</param>
        /// <returns>True if the adjecent tile exists and is solid.</returns>
        static private Boolean IsTileInDirectionSolid(Level.Tile.AdjacentTileDir dir, Level.Tile rootTile)
        {
            return (rootTile.mAdjecentTiles[(Int32)dir] != null && rootTile.mAdjecentTiles[(Int32)dir].mType != Level.Tile.TileTypes.Empty);
        }

        /// <summary>
        /// When choosing a path we want to avoid clipping the edges of solid tiles. For example if you are
        /// going LEFT_DOWN, there should be no solid tile LEFT or DOWN or else the character would clip
        /// into them.
        /// It also avoids the problem where the path slips between kitty-cornered tiles.
        /// </summary>
        /// <param name="dir">The direction we want to move.</param>
        /// <param name="rootTile">The tile we are moving from.</param>
        /// <returns>True if this is an invalid move.</returns>
        static public Boolean IsAttemptingInvalidDiagonalMove(Level.Tile.AdjacentTileDir dir, Level.Tile rootTile)
        {
            switch ((Int32)dir)
            {
            // The path wants to move down and to the left...
            case (Int32)Level.Tile.AdjacentTileDir.LEFT_DOWN:
                {
                    // But it should only do so if their are no solid tiles to the left and
                    // no solid tiles below.  If there are, it needs to find another way round.
                    if (IsTileInDirectionSolid(Level.Tile.AdjacentTileDir.LEFT, rootTile) ||
                        IsTileInDirectionSolid(Level.Tile.AdjacentTileDir.DOWN, rootTile))
                    {
                        return true;
                    }
                    break;
                }
            case (Int32)Level.Tile.AdjacentTileDir.LEFT_UP:
                {
                    if (IsTileInDirectionSolid(Level.Tile.AdjacentTileDir.LEFT, rootTile) ||
                        IsTileInDirectionSolid(Level.Tile.AdjacentTileDir.UP, rootTile))
                    {
                        return true;
                    }
                    break;
                }
            case (Int32)Level.Tile.AdjacentTileDir.RIGHT_DOWN:
                {
                    if (IsTileInDirectionSolid(Level.Tile.AdjacentTileDir.RIGHT, rootTile) ||
                        IsTileInDirectionSolid(Level.Tile.AdjacentTileDir.DOWN, rootTile))
                    {
                        return true;
                    }
                    break;
                }
            case (Int32)Level.Tile.AdjacentTileDir.RIGHT_UP:
                {
                    if (IsTileInDirectionSolid(Level.Tile.AdjacentTileDir.RIGHT, rootTile) ||
                        IsTileInDirectionSolid(Level.Tile.AdjacentTileDir.UP, rootTile))
                    {
                        return true;
                    }
                    break;
                }
            };

            return false;
        }
    }
}
