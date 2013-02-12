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
        /// 
        /// </summary>
        /// <param name="tile"></param>
        public TileGraphNode(Level.Tile tile) : base()
        {
            mTile = tile;
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
            if (mTile == null || mTile.mType != Level.Tile.TileTypes.Empty)
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
                return mTile.mCollisionRect.pCenterPoint;
            }
        }

        public override object pData
        {
            get 
            {
                return mTile;
            }
        }
    }
}
