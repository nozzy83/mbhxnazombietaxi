using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MBHEngine.Behaviour;
using Microsoft.Xna.Framework;
using MBHEngine.GameObject;
using MBHEngine.StateMachine;
using ZombieTaxiContentDefs;
using Microsoft.Xna.Framework.Graphics;
using MBHEngineContentDefs;
using MBHEngine.Debug;
using MBHEngine.World;
using MBHEngine.Math;

namespace ZombieTaxi.Behaviours
{
    /// <summary>
    /// The civilian type of stranded.
    /// </summary>
    class Civilian : MBHEngine.Behaviour.FiniteStateMachine
    {
        #region FSMStates

        /// <summary>
        /// State where the Game Object sits in a cowering pose.
        /// </summary>
        private class FSMStateCower : FSMState
        {
            /// <summary>
            /// Preallocate messages to avoid GC.
            /// </summary>
            private SpriteRender.SetActiveAnimationMessage mSetActiveAnimationMsg;

            /// <summary>
            /// Constructor.
            /// </summary>
            public FSMStateCower()
            {
                mSetActiveAnimationMsg = new SpriteRender.SetActiveAnimationMessage();
            }

            /// <summary>
            /// Called once when the state starts.
            /// </summary>
            public override void OnBegin()
            {
                mSetActiveAnimationMsg.mAnimationSetName = "Hide";
                pParentGOH.OnMessage(mSetActiveAnimationMsg);
            }

            /// <summary>
            /// Call repeatedly until it returns a valid new state to transition to.
            /// </summary>
            /// <returns>Identifier of a state to transition to.</returns>
            public override String OnUpdate()
            {
                if (pParentGOH.pCollisionRect.Intersects(GameObjectManager.pInstance.pPlayer.pCollisionRect))
                {
                    return "Follow";
                }

                return null;
            }
        }

        /// <summary>
        /// State where the game object follows its target.
        /// </summary>
        private class FSMStateFollowTarget : FSMState
        {
            /// <summary>
            /// Used to store any safe houses in range.
            /// </summary>
            private List<GameObject> mSafeHouseInRange;

            /// <summary>
            /// A list of the types of objects that this does damage to when exploding.
            /// </summary>
            private List<GameObjectDefinition.Classifications> mSafeHouseClassifications;

            /// <summary>
            /// Preallocate messages to avoid GC.
            /// </summary>
            private SpriteRender.SetActiveAnimationMessage mSetActiveAnimationMsg;
            private PathFind.SetDestinationMessage mSetDestinationMsg;
            private PathFind.SetSourceMessage mSetSourceMsg;
            private PathFind.GetCurrentBestNode mGetCurrentBestNodeMsg;
            private SetSafeHouseMessage mSetSafeHouseMsg;

            /// <summary>
            /// Constructor.
            /// </summary>
            public FSMStateFollowTarget()
            {
                // We need to detect when the GameObject reaches a safe house and to do so we need
                // to do a collision check against all objects of a particular classification, in this
                // case "SAFE_HOUSE".  We preallocate the two lists needed to do the check to avoid
                // triggering the GC.
                mSafeHouseInRange = new List<GameObject>(16);
                mSafeHouseClassifications = new List<GameObjectDefinition.Classifications>();
                mSafeHouseClassifications.Add(GameObjectDefinition.Classifications.SAFE_HOUSE);

                mSetActiveAnimationMsg = new SpriteRender.SetActiveAnimationMessage();
                mSetDestinationMsg = new PathFind.SetDestinationMessage();
                mSetSourceMsg = new PathFind.SetSourceMessage();
                mGetCurrentBestNodeMsg = new PathFind.GetCurrentBestNode();
                mSetSafeHouseMsg = new SetSafeHouseMessage();
            }

            /// <summary>
            /// Called once when the state starts.
            /// </summary>
            public override void OnBegin()
            {
                mSetActiveAnimationMsg.mAnimationSetName = "Run";
                pParentGOH.OnMessage(mSetActiveAnimationMsg);

                mSetSourceMsg.mSource = pParentGOH.pOrientation.mPosition;
                pParentGOH.OnMessage(mSetSourceMsg);
                mSetDestinationMsg.mDestination = GameObjectManager.pInstance.pPlayer.pOrientation.mPosition;
                pParentGOH.OnMessage(mSetDestinationMsg);
            }

            /// <summary>
            /// Call repeatedly until it returns a valid new state to transition to.
            /// </summary>
            /// <returns>Identifier of a state to transition to.</returns>
            public override String OnUpdate()
            {
                Follow();

                // After moving this frame, check if we are within rangle of a SafeHouse.
                mSafeHouseInRange.Clear();
                GameObjectManager.pInstance.GetGameObjectsInRange(pParentGOH, ref mSafeHouseInRange, mSafeHouseClassifications);

                // Are we intersecting with any safehouses?
                if (mSafeHouseInRange.Count != 0)
                {
#if ALLOW_GARBAGE
                    DebugMessageDisplay.pInstance.AddConstantMessage("Reached SafeHouse.");
#endif

                    // If there are multiple safehouses overlapping we just take the first one we find.
                    mSetSafeHouseMsg.mSafeHouse = mSafeHouseInRange[0];
                    pParentGOH.OnMessage(mSetSafeHouseMsg);

                    // Now just stand around for a little bit.
                    return "WaitInSafeHouse";
                }
                // Has the Player run too far away causing us to get scared?
                else if (Vector2.DistanceSquared(GameObjectManager.pInstance.pPlayer.pOrientation.mPosition, pParentGOH.pOrientation.mPosition) > 64 * 64)
                {
                    return "Cower";
                }
                // Are we close enough that we should just stand still until the player starts moving again.
                else if (Vector2.DistanceSquared(GameObjectManager.pInstance.pPlayer.pOrientation.mPosition, pParentGOH.pOrientation.mPosition) < 16 * 16)
                {
                    return "Stay";
                }

                return null;
            }

            /// <summary>
            /// Called once when leaving this state.  Called the frame after the Update which returned
            /// a valid state to transition to.
            /// </summary>
            public override void OnEnd()
            {
                // Clear the forward direction of this object so that it doesn't keep moving.
                pParentGOH.pDirection.mForward = Vector2.Zero;
            }

            /// <summary>
            /// Logic for basic follow behaviour.  Uses path find behaviour to follow the player.
            /// </summary>
            /// <remarks>
            /// This is almost identicle to what is found in the Kamikaze Behaviour.  They should be combined.
            /// </remarks>
            private void Follow()
            {
                GameObject player = GameObjectManager.pInstance.pPlayer;

                // Get the curent path to the player. It may not be complete at this point, but should include enough
                // information to start moving.
                pParentGOH.OnMessage(mGetCurrentBestNodeMsg);

                // If we have a best node chosen (again maybe not a complete path, but the best so far), start
                // moving towards the next point on the path.
                if (mGetCurrentBestNodeMsg.mBest != null)
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
                    Single minDist = pParentGOH.pDirection.mSpeed * 2.0f;

                    // Once we are within one unit of the target consider it reached.
                    if (Vector2.Distance(p.mTile.mCollisionRect.pCenterPoint, pParentGOH.pOrientation.mPosition) <= minDist)
                    {
                        // This node has been reached, so next update it will start moving towards the next node.
                        p.mReached = true;

                        // Recalculate the path every time we reach a node in the path.  This accounts for things like
                        // the target moving.
                        //DebugMessageDisplay.pInstance.AddConstantMessage("Reached target.  Setting new destination.");

                        mSetSourceMsg.mSource = pParentGOH.pOrientation.mPosition;
                        pParentGOH.OnMessage(mSetSourceMsg);
                        mSetDestinationMsg.mDestination = player.pOrientation.mPosition;
                        pParentGOH.OnMessage(mSetDestinationMsg);
                    }

                    //DebugMessageDisplay.pInstance.AddConstantMessage("Moving towards target.");

                    // Move towards the nodes center point.
                    Vector2 d = p.mTile.mCollisionRect.pCenterPoint - pParentGOH.pOrientation.mPosition;
                    if (d.Length() != 0.0f)
                    {
                        d = Vector2.Normalize(d);
                        pParentGOH.pDirection.mForward = d;
                    }
                }
                else
                {
                    //DebugMessageDisplay.pInstance.AddConstantMessage("Setting first path destination.");

                    // If we don't have a destination set yet, set it up now.
                    mSetSourceMsg.mSource = pParentGOH.pOrientation.mPosition;
                    pParentGOH.OnMessage(mSetSourceMsg);
                    mSetDestinationMsg.mDestination = player.pOrientation.mPosition;
                    pParentGOH.OnMessage(mSetDestinationMsg);
                }
            }
        }

        /// <summary>
        /// State where the Game Object stands in place waiting for the target to get far enough away
        /// to trigger a transition back to the follow state.
        /// </summary>
        private class FSMStateStay : FSMState
        {
            /// <summary>
            /// Preallocate messages to avoid GC.
            /// </summary>
            private SpriteRender.SetActiveAnimationMessage mSetActiveAnimationMsg;

            /// <summary>
            /// Constructor.
            /// </summary>
            public FSMStateStay()
            {
                mSetActiveAnimationMsg = new SpriteRender.SetActiveAnimationMessage();
            }

            /// <summary>
            /// Called once when the state starts.
            /// </summary>
            public override void OnBegin()
            {
                mSetActiveAnimationMsg.mAnimationSetName = "Idle";
                pParentGOH.OnMessage(mSetActiveAnimationMsg);
            }

            /// <summary>
            /// Call repeatedly until it returns a valid new state to transition to.
            /// </summary>
            /// <returns>Identifier of a state to transition to.</returns>
            public override String OnUpdate()
            {
                // Has the player moved far enough that we should start moving again?
                if (Vector2.DistanceSquared(GameObjectManager.pInstance.pPlayer.pOrientation.mPosition, pParentGOH.pOrientation.mPosition) > 24 * 24)
                {
                    return "Follow";
                }

                return null;
            }
        }

        /// <summary>
        /// State where the Game Object has reached the safehouse and should now wait there for
        /// extraction.
        /// </summary>
        private class FSMStateWaitInSafeHouse : FSMState
        {
            /// <summary>
            /// Waiting in the safe house cycles between this state and FSMStateWanderInSafeHouse.  In this state
            /// we stand around for a set period of time.  This is that time.
            /// </summary>
            private StopWatch mWatch;

            /// <summary>
            /// Preallocate messages to avoid GC.
            /// </summary>
            private SpriteRender.SetActiveAnimationMessage mSetActiveAnimationMsg;

            /// <summary>
            /// Constructor.
            /// </summary>
            public FSMStateWaitInSafeHouse()
            {
                mWatch = StopWatchManager.pInstance.GetNewStopWatch();
                mWatch.pLifeTime = 90;
                
                mSetActiveAnimationMsg = new SpriteRender.SetActiveAnimationMessage();
            }

            /// <summary>
            /// Destrucor.
            /// </summary>
            ~FSMStateWaitInSafeHouse()
            {
                StopWatchManager.pInstance.RecycleStopWatch(mWatch);
            }

            /// <summary>
            /// Called once when the state starts.
            /// </summary>
            public override void OnBegin()
            {
                mSetActiveAnimationMsg.mAnimationSetName = "Idle";
                pParentGOH.OnMessage(mSetActiveAnimationMsg);

                mWatch.Restart();
            }

            /// <summary>
            /// Call repeatedly until it returns a valid new state to transition to.
            /// </summary>
            /// <returns>Identifier of a state to transition to.</returns>
            public override String OnUpdate()
            {
                // Has enough time passed that we should try moving to a new space?
                if (mWatch.IsExpired())
                {
                    return "WanderInSafeHouse";
                }

                return null;
            }
        }

        /// <summary>
        /// State where the Game Object has reached the safehouse and should now wait there for
        /// extraction.
        /// </summary>
        private class FSMStateWanderInSafeHouse : FSMState
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
            private Level.GetTileAtPositionMessage mGetTileAtPositionMsg;
            private GetSafeHouseMessage mGetSafeHouseMsg;

            /// <summary>
            /// Constructor.
            /// </summary>
            public FSMStateWanderInSafeHouse()
            {
                mSetActiveAnimationMsg = new SpriteRender.SetActiveAnimationMessage();
                mGetTileAtPositionMsg = new Level.GetTileAtPositionMessage();
                mGetSafeHouseMsg = new GetSafeHouseMessage();
            }

            /// <summary>
            /// Called once when the state starts.
            /// </summary>
            public override void OnBegin()
            {
                GameObject curLvl = WorldManager.pInstance.pCurrentLevel;

                // Grab the tile at the source position.
                mGetTileAtPositionMsg.mPosition = pParentGOH.pOrientation.mPosition;
                curLvl.OnMessage(mGetTileAtPositionMsg);

                // Pick a random direction to move in.  Don't do diagonals because that can cause 
                // the sprite to clip through walls.
                #region PickRandomDirection
                Int32 dir = RandomManager.pInstance.RandomNumber() % 4;
                switch (dir)
                {
                    case 0:
                        {
                            dir = (Int32)Level.Tile.AdjectTileDir.LEFT;
                            break;
                        }
                    case 1:
                        {
                            dir = (Int32)Level.Tile.AdjectTileDir.UP;
                            break;
                        }
                    case 2:
                        {
                            dir = (Int32)Level.Tile.AdjectTileDir.RIGHT;
                            break;
                        }
                    case 3:
                        {
                            dir = (Int32)Level.Tile.AdjectTileDir.DOWN;
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
                Level.Tile newTarget = mGetTileAtPositionMsg.mTile.mAdjecentTiles[dir];

                // Only try to move to a tile if it is empty.
                if (newTarget.mType == Level.Tile.TileTypes.Empty)
                {
                    pParentGOH.OnMessage(mGetSafeHouseMsg);

                    // And make sure we aren't leaving the safe house.
                    if (mGetSafeHouseMsg.mSafeHouse != null &&
                        mGetSafeHouseMsg.mSafeHouse.pCollisionRect.Intersects(newTarget.mCollisionRect.pCenterPoint))
                    {
                        mSetActiveAnimationMsg.mAnimationSetName = "Walk";
                        pParentGOH.OnMessage(mSetActiveAnimationMsg);

                        SetNewTarget(mGetTileAtPositionMsg.mTile.mAdjecentTiles[dir]);
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
                Vector2 d = mTarget.mCollisionRect.pCenterPoint - pParentGOH.pOrientation.mPosition;
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
                    // We keep moving towards the target tile until a min distance for its centerpoint.
                    // The min distance is based on the speed of this Game Object to avoid overhsooting the
                    // target over and over again.
                    Single minDist = pParentGOH.pDirection.mSpeed * pParentGOH.pDirection.mSpeed;
                    if (Vector2.DistanceSquared(mTarget.mCollisionRect.pCenterPoint, pParentGOH.pOrientation.mPosition) < minDist)
                    {
                        // Target reached.  Sit around for a bit.
                        return "WaitInSafeHouse";
                    }
                }
                else
                {
                    // We had an invalid target, try again later.
                    return "WaitInSafeHouse";
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

        #endregion // FSMStates

        /// <summary>
        /// Message to store a safehouse when a state arrives at one.
        /// </summary>
        public class GetSafeHouseMessage : BehaviourMessage
        {
            public GameObject mSafeHouse;
        }

        /// <summary>
        /// Retrives the safe house previous set with GetSafeHouseMessage. 
        /// </summary>
        public class SetSafeHouseMessage : BehaviourMessage
        {
            public GameObject mSafeHouse;
        }

        /// <summary>
        /// Once the civilian reaches a safehouse they need to stay there.  To do so they will need
        /// access to the safehouse game object.  We hold on to it at the statemachine level so that
        /// multiple states can all access it.
        /// </summary>
        private GameObject mSafeHouse;

        /// <summary>
        /// Preallocate messages to avoid GC.
        /// </summary>
        private SpriteRender.SetSpriteEffectsMessage mSetSpriteFxMsg;

        /// <summary>
        /// Constructor which also handles the process of loading in the Behaviour
        /// Definition information.
        /// </summary>
        /// <param name="parentGOH">The game object that this behaviour is attached to.</param>
        /// <param name="fileName">The file defining this behaviour.</param>
        public Civilian(GameObject parentGOH, String fileName)
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

            CivilianDefinition def = GameObjectManager.pInstance.pContentManager.Load<CivilianDefinition>(fileName);

            //mFSM = new MBHEngine.StateMachine.FiniteStateMachine(mParentGOH);
            AddState(new FSMStateCower(), "Cower");
            AddState(new FSMStateFollowTarget(), "Follow");
            AddState(new FSMStateStay(), "Stay");
            AddState(new FSMStateWaitInSafeHouse(), "WaitInSafeHouse");
            AddState(new FSMStateWanderInSafeHouse(), "WanderInSafeHouse");

            mParentGOH.pDirection.mSpeed = 0.5f;

            mSetSpriteFxMsg = new SpriteRender.SetSpriteEffectsMessage();
        }

        /// <summary>
        /// Called once per frame by the game object.
        /// </summary>
        /// <param name="gameTime">The amount of time that has passed this frame.</param>
        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (mParentGOH.pDirection.mForward.X < 0)
            {
                mSetSpriteFxMsg.mSpriteEffects = SpriteEffects.FlipHorizontally;
                mParentGOH.OnMessage(mSetSpriteFxMsg);
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
            base.OnMessage(ref msg);

            if (msg is GetSafeHouseMessage)
            {
                GetSafeHouseMessage temp = (GetSafeHouseMessage)msg;
                temp.mSafeHouse = mSafeHouse;
            }
            else if (msg is SetSafeHouseMessage)
            {
                SetSafeHouseMessage temp = (SetSafeHouseMessage)msg;
                mSafeHouse = temp.mSafeHouse;
            }
        }
    }
}
