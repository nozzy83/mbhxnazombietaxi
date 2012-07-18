using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace MBHEngineContentDefs
{
    public class LevelDefinition : BehaviourDefinition
    {
        /// <summary>
        /// The number of tiles defining the dimensions of this level.
        /// </summary>
        public Vector2 mMapDimensions;

        /// <summary>
        /// The dimensions of a single Tile.
        /// </summary>
        public Vector2 mTileDimensions;

        /// <summary>
        /// The texture to use for rendering the level's tilemap.
        /// </summary>
        public String mTileMapImageName;
    }
}
