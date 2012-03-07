﻿using System;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using MBHEngine.GameObject;
using MBHEngine.Behaviour;

namespace ZombieTaxi.Behaviours
{
    class Kamikaze : MBHEngine.Behaviour.Behaviour
    {
        /// <summary>
        /// The different types of path finding this object performs.
        /// </summary>
        private enum FollowType
        {
            None = 0,
            PathFinding,
            DirectApproach,
            Frozen,
        };

        /// <summary>
        /// The current path finding this object is performing.
        /// </summary>
        private FollowType mCurrentFollowType;

        /// <summary>
        /// The distance at which it considers itself close to the target.
        /// </summary>
        private Single mCloseRangeDistanceSqr;

        /// <summary>
        /// The distance at which it considers itself far enough away that it should become frozen.
        /// </summary>
        private Single mFrozenDistanceSqr;

        /// <summary>
        /// The distance at which is considers itself close enough to explode.
        /// </summary>
        private Single mExplodeDistanceSqr;

        /// <summary>
        /// The speed it moves when path finding.
        /// </summary>
        private Single mMoveSpeedPathFinding;

        /// <summary>
        /// The speed it moves when direct approach pathing is used.
        /// </summary>
        private Single mMoveSpeedDirectApproach;

        /// <summary>
        /// Preallocated messages to avoid GC.
        /// </summary>
        private PathFind.SetDestinationMessage mSetDestinationMsg;
        private PathFind.SetSourceMessage mSetSourceMsg;
        private PathFind.GetCurrentBestNode mGetCurrentBestNodeMsg;
        private Explosive.DetonateMessage mDetonateMsg;

        /// <summary>
        /// Constructor which also handles the process of loading in the Behaviour
        /// Definition information.
        /// </summary>
        /// <param name="parentGOH">The game object that this behaviour is attached to.</param>
        /// <param name="fileName">The file defining this behaviour.</param>
        public Kamikaze(GameObject parentGOH, String fileName)
            : base(parentGOH, fileName)
        {
        }

        /// <summary>
        /// Call this to initialize a Behaviour with data supplied in a file.
        /// </summary>
        /// <param name="fileName">The file to load from.</param>
        public override void LoadContent(String fileName)
        {
            base.LoadContent(fileName);

            mCurrentFollowType = FollowType.None;
            mCloseRangeDistanceSqr = 80 * 80;
            mFrozenDistanceSqr = 160 * 160;
            mExplodeDistanceSqr = 8 * 8;

            mMoveSpeedPathFinding = 0.8f;
            mMoveSpeedDirectApproach = 0.5f;

            // Allocate these ahead of time to avoid triggering GC.
            mSetDestinationMsg = new PathFind.SetDestinationMessage();
            mSetSourceMsg = new PathFind.SetSourceMessage();
            mGetCurrentBestNodeMsg = new PathFind.GetCurrentBestNode();
            mDetonateMsg = new Explosive.DetonateMessage();
        }

        /// <summary>
        /// Called once per frame by the game object.
        /// </summary>
        /// <param name="gameTime">The amount of time that has passed this frame.</param>
        public override void Update(GameTime gameTime)
        {
            GameObject player = GameObjectManager.pInstance.pPlayer;

            // Don't try anything until the player has actually be loaded.
            if (player == null) return;

            mParentGOH.pDoRender = true;

            // Once the Kamikaze gets close to the player it stops trying to path find, and starts charging at the player.
            Single distToPlayer = Vector2.DistanceSquared(mParentGOH.pOrientation.mPosition, player.pOrientation.mPosition);

            // Once they are close enough, exlode!
            if (distToPlayer < mExplodeDistanceSqr)
            {
                mParentGOH.OnMessage(mDetonateMsg);
                mParentGOH.pDoUpdate = false;
                GameObjectManager.pInstance.Remove(mParentGOH);
            }
            // When they are really faraway, freeze them in place.  This is for performance issues, as well as
            // making sure the player doesnt have every enemy in the game attacking at the same time.
            else if (distToPlayer > mFrozenDistanceSqr)
            {
                mCurrentFollowType = FollowType.Frozen;
                mParentGOH.pDirection.mSpeed = 0.0f;
                mParentGOH.pDoRender = false;
            }
            // They are close enough to want to attack the player, but far enoguh away that smart path finding
            // isn't needed.
            // Note: We avoid going back from PathFinding to DirectApproach because that can cause the entity to try
            //       and go back and forth between the two ranges.
            else if (distToPlayer > mCloseRangeDistanceSqr && mCurrentFollowType != FollowType.PathFinding)
            {
                // Path finding at this level just means walking towards the player.  This will result in getting
                // stuck on walls and things of that nature.
                mParentGOH.pDirection.mForward = Vector2.Normalize(player.pOrientation.mPosition - mParentGOH.pOrientation.mPosition);

                // If we weren't already charging update some stuff.
                if (mCurrentFollowType != FollowType.DirectApproach)
                {
                    mParentGOH.pDirection.mSpeed = mMoveSpeedDirectApproach;
                    mCurrentFollowType = FollowType.DirectApproach;
                }
            }
            // If we hit this point we are in the sweet spot where they are close enough to start really attempting
            // to get to the player.
            else
            {
                // Get the curent path to the player. It may not be complete at this point, but should include enough
                // information to start moving.
                mParentGOH.OnMessage(mGetCurrentBestNodeMsg);

                // If we have a best node chosen (again maybe not a complete path, but the best so far), start
                // moving towards the next point on the path.
                if (mGetCurrentBestNodeMsg.mBest != null && mCurrentFollowType == FollowType.PathFinding)
                {
                    // This is the node closest to the destination that we have found.
                    PathFind.PathNode p = mGetCurrentBestNodeMsg.mBest;

                    // Traverse back towards the source node until the previous one has already been reached.
                    // That means the current one is the next one that has not been reached yet.
                    while (p.mPrev != null && !p.mPrev.mReached)
                    {
                        p = p.mPrev;
                    }


                    // Once we are within one unit of the target consider it reached.
                    // TODO: This should realy take into account the movement speed of the GOH.
                    if (Vector2.Distance(p.mTile.mCollisionRect.pCenterPoint, mParentGOH.pOrientation.mPosition) <= 1.0f)
                    {
                        // This node has been reached, so next update it will start moving towards the next node.
                        p.mReached = true;

                        // However, if this is the destination node (or the closest we have found so far), then
                        // recalculate a path starting here.
                        //if (mGetCurrentBestNodeMsg.mBest == p)

                        // Recalculate the path every time we reach a node in the path.  This accounts for things like
                        // the target moving.
                        {
                            mSetSourceMsg.mSource = mParentGOH.pOrientation.mPosition;
                            mParentGOH.OnMessage(mSetSourceMsg);
                            mSetDestinationMsg.mDestination = player.pOrientation.mPosition;
                            mParentGOH.OnMessage(mSetDestinationMsg);
                        }
                    }
                    else
                    {

                        // Move towards the nodes center point.
                        Vector2 d = p.mTile.mCollisionRect.pCenterPoint - mParentGOH.pOrientation.mPosition;
                        d = Vector2.Normalize(d);
                        mParentGOH.pDirection.mForward = d;
                    }
                }
                else
                {
                    if (mCurrentFollowType != FollowType.PathFinding)
                    {
                        mParentGOH.pDirection.mSpeed = mMoveSpeedPathFinding;
                        mCurrentFollowType = FollowType.PathFinding;
                    }

                    // If we don't have a destination set yet, set it up now.
                    mSetSourceMsg.mSource = mParentGOH.pOrientation.mPosition;
                    mParentGOH.OnMessage(mSetSourceMsg);
                    mSetDestinationMsg.mDestination = player.pOrientation.mPosition;
                    mParentGOH.OnMessage(mSetDestinationMsg);
                }
            }
        }
    }
}
