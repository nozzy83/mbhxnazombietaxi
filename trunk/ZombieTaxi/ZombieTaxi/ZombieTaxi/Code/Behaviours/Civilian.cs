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
using ZombieTaxi.Behaviours.HUD;
using ZombieTaxi.States.Civilian;

namespace ZombieTaxi.Behaviours
{
    /// <summary>
    /// The civilian type of stranded.
    /// </summary>
    class Civilian : MBHEngine.Behaviour.FiniteStateMachine
    {
        /// <summary>
        /// Get the GameObject representing the current extraction point.
        /// </summary>
        public class GetExtractionPointMessage : BehaviourMessage
        {
            /// <summary>
            /// The current extraction point GOH.
            /// </summary>
            public GameObject mExtractionPoint_Out;

            /// <summary>
            /// Call this to put a message back to its default state.
            /// </summary>
            public override void Reset()
            {
                mExtractionPoint_Out = null;
            }
        }

        /// <summary>
        /// Retrive the score value awarded when this object reaches the SafeHouse.
        /// </summary>
        public class GetSafeHouseScoreMessage : BehaviourMessage
        {
            /// <summary>
            /// The score awarded for reaching the SafeHouse.
            /// </summary>
            public Int32 mSafeHouseScore_Out;

            /// <summary>
            /// Call this to put a message back to its default state.
            /// </summary>
            public override void Reset()
            {
                mSafeHouseScore_Out = 0;
            }
        }

        /// <summary>
        /// The currently active extraction point.
        /// </summary>
        private GameObject mExtractionPoint;

        /// <summary>
        /// The number of points awarded when this GameObject arrives
        /// at a SafeHouse.
        /// </summary>
        private Int32 mSafeHouseScore;

        /// <summary>
        /// Preallocate messages to avoid GC.
        /// </summary>
        private SpriteRender.SetSpriteEffectsMessage mSetSpriteFxMsg;
        private PathFind.ClearDestinationMessage mClearDestinationMsg;

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
            //AddState(new FSMStateWaitInSafeHouse(), "WaitInSafeHouse");
            //AddState(new FSMStateWanderInSafeHouse(), "WanderInSafeHouse");
            AddState(new FSMStateGoToStandingPosition(), "GoToStandingPosition");
            AddState(new FSMStateWaitAtStandingPosition(), "WaitAtStandingPosition");
            AddState(new FSMStateDead(), "Dead");
            AddState(new FSMStateGoToExtraction(), "GoToExtraction");
            AddState(new FSMStateResearchStatBoost(), "ResearchStatBoost");

            mParentGOH.pDirection.mSpeed = 0.5f;

            mSafeHouseScore = def.mSafeHouseScore;

            mSetSpriteFxMsg = new SpriteRender.SetSpriteEffectsMessage();
            mClearDestinationMsg = new PathFind.ClearDestinationMessage();
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
            base.Update(gameTime);

            if (mParentGOH.pDirection.mForward.X < 0)
            {
                mSetSpriteFxMsg.mSpriteEffects_In = SpriteEffects.FlipHorizontally;
                mParentGOH.OnMessage(mSetSpriteFxMsg);
            }
            else if (mParentGOH.pDirection.mForward.X > 0)
            {
                mSetSpriteFxMsg.mSpriteEffects_In = SpriteEffects.None;
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

            if (msg is Health.OnZeroHealthMessage)
            {
                AdvanceToState("Dead");
            }
            else if (msg is ExtractionPoint.OnExtractionPointActivatedMessage)
            {
                // Store the next extraction point.
                ExtractionPoint.OnExtractionPointActivatedMessage temp = (ExtractionPoint.OnExtractionPointActivatedMessage)msg;
                mExtractionPoint = msg.pSender;

                FSMState curState = GetCurrentState();

                // If anyone is in the Safe House, they should make a run for the Extraction point now.
                if (//curState is FSMStateWaitInSafeHouse ||
                    //curState is FSMStateWanderInSafeHouse ||
                    curState is FSMStateGoToStandingPosition ||
                    curState is FSMStateWaitAtStandingPosition)
                {
                    AdvanceToState("GoToExtraction");
                }
            }
            else if (msg is GetExtractionPointMessage)
            {
                GetExtractionPointMessage temp = (GetExtractionPointMessage)msg;
                temp.mExtractionPoint_Out = mExtractionPoint;
            }
            else if (msg is GetSafeHouseScoreMessage)
            {
                GetSafeHouseScoreMessage temp = (GetSafeHouseScoreMessage)msg;
                temp.mSafeHouseScore_Out = mSafeHouseScore;
            }
        }
    }
}
