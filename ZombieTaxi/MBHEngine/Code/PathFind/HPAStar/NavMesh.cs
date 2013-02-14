using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MBHEngine.Behaviour;
using MBHEngine.World;
using MBHEngine.Debug;
using Microsoft.Xna.Framework;
using MBHEngine.PathFind.GenericAStar;
using MBHEngine.Render;

namespace MBHEngine.PathFind.HPAStar
{
    /// <summary>
    /// Navigation mesh used for Near Perfect Hierarchial Path-Finding A*. It breaks the Graph into
    /// Clusters of nodes, and tracks the connection points between neighbouring clusters.
    /// </summary>
    public class NavMesh : Graph
    {
        /// <summary>
        /// The number of Tile objects that make up a single wall of a cluster. Assumes square clusters.
        /// </summary>
        private Int32 mClusterSize;

        /// <summary>
        /// Array of all the clusters in this Graph indexed by X, Y value.
        /// </summary>
        private Cluster[,] mClusters;

        /// <summary>
        /// Preallocated messages to avoid GC.
        /// </summary>
        private Level.GetMapInfoMessage mGetMapInfoMsg;
        private Level.GetTileAtPositionMessage mGetTileAtPositionMsg;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="clusterSize">
        /// The number of Tile objects that make up a single wall of a cluster. Assumes square clusters.
        /// </param>
        public NavMesh(Int32 clusterSize)
            : base()
        {
            mClusterSize = clusterSize;

            mGetMapInfoMsg = new Level.GetMapInfoMessage();
            mGetTileAtPositionMsg = new Level.GetTileAtPositionMessage();
        }

        /// <summary>
        /// Steps through all Tile objects in the level and generates a nave mesh from it.
        /// </summary>
        /// <param name="level"></param>
        public void CreateNavMesh(GameObject.GameObject level)
        {
            level.OnMessage(mGetMapInfoMsg);

            Int32 mapWidth = mGetMapInfoMsg.mInfo_Out.mMapWidth;
            Int32 mapHeight = mGetMapInfoMsg.mInfo_Out.mMapHeight;

            Int32 tileWidth = mGetMapInfoMsg.mInfo_Out.mTileWidth;
            Int32 tileHeight = mGetMapInfoMsg.mInfo_Out.mTileHeight;

            mClusters = new Cluster[mapWidth / mClusterSize, mapHeight / mClusterSize];

            // Loop through Cluster by Cluster initializing them.
            for (Int32 y = 0; y < mClusters.GetLength(1); y++)
            {
                for (Int32 x = 0; x < mClusters.GetLength(0); x++)
                {
                    Cluster temp = new Cluster(mClusterSize, tileWidth, tileHeight);

                    mGetTileAtPositionMsg.mPosition_In.X = x * (tileWidth * mClusterSize);
                    mGetTileAtPositionMsg.mPosition_In.Y = y * (tileHeight * mClusterSize);

                    level.OnMessage(mGetTileAtPositionMsg);

                    // The top left tile becomes a handy spot to start iterations over all Tile objects
                    // in a Cluster.
                    temp.pTopLeft = mGetTileAtPositionMsg.mTile_Out;

                    // Link Left <-> Right
                    if (x > 0)
                    {
                        temp.pNeighbouringClusters[(Int32)Cluster.AdjacentClusterDirections.Left] = mClusters[x - 1, y];
                        mClusters[x - 1, y].pNeighbouringClusters[(Int32)Cluster.AdjacentClusterDirections.Right] = temp;
                    }

                    // Link Up <-> Down
                    if (y > 0)
                    {
                        temp.pNeighbouringClusters[(Int32)Cluster.AdjacentClusterDirections.Up] = mClusters[x, y - 1];
                        mClusters[x, y - 1].pNeighbouringClusters[(Int32)Cluster.AdjacentClusterDirections.Down] = temp;
                    }

                    // Only walk the top and left walls for each cluster. The neighbouring clusters will do the same
                    // and as a result all walls will have been evaluated.
                    WalkWall(temp, temp.pTopLeft, null, Level.Tile.AdjacentTileDir.RIGHT, Level.Tile.AdjacentTileDir.UP, Cluster.AdjacentClusterDirections.Up);
                    WalkWall(temp, temp.pTopLeft, null, Level.Tile.AdjacentTileDir.DOWN, Level.Tile.AdjacentTileDir.LEFT, Cluster.AdjacentClusterDirections.Left);

                    // Store the cluster in the array index relative to its position in the world.
                    mClusters[x, y] = temp;
                }
            }

            // At this point all intra-connections have been made (cluster crossing connections), so now we need to
            // make all inter-connections (nodes linked inside of a cluster). We have to wait till now since the WallWalk 
            // done above only does 2 sides at a time.
            for (Int32 y = 0; y < mClusters.GetLength(1); y++)
            {
                for (Int32 x = 0; x < mClusters.GetLength(0); x++)
                {
                    Cluster temp = mClusters[x, y];

                    // Iterate throught the nodes of a Cluster 2 at a time, linking each node with all
                    // the nodes that follow it (and back), so by the end of the loop everyone should be
                    // linked to each other.
                    // eg.  A <-> BCD
                    //      B <-> CD
                    //      C <-> D
                    for (Int32 i = 0; i < temp.pGraphNodes.Count; i++)
                    {
                        for (Int32 j = i + 1; j < temp.pGraphNodes.Count; j++)
                        {
                            /// <todo>
                            /// A* search to make sure these can connect and get a real cost.
                            /// </todo>
                            /// 

                            // Link the two neightbours.
                            temp.pGraphNodes[i].AddNeighbour(temp.pGraphNodes[j]);
                            temp.pGraphNodes[j].AddNeighbour(temp.pGraphNodes[i]);
                        }

                        // Storing the GraphNode objects in the Cluster is just to make this a little eaiser, but
                        // for the PathPlanner to work, all the GraphNode data needs to be in this Graph.
                        AddNode(temp.pGraphNodes[i]);
                    }
                }
            }
        }

        /// <summary>
        /// Recursive algorithm for finding all entraces/exits in a cluster, and creating GraphNodes at those points
        /// in neighbouring Clusters and linking them together. Has logic to only create on entrance/exit per concurent
        /// set of empty tiles.
        /// </summary>
        /// <param name="cluster">The cluster we are evaluating. Hitting a Tile outside this cluster will end that wall.</param>
        /// <param name="tile">The current tile to evaluate.</param>
        /// <param name="sequeceStart">The tile which started this sequence. Use null to start.</param>
        /// <param name="dirWalk">The direction to walk along the wall.</param>
        /// <param name="dirCheck">The direction of the neighbouring Cluster to check. It isn't enough to check outselves; the neighbour may be blocking travel.</param>
        /// <param name="dirNeighbourCluster">Same as dirCheck but using the enum that Clusters understand.</param>
        private void WalkWall(Cluster cluster, Level.Tile tile, Level.Tile sequeceStart, Level.Tile.AdjacentTileDir dirWalk, Level.Tile.AdjacentTileDir dirCheck, Cluster.AdjacentClusterDirections dirNeighbourCluster)
        {
            // Get the Tile in the neighbouring Cluster. It being solid creates a wall just the same as 
            // if the tile in this Cluster is solid.
            Level.Tile adj = tile.mAdjecentTiles[(Int32)dirCheck];

            if (null != adj)
            {
                // If we don't yet have a sequence start point, and this tile is an entrace,
                // it becomes the new sequence start.
                if (null == sequeceStart && tile.mType == Level.Tile.TileTypes.Empty && adj.mType == Level.Tile.TileTypes.Empty)
                {
                    sequeceStart = tile;
                }
                // The sequence has started already and we just hit a wall. Time to create an entrance in the
                // center of this sequence.
                else if (null != sequeceStart && (tile.mType != Level.Tile.TileTypes.Empty || adj.mType != Level.Tile.TileTypes.Empty))
                {
                    CreateEntrance(cluster, tile, ref sequeceStart, dirWalk, dirCheck, dirNeighbourCluster, true);
                }
            }

            // Walk to the next Tile.
            adj = tile.mAdjecentTiles[(Int32)dirWalk];

            // Are we still in the Cluster/Level?
            if (null != adj && cluster.Contains(adj))
            {
                // Recursivly visit the next Tile.
                WalkWall(cluster, adj, sequeceStart, dirWalk, dirCheck, dirNeighbourCluster);
            }
            else
            {
                // We have left either the map or the Cluster. Either way that is considered an end to
                // the current sequence, should one be in progress.
                if (null != sequeceStart)
                {
                    CreateEntrance(cluster, tile, ref sequeceStart, dirWalk, dirCheck, dirNeighbourCluster, false);
                }
            }
        }

        /// <summary>
        /// Helper function to do the work needed to create a single new extrance/exit based on the current tile being walked,
        /// and the Tile at the start of the sequence.
        /// </summary>
        /// <param name="cluster">The cluster we are evaluating. Hitting a Tile outside this cluster will end that wall.</param>
        /// <param name="tile">The current tile to evaluate.</param>
        /// <param name="sequeceStart">The tile which started this sequence. Use null to start.</param>
        /// <param name="dirWalk">The direction to walk along the wall.</param>
        /// <param name="dirCheck">The direction of the neighbouring Cluster to check. It isn't enough to check outselves; the neighbour may be blocking travel.</param>
        /// <param name="dirNeighbourCluster">Same as dirCheck but using the enum that Clusters understand.</param>
        /// <param name="removeSelf">True if the tile that triggered this call should not be included in the sequence. Useful if this sequence ended because you hit a wall.</param>
        private void CreateEntrance(Cluster cluster, Level.Tile tile, ref Level.Tile sequenceStart, Level.Tile.AdjacentTileDir dirWalk, Level.Tile.AdjacentTileDir dirCheck, Cluster.AdjacentClusterDirections dirNeighbourCluster, Boolean removeSelf)
        {
            // Find the center point between the tile at the start of the sequence of enpty tiles
            // and the current tile.
            Vector2 middle = tile.mCollisionRect.pCenterPoint - sequenceStart.mCollisionRect.pCenterPoint;

            // If we enter this block by hitting a wall, we need to remove that a Tile length from our
            // calculations since that wall is not part of the entrance/exit.
            if (removeSelf)
            {
                middle -= Vector2.Normalize(middle) * new Vector2(cluster.pTileDimensions.X, cluster.pTileDimensions.Y);
            }

            // Add half the length in order to put us in the middle of the sequence.
            middle = (middle * 0.5f) + sequenceStart.mCollisionRect.pCenterPoint;

            // We need to find the tile at that position because our GraphNode depends on that data.
            mGetTileAtPositionMsg.mPosition_In = middle;
            WorldManager.pInstance.pCurrentLevel.OnMessage(mGetTileAtPositionMsg);

            System.Diagnostics.Debug.Assert(null != mGetTileAtPositionMsg.mTile_Out, "Unable to find tile.");

            // Create the new nodes with the appropriate Tile data.
            TileGraphNode localNode = new TileGraphNode(mGetTileAtPositionMsg.mTile_Out);
            TileGraphNode otherNode = new TileGraphNode(mGetTileAtPositionMsg.mTile_Out.mAdjecentTiles[(Int32)dirCheck]);

            // Link the two nodes together creating an Intra-Connection.
            localNode.AddNeighbour(otherNode);
            otherNode.AddNeighbour(localNode);

            // Add the nodes the appropriate Cluster objects.
            cluster.AddNode(localNode);
            cluster.pNeighbouringClusters[(Int32)dirNeighbourCluster].AddNode(otherNode);

            // Start a new sequence.
            sequenceStart = null;
        }

        
        /// <summary>
        /// Since this Graph is organized into clusters, we can severly limit the amount of debug draw needed
        /// by indexing directly into clusters base on camera position.
        /// </summary>
        /// <param name="showLinks"></param>
        public override void DebugDraw(Boolean showLinks)
        {
            Color col = Color.BlanchedAlmond;

            // How many pixels wide/high is a single cluster? This will be needed to go from
            // screen size, to cluster index.
            Int32 pixelsPerClusterX = mClusterSize * mGetMapInfoMsg.mInfo_Out.mTileWidth;
            Int32 pixelsPerClusterY = mClusterSize * mGetMapInfoMsg.mInfo_Out.mTileHeight;

            // Based on the current position of the camera, figure out where in the array of clusters they
            // start to become visible on screen. Also figure out where they stop being visible again.
            MBHEngine.Math.Rectangle view = CameraManager.pInstance.pViewRect;
            Int32 startX = (Int32)MathHelper.Max((Int32)view.pLeft / pixelsPerClusterX, 0);
            Int32 endX = (Int32)MathHelper.Min((Int32)view.pRight / pixelsPerClusterX + 1, mClusters.GetLength(0));
            Int32 startY = (Int32)MathHelper.Max((Int32)view.pTop / pixelsPerClusterY, 0);
            Int32 endY = (Int32)MathHelper.Min((Int32)view.pBottom / pixelsPerClusterY + 1, mClusters.GetLength(1));

            for (Int32 y = startY; y < endY; y++)
            {
                for (Int32 x = startX; x < endX; x++)
                {
                    Cluster temp = mClusters[x, y];

                    for (Int32 i = 0; i < temp.pGraphNodes.Count; i++)
                    {
                        DrawNode(temp.pGraphNodes[i], showLinks);
                    }

                    // Render the cluster boundaries as well.
                    MBHEngine.Math.Rectangle tempRect = temp.pTopLeft.mCollisionRect; 
                    //DebugShapeDisplay.pInstance.AddAABB(tempRect, col);
                    DebugShapeDisplay.pInstance.AddAABB(temp.pBounds, col, false);
                }
            }
        }
        
    }
}
