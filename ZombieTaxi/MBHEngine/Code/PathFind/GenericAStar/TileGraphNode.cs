using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MBHEngine.Behaviour;
using Microsoft.Xna.Framework;

namespace MBHEngine.PathFind.GenericAStar
{
    public class TileGraphNode : GraphNode
    {
        /// <summary>
        /// Every node in the graph was created to map to a tile in the Level. This is the 
        /// Tile that this GraphNode maps to.
        /// </summary>
        private Level.Tile mTile;

        /// <summary>
        /// Constructor.
        /// </summary>
        public TileGraphNode()
            : base()
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="tile">The Tile at this Node's location in the world.</param>
        public TileGraphNode(Level.Tile tile)
            : base()
        {
            mTile = tile;
        }

        /// <summary>
        /// Put the Node back into a default state.
        /// </summary>
        public override void Reset()
        {
            mTile = null;

            base.Reset();
        }

        /// <summary>
        /// Check if this GraphNode can be passed; eg. is it solid of empty?
        /// </summary>
        /// <param name="startingNode">The node we are travelling from.</param>
        /// <returns>True if if the node can be travelled to.</returns>
        public override Boolean IsPassable(GraphNode startingNode)
        {
            // If this GraphNode is not storing a tile, or that Tile is not empty, thats
            // an instant fail.
            if (!IsEmpty())
            {
                return false;
            }

            // We should not have mismatch GraphNode objects, so pData should be Level.Tile in this case.
            Level.Tile tile = startingNode.pData as Level.Tile;

            // Loop through all adjacent tiles to figure out which direction we are travelling in.
            // We need that data in order to determine if this is a legal diagonal move.
            for (Int32 i = 0; i < tile.mAdjecentTiles.Length; i++)
            {
                if (mTile == tile.mAdjecentTiles[i])
                {
                    if (Level.IsAttemptingInvalidDiagonalMove((Level.Tile.AdjacentTileDir)i, tile))
                    {
                        return false;
                    }
                    else
                    {
                        // No need to continue searching.
                        break;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Checks if this Node is "Empty" meaning, is something occupying this space.
        /// </summary>
        /// <returns></returns>
        public override Boolean IsEmpty()
        {
            return (mTile != null && mTile.mType == Level.Tile.TileTypes.Empty);
        }

        /// <summary>
        /// Where does this GraphNode sit in world space?
        /// </summary>
        public override Vector2 pPosition
        {
            get
            {
                //System.Diagnostics.Debug.Assert(mTile != null, "Accessing unset pData. Returning default value.");

                if (mTile != null)
                {
                    return mTile.mCollisionRect.pCenterPoint;
                }
                else
                {
                    return Vector2.Zero;
                }
            }
        }

        /// <summary>
        /// Access to the "Data" stored in this Node.
        /// </summary>
        public override object pData
        {
            get 
            {
                return mTile;
            }
            set
            {
                mTile = value as Level.Tile;
            }
        }
    }
}
