using System;
using MBHEngine.StateMachine;
using MBHEngine.Behaviour;
using MBHEngine.GameObject;
using Microsoft.Xna.Framework;
using MBHEngine.Math;
using MBHEngine.Debug;
using MBHEngine.World;
using System.Collections.Generic;
using MBHEngineContentDefs;
using ZombieTaxi.Behaviours;

namespace ZombieTaxi.States.Engineer
{
    /// <summary>
    /// State where the object tries to find a damaged wall, and upon finding one, walks
    /// over to it.
    /// </summary>
    class FSMStateRepair : FSMState
    {
        /// <summary>
        /// In the chance that no valid targets are found, this flag will signal it to try searching
        /// again later.
        /// </summary>
        private Boolean mNoValidTargets;

        /// <summary>
        /// Keep track of all the damaged tiles we find, so that one can be chosen at random.
        /// </summary>
        private List<Level.Tile> mDamagedTiles;

        /// <summary>
        /// Will be used to store tiles which have been damaged.
        /// </summary>
        private List<GameObject> mFoundTileObjects;

        /// <summary>
        /// Used in the search for damaged tiles.
        /// </summary>
        private List<GameObjectDefinition.Classifications> mTileClassifications;

        /// <summary>
        /// Debug data.
        /// </summary>
        private Vector2 mDebugOriginPos;
        private Vector2 mDebugStart;
        private Vector2 mDebugEnd;

        /// <summary>
        /// Preallocated messages.
        /// </summary>
        private PathFollow.SetTargetObjectMessage mSetTargetObjectMsg;
        private Level.GetTileAtPositionMessage mGetTileAtPositionMsg;
        private PathFind.SetDestinationMessage mSetDestinationMsg;
        private PathFind.SetSourceMessage mSetSourceMsg;
        private SpriteRender.SetActiveAnimationMessage mSetActiveAnimationMsg;
        private FiniteStateMachine.SetStateMessage mSetStateMsg;
        private Health.GetHealthMessage mGetHealthMsg;
        private FSMEngineer.SetTileToRepairMessage mSetTileToRepairMsg;

        /// <summary>
        /// Constructor.
        /// </summary>
        public FSMStateRepair()
        {
            mDamagedTiles = new List<Level.Tile>(16);
            mFoundTileObjects = new List<GameObject>(1); 
            mTileClassifications= new List<GameObjectDefinition.Classifications>(1);
            mTileClassifications.Add(GameObjectDefinition.Classifications.WALL);

            mSetTargetObjectMsg = new PathFollow.SetTargetObjectMessage();
            mGetTileAtPositionMsg = new Level.GetTileAtPositionMessage();
            mSetDestinationMsg = new PathFind.SetDestinationMessage();
            mSetSourceMsg = new PathFind.SetSourceMessage();
            mSetActiveAnimationMsg = new SpriteRender.SetActiveAnimationMessage();
            mSetStateMsg = new FiniteStateMachine.SetStateMessage();
            mGetHealthMsg = new Health.GetHealthMessage();
            mSetTileToRepairMsg = new FSMEngineer.SetTileToRepairMessage();
        }

        /// <summary>
        /// Called once when the state starts.
        /// </summary>
        public override void OnBegin()
        {
            mNoValidTargets = false;

            // Figure out where we are going this time.
            FindNextRepairPoint();

            // This state is basically going to exit the moment it hits OnUpdate if a repair point
            // wasn't found, so avoid glitches like starting a new animation for 1 frame.
            if (!mNoValidTargets)
            {
                // When patrolling the characters moves a lot slower, so the animation needs to match.
                mSetActiveAnimationMsg.mAnimationSetName_In = "Walk";
                pParentGOH.OnMessage(mSetActiveAnimationMsg);

                // FindNextPatrolPoint will have set all the PathFollow and PathFind data, now it
                // just needs to be re-enabled.
                pParentGOH.SetBehaviourEnabled<PathFollow>(true);
            }
        }

        /// <summary>
        /// Call repeatedly until it returns a valid new state to transition to.
        /// </summary>
        /// <returns>Identifier of a state to transition to.</returns>
        public override String OnUpdate()
        {
            if (mNoValidTargets)
            {
                return "WaitForRepairChance";
            }
            else
            {
                DebugShapeDisplay.pInstance.AddSegment(mDebugStart, mDebugEnd, Color.Pink);
                DebugShapeDisplay.pInstance.AddSegment(mDebugOriginPos, mDebugEnd, Color.HotPink);
            }

            return null;
        }

        /// <summary>
        /// Called once when leaving this state.  Called the frame after the Update which returned
        /// a valid state to transition to.  This is a chance to do any clean up needed.
        /// </summary>
        public override void OnEnd()
        {
            pParentGOH.pDirection.mForward = Vector2.Zero;

            pParentGOH.SetBehaviourEnabled<PathFollow>(false);
        }

        /// <summary>
        /// The main interface for communicating between behaviours.  Using polymorphism, we
        /// define a bunch of different messages deriving from BehaviourMessage.  Each behaviour
        /// can then check for particular upcasted messahe types, and either grab some data 
        /// from it (set message) or store some data in it (get message).
        /// </summary>
        /// <param name="msg">The message being communicated to the behaviour.</param>
        public override void OnMessage(ref BehaviourMessage msg)
        {
            if (msg is PathFollow.OnReachedPathEndMessage)
            {
                // Once we reach our destination, sit and wait for a spell.
                mSetStateMsg.mNextState_In = "DoRepair";
                pParentGOH.OnMessage(mSetStateMsg);
            }
            else if (msg is PathFind.OnPathFindFailedMessage)
            {
                // It may be a while until we find another valid patrol point if the player
                // has boxed in the Militant. Stop him from sliding.
                pParentGOH.pDirection.mForward = Vector2.Zero; 

                // Handle the case where the user places a wall right where the patrol is trying to
                // reach.
                FindNextRepairPoint();
            }
        }

        /// <summary>
        /// Does a search for a valid location to try to patrol to.
        /// </summary>
        private void FindNextRepairPoint()
        {
            // How far do we want to search for damaged walls?
            // This is in "tiles" not pixels.
            const int range = 16;
            const int range_half = range / 2;

            // Start by finding a center point for the search. For that we find a random
            // spot in the safe house.
            List<GameObject> safeHouses = GameObjectManager.pInstance.GetGameObjectsOfClassification(GameObjectDefinition.Classifications.SAFE_HOUSE);
            Int32 index = RandomManager.pInstance.pRand.Next(safeHouses.Count);

            // We are going to search for damaged walls in a square around that safehouse position.
            // We also need to convert it to world space.
            Vector2 startPos = safeHouses[index].pPosition - new Vector2(range_half * 8.0f, range_half * 8.0f);

            mDebugOriginPos = safeHouses[index].pPosition;
                
            mGetTileAtPositionMsg.mPosition_In = startPos;

            WorldManager.pInstance.pCurrentLevel.OnMessage(mGetTileAtPositionMsg, pParentGOH);

            // The top left most tile in our search.
            Level.Tile tile = mGetTileAtPositionMsg.mTile_Out;

            mDamagedTiles.Clear();

            // Loop through the tiles surrounding the randomly chosen center point.
            for (Int32 y = 0; y < range && tile != null; y++)
            {
                // The tile a the start of this row.
                Level.Tile startTile = tile;

                for (Int32 x = 0; x < range && tile != null; x++)
                {
                    // Is this a wall?
                    if (tile.mType == Level.Tile.TileTypes.Solid)
                    {
                        // The level only stores collision info, but we need the actual wall object so that
                        // we can check its health levels.
                        mFoundTileObjects.Clear();
                        GameObjectManager.pInstance.GetGameObjectsInRange(tile.mCollisionRect.pCenterPoint, 1.0f, ref mFoundTileObjects, mTileClassifications);

                        System.Diagnostics.Debug.Assert(mFoundTileObjects.Count == 1, "Unexpected number of objects at tile position: " + mFoundTileObjects.Count);

                        // This should never happen.
                        if (null != mFoundTileObjects[0])
                        {
                            mFoundTileObjects[0].OnMessage(mGetHealthMsg, pParentGOH);

                            // Is this wall damaged?
                            if (mGetHealthMsg.mCurrentHealth_Out < mGetHealthMsg.mMaxHealth_Out)
                            {
                                // Add it to a list so that a random damaged wall can be chosen.
                                mDamagedTiles.Add(tile);
                            }
                        }
                    }

                    // Move to the next tile.
                    tile = tile.mAdjecentTiles[(Int32)Level.Tile.AdjacentTileDir.RIGHT];
                }

                // Move down to the start of the next row.
                tile = startTile.mAdjecentTiles[(Int32)Level.Tile.AdjacentTileDir.DOWN];
            }

            bool tileFound = false;

            // Keep goind until we find a valid tile, or all posibilites have been exhusted.
            while (mDamagedTiles.Count > 0 && !tileFound) 
            {
                // Pick a random tile to try.
                Int32 damagedTileIndex = RandomManager.pInstance.pRand.Next(mDamagedTiles.Count);

                Level.Tile chosenTile = mDamagedTiles[damagedTileIndex];

                // Remove it from the list so that it can't be considered again if it is not 
                // chosen now.
                mDamagedTiles.RemoveAt(damagedTileIndex);

                // Loop through all surrounding tiles to see if any of them are empty.
                // A tile cannot be repaired if the character cannot stand next to it.
                for (UInt32 tileIndex = (UInt32)Level.Tile.AdjacentTileDir.START_HORZ; (tileIndex < (UInt32)Level.Tile.AdjacentTileDir.NUM_DIRECTIONS); tileIndex += 1)
                {
                    // Is the tile next to the damaged one empty, giving us a place to stand?
                    if (null != chosenTile.mAdjecentTiles[tileIndex] && 
                        chosenTile.mAdjecentTiles[tileIndex].mType == Level.Tile.TileTypes.Empty)
                    {
                        Level.Tile nearby = chosenTile.mAdjecentTiles[tileIndex];

                        // On the offset chance that the Follow Behaviour already has a target set.
                        mSetTargetObjectMsg.mTarget_In = null;
                        pParentGOH.OnMessage(mSetTargetObjectMsg);

                        mSetDestinationMsg.mDestination_In = nearby.mCollisionRect.pCenterPoint;
                        pParentGOH.OnMessage(mSetDestinationMsg);

                        mSetSourceMsg.mSource_In = pParentGOH.pCollisionRect.pCenterPoint;
                        pParentGOH.OnMessage(mSetSourceMsg);

                        // Store for debug drawing.
                        mDebugStart = pParentGOH.pPosition;
                        mDebugEnd = nearby.mCollisionRect.pCenterPoint;

                        mFoundTileObjects.Clear();
                        GameObjectManager.pInstance.GetGameObjectsInRange(chosenTile.mCollisionRect.pCenterPoint, 1.0f, ref mFoundTileObjects, mTileClassifications);

                        System.Diagnostics.Debug.Assert(mFoundTileObjects.Count == 1, "Unexpected number of objects at tile position: " + mFoundTileObjects.Count);

                        // This should never happen.
                        if (null != mFoundTileObjects[0])
                        {
                            mSetTileToRepairMsg.mTile_In = mFoundTileObjects[0];
                            pParentGOH.OnMessage(mSetTileToRepairMsg);
                        }

                        tileFound = true;
                    }
                }
            }

            mNoValidTargets = !tileFound;
        }
    }
}
