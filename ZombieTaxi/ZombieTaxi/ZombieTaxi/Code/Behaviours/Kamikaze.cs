using System;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using MBHEngine.GameObject;
using MBHEngine.Behaviour;
using MBHEngine.Input;
using MBHEngine.Debug;
using ZombieTaxi.Behaviours.HUD;
using MBHEngine.Math;

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
            PathFinding,    // Smartly finds a path towards the player.
            DirectApproach, // Walks towards the player without regard for obstructions.
            Frozen,         // Does nothing. Stands in place and does not render.
            BLine,          // Similar to DirectApproach except much faster, and expected to be seen my user.
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
        /// The distance at which it starts to BLine it for the target.
        /// </summary>
        private Single mBLineDistanceSqr;

        /// <summary>
        /// The speed it moves when path finding.
        /// </summary>
        private Single mMoveSpeedPathFinding;

        /// <summary>
        /// The speed it moves when direct approach pathing is used.
        /// </summary>
        private Single mMoveSpeedDirectApproach;

        /// <summary>
        /// The speed it moves when B-Lining it for the player.
        /// </summary>
        private Single mMoveSpeedBLine;

        /// <summary>
        /// Once we start BLining, we want to explode after a short amount of time.
        /// </summary>
        private StopWatch mBLineTimer;

        /// <summary>
        /// This flag allows us to easily switch to the BLine behaviour when needed. For instance,
        /// if we can't find a path to the destination.
        /// </summary>
        private Boolean mForceBLine;

        /// <summary>
        /// Preallocated messages to avoid GC.
        /// </summary>
        private PathFind.SetDestinationMessage mSetDestinationMsg;
        private PathFind.SetSourceMessage mSetSourceMsg;
        private PathFind.GetCurrentBestNodeMessage mGetCurrentBestNodeMsg;
        private PathFind.ClearDestinationMessage mClearDestinationMsg;
        private Explosive.DetonateMessage mDetonateMsg;
        private PlayerScore.IncrementScoreMessage mIncrementScoreMsg;
        private SpriteRender.SetActiveAnimationMessage mSetActiveAnimMsg;
        private PathFollow.SetTargetObjectMessage mSetTargetObjectMsg;

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
            mExplodeDistanceSqr = 8 * 8;
            mBLineDistanceSqr = 40 * 40;
            mCloseRangeDistanceSqr = 88 * 88;
            mFrozenDistanceSqr = 160 * 160;

            mMoveSpeedPathFinding = 0.8f;
            mMoveSpeedDirectApproach = 0.5f;
            mMoveSpeedBLine = mMoveSpeedDirectApproach * 1.5f;

            mForceBLine = false;

            mBLineTimer = StopWatchManager.pInstance.GetNewStopWatch();
            mBLineTimer.pLifeTime = 120.0f;
            mBLineTimer.pIsPaused = true;
            mBLineTimer.SetUpdatePass(MBHEngineContentDefs.BehaviourDefinition.Passes.DEFAULT);

            // Allocate these ahead of time to avoid triggering GC.
            mSetDestinationMsg = new PathFind.SetDestinationMessage();
            mSetSourceMsg = new PathFind.SetSourceMessage();
            mGetCurrentBestNodeMsg = new PathFind.GetCurrentBestNodeMessage();
            mClearDestinationMsg = new PathFind.ClearDestinationMessage();
            mDetonateMsg = new Explosive.DetonateMessage();
            mIncrementScoreMsg = new PlayerScore.IncrementScoreMessage();
            mSetActiveAnimMsg = new SpriteRender.SetActiveAnimationMessage();
            mSetTargetObjectMsg = new PathFollow.SetTargetObjectMessage();
        }

        /// <summary>
        /// Destructor.
        /// </summary>
        ~Kamikaze()
        {
            StopWatchManager.pInstance.RecycleStopWatch(mBLineTimer);
        }

        /// <summary>
        /// Called at the end of the frame on which this Behaviour's mParentGOH was removed from
        /// the GameObjectManager.
        /// </summary>
        public override void OnRemove()
        {
            mParentGOH.OnMessage(mClearDestinationMsg);
        }

        /// <summary>
        /// Called once per frame by the game object.
        /// </summary>
        /// <param name="gameTime">The amount of time that has passed this frame.</param>
        public override void Update(GameTime gameTime)
        {
            GameObject player = GameObjectManager.pInstance.pPlayer;

            // Don't try anything until the player has actually been loaded.
            if (player == null) return;

            mParentGOH.pDoRender = true;

            // Once the Kamikaze gets close to the player it stops trying to path find, and starts charging at the player.
            Single distToPlayer = Vector2.DistanceSquared(mParentGOH.pPosition, player.pPosition);

            // Once they are close enough, exlode!
            if (distToPlayer < mExplodeDistanceSqr)
            {
                //DebugMessageDisplay.pInstance.AddConstantMessage("Reached explosion Distance.");

                mParentGOH.OnMessage(mDetonateMsg);
                GameObjectManager.pInstance.Remove(mParentGOH);

                mParentGOH.SetBehaviourEnabled<PathFollow>(false);
            }
            else if (distToPlayer < mBLineDistanceSqr || mForceBLine)
            {
                // BLining it means just running straight at the player, regardless
                // of what is in the way. This can result is dumb looking AI, but
                // we have some ways of hiding it, such as exploding after a certain
                // amount of time.
                //

                // Run at the player.
                mParentGOH.pDirection.mForward = Vector2.Normalize(player.pPosition - mParentGOH.pPosition);

                // If we weren't already charging update some stuff.
                if (mCurrentFollowType != FollowType.BLine)
                {
                    // Reset the timer that counts down to when this guys should explode.
                    mBLineTimer.Restart();

                    // Start the timer.
                    mBLineTimer.pIsPaused = false;

                    // The BLining is a very fast run.
                    mParentGOH.pDirection.mSpeed = mMoveSpeedBLine;

                    // Update the state.
                    mCurrentFollowType = FollowType.BLine;

                    // Give him a special animation to show that he is pissed!
                    mSetActiveAnimMsg.mAnimationSetName_In = "RunMad";
                    mParentGOH.OnMessage(mSetActiveAnimMsg);

                    // If the path finder is running we need to stop it so it doesn't keep running 
                    // during the BLine and bog things down.
                    mParentGOH.OnMessage(mClearDestinationMsg);

                    mParentGOH.SetBehaviourEnabled<PathFollow>(false);
                }

                // After a certain number of frames just explode.
                if (mBLineTimer.IsExpired())
                {
                    mParentGOH.OnMessage(mDetonateMsg);
                    GameObjectManager.pInstance.Remove(mParentGOH);
                }
            }
            // When they are really faraway, freeze them in place.  This is for performance issues, as well as
            // making sure the player doesnt have every enemy in the game attacking at the same time.
            else if (distToPlayer > mFrozenDistanceSqr)
            {
                //DebugMessageDisplay.pInstance.AddConstantMessage("In Frozen Distance.");

                if (mCurrentFollowType != FollowType.Frozen)
                {
                    mCurrentFollowType = FollowType.Frozen;
                    mParentGOH.pDirection.mSpeed = 0.0f;
                    mParentGOH.pDoRender = false;
                    mParentGOH.SetBehaviourEnabled<PathFollow>(false);
                }
            }
            // They are close enough to want to attack the player, but far enoguh away that smart path finding
            // isn't needed.
            // Note: We avoid going back from PathFinding to DirectApproach because that can cause the entity to try
            //       and go back and forth between the two ranges.
            else if (distToPlayer > mCloseRangeDistanceSqr && mCurrentFollowType != FollowType.PathFinding)
            {
                // Path finding at this level just means walking towards the player.  This will result in getting
                // stuck on walls and things of that nature.
                mParentGOH.pDirection.mForward = Vector2.Normalize(player.pPosition - mParentGOH.pPosition);

                // If we weren't already charging update some stuff.
                if (mCurrentFollowType != FollowType.DirectApproach)
                {
                    mParentGOH.pDirection.mSpeed = mMoveSpeedDirectApproach;
                    mParentGOH.SetBehaviourEnabled<PathFollow>(false);
                    mCurrentFollowType = FollowType.DirectApproach;
                }
            }
            // If we hit this point we are in the sweet spot where they are close enough to start really attempting
            // to get to the player.
            else
            {
                if (mCurrentFollowType != FollowType.PathFinding)
                {
                    //DebugMessageDisplay.pInstance.AddConstantMessage("Starting Path Finding.");

                    mParentGOH.pDirection.mSpeed = mMoveSpeedPathFinding;

                    mParentGOH.SetBehaviourEnabled<PathFollow>(true);
                    mSetTargetObjectMsg.mTarget_In = GameObjectManager.pInstance.pPlayer;
                    mParentGOH.OnMessage(mSetTargetObjectMsg);

                    mCurrentFollowType = FollowType.PathFinding;
                }
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
            if (msg is Health.OnZeroHealthMessage)
            {
                mIncrementScoreMsg.mAmount_In = 10;
                GameObjectManager.pInstance.BroadcastMessage(mIncrementScoreMsg, mParentGOH);
            }
            else if (msg is PathFind.OnPathFindFailedMessage)
            {
                // If we didn't find the path in a single frame, just give up and BLine it for 
                // the target.
                mForceBLine = true;
            }
        }

#if ALLOW_GARBAGE
        /// <summary>
        /// Returns a bunch of information about the behaviour which can be dumped to
        /// a debug display for debugging at runtime.
        /// </summary>
        /// <returns>A formatted string of debug information.</returns>
        public override String[] GetDebugInfo()
        {
            String [] info = new String[1];

            info[0] = "Follow: " + mCurrentFollowType.ToString();

            return info;
        }
#endif // ALLOW_GARBAGE
    }
}
