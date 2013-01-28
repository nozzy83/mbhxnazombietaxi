using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MBHEngine.Behaviour;

namespace MBHEngine.GameObject
{
    /// <summary>
    /// Peforms a kind of flood fill but with GameObject. It spawns them at every tile, until it hits
    /// a specified limit or fills a room. There is a lot of special handling for allowing single tile
    /// gaps to be treated as walls when exiting a structure but then empty when going from one room
    /// to another.
    /// </summary>
    public class GameObjectFloodFill
    {
        /// <summary>
        /// Used to store the configuration of a door. Every doorway has a set requires two solid
        /// tiles, and an empty tile, in a particular configuation. This only stores 1 solid tile
        /// because the other one is use to index into an array of DoorwayConfiguration. It is ok
        /// for additional tiles to be solid of empty; the configuration is just the requirements.
        /// </summary>
        struct DoorwayConfiguration
        {
            /// <summary>
            /// Constrctor.
            /// </summary>
            /// <param name="solid">The direction of the solid tile relative to the tile that would be a doorway.</param>
            /// <param name="empty">The direction of the emoty tile relative to teh tile that would be a doorway.</param>
            public DoorwayConfiguration(Level.Tile.AdjacentTileDir solid, Level.Tile.AdjacentTileDir empty)
            {
                mSolid = solid;
                mEmpty = empty;
            }

            /// <summary>
            /// The direction of the solid tile relative to the tile that would be a doorway.
            /// </summary>
            public Level.Tile.AdjacentTileDir mSolid;

            /// <summary>
            /// The direction of the emoty tile relative to teh tile that would be a doorway.
            /// </summary>
            public Level.Tile.AdjacentTileDir mEmpty;
        }

        /// <summary>
        /// A array which when indexed by a direction of an adjecent tile, will tell you the possible configuration which
        /// lead to the center tile being considered a doorway.
        /// </summary>
        private List<DoorwayConfiguration> [] mDoorwayConfigurationTable;

        /// <summary>
        /// A running list of every tile checked so far. Used to make sure we
        /// don't check the same tile more than once.
        /// </summary>
        List<Level.Tile> mProcessedTiles;

        /// <summary>
        /// Tiles that sit at the entrace of a new room. Can also be tiles in a hallway
        /// as those also meet the requirements of being considered a room.
        /// </summary>
        Queue<Level.Tile> mRoomStarters;

        /// <summary>
        /// The general search alorithm used here is a breath first search, and as such
        /// a temporary Queue is needed to keep track of which nodes should be searched
        /// next. In this case it adds all the tiles surrounding the current tile.
        /// </summary>
        Queue<Level.Tile> mTilesToCheck;

        /// <summary>
        /// As we find tiles which meet the requirements to be changed, they get added to 
        /// this list so that they can be changed once we determine that the room they were
        /// found in was actually small enough to fill.
        /// </summary>
        List<Level.Tile> mTilesToChange;

        /// <summary>
        /// Like tilesToChange, we don't want to add new rooms the moment we find them.
        /// In the case where a room was found as a result of traversing a room that was
        /// not able to be finished, then it should not be added to roomStarters.
        /// </summary>
        List<Level.Tile> mRoomStartersToAdd;

        /// <summary>
        /// The final list of tiles to change after calling FloodFill. This is the list used
        /// in ProcessFill.
        /// </summary>
        List<Level.Tile> mTilesToChangeFinal;

        /// <summary>
        /// The name of the type of GameObject to spawn.
        /// </summary>
        String mGameObjectTemplateName;

        /// <summary>
        /// Constructor.
        /// </summary>
        public GameObjectFloodFill()
        {
            mDoorwayConfigurationTable = BuildDoorwayConfigurationTable();

            mProcessedTiles = new List<Level.Tile>();
            mRoomStarters = new Queue<Level.Tile>();
            mTilesToCheck = new Queue<Level.Tile>();
            mTilesToChange = new List<Level.Tile>();
            mRoomStartersToAdd = new List<Level.Tile>();
            mTilesToChangeFinal = new List<Level.Tile>();
        }

        /// <summary>
        /// Does a kind of flood fill of game objects inside a structure in a way that the user 
        /// would expect, meaning 1 tile opening are treating like doors, which can connect rooms
        /// or be an exit to the outside world.
        /// </summary>
        /// <param name="firstTile">The Tile to start the fill at.</param>
        /// <param name="maxFillCount">
        /// The maximum number of tiles to search before considering 
        /// the SafeHouse filled. Any rooms to completely changed when this cap is hit will not 
        /// be filled.
        /// </param>
        /// <param name="gameObjectTemplateName">The name of the GameObject which will be flood filled.</param>
        /// <returns>True if at least one room was filled.</returns>
        public Boolean FloodFill(Level.Tile firstTile, UInt32 maxFillCount, String gameObjectTemplateName)
        {
            // Tracks whether or not any rooms were filled.
            Boolean success = false;

            // Store for use later.
            mGameObjectTemplateName = gameObjectTemplateName;

            // A new fill means any currently running should stop.
            mTilesToChangeFinal.Clear();

            // This algorithm works by breaking the world into rooms. A room is an area surrounded
            // by wall, with the one cavet that spaces 1 pixel wide are allowed when moving from
            // one room to another, but are considered a wall when exiting the entire structure.
            // Filling is done on a per room basis. If we reach maxFillCount before a room is filled,
            // none of the room gets filled in. However, any rooms previously filled in stay, and 
            // rooms still in the queue can potentially be filled if they are smaller than the one
            // that failed.
            mRoomStarters.Enqueue(firstTile);

            // Keep track of how many tiles have been changed overall in order to make sure
            // we don't go over maxFillCount over all.
            Int32 curFillCount = 0;

            // Go room by room trying to cover it in the desired object.
            while (mRoomStarters.Count > 0 && (curFillCount <= maxFillCount))
            {
                // The starting tile is the tile in the roomStarters Queue.
                Level.Tile startTile = mRoomStarters.Dequeue();

                // Clear out any data that might be sitting around from the previous room.
                mTilesToCheck.Clear();
                mTilesToChange.Clear();
                mRoomStartersToAdd.Clear();

                // There is a chance we were given a Tile that is null or not empty. In those cases
                // we just want to skip over them. It was Dequeued from roomStarters so it will be
                // forgotten.
                if (null != startTile && startTile.mType == Level.Tile.TileTypes.Empty)
                {
                    // Start the tile travesal with the roomStarter.
                    mTilesToCheck.Enqueue(startTile);

                    mProcessedTiles.Add(startTile);
                }

                // Breath first search through the tile map stopping at walls and doors, or at a point
                // where we have visiting more tiles than allowed by maxFillCount.
                while ((mTilesToCheck.Count) > 0 && (mTilesToChange.Count + curFillCount <= maxFillCount))
                {
                    // Grab the next Tile, or in the case of the first interation it will be a roomStarter.
                    Level.Tile currentTile = mTilesToCheck.Dequeue();

                    // Safety check in case we got sent a bad starting tile.
                    if (null == currentTile || currentTile.mType != Level.Tile.TileTypes.Empty)
                    {
                        continue;
                    }

                    // Should this room be completed, this tile should be changed.
                    mTilesToChange.Add(currentTile);

                    // Loop through all the surrounding tiles adding the appropriate ones to the 
                    // tiles Queue.
                    for (UInt32 tileIndex = (UInt32)Level.Tile.AdjacentTileDir.START_HORZ; (tileIndex < (UInt32)Level.Tile.AdjacentTileDir.NUM_DIRECTIONS); tileIndex += 1)
                    {
                        Level.Tile nextTile = currentTile.mAdjecentTiles[tileIndex];

                        if (null != nextTile && nextTile.mType == Level.Tile.TileTypes.Empty && // Safety check
                            false == mProcessedTiles.Contains(nextTile) && // Don't check the same tile more than once.
                            !Level.IsAttemptingInvalidDiagonalMove((Level.Tile.AdjacentTileDir)tileIndex, currentTile)) // Don't clip through diagonal walls.
                        {
                            if (IsDoorway(nextTile))
                            {
                                // If this tile is actually a doorway, it becomes the start of a new room.
                                mRoomStartersToAdd.Add(nextTile);
                            }
                            else
                            {
                                // This is just a regular tile, so it should be changed, should this room be completed.
                                mTilesToCheck.Enqueue(nextTile);
                            }

                            // This tile has been processed so it should not be checked again.
                            mProcessedTiles.Add(nextTile);
                        }
                    }
                }

                // The breath first search has completed. If it exusted all tiles, then the room is considered 
                // completed, and we can start actually changing tiles and adding the connected rooms.
                if (mTilesToCheck.Count <= 0)
                {
                    // Once any room gets filled it is considered a success.
                    success = true;

                    // This is a legit change now, so count it towards to total fill count.
                    curFillCount += mTilesToChange.Count;

                    mTilesToChangeFinal.AddRange(mTilesToChange);

                    // Any new rooms that were queued up can now be safely added as the room that
                    // linked to them was completed.
                    for (Int32 i = 0; i < mRoomStartersToAdd.Count; i++)
                    {
                        mRoomStarters.Enqueue(mRoomStartersToAdd[i]);
                    }
                }
            }

            // Don't want to be hanging onto this data.
            mProcessedTiles.Clear();
            mRoomStarters.Clear();
            mTilesToCheck.Clear();
            mTilesToChange.Clear();
            mRoomStartersToAdd.Clear();

            return success;
        }

        /// <summary>
        /// Once the Flood Fill has been started with a call to FloodFill, calling this function 
        /// will do the actual flooding.
        /// </summary>
        public void ProcessFill()
        {
            // Go through all the tiles that we determined as value tiles to change, and change them.
            if(mTilesToChangeFinal.Count > 0)
            {
                Level.Tile tile = mTilesToChangeFinal[0];

                // Create a new game object and place it at the location of the tile.
                GameObject newFloor =  GameObjectFactory.pInstance.GetTemplate(mGameObjectTemplateName);

                newFloor.pPosition = tile.mCollisionRect.pCenterPoint;

                GameObjectManager.pInstance.Add(newFloor);

                mTilesToChangeFinal.RemoveAt(0);
            }
        }

        /// <summary>
        /// A somewhat complex check to determine if a tile is what would be considered a doorway.
        /// A doorway is an empty tile, surrounded by 2 or more empty tiles on opposite sides. However,
        /// it is not quite as simple as that, since some combination block what would be the room after
        /// the door way, so we also need to verify that for a given combination of walls, there is a
        /// another empty tile at a particular location.
        /// </summary>
        /// <param name="tile"></param>
        /// <returns></returns>
        private Boolean IsDoorway(Level.Tile tile)
        {
            // Build this mapping of walls and empty spaces required to make up a doorway.
            /// <todo>Do this once.</todo>
            List<DoorwayConfiguration> [] relaventSiblings = BuildDoorwayConfigurationTable();

            // We need to loop through every surround tile and see if they are walls. If they ar walls,
            // then we start checking the relaventSiblings for the required corrisponding walls and empty tiles
            // which mean this tile is a doorway.
            for (UInt32 tileIndex = (UInt32)Level.Tile.AdjacentTileDir.START_HORZ; (tileIndex < (UInt32)Level.Tile.AdjacentTileDir.NUM_DIRECTIONS); tileIndex++)
            {
                // Grab the next tile surrounding the one we are checking.
                Level.Tile siblingTile = tile.mAdjecentTiles[tileIndex];

                // None of this matters if the adjacent tile is empty.
                if (null != siblingTile && siblingTile.mType != Level.Tile.TileTypes.Empty)
                {
                    // Every surrounding tile has a list of corrisponding tiles that when in the proper
                    // configuration mean that the center tile is a doorway. Loop through those conigurations.
                    for (Int32 i = 0; i < relaventSiblings[tileIndex].Count; i++)
                    {
                        // When siblingTile is Solid, another tile on the opposite side also needs to be solid.
                        Level.Tile adjTileSolid = tile.mAdjecentTiles[(Int32)relaventSiblings[tileIndex][i].mSolid];

                        // Is a relavent sibling solid?
                        if (null != adjTileSolid && adjTileSolid.mType != Level.Tile.TileTypes.Empty)
                        {
                            // But it isn't enough to have solid tiles on opposite sides of the center tile.
                            // There also needs to be an empty tile in the right direction or else this is
                            // just a dead end and should be considered part of the current room, not the 
                            // start of a new one.
                            Level.Tile adjEmpty = tile.mAdjecentTiles[(Int32)relaventSiblings[tileIndex][i].mEmpty];

                            // Is the relavent sibling Empty?
                            if (null != adjEmpty && adjEmpty.mType == Level.Tile.TileTypes.Empty)
                            {
                                // Once this is a doorway in one case, nothing can change that; it can only
                                // have additional paths, but that will be handled when this Tile becomes
                                // the starting point of the next room.
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Builds an array of List objects containing DoorwayConfiguration mapping to each possible adjacent
        /// tile to a potential doorway.
        /// </summary>
        /// <returns>
        /// A array which when indexed by a direction of an adjecent tile, will tell you the possible configuration which
        /// lead to the center tile being considered a doorway.
        /// </returns>
        private List<DoorwayConfiguration>[] BuildDoorwayConfigurationTable()
        {
            List<DoorwayConfiguration> [] relaventSiblings = new List<DoorwayConfiguration>[(Int32)Level.Tile.AdjacentTileDir.NUM_DIRECTIONS];

            Int32 index = (Int32)Level.Tile.AdjacentTileDir.LEFT;
            relaventSiblings[index] = new List<DoorwayConfiguration>();
            relaventSiblings[index].Add(new DoorwayConfiguration(Level.Tile.AdjacentTileDir.RIGHT_UP, Level.Tile.AdjacentTileDir.UP));
            //relaventSiblings[index].Add(new DoorCheckSiblings(Level.Tile.AdjacentTileDir.RIGHT));
            relaventSiblings[index].Add(new DoorwayConfiguration(Level.Tile.AdjacentTileDir.RIGHT_DOWN, Level.Tile.AdjacentTileDir.DOWN));

            index = (Int32)Level.Tile.AdjacentTileDir.RIGHT;
            relaventSiblings[index] = new List<DoorwayConfiguration>();
            relaventSiblings[index].Add(new DoorwayConfiguration(Level.Tile.AdjacentTileDir.LEFT_UP, Level.Tile.AdjacentTileDir.UP));
            //relaventSiblings[index].Add(new DoorCheckSiblings(Level.Tile.AdjacentTileDir.LEFT));
            relaventSiblings[index].Add(new DoorwayConfiguration(Level.Tile.AdjacentTileDir.LEFT_DOWN, Level.Tile.AdjacentTileDir.DOWN));

            index = (Int32)Level.Tile.AdjacentTileDir.UP;
            relaventSiblings[index] = new List<DoorwayConfiguration>();
            relaventSiblings[index].Add(new DoorwayConfiguration(Level.Tile.AdjacentTileDir.LEFT_DOWN, Level.Tile.AdjacentTileDir.LEFT));
            //relaventSiblings[index].Add(new DoorCheckSiblings(Level.Tile.AdjacentTileDir.DOWN));
            relaventSiblings[index].Add(new DoorwayConfiguration(Level.Tile.AdjacentTileDir.RIGHT_DOWN, Level.Tile.AdjacentTileDir.RIGHT));

            index = (Int32)Level.Tile.AdjacentTileDir.DOWN;
            relaventSiblings[index] = new List<DoorwayConfiguration>();
            relaventSiblings[index].Add(new DoorwayConfiguration(Level.Tile.AdjacentTileDir.LEFT_UP, Level.Tile.AdjacentTileDir.LEFT));
            //relaventSiblings[index].Add(new DoorCheckSiblings(Level.Tile.AdjacentTileDir.UP));
            relaventSiblings[index].Add(new DoorwayConfiguration(Level.Tile.AdjacentTileDir.RIGHT_UP, Level.Tile.AdjacentTileDir.RIGHT));

            index = (Int32)Level.Tile.AdjacentTileDir.LEFT_UP;
            relaventSiblings[index] = new List<DoorwayConfiguration>();
            relaventSiblings[index].Add(new DoorwayConfiguration(Level.Tile.AdjacentTileDir.RIGHT_UP, Level.Tile.AdjacentTileDir.UP));
            relaventSiblings[index].Add(new DoorwayConfiguration(Level.Tile.AdjacentTileDir.RIGHT, Level.Tile.AdjacentTileDir.UP));
            //relaventSiblings[index].Add(new DoorCheckSiblings(Tile.AdjacentTileDir.RIGHT_DOWN));
            relaventSiblings[index].Add(new DoorwayConfiguration(Level.Tile.AdjacentTileDir.DOWN, Level.Tile.AdjacentTileDir.LEFT));
            relaventSiblings[index].Add(new DoorwayConfiguration(Level.Tile.AdjacentTileDir.LEFT_DOWN, Level.Tile.AdjacentTileDir.LEFT));

            index = (Int32)Level.Tile.AdjacentTileDir.RIGHT_UP;
            relaventSiblings[index] = new List<DoorwayConfiguration>();
            relaventSiblings[index].Add(new DoorwayConfiguration(Level.Tile.AdjacentTileDir.LEFT_UP, Level.Tile.AdjacentTileDir.UP));
            relaventSiblings[index].Add(new DoorwayConfiguration(Level.Tile.AdjacentTileDir.LEFT, Level.Tile.AdjacentTileDir.UP));
            //relaventSiblings[index].Add(new DoorCheckSiblings(Level.Tile.AdjacentTileDir.LEFT_DOWN));
            relaventSiblings[index].Add(new DoorwayConfiguration(Level.Tile.AdjacentTileDir.DOWN, Level.Tile.AdjacentTileDir.RIGHT));
            relaventSiblings[index].Add(new DoorwayConfiguration(Level.Tile.AdjacentTileDir.RIGHT_DOWN, Level.Tile.AdjacentTileDir.RIGHT));

            index = (Int32)Level.Tile.AdjacentTileDir.LEFT_DOWN;
            relaventSiblings[index] = new List<DoorwayConfiguration>();
            relaventSiblings[index].Add(new DoorwayConfiguration(Level.Tile.AdjacentTileDir.LEFT_UP, Level.Tile.AdjacentTileDir.LEFT));
            relaventSiblings[index].Add(new DoorwayConfiguration(Level.Tile.AdjacentTileDir.UP, Level.Tile.AdjacentTileDir.LEFT));
            //relaventSiblings[index].Add(new DoorCheckSiblings(Level.Tile.AdjacentTileDir.RIGHT_UP));
            relaventSiblings[index].Add(new DoorwayConfiguration(Level.Tile.AdjacentTileDir.RIGHT, Level.Tile.AdjacentTileDir.DOWN));
            relaventSiblings[index].Add(new DoorwayConfiguration(Level.Tile.AdjacentTileDir.RIGHT_DOWN, Level.Tile.AdjacentTileDir.DOWN));

            index = (Int32)Level.Tile.AdjacentTileDir.RIGHT_DOWN;
            relaventSiblings[index] = new List<DoorwayConfiguration>();
            relaventSiblings[index].Add(new DoorwayConfiguration(Level.Tile.AdjacentTileDir.LEFT_DOWN, Level.Tile.AdjacentTileDir.DOWN));
            relaventSiblings[index].Add(new DoorwayConfiguration(Level.Tile.AdjacentTileDir.LEFT, Level.Tile.AdjacentTileDir.DOWN));
            //relaventSiblings[index].Add(new DoorCheckSiblings(Level.Tile.AdjacentTileDir.LEFT_UP));
            relaventSiblings[index].Add(new DoorwayConfiguration(Level.Tile.AdjacentTileDir.UP, Level.Tile.AdjacentTileDir.RIGHT));
            relaventSiblings[index].Add(new DoorwayConfiguration(Level.Tile.AdjacentTileDir.RIGHT_UP, Level.Tile.AdjacentTileDir.RIGHT));

            return relaventSiblings;
        }
    }
}
