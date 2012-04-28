using System;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using MBHEngine.GameObject;
using MBHEngine.Behaviour;
using MBHEngine.Input;
using MBHEngine.Debug;
using ZombieTaxi.Behaviours.HUD;

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
        private SpriteRender.SetSpriteEffectsMessage mSetSpriteFxMsg;
        private PlayerScore.IncrementScoreMessage mIncrementScoreMsg;

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
            mCloseRangeDistanceSqr = 88 * 88;
            mFrozenDistanceSqr = 160 * 160;
            mExplodeDistanceSqr = 8 * 8;

            mMoveSpeedPathFinding = 0.8f;
            mMoveSpeedDirectApproach = 0.5f;

            // Allocate these ahead of time to avoid triggering GC.
            mSetDestinationMsg = new PathFind.SetDestinationMessage();
            mSetSourceMsg = new PathFind.SetSourceMessage();
            mGetCurrentBestNodeMsg = new PathFind.GetCurrentBestNode();
            mDetonateMsg = new Explosive.DetonateMessage();
            mSetSpriteFxMsg = new SpriteRender.SetSpriteEffectsMessage();
            mIncrementScoreMsg = new PlayerScore.IncrementScoreMessage();
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

            /*
            if (!InputManager.pInstance.CheckAction(InputManager.InputActions.B, true))
            {
                mParentGOH.pDirection.mForward = Vector2.Zero;
                return;
            }
            else
            {
                DebugMessageDisplay.pInstance.AddConstantMessage("Updated!");
            }
            */

            mParentGOH.pDoRender = true;

            // Once the Kamikaze gets close to the player it stops trying to path find, and starts charging at the player.
            Single distToPlayer = Vector2.DistanceSquared(mParentGOH.pOrientation.mPosition, player.pOrientation.mPosition);

            // Once they are close enough, exlode!
            if (distToPlayer < mExplodeDistanceSqr)
            {
                //DebugMessageDisplay.pInstance.AddConstantMessage("Reached explosion Distance.");

                mParentGOH.OnMessage(mDetonateMsg);
                GameObjectManager.pInstance.Remove(mParentGOH);
            }
            // When they are really faraway, freeze them in place.  This is for performance issues, as well as
            // making sure the player doesnt have every enemy in the game attacking at the same time.
            else if (distToPlayer > mFrozenDistanceSqr)
            {
                //DebugMessageDisplay.pInstance.AddConstantMessage("In Frozen Distance.");

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
                    // We also want to make sure we don't try to get to the starting node since we should be 
                    // standing on top of it already (hence the check for prev.prev).
                    while (p.mPrev != null && p.mPrev.mPrev != null && !p.mPrev.mReached)
                    {
                        p = p.mPrev;
                    }

                    // The distance to check agaist is based on the move speed, since that is the amount
                    // we will move this frame, and we want to avoid trying to hit the center point directly, since
                    // that will only happen if moving in 1 pixel increments.
                    // Also, we check double move speed because we are going to move this frame no matter what,
                    // so what we are really checking is, are we going to be ther NEXT update.
                    Single minDist = mParentGOH.pDirection.mSpeed * 2.0f;

                    // Once we are within one unit of the target consider it reached.
                    if (Vector2.Distance(p.mTile.mCollisionRect.pCenterPoint, mParentGOH.pOrientation.mPosition) <= minDist)
                    {
                        // This node has been reached, so next update it will start moving towards the next node.
                        p.mReached = true;
                        
                        // However, if this is the destination node (or the closest we have found so far), then
                        // recalculate a path starting here.
                        //if (mGetCurrentBestNodeMsg.mBest == p)

                        // Recalculate the path every time we reach a node in the path.  This accounts for things like
                        // the target moving.
                        {
                            //DebugMessageDisplay.pInstance.AddConstantMessage("Reached target.  Setting new destination.");

                            mSetSourceMsg.mSource = mParentGOH.pOrientation.mPosition;
                            mParentGOH.OnMessage(mSetSourceMsg);
                            mSetDestinationMsg.mDestination = player.pOrientation.mPosition;
                            mParentGOH.OnMessage(mSetDestinationMsg);
                        }
                    }
                    //else

                    // Regardless of whether or not we are going to do a recalculation next update, move towards the 
                    // current target now.
                    {
                        //DebugMessageDisplay.pInstance.AddConstantMessage("Moving towards target.");

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
                        //DebugMessageDisplay.pInstance.AddConstantMessage("Starting Path Finding.");
                        mParentGOH.pDirection.mSpeed = mMoveSpeedPathFinding;
                        mCurrentFollowType = FollowType.PathFinding;
                    }

                    //DebugMessageDisplay.pInstance.AddConstantMessage("Setting first path destination.");

                    // If we don't have a destination set yet, set it up now.
                    mSetSourceMsg.mSource = mParentGOH.pOrientation.mPosition;
                    mParentGOH.OnMessage(mSetSourceMsg);
                    mSetDestinationMsg.mDestination = player.pOrientation.mPosition;
                    mParentGOH.OnMessage(mSetDestinationMsg);
                }
            }

            if (mParentGOH.pDirection.mForward.X < 0)
            {
                mSetSpriteFxMsg.mSpriteEffects = SpriteEffects.FlipHorizontally;
                mParentGOH.OnMessage( mSetSpriteFxMsg );
            }
            else if (mParentGOH.pDirection.mForward.X > 0)
            {
                mSetSpriteFxMsg.mSpriteEffects = SpriteEffects.None;
                mParentGOH.OnMessage(mSetSpriteFxMsg);
            }
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
            // Which type of message was sent to us?
            if (msg is Health.OnZeroHealth)
            {
                mIncrementScoreMsg.mAmount = 10;
                GameObjectManager.pInstance.BroadcastMessage(mIncrementScoreMsg, mParentGOH);
            }
        }
    }
}
