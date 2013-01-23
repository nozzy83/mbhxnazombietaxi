using System;
using System.Collections.Generic;
using MBHEngine.StateMachine;
using MBHEngine.Behaviour;
using MBHEngine.GameObject;
using MBHEngineContentDefs;
using Microsoft.Xna.Framework;
using MBHEngine.Math;
using MBHEngine.World;

namespace ZombieTaxi.States.Civilian
{
    /// <summary>
    /// State where the Game Object has reached the safehouse and should now wait there for
    /// extraction.
    /// </summary>
    class FSMStateWanderInSafeHouse : FSMState
    {
        /// <summary>
        /// The tile we are going to move to.
        /// </summary>
        private Level.Tile mTarget;

        /// <summary>
        /// During this state, the GameObject should move slower, so we save the move speed so that
        /// it can be restored OnExit.
        /// </summary>
        private Single mStartMoveSpeed;

        /// <summary>
        /// Preallocate messages to avoid GC.
        /// </summary>
        private SpriteRender.SetActiveAnimationMessage mSetActiveAnimationMsg;
        private Level.GetTileAtObjectMessage mGetTileAtObjectMsg;

        /// <summary>
        /// Constructor.
        /// </summary>
        public FSMStateWanderInSafeHouse()
        {
            mSetActiveAnimationMsg = new SpriteRender.SetActiveAnimationMessage();
            mGetTileAtObjectMsg = new Level.GetTileAtObjectMessage();
        }

        /// <summary>
        /// Called once when the state starts.
        /// </summary>
        public override void OnBegin()
        {
            GameObject curLvl = WorldManager.pInstance.pCurrentLevel;

            // Grab the tile at the source position.
            mGetTileAtObjectMsg.mObject_In = pParentGOH;
            curLvl.OnMessage(mGetTileAtObjectMsg);

            // Pick a random direction to move in.  Don't do diagonals because that can cause 
            // the sprite to clip through walls.
            #region PickRandomDirection
            Int32 dir = RandomManager.pInstance.RandomNumber() % 4;
            switch (dir)
            {
            case 0:
                {
                    dir = (Int32)Level.Tile.AdjacentTileDir.LEFT;
                    break;
                }
            case 1:
                {
                    dir = (Int32)Level.Tile.AdjacentTileDir.UP;
                    break;
                }
            case 2:
                {
                    dir = (Int32)Level.Tile.AdjacentTileDir.RIGHT;
                    break;
                }
            case 3:
                {
                    dir = (Int32)Level.Tile.AdjacentTileDir.DOWN;
                    break;
                }

            }
            #endregion

            // Save the original move speed and set a slower one.
            mStartMoveSpeed = pParentGOH.pDirection.mSpeed;
            pParentGOH.pDirection.mSpeed *= 0.5f;

            // The tile we want to move to might end up being invalid, so start with that
            // assumption.
            mTarget = null;

            // The tile we are thinking about moving to.
            Level.Tile newTarget = mGetTileAtObjectMsg.mTile_Out.mAdjecentTiles[dir];

            // Only try to move to a tile if it is empty.
            if ((newTarget.mType & Level.Tile.TileTypes.Solid) != Level.Tile.TileTypes.Solid)
            {
                List<GameObject> mSafeHouseInRange = GameObjectManager.pInstance.GetGameObjectsOfClassification(GameObjectDefinition.Classifications.SAFE_HOUSE);

                for (Int32 i = 0; i < mSafeHouseInRange.Count; i++)
                {
                    GameObject safeHouse = mSafeHouseInRange[i];
                    // And make sure we aren't leaving the safe house.
                    if (safeHouse != null &&
                        safeHouse.pCollisionRect.Intersects(newTarget.mCollisionRect.pCenterPoint))
                    {
                        mSetActiveAnimationMsg.mAnimationSetName_In = "Walk";
                        pParentGOH.OnMessage(mSetActiveAnimationMsg);

                        SetNewTarget(mGetTileAtObjectMsg.mTile_Out.mAdjecentTiles[dir]);
                    }
                }
            }
        }

        /// <summary>
        /// Stores the target and updates the Game Object to move towards it.
        /// </summary>
        /// <param name="target"></param>
        private void SetNewTarget(Level.Tile target)
        {
            mTarget = target;

            // Move towards the nodes center point.
            Vector2 d = mTarget.mCollisionRect.pCenterBottom - pParentGOH.pPosition;
            if (d.Length() != 0.0f)
            {
                d = Vector2.Normalize(d);
                pParentGOH.pDirection.mForward = d;
            }
        }

        /// <summary>
        /// Call repeatedly until it returns a valid new state to transition to.
        /// </summary>
        /// <returns>Identifier of a state to transition to.</returns>
        public override String OnUpdate()
        {
            // There is a chance the target was not valid, in which case we just fall back to the
            // WaitInSafeHouse state.
            if (mTarget != null)
            {
                // Debug to show which Tile objects the Civilian is moving to and from.
                //mTarget.mType |= Level.Tile.TileTypes.CollisionChecked;
                //mGetTileAtObjectMsg.mTile.mType |= Level.Tile.TileTypes.CollisionChecked;

                // We keep moving towards the target tile until a min distance for its centerpoint.
                // The min distance is based on the speed of this Game Object to avoid overhsooting the
                // target over and over again.
                Single minDist = pParentGOH.pDirection.mSpeed * pParentGOH.pDirection.mSpeed;
                if (Vector2.DistanceSquared(mTarget.mCollisionRect.pCenterBottom, pParentGOH.pPosition) < minDist)
                {
                    // Target reached.  Sit around for a bit.
                    return "WaitInSafeHouse";
                }
            }
            else
            {
                // We had an invalid target, try again.
                return "WanderInSafeHouse";
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
            pParentGOH.pDirection.mSpeed = mStartMoveSpeed;
        }
    }
}
