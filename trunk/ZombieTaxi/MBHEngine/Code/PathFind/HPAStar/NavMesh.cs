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
        /// Helper class for managing preallocated NavMeshTileGraphNode objects.
        /// </summary>
        class NavMeshTileGraphNodeFactory
        {
            /// <summary>
            /// A giant list of Nodes which get preallocated on creation.
            /// </summary>
            private Queue<NavMeshTileGraphNode> mUnusedNavMeshTileGraphNodes;

            /// <summary>
            /// Constructor.
            /// </summary>
            public NavMeshTileGraphNodeFactory()
            {
                const Int32 num = 10000;

                mUnusedNavMeshTileGraphNodes = new Queue<NavMeshTileGraphNode>(num);

                for (Int32 i = 0; i < num; i++)
                {
                    NavMeshTileGraphNode temp = new NavMeshTileGraphNode();

                    mUnusedNavMeshTileGraphNodes.Enqueue(temp);
                }
            }

            /// <summary>
            /// Returns a NavMeshTileGraphNode reset and ready for use. When no longer needed,
            /// should be returned to the factory through RecycleNode.
            /// </summary>
            /// <returns>A NavMeshTileGraphNode reset and ready for use.</returns>
            public GraphNode GetNode()
            {
                NavMeshTileGraphNode node = mUnusedNavMeshTileGraphNodes.Dequeue();
#if DEBUG
                node.mInUse = true;
#endif // DEBUG
                return node;
            }

            /// <summary>
            /// When finished using a Node which was retrieved from GetNode, pass it back to
            /// this function for clean up.
            /// </summary>
            /// <param name="node"></param>
            public void RecycleNode(GraphNode node)
            {
                node.Reset();
#if DEBUG
                node.mInUse = false;
#endif // DEBUG
                mUnusedNavMeshTileGraphNodes.Enqueue(node as NavMeshTileGraphNode);
            }

            /// <summary>
            /// How many nodes remain (are not in use).
            /// </summary>
            public Int32 pUnusedCount
            {
                get
                {
                    return mUnusedNavMeshTileGraphNodes.Count;
                }
            }
        }

        /// <summary>
        /// The number of Tile objects that make up a single wall of a cluster. Assumes square clusters.
        /// </summary>
        private Int32 mClusterSize;

        /// <summary>
        /// Array of all the clusters in this Graph indexed by X, Y value.
        /// </summary>
        private Cluster[,] mClusters;

        /// <summary>
        /// Used for precomputing the cost of travelling between nodes in the Graph.
        /// </summary>
        private GenericAStar.Planner mPlanner;

        /// <summary>
        /// To avoid allocating Node objects during gameplay (and possibly triggering the GC), this factory
        /// preallocates a bunch ahead of time.
        /// </summary>
        static private NavMeshTileGraphNodeFactory mNodeFactory;

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

            mPlanner = new Planner();

            // Remember the factory is static so make sure to only allocate it once.
            if (null == mNodeFactory)
            {
                mNodeFactory = new NavMeshTileGraphNodeFactory();
            }

            mGetMapInfoMsg = new Level.GetMapInfoMessage();
            mGetTileAtPositionMsg = new Level.GetTileAtPositionMessage();
        }

        /// <summary>
        /// Steps through all Tile objects in the level and generates a nave mesh from it.
        /// </summary>
        /// <param name="level"></param>
        public void CreateNavMesh(GameObject.GameObject level)
        {
            // The gets used over and over again throughout the life if this object.
            level.OnMessage(mGetMapInfoMsg);

            Int32 mapWidth = mGetMapInfoMsg.mInfo_Out.mMapWidth;
            Int32 mapHeight = mGetMapInfoMsg.mInfo_Out.mMapHeight;

            Int32 tileWidth = mGetMapInfoMsg.mInfo_Out.mTileWidth;
            Int32 tileHeight = mGetMapInfoMsg.mInfo_Out.mTileHeight;

            // Clusters get indexed based on their X, Y positions in the world.
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

                    LinkClusterGraphNodes(temp);
                }
            }
        }

        /// <summary>
        /// Clears out a cluster and regenerates all the nodes inside and any links to outside
        /// cluster GraphNode objects.
        /// </summary>
        /// <param name="pos">The position inside a cluster which should be regenerated.</param>
        public void RegenerateCluster(Vector2 pos)
        {
            // To start, find if and what cluster was invalidated.
            Cluster cluster = GetClusterAtPosition(pos);

            // Remove all the GraphNode objects from the Cluster and remove any links between GraphNodes
            // and adjacent Cluster objects.
            ClearCluster(cluster);

            Level.Tile nextStartPoint = cluster.pTopLeft;

            // With the Cluster cleared out, it is now required that entrances/exits be regenerated.
            nextStartPoint = WalkWall(cluster, nextStartPoint, null, Level.Tile.AdjacentTileDir.RIGHT, Level.Tile.AdjacentTileDir.UP, Cluster.AdjacentClusterDirections.Up);
            nextStartPoint = WalkWall(cluster, nextStartPoint, null, Level.Tile.AdjacentTileDir.DOWN, Level.Tile.AdjacentTileDir.RIGHT, Cluster.AdjacentClusterDirections.Right);
            nextStartPoint = WalkWall(cluster, nextStartPoint, null, Level.Tile.AdjacentTileDir.LEFT, Level.Tile.AdjacentTileDir.DOWN, Cluster.AdjacentClusterDirections.Down);
            nextStartPoint = WalkWall(cluster, nextStartPoint, null, Level.Tile.AdjacentTileDir.UP, Level.Tile.AdjacentTileDir.LEFT, Cluster.AdjacentClusterDirections.Left);

            // Link all the GraphNode within this cluster.
            LinkClusterGraphNodes(cluster);

            // Neighbouring Clusters may have had GraphNode added to them, and so they may require
            // new linkages to be set up.
            for (Int32 i = 0; i < cluster.pNeighbouringClusters.Length; i++)
            {
                LinkClusterGraphNodes(cluster.pNeighbouringClusters[i]);
            }
        }

        /// <summary>
        /// Nasty little function which essentially removes a Cluster's nodes and any links associated
        /// with them. Things get complicated with the fact that adjecent Clusters have GraphNode in them
        /// which link back to GraphNode in this Cluster, which need to be cleaned up, and sometimes completely
        /// removed. The method handles it all.
        /// </summary>
        /// <param name="cluster"></param>
        private void ClearCluster(Cluster cluster)
        {
            if (null != cluster)
            {
                // Loop through every node in the cluster cleaning up its links to other nodes
                // as we go.
                for (Int32 i = cluster.pNodes.Count - 1; i >= 0; i--)
                {
                    GraphNode node = cluster.pNodes[i];

                    // Loop through all the neighbours removing the links as we go.
                    for (Int32 j = node.pNeighbours.Count - 1; j >= 0; j--)
                    {
                        GraphNode neighbourNode = node.pNeighbours[j].mGraphNode;

                        // Since this is in the cluster being regenerated, all links to other nodes
                        // should be removed; this node/cluster isn't going to exist in a moment.
                        UnlinkGraphNodes(node, neighbourNode);

                        //
                        // Next comes the convoluted process for checking if the GraphNode just unlinked
                        // actually lives in another cluster, and if so, possibly removing that GraphNode
                        // as well, but only in the case where the neighbour ONLY links to GraphNodes 
                        // within its own Cluster (remember we already removed the link to the cluster we 
                        // are destroying. In the case where it links out to another Cluster, that node 
                        // still has some purpose, so it shouldn't be removed; just the links to the
                        // Cluster being destroyed.
                        //

                        Cluster neighbourCluster = GetClusterAtPosition(neighbourNode.pPosition);

                        // Is this a GraphNode that lives in a Cluster outside the one being cleared?
                        if (neighbourCluster != cluster)
                        {
                            // Search through all the neighbours trying to find one that is in a different Cluster,
                            // signifying that this GraphNode needs to live on. Remember that nodes in corners can
                            // be linked to multiple adjacent Clusters.
                            Boolean foundOther = false;

                            for (Int32 k = neighbourNode.pNeighbours.Count - 1; k >= 0; k--)
                            {
                                // Slicks naming...
                                GraphNode otherNeighbourNode = neighbourNode.pNeighbours[k].mGraphNode;

                                Cluster otherNeighbourCluster = GetClusterAtPosition(otherNeighbourNode.pPosition);

                                // If the neighbour lives outside this Cluster than we don't want to remove the current
                                // neighbour being evaluated.
                                if (otherNeighbourCluster != neighbourCluster)
                                {
                                    foundOther = true;
                                    break;
                                }
                            }

                            if (!foundOther)
                            {
                                // Now loop through all the neighbours AGAIN, this time removing all links between the
                                // GraphNode about to be removed, and all the others that will live on.
                                for (Int32 k = neighbourNode.pNeighbours.Count - 1; k >= 0; k--)
                                {
                                    GraphNode otherNeighbourNode = neighbourNode.pNeighbours[k].mGraphNode;

                                    UnlinkGraphNodes(neighbourNode, otherNeighbourNode);
                                }

                                // Remove this neighbour from the Graph objects.
                                //DebugCheckForReferences(neighbourNode);

                                mNodeFactory.RecycleNode(neighbourNode);
                                RemoveNode(neighbourNode);
                                neighbourCluster.RemoveNode(neighbourNode);
                            }
                        }
                    }

                    // If this is a temporary node it should not be removed from the Graph because it will not
                    // be automatically re-added. It still gets all the links removed because those WILL be 
                    // automatically generated.
                    if (!(node as NavMeshTileGraphNode).pIsTemporary)
                    {
                        //DebugCheckForReferences(node);
                        mNodeFactory.RecycleNode(node);
                        RemoveNode(node);
                        cluster.RemoveNode(node);
                    }
                }
            }

            //DebugCheckNodes();
        }

        /// <summary>
        /// Create all the intra-connects within a Cluster.
        /// </summary>
        /// <param name="cluster">The Cluster to perform the links in.</param>
        private void LinkClusterGraphNodes(Cluster cluster)
        {
            if (cluster != null)
            {
                // Iterate throught the nodes of a Cluster 2 at a time, linking each node with all
                // the nodes that follow it (and back), so by the end of the loop everyone should be
                // linked to each other.
                // eg.  A <-> BCD
                //      B <-> CD
                //      C <-> D
                for (Int32 i = 0; i < cluster.pNodes.Count; i++)
                {
                    for (Int32 j = i + 1; j < cluster.pNodes.Count; j++)
                    {
                        // Avoid linking the same node multiple times.
                        if (!cluster.pNodes[i].HasNeighbour(cluster.pNodes[j]))
                        {
                            LinkGraphNodes(cluster.pNodes[i], cluster.pNodes[j], cluster);
                        }
                    }

                    //AddNode(cluster.pNodes[i]);
                }
            }

            //if (cluster.pNodes[0] != null)
            //{
                //DebugWalkGraphForErrors(cluster.pNodes[0] as NavMeshTileGraphNode);
            //}
        }

        /// <summary>
        /// Links two GraphNode objects as Neighbour. Does the slightly expensive task of calculating actual
        /// A* path between the two, and caches that value as the cost between the two.
        /// </summary>
        /// <param name="a">A node to link to <paramref name="b"/>.</param>
        /// <param name="b">A node to link to <paramref name="a"/>.</param>
        /// <param name="cluster">The cluster containing both nodes.</param>
        private void LinkGraphNodes(GraphNode a, GraphNode b, Cluster cluster, Boolean oneWay = false)
        {
            mPlanner.Reset();
            mPlanner.SetSource((a.pData as Level.Tile).mGraphNode);
            mPlanner.SetDestination((b.pData as Level.Tile).mGraphNode);

            // Do a standard A* search with the adde constraint of staying within the 
            // bounds of this cluster.
            Planner.Result result = mPlanner.PlanPath(cluster.pBounds, false);

            // Keep searching unti we either fail or succeed.
            while (result != Planner.Result.Failed && result != Planner.Result.Solved)
            {
                result = mPlanner.PlanPath(cluster.pBounds, false);
            }

            // Only connect the nodes if they can be reached from one another within the same cluster.
            if (result == Planner.Result.Solved)
            {
                PathNode path = mPlanner.pCurrentBest;

                // Link the two neightbours.
                a.AddNeighbour(b, path.pFinalCost);
                if (!oneWay)
                {
                    b.AddNeighbour(a, path.pFinalCost);
                }
            }

            mPlanner.ClearDestination();

            //if (a != null)
            //{
                //DebugWalkGraphForErrors(a as NavMeshTileGraphNode);
            //}

            //DebugCheckNode(a);
            //DebugCheckNode(b);
        }

        /// <summary>
        /// Take two GraphNode objects and remove any link they have as Neighbour.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        public void UnlinkGraphNodes(GraphNode a, GraphNode b)
        {
            a.RemoveNeighbour(b);
            b.RemoveNeighbour(a);

            //if (a != null)
            //{
                //DebugWalkGraphForErrors(a as NavMeshTileGraphNode);
            //}

            //DebugCheckNode(a);
            //DebugCheckNode(b);
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
        /// <returns>The last tile visited on this wall. Useful for walking an entire perimeter.</returns>
        private Level.Tile WalkWall(Cluster cluster, Level.Tile tile, Level.Tile sequeceStart, Level.Tile.AdjacentTileDir dirWalk, Level.Tile.AdjacentTileDir dirCheck, Cluster.AdjacentClusterDirections dirNeighbourCluster)
        {
            // Get the Tile in the neighbouring Cluster. It being solid creates a wall just the same as 
            // if the tile in this Cluster is solid.
            Level.Tile adj = tile.mAdjecentTiles[(Int32)dirCheck];

            Boolean entraceMade = false;

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

                    entraceMade = true;
                }
            }

            // Walk to the next Tile.
            adj = tile.mAdjecentTiles[(Int32)dirWalk];

            // Are we still in the Cluster/Level?
            if (null != adj && cluster.IsInBounds(adj))
            {
                // Recursivly visit the next Tile.
                return WalkWall(cluster, adj, sequeceStart, dirWalk, dirCheck, dirNeighbourCluster);
            }
            else
            {
                // We have left either the map or the Cluster. Either way that is considered an end to
                // the current sequence, should one be in progress.
                if (null != sequeceStart)
                {
                    System.Diagnostics.Debug.Assert(!entraceMade, "Entrance made twice.");

                    CreateEntrance(cluster, tile, ref sequeceStart, dirWalk, dirCheck, dirNeighbourCluster, false);
                }

                return tile;
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
            Vector2 sequenceVector = tile.mCollisionRect.pCenterPoint - sequenceStart.mCollisionRect.pCenterPoint;

            // If we enter this block by hitting a wall, we need to remove that a Tile length from our
            // calculations since that wall is not part of the entrance/exit.
            if (removeSelf)
            {
                sequenceVector -= Vector2.Normalize(sequenceVector) * new Vector2(cluster.pTileDimensions.X, cluster.pTileDimensions.Y);
            }

            // If the sequence is long enough, instead of putting a GraphNode in the center, create 2 GraphNode objects,
            // and place them at opposite ends of the Sequence. This is recommended by the original HPA* white paper.
            if (sequenceVector.LengthSquared() >= (mClusterSize * mGetMapInfoMsg.mInfo_Out.mMapWidth * 0.5f))
            {                
                // Add the length of the Sequence to the starting point to get our ending position.
                Vector2 end = (sequenceVector) + sequenceStart.mCollisionRect.pCenterPoint;

                // We need to find the tile at that position because our GraphNode depends on that data.
                mGetTileAtPositionMsg.mPosition_In = end;
                WorldManager.pInstance.pCurrentLevel.OnMessage(mGetTileAtPositionMsg);

                System.Diagnostics.Debug.Assert(null != mGetTileAtPositionMsg.mTile_Out, "Unable to find tile.");

                CreateEntranceNodes(
                    cluster,
                    cluster.pNeighbouringClusters[(Int32)dirNeighbourCluster],
                    mGetTileAtPositionMsg.mTile_Out,
                    mGetTileAtPositionMsg.mTile_Out.mAdjecentTiles[(Int32)dirCheck]);

                CreateEntranceNodes(
                    cluster,
                    cluster.pNeighbouringClusters[(Int32)dirNeighbourCluster],
                    sequenceStart,
                    sequenceStart.mAdjecentTiles[(Int32)dirCheck]);
            }
            else
            {
                // Add half the length in order to put us in the middle of the sequence.
                Vector2 middle = (sequenceVector * 0.5f) + sequenceStart.mCollisionRect.pCenterPoint;

                // We need to find the tile at that position because our GraphNode depends on that data.
                mGetTileAtPositionMsg.mPosition_In = middle;
                WorldManager.pInstance.pCurrentLevel.OnMessage(mGetTileAtPositionMsg);

                System.Diagnostics.Debug.Assert(null != mGetTileAtPositionMsg.mTile_Out, "Unable to find tile.");

                CreateEntranceNodes(
                    cluster,
                    cluster.pNeighbouringClusters[(Int32)dirNeighbourCluster],
                    mGetTileAtPositionMsg.mTile_Out,
                    mGetTileAtPositionMsg.mTile_Out.mAdjecentTiles[(Int32)dirCheck]);
            }

            // Start a new sequence.
            sequenceStart = null;
        }

        /// <summary>
        /// Helper function for creating 2 linked GraphNodes in neighnbouring clusters.
        /// </summary>
        /// <param name="localCluster">The cluster currently being evaluated.</param>
        /// <param name="otherCluster">The neighbouring cluster of <paramref name="localCluster"/>.</param>
        /// <param name="localTile">The Tile on which the new entrance sits on top of.</param>
        /// <param name="otherTile">The Tile on wihich the other new entrace sits on top of.</param>
        private void CreateEntranceNodes(Cluster localCluster, Cluster otherCluster, Level.Tile localTile, Level.Tile otherTile)
        {
            GraphNode localNode;
            GraphNode otherNode;

            // Create the new nodes with the appropriate Tile data.
            Boolean localCreated = CreateEntraceNode(localCluster, localTile, out localNode);
            Boolean otherCreated = CreateEntraceNode(otherCluster, otherTile, out otherNode);

            // Link the two nodes together creating an Intra-Connection.
            localNode.AddNeighbour(otherNode);
            otherNode.AddNeighbour(localNode);

            // Add the nodes the appropriate Cluster objects.
            if (localCreated)
            {
                localCluster.AddNode(localNode);
            }
            if (otherCreated)
            {
                otherCluster.AddNode(otherNode);
            }
        }


        /// <summary>
        /// Helper function for creating a Node for an entrace but also checking that it hasn't already been
        /// created, and if it has, just using that one instead to avoid multiple Nodes on top of the same
        /// tile.
        /// </summary>
        /// <param name="cluster">The custer which this node lives in.</param>
        /// <param name="tile">The tile which this node wraps.</param>
        /// <param name="node">
        /// If a node already exists over <paramref name="tile"/> this will be that GraphNode. If not it will 
        /// be a newly created GraphNode.
        /// </param>
        /// <returns>True if a new GraphNode was created.</returns>
        private Boolean CreateEntraceNode(Cluster cluster, Level.Tile tile, out GraphNode node)
        {
            Boolean created = false;

            // First see if this Tile is already managed by this cluster. If it is, it will return us
            // the node which contains it.
            node = cluster.GetNodeContaining(tile);

            // If the node isn't already being managed, we need to create a new one.
            if (null == node)
            {
                node = mNodeFactory.GetNode();
                node.pData = tile;

                created = true;

                // New nodes need to be registers with the Graph.
                AddNode(node);
            }

            return created;
        }

        /// <summary>
        /// Find the GraphNode in this NavMesh, closest to a given positon within the cluster at that
        /// location.
        /// </summary>
        /// <param name="pos">The position the search near.</param>
        /// <returns>The clostest GraphNode in the Cluster at the given position.</returns>
        public GraphNode GetClostestNode(Vector2 pos)
        {
            Cluster cluster = GetClusterAtPosition(pos);
            
            GraphNode best = null;

            if (null != cluster)
            {
                Single bestDist = Single.MaxValue;

                for (Int32 i = 0; i < cluster.pNodes.Count; i++)
                {
                    GraphNode node = cluster.pNodes[i];

                    Single dist = Vector2.DistanceSquared(node.pPosition, pos);

                    if (null == best || dist < bestDist)
                    {
                        best = node;
                        bestDist = dist;
                    }
                }
            }

            return best;
        }

        /// <summary>
        /// Finds the GraphNode at a given position (should one exist).
        /// </summary>
        /// <param name="pos">The position to search at.</param>
        /// <returns>The GraphNode at <paramref name="pos"/></returns>
        public GraphNode FindNodeAt(Vector2 pos)
        {
            Cluster cluster = GetClusterAtPosition(pos);

            if (null != cluster)
            {
                mGetTileAtPositionMsg.Reset();
                mGetTileAtPositionMsg.mPosition_In = pos;
                WorldManager.pInstance.pCurrentLevel.OnMessage(mGetTileAtPositionMsg);

                return cluster.GetNodeContaining(mGetTileAtPositionMsg.mTile_Out);
            }

            return null;
        }
        
        /// <summary>
        /// Inserts a new GraphNode into the Graph 'after the fact'.
        /// </summary>
        /// <param name="pos">The position at which to insert the GraphNode.</param>
        /// <returns></returns>
        public GraphNode InsertTempNode(Vector2 pos)
        {            
            // How many pixels wide/high is a single cluster? This will be needed to go from
            // screen size, to cluster index.
            Int32 pixelsPerClusterX = mClusterSize * mGetMapInfoMsg.mInfo_Out.mTileWidth;
            Int32 pixelsPerClusterY = mClusterSize * mGetMapInfoMsg.mInfo_Out.mTileHeight;

            Point index = new Point((Int32)(pos.X / pixelsPerClusterX), (Int32)(pos.Y / pixelsPerClusterY));

            GraphNode node = null;

            if (index.X > 0 && index.Y > 0 && index.X < mClusters.GetLength(0) && index.Y < mClusters.GetLength(1))
            {
                Cluster cluster = mClusters[index.X, index.Y];

                mGetTileAtPositionMsg.Reset();
                mGetTileAtPositionMsg.mPosition_In = pos;
                WorldManager.pInstance.pCurrentLevel.OnMessage(mGetTileAtPositionMsg);

                // Note:    We don't attempt to find an existing node with cluster.GetNodeContaining because
                //          this node will get removed once the search is complete. It is only temporary, and
                //          if we used a "real" GraphNode it would need to make sure it didn't get removed, so
                //          this is just simpler.

                if (node == null)
                {
                    node = mNodeFactory.GetNode();
                    node.pData = mGetTileAtPositionMsg.mTile_Out;

                    (node as NavMeshTileGraphNode).pIsTemporary = true;

                    // Iterate throught the nodes of a Cluster 2 at a time, linking each node with all
                    // the nodes that follow it (and back), so by the end of the loop everyone should be
                    // linked to each other.
                    // eg.  A <-> BCD
                    //      B <-> CD
                    //      C <-> D
                    for (Int32 i = 0; i < cluster.pNodes.Count; i++)
                    {
                        LinkGraphNodes(node, cluster.pNodes[i], cluster);
                    }

                    // Added the nodes to the Graph objects is not required but is needed for them to show up\
                    // in the debug render.
                    cluster.AddNode(node);
                    AddNode(node);
                }
            }

            return node;
        }

        /// <summary>
        /// Removes a GraphNode from the Graph and also removes all the associated links to and from other
        /// GraphNode objects in this Graph.
        /// </summary>
        /// <param name="node">The GraphNode to remove.</param>
        public void RemoveTempNode(GraphNode node)
        {            
            // How many pixels wide/high is a single cluster? This will be needed to go from
            // screen size, to cluster index.
            Int32 pixelsPerClusterX = mClusterSize * mGetMapInfoMsg.mInfo_Out.mTileWidth;
            Int32 pixelsPerClusterY = mClusterSize * mGetMapInfoMsg.mInfo_Out.mTileHeight;

            Point index = new Point((Int32)(node.pPosition.X / pixelsPerClusterX), (Int32)(node.pPosition.Y / pixelsPerClusterY));

            if (index.X > 0 && index.Y > 0 && index.X < mClusters.GetLength(0) && index.Y < mClusters.GetLength(1))
            {
                Cluster cluster = mClusters[index.X, index.Y];

                for (Int32 i = 0; i < cluster.pNodes.Count; i++)
                {
                    if (node != cluster.pNodes[i])
                    {
                        UnlinkGraphNodes(node, cluster.pNodes[i]);
                    }
                }

                cluster.RemoveNode(node);
            }

            //DebugCheckForReferences(node);
            mNodeFactory.RecycleNode(node);
            RemoveNode(node);

            //DebugCheckNodes();
        }

        /// <summary>
        /// Since this Graph is organized into clusters, we can severly limit the amount of debug draw needed
        /// by indexing directly into clusters base on camera position.
        /// </summary>
        /// <param name="showLinks"></param>
        public override void DebugDraw(Boolean showLinks)
        {
            //DebugCheckNodes();

            Color col = Color.Orange;

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

                    for (Int32 i = 0; i < temp.pNodes.Count; i++)
                    {
                        DrawNode(temp.pNodes[i], showLinks);
                    }

                    // Render the cluster boundaries as well.
                    MBHEngine.Math.Rectangle tempRect = temp.pTopLeft.mCollisionRect; 
                    //DebugShapeDisplay.pInstance.AddAABB(tempRect, col);
                    DebugShapeDisplay.pInstance.AddAABB(temp.pBounds, col, false);
                }
            }
        }

        /// <summary>
        /// Given a position, find the Cluster that the position is inside of.
        /// </summary>
        /// <param name="pos">A position in the world which lies inside of a Cluster bounds.</param>
        /// <returns>The Cluster which surrounds <paramref name="pos"/>.</returns>
        public Cluster GetClusterAtPosition(Vector2 pos)
        {
            // How many pixels wide/high is a single cluster? This will be needed to go from
            // screen size, to cluster index.
            Int32 pixelsPerClusterX = mClusterSize * mGetMapInfoMsg.mInfo_Out.mTileWidth;
            Int32 pixelsPerClusterY = mClusterSize * mGetMapInfoMsg.mInfo_Out.mTileHeight;

            Point index = new Point((Int32)(pos.X / pixelsPerClusterX), (Int32)(pos.Y / pixelsPerClusterY));

            if (index.X > 0 && index.Y > 0 && index.X < mClusters.GetLength(0) && index.Y < mClusters.GetLength(1))
            {
                return mClusters[index.X, index.Y];
            }

            return null;
        }

#if DEBUG
        /// <summary>
        /// Checks all Node objects in the Graph to see if they contain any bad data.
        /// </summary>
        public void DebugCheckNodes()
        {
            for (Int32 i = 0; i < pNodes.Count; i++)
            {
                DebugCheckNode(pNodes[i]);
            }
        }
#endif // DEBUG

#if DEBUG
        /// <summary>
        /// Checks if a Node and it's neighbours are erronously flagged as being not in use or contain
        /// null data.
        /// </summary>
        /// <param name="node">The node to check. The node assumed to be in use.</param>
        static public void DebugCheckNode(GraphNode node)
        {
            if (node is NavMeshTileGraphNode)
            {
                System.Diagnostics.Debug.Assert(node.mInUse, "Node being used but flag as not in use.");
                System.Diagnostics.Debug.Assert(node.pData != null, "pData is null.");

                for (Int32 j = 0; j < node.pNeighbours.Count; j++)
                {
                    System.Diagnostics.Debug.Assert(node.pNeighbours[j].mGraphNode.mInUse, "Node being used but flag as not in use.");
                    System.Diagnostics.Debug.Assert(node.pNeighbours[j].mGraphNode.pData != null, "pData is null.");
                }
            }
        }
#endif // DEBUG

#if DEBUG
        /// <summary>
        /// Checks if a node is reference by any Node in the Graph and their neighbours.
        /// </summary>
        /// <param name="node">The node to search for.</param>
        public void DebugCheckForReferences(GraphNode node)
        {
            for (Int32 i = 0; i < pNodes.Count; i++)
            {
                GraphNode next = pNodes[i];

                for (Int32 j = 0; j < next.pNeighbours.Count; j++)
                {
                    GraphNode nextNeighbour = next.pNeighbours[j].mGraphNode;

                    System.Diagnostics.Debug.Assert(nextNeighbour != node, "Found node!");
                }
            }
        }
#endif // DEBUG

#if DEBUG
        /// <summary>
        /// Walks the the Graph starting at <paramref name="node"/> and visiting all its neighbours,
        /// and then their neighbours and so on. Limited recursive depth to avoid blowing the callstack.
        /// </summary>
        /// <param name="node">The node to start the search at.</param>
        /// <param name="depth">The current depth. Leave empty.</param>
        public void DebugWalkGraphForErrors(NavMeshTileGraphNode node, Int32 depth = 0)
        {
            System.Diagnostics.Debug.Assert(node.mInUse, "Node not in use!");

            if (depth >= 1)
            {
                return;
            }

            for (Int32 i = 0; i < node.pNeighbours.Count; i++)
            {
                DebugWalkGraphForErrors(node.pNeighbours[i].mGraphNode as NavMeshTileGraphNode, depth + 1);
            }
        }
#endif // DEBUG

        /// <summary>
        /// Accessor to see how many GraphNode are currently unused.
        /// </summary>
        static public Int32 pUnusedGraphNodes
        {
            get
            {
                if (mNodeFactory != null)
                {
                    return mNodeFactory.pUnusedCount;
                }
                else
                {
                    return -1;
                }
            }
        }
    }
}
