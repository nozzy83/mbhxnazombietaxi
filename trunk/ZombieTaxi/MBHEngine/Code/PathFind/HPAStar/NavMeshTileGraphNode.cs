using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MBHEngine.Behaviour;
using MBHEngine.PathFind.GenericAStar;

namespace MBHEngine.PathFind.HPAStar
{
    /// <summary>
    /// The NavMesh functions very similar to a regular Graph on TileGraphNode objects, except that
    /// a NavMesh doesn't care about things like Tile objects with are "Solid", or restricting diagonal
    /// movement along corners. Because of this more lax rules, we need create a special verison of 
    /// TileGraphNode which overrides the function which check that stuff.
    /// </summary>
    public class NavMeshTileGraphNode : MBHEngine.PathFind.GenericAStar.TileGraphNode
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="tile">The Tile at the location of this GraphNode.</param>
        public NavMeshTileGraphNode(Level.Tile tile)
            : base(tile)
        {
        }

        /// <summary>
        /// Check if this GraphNode can be passed; eg. is it solid of empty?
        /// </summary>
        /// <param name="startingNode">The node we are travelling from.</param>
        /// <returns>True if if the node can be travelled to.</returns>
        public override Boolean IsPassable(GraphNode startingNode)
        {
            // This Node would never have been created if it weren't passable.
            return true;
        }
    }
}
