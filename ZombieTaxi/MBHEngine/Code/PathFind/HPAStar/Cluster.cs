﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MBHEngine.PathFind.GenericAStar;
using MBHEngine.Behaviour;
using Microsoft.Xna.Framework;

namespace MBHEngine.PathFind.HPAStar
{
    /// <summary>
    /// For HPAStar a Graph needs to be broken up into smaller sub-sections called 'Clusters'. A Cluster
    /// calculate all the entrances/exits in the cluster and links them to entrances/exits in the neighbouring
    /// Cluster.
    /// </summary>
    public class Cluster : Graph
    {
        /// <summary>
        /// Clusters are connected by the 4 basic directions.
        /// </summary>
        public enum AdjacentClusterDirections
        {
            Left = 0,
            Up,
            Right,
            Down,
        }

        /// <summary>
        /// The top left most tile in the cluster. A starting point for walking through the
        /// tiles in the cluster.
        /// </summary>
        private Level.Tile mTopLeft;
		
        /// <summary>
        /// Clusters are connected to their neighbours left, right, up and down.
        /// </summary>
        private Cluster[] mNeighbours;

        /// <summary>
        /// A Rectangle defining the bounds of this Cluster.
        /// </summary>
        private MBHEngine.Math.Rectangle mBounds;

        /// <summary>
        /// How many tiles along each wall of a Cluster. Assumes square tiles.
        /// </summary>
        private Int32 mClusterSize;

        /// <summary>
        /// Cache the size of Level.Tile objects on this map so that we don't need to keep
        /// asking the Level for that information.
        /// </summary>
        private Point mTileDimensions;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="clusterSize">How many tiles along each wall of a Cluster. Assumes square tiles.</param>
        /// <param name="tileWidth">The width in pixels of a single Tile in this Level.</param>
        /// <param name="tileHeight">The height in pixels of a single Tile in this Level.</param>
        public Cluster(Int32 clusterSize, Int32 tileWidth, Int32 tileHeight)
            : base()
        {
            mClusterSize = clusterSize;

            mTileDimensions = new Point(tileWidth, tileHeight);

            mBounds = new Math.Rectangle(tileWidth * clusterSize, tileHeight * clusterSize);

            mNeighbours = new Cluster[Enum.GetNames(typeof(AdjacentClusterDirections)).Length];
        }

        /// <summary>
        /// Doesn't actually do anthing different in Release, but in Debug it does some checks for 
        /// duplicate entries. Doing this for all Graph objects would be too expensive even for testing.
        /// </summary>
        /// <param name="node">The node to add.</param>
        public override void AddNode(GraphNode node)
        {
            /*
            for(Int32 i = 0; i < pNodes.Count; i++)
            {
                if (pNodes[i].pPosition == node.pPosition)
                {
                    //System.Diagnostics.Debug.Assert(false, "Attempting to add GraphNode at dupe position.");
                }

                if (pNodes.Contains(node))
                {
                    //System.Diagnostics.Debug.Assert(false, "Attempting to add Dupe GraphNode.");
                }
            }
            */

            base.AddNode(node);
        }

        /// <summary>
        /// Checks if a Tile is inside of this cluster (at all).
        /// </summary>
        /// <param name="tile">The Tile to check for.</param>
        /// <returns>True if tile is inside this cluster.</returns>
        public Boolean IsInBounds(Level.Tile tile)
        {
            return (mBounds.Intersects(tile.mCollisionRect));
        }

        /// <summary>
        /// Is a particular Tile already part of this Cluster, meaning that a GraphNode in this
        /// Cluster is wrapping that tile. If it is, then that GraphNode gets returned so that it
        /// can be reused.
        /// </summary>
        /// <param name="tile">The tile to check for.</param>
        /// <returns>The GraphNode in this Cluster which wraps the given Tile.</returns>
        public GraphNode GetNodeContaining(Level.Tile tile)
        {
            // Just loop through every GraphNode looking for one that is storing the Tile.
            for (Int32 i = 0; i < pNodes.Count; i++)
            {
                if (pNodes[i].pData as Level.Tile == tile)
                {
                    if (!(pNodes[i] as NavMeshTileGraphNode).pIsTemporary)
                    {
                        return pNodes[i];
                    }
                }
                else if (pNodes[i].pPosition == tile.mCollisionRect.pCenterPoint)
                {
                    System.Diagnostics.Debug.Assert(false, "Tile at same position but claiming to be different.");
                }
            }

            return null;
        }

        /// <summary>
        /// Access to the Tile at the top left of this cluster.
        /// </summary>
        public Level.Tile pTopLeft
        {
            get
            {
                return mTopLeft;
            }
            set
            {
                mTopLeft = value;

                mBounds.pTopLeft = mTopLeft.mCollisionRect.pTopLeft;
            }
        }

        /// <summary>
        /// Access to a Rectangle defining the size and poistion of this cluster.
        /// </summary>
        public MBHEngine.Math.Rectangle pBounds
        {
            get
            {
                return mBounds;
            }
        }

        /// <summary>
        /// Access to the chached information about the dimension of Tile objects on this map.
        /// </summary>
        public Point pTileDimensions
        {
            get
            {
                return mTileDimensions;
            }
        }

        /// <summary>
        /// Access to the other Cluster objects that neighbour this one.
        /// </summary>
        public Cluster[] pNeighbouringClusters
        {
            get
            {
                return mNeighbours;
            }
        }
    }
}
