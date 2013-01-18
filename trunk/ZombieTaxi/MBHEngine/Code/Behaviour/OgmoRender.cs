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

namespace MBHEngine.Behaviour
{
    /// <summary>
    /// The root of most content for the game.  Contains all the data about a particular area of the game.
    /// </summary>
    public class OgmoRender : MBHEngine.Behaviour.Behaviour
    {
        /// <summary>
        /// A single square of the world.  This seems to duplicate a lot of the logic of the 
        /// sprite behaviour so it should probably be converted over at some point.
        /// </summary>
        class Tile
        {
            /// <summary>
            /// This implementation of Levels is heavily based on the Ogmo editor, so
            /// we rely directly on some of its data structures.  This structure contains
            /// information and content for all the tilesets in this level.
            /// </summary>
            private OgmoTileset mTileSet;

            /// <summary>
            /// The position of this tile in world coordinates.
            /// </summary>
            public Vector2 mPosition;

            /// <summary>
            /// Rectangle defining where on the source texture this tile should render from.
            /// </summary>
            public Rectangle mSource;

            /// <summary>
            /// The texture used for this tile.
            /// </summary>
            public Texture2D mTexture;

            /// <summary>
            /// The colour applied to the texture.
            /// </summary>
            public Color mTint;

            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="tile">The tile as read in from the Ogmo library.</param>
            /// <param name="useSourceIndex">Not sure.</param>
            public Tile(OgmoTile tile, bool useSourceIndex)
            {
                mTint = Color.White;
                mPosition = tile.Position;
                mTileSet = tile.Tileset;
                mTexture = mTileSet.Texture;

                if (useSourceIndex)
                    mSource = mTileSet.Sources[tile.SourceIndex];
                else
                    mSource = new Rectangle(tile.TextureOffset.X,
                        tile.TextureOffset.Y,
                        mTileSet.TileWidth,
                        mTileSet.TileHeight);
            }
        };

        /// <summary>
        /// All the tiles that make up this level.  This includes multiple layers all concatinated into
        /// a single list.
        /// </summary>
        List<Tile> mTiles = new List<Tile>();

        /// <summary>
        /// Collision information for this level.
        /// </summary>
        Int32[,] mCollisionGrid;

        /// <summary>
        /// Constructor which also handles the process of loading in the Behaviour
        /// Definition information.
        /// </summary>
        /// <param name="parentGOH">The game object that this behaviour is attached to.</param>
        /// <param name="fileName">The file defining this behaviour.</param>
        public OgmoRender(GameObject.GameObject parentGOH, String fileName)
            : base(parentGOH, fileName)
        {
        }

        /// <summary>
        /// Call this to initialize a Behaviour with data supplied in a file.
        /// </summary>
        /// <param name="fileName">The file to load from.</param>
        public override void LoadContent(String fileName)
        {
            OgmoRenderDefinition def = GameObjectManager.pInstance.pContentManager.Load<OgmoRenderDefinition>(fileName);

            // Load the actual level file.
            OgmoLevel level = GameObjectManager.pInstance.pContentManager.Load<OgmoLevel>(def.mOgmoLevel);

            // Get the loaded grid data and use it for a collision layer.
            mCollisionGrid = level.GetLayer<OgmoGridLayer>(def.mGridLayer).RawData;

            // Loop through all the layers and add them to the tiles array.  Multiple layers get added to the same
            // List because they have no impact on each other; it's just a list of things to draw.
            for (Int32 i = 0; i < def.mTileLayers.Count; i++)
            {
                // Cache this stuff ahead of time to simplify code.
                Int32 count = level.GetLayer<OgmoTileLayer>(def.mTileLayers[i].mName).Tiles.Count<OgmoTile>();
                OgmoTile[] tiles = level.GetLayer<OgmoTileLayer>(def.mTileLayers[i].mName).Tiles;
                Boolean useIndex = def.mTileLayers[i].mUsesTextureIndex;

                for (Int32 j = 0; j < count; j++)
                {
                    mTiles.Add(new Tile(tiles[j], useIndex));
                }
            }

            base.LoadContent(fileName);
        }

        /// <summary>
        /// Called once render cycle by the game object manager.
        /// </summary>
        /// <param name="batch">The sprite batch to render to.</param>
        /// <param name="effect">The currently set shader.</param>
        public override void Render(SpriteBatch batch, Effect effect)
        {
            for (int i = 0; i < mTiles.Count; i++)
            {
                Tile tile = mTiles[i];
                batch.Draw(tile.mTexture, tile.mPosition, tile.mSource, tile.mTint);
            }
        }
    }
}
