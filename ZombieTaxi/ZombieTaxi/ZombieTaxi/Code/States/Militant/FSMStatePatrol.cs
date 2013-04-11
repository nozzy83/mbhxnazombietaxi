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

namespace ZombieTaxi.States.Militant
{
    /// <summary>
    /// State where the Game Object stands in place waiting for the target to get far enough away
    /// to trigger a transition back to the follow state.
    /// </summary>
    class FSMStatePatrol : FSMState
    {
        /// <summary>
        /// Remember the move speed when this State started so that it can be restored at the end.
        /// </summary>
        private Single mStartMoveSpeed;

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

        /// <summary>
        /// Constructor.
        /// </summary>
        public FSMStatePatrol()
        {
            mSetTargetObjectMsg = new PathFollow.SetTargetObjectMessage();
            mGetTileAtPositionMsg = new Level.GetTileAtPositionMessage();
            mSetDestinationMsg = new PathFind.SetDestinationMessage();
            mSetSourceMsg = new PathFind.SetSourceMessage();
            mSetActiveAnimationMsg = new SpriteRender.SetActiveAnimationMessage();
            mSetStateMsg = new FiniteStateMachine.SetStateMessage();
        }

        /// <summary>
        /// Called once when the state starts.
        /// </summary>
        public override void OnBegin()
        {
            // When patrolling the characters moves a lot slower, so the animation needs to match.
            mSetActiveAnimationMsg.mAnimationSetName_In = "Walk";
            pParentGOH.OnMessage(mSetActiveAnimationMsg);

            // Save the original move speed and set a slower one.
            mStartMoveSpeed = pParentGOH.pDirection.mSpeed;
            pParentGOH.pDirection.mSpeed *= 0.5f;

            // Figure out where we are going this time.
            FindNextPatrolPoint();

            // FindNextPatrolPoint will have set all the PathFollow and PathFind data, now it
            // just needs to be re-enabled.
            pParentGOH.SetBehaviourEnabled<PathFollow>(true);
        }

        /// <summary>
        /// Call repeatedly until it returns a valid new state to transition to.
        /// </summary>
        /// <returns>Identifier of a state to transition to.</returns>
        public override String OnUpdate()
        {
            DebugShapeDisplay.pInstance.AddSegment(mDebugStart, mDebugEnd, Color.Pink);
            DebugShapeDisplay.pInstance.AddSegment(mDebugOriginPos, mDebugEnd, Color.HotPink);
            
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
                mSetStateMsg.mNextState_In = "PatrolPause";
                pParentGOH.OnMessage(mSetStateMsg);
            }
            else if (msg is PathFind.OnPathFindFailedMessage)
            {
                // Handle the case where the user places a wall right where the patrol is trying to
                // reach.
                FindNextPatrolPoint();
            }
        }

        /// <summary>
        /// Does a search for a valid location to try to patrol to.
        /// </summary>
        private void FindNextPatrolPoint()
        {
            // This function will loop until if finds a valid tile to move to,
            // potentially indefinitly.
            Boolean tileFound = false;

            // We want to pick a position relative to the safe house. To do this
            // we just find a random tile in the safe house.
            List<GameObject> safeHouses = GameObjectManager.pInstance.GetGameObjectsOfClassification(GameObjectDefinition.Classifications.SAFE_HOUSE);
            Int32 index = RandomManager.pInstance.pRand.Next(safeHouses.Count);
            
            // Store this for debug drawing.
            mDebugOriginPos = safeHouses[index].pPosition;

            // Fail safe incase we don't find any valid spots.
            Int32 count = 0;
            const Int32 maxCount = 10;

            while (!tileFound && maxCount >= count)
            {
                // Start with a position in the safehouse and then get a random offset from
                // there to travel to.
                Vector2 nextPos = mDebugOriginPos;

                // Find a random offset in either direction.
                Vector2 nextOff = new Vector2();
                nextOff.X = (RandomManager.pInstance.RandomNumber() % 60) - 30;
                nextOff.Y = (RandomManager.pInstance.RandomNumber() % 60) - 30;

                // We want to avoid the area inside the safehouse for the most part, so offset the
                // chosen position over further, creating a donut area to choose from.
                if (nextOff.X < 0) nextPos.X -= 30;
                if (nextOff.X > 0) nextPos.X += 30;
                if (nextOff.Y < 0) nextPos.Y -= 30;
                if (nextOff.Y > 0) nextPos.Y += 30;

                nextPos += nextOff;

                mGetTileAtPositionMsg.mPosition_In = nextPos;
                WorldManager.pInstance.pCurrentLevel.OnMessage(mGetTileAtPositionMsg, pParentGOH);

                // Only try to travel to empty tiles.
                if (mGetTileAtPositionMsg.mTile_Out.mType == Level.Tile.TileTypes.Empty)
                {
                    // On the offset chance that the Follow Behaviour already has a target set.
                    mSetTargetObjectMsg.mTarget_In = null;
                    pParentGOH.OnMessage(mSetTargetObjectMsg);

                    mSetDestinationMsg.mDestination_In = mGetTileAtPositionMsg.mTile_Out.mCollisionRect.pCenterPoint;
                    pParentGOH.OnMessage(mSetDestinationMsg);

                    mSetSourceMsg.mSource_In = pParentGOH.pCollisionRect.pCenterPoint;
                    pParentGOH.OnMessage(mSetSourceMsg);

                    // Store for debug drawing.
                    mDebugStart = pParentGOH.pPosition;
                    mDebugEnd = mGetTileAtPositionMsg.mTile_Out.mCollisionRect.pCenterPoint;

                    tileFound = true;
                }
                else
                {
                    count++;
                }
            }

            System.Diagnostics.Debug.Assert(tileFound, "Failed to find Tile! The state likely won't function properly after this.");
        }
    }
}
