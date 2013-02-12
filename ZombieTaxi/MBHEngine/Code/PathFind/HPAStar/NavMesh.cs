using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MBHEngine.Behaviour;
using MBHEngine.World;
using MBHEngine.Debug;
using Microsoft.Xna.Framework;

namespace MBHEngine.PathFind.HPAStar
{
    /// <summary>
    /// Navigation mesh used for Near Perfect Hierarchial Path-Finding A*. It breaks the graph into
    /// clusters of nodes, and tracks the connection points between neighbouring clusters.
    /// </summary>
    public class NavMesh
    {
        /// <summary>
        /// A small section of the map of a size defined by the client.
        /// </summary>
        public class Cluster
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
            /// A single node in the search graph. It knows about the Level.Tile that it is on
            /// top of, and all of the other GraphNodes that it connects to directly.
            /// </summary>
            public class GraphNode
            {
                /// <summary>
                /// Neighbouring GraphNode objects get stored in this wrapper so that things like
                /// the cost to travel there can be cached.
                /// </summary>
                public class Neighbour
                {
                    /// <summary>
                    /// The GraphNode that this object wraps.
                    /// </summary>
                    public GraphNode mGraphNode;

                    /// <summary>
                    /// How much is costs to travel from the GraphNode containing this neighbour,
                    /// to this neighbour. This is precomputed and chached for quicker lookups later.
                    /// </summary>
                    public Single mCostToTravel;
                }

                /// <summary>
                /// Every node in the graph was created to map to a tile in the Level. This is the 
                /// Tile that this GraphNode maps to.
                /// </summary>
                private Level.Tile mTile;

                /// <summary>
                /// A list of all the GraphNodes that this GraphNode connects to directly, both
                /// inter-connections and intra-connections.
                /// </summary>
                private List<Neighbour> mNeighbours;

                public GraphNode(Level.Tile tile)
                {
                    mNeighbours = new List<Neighbour>(8);

                    mTile = tile;
                }

                public void AddNeighbour(GraphNode node)
                {
                    Neighbour temp = new Neighbour();

                    temp.mGraphNode = node;

                    temp.mCostToTravel = 8.0f;

                    mNeighbours.Add(temp);
                }

                public Vector2 pPosition
                {
                    get
                    {
                        return mTile.mCollisionRect.pCenterPoint;
                    }
                }

                public List<Neighbour> pNeighbours
                {
                    get
                    {
                        return mNeighbours;
                    }
                }
            }

            private Level.Tile mTopLeft;

            private List<GraphNode> mNodes;

            private Cluster[] mNeighbours;

            private MBHEngine.Math.Rectangle mBounds;

            private Int32 mClusterSize;

            private Point mTileDimensions;
            
            public Cluster(Int32 clusterSize, Int32 tileWidth, Int32 tileHeight)
            {
                // On average we probably have 2 entraces per side.
                mNodes = new List<GraphNode>(8);

                mClusterSize = clusterSize;

                mTileDimensions = new Point(tileWidth, tileHeight);

                mBounds = new Math.Rectangle(tileWidth * clusterSize, tileHeight * clusterSize);

                mNeighbours = new Cluster[4];
            }

            public void AddNode(GraphNode node)
            {
                mNodes.Add(node);
            }

            public Boolean Contains(Level.Tile tile)
            {
                return (mBounds.Intersects(tile.mCollisionRect));
            }

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

            public MBHEngine.Math.Rectangle pBounds
            {
                get
                {
                    return mBounds;
                }
            }

            public List<GraphNode> pGraphNodes
            {
                get
                {
                    return mNodes;
                }
            }

            public Point pTileDimensions
            {
                get
                {
                    return mTileDimensions;
                }
            }

            public Cluster[] pNeighbouringClusters
            {
                get
                {
                    return mNeighbours;
                }
            }
        }

        private Int32 mClusterSize;

        private Cluster[,] mClusters;

        private Level.GetMapInfoMessage mGetMapInfoMsg;
        private Level.GetTileAtPositionMessage mGetTileAtPositionMsg;

        public NavMesh(Int32 clusterSize)
        {
            mClusterSize = clusterSize;

            mGetMapInfoMsg = new Level.GetMapInfoMessage();
            mGetTileAtPositionMsg = new Level.GetTileAtPositionMessage();
        }

        public void CreateNavMesh(GameObject.GameObject level)
        {
            level.OnMessage(mGetMapInfoMsg);

            Int32 mapWidth = mGetMapInfoMsg.mInfo_Out.mMapWidth;
            Int32 mapHeight = mGetMapInfoMsg.mInfo_Out.mMapHeight;

            Int32 tileWidth = mGetMapInfoMsg.mInfo_Out.mTileWidth;
            Int32 tileHeight = mGetMapInfoMsg.mInfo_Out.mTileHeight;

            mClusters = new Cluster[mapWidth / mClusterSize, mapHeight / mClusterSize];

            // Based on the map size and number of tiles in a cluster, how many clusters are there?
            Int32 numClusters = (mapWidth / mClusterSize) * (mapHeight / mClusterSize);

            for (Int32 y = 0; y < mClusters.GetLength(1); y++)
            {
                for (Int32 x = 0; x < mClusters.GetLength(0); x++)
                {
                    Cluster temp = new Cluster(mClusterSize, tileWidth, tileHeight);

                    mGetTileAtPositionMsg.mPosition_In.X = x * (tileWidth * mClusterSize);
                    mGetTileAtPositionMsg.mPosition_In.Y = y * (tileHeight * mClusterSize);

                    level.OnMessage(mGetTileAtPositionMsg);

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

                    /*
                    Level.Tile next = WalkWall(temp, temp.pTopLeft, null, Level.Tile.AdjacentTileDir.RIGHT, Level.Tile.AdjacentTileDir.UP);
                    next = WalkWall(temp, next, null, Level.Tile.AdjacentTileDir.DOWN, Level.Tile.AdjacentTileDir.RIGHT);
                    next = WalkWall(temp, next, null, Level.Tile.AdjacentTileDir.LEFT, Level.Tile.AdjacentTileDir.DOWN);
                    next = WalkWall(temp, next, null, Level.Tile.AdjacentTileDir.UP, Level.Tile.AdjacentTileDir.LEFT);
                    */

                    // Only walk the top and left walls for each cluster. The neighbouring clusters will do the same
                    // and as a result all walls will have been evaluated.
                    WalkWall(temp, temp.pTopLeft, null, Level.Tile.AdjacentTileDir.RIGHT, Level.Tile.AdjacentTileDir.UP, Cluster.AdjacentClusterDirections.Up);
                    WalkWall(temp, temp.pTopLeft, null, Level.Tile.AdjacentTileDir.DOWN, Level.Tile.AdjacentTileDir.LEFT, Cluster.AdjacentClusterDirections.Left);

                    //System.Diagnostics.Debug.Assert(next == temp.pTopLeft, "Walking walls did not end at starting point.");
                    
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
                    }
                }
            }
        }

        private Level.Tile WalkWall(Cluster cluster, Level.Tile tile, Level.Tile sequeceStart, Level.Tile.AdjacentTileDir dirWalk, Level.Tile.AdjacentTileDir dirCheck, Cluster.AdjacentClusterDirections dirNeighbourCluster)
        {
            Level.Tile adj = tile.mAdjecentTiles[(Int32)dirCheck];

            if (null != adj)
            {
                // If we don't yet have a sequence start point, and this tile is an entrace,
                // it becomes the new sequence start.
                if (null == sequeceStart && tile.mType == Level.Tile.TileTypes.Empty && adj.mType == Level.Tile.TileTypes.Empty)
                {
                    //cluster.AddEntrace(tile);
                    sequeceStart = tile;
                }
                else if (null != sequeceStart && (tile.mType != Level.Tile.TileTypes.Empty || adj.mType != Level.Tile.TileTypes.Empty))
                {
                    Vector2 middle = tile.mCollisionRect.pCenterPoint - sequeceStart.mCollisionRect.pCenterPoint;

                    middle -= Vector2.Normalize(middle) * new Vector2(cluster.pTileDimensions.X, cluster.pTileDimensions.Y);

                    middle = (middle * 0.5f) + sequeceStart.mCollisionRect.pCenterPoint;

                    mGetTileAtPositionMsg.mPosition_In = middle;

                    WorldManager.pInstance.pCurrentLevel.OnMessage(mGetTileAtPositionMsg);

                    System.Diagnostics.Debug.Assert(null != mGetTileAtPositionMsg.mTile_Out, "Unable to find tile.");

                    //cluster.AddNode(mGetTileAtPositionMsg.mTile_Out, mGetTileAtPositionMsg.mTile_Out.mAdjecentTiles[(Int32)dirCheck]);

                    Cluster.GraphNode localNode = new Cluster.GraphNode(mGetTileAtPositionMsg.mTile_Out);
                    Cluster.GraphNode otherNode = new Cluster.GraphNode(mGetTileAtPositionMsg.mTile_Out.mAdjecentTiles[(Int32)dirCheck]);

                    localNode.AddNeighbour(otherNode);
                    otherNode.AddNeighbour(localNode);

                    cluster.AddNode(localNode);
                    cluster.pNeighbouringClusters[(Int32)dirNeighbourCluster].AddNode(otherNode);

                    sequeceStart = null;
                }
            }

            adj = tile.mAdjecentTiles[(Int32)dirWalk];

            if (null != adj && cluster.Contains(adj))
            {
                return WalkWall(cluster, adj, sequeceStart, dirWalk, dirCheck, dirNeighbourCluster);
            }
            else
            {
                if (null != sequeceStart)
                {
                    Vector2 middle = tile.mCollisionRect.pCenterPoint - sequeceStart.mCollisionRect.pCenterPoint;

                    middle = (middle * 0.5f) + sequeceStart.mCollisionRect.pCenterPoint;

                    mGetTileAtPositionMsg.mPosition_In = middle;

                    WorldManager.pInstance.pCurrentLevel.OnMessage(mGetTileAtPositionMsg);

                    System.Diagnostics.Debug.Assert(null != mGetTileAtPositionMsg.mTile_Out, "Unable to find tile.");

                    Cluster.GraphNode localNode = new Cluster.GraphNode(mGetTileAtPositionMsg.mTile_Out);
                    Cluster.GraphNode otherNode = new Cluster.GraphNode(mGetTileAtPositionMsg.mTile_Out.mAdjecentTiles[(Int32)dirCheck]);

                    localNode.AddNeighbour(otherNode);
                    otherNode.AddNeighbour(localNode);

                    cluster.AddNode(localNode);
                    cluster.pNeighbouringClusters[(Int32)dirNeighbourCluster].AddNode(otherNode);
                    sequeceStart = null;
                }

                return tile;
            }
        }

        public void DebugDraw()
        {
            Color col = Color.BlanchedAlmond;

            for (Int32 y = 0; y < mClusters.GetLength(1); y++)
            {
                for (Int32 x = 0; x < mClusters.GetLength(0); x++)
                {
                    Cluster temp = mClusters[x, y];

                    MBHEngine.Math.Rectangle tempRect = temp.pTopLeft.mCollisionRect; 

                    for (Int32 i = 0; i < temp.pGraphNodes.Count; i++)
                    {
                        DebugShapeDisplay.pInstance.AddCircle(temp.pGraphNodes[i].pPosition, 4.0f, Color.DarkRed);

                        for (Int32 j = 0; j < temp.pGraphNodes[i].pNeighbours.Count; j++)
                        {
                            DebugShapeDisplay.pInstance.AddSegment(temp.pGraphNodes[i].pPosition, temp.pGraphNodes[i].pNeighbours[j].mGraphNode.pPosition, Color.DarkRed);
                        }
                    }

                    DebugShapeDisplay.pInstance.AddAABB(tempRect, col);

                    DebugShapeDisplay.pInstance.AddAABB(temp.pBounds, col, false);
                }
            }
        }
    }
}
