using System;
using MBHEngine.Behaviour;
using Microsoft.Xna.Framework;
using MBHEngine.GameObject;
using ZombieTaxi.States.Militant;
using MBHEngine.StateMachine;
using ZombieTaxiContentDefs;

namespace ZombieTaxi.Behaviours
{
    /// <summary>
    /// The Scout type of stranded.
    /// </summary>
    class FSMMilitant : MBHEngine.Behaviour.FiniteStateMachine
    {
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
        /// Constructor which also handles the process of loading in the Behaviour
        /// Definition information.
        /// </summary>
        /// <param name="parentGOH">The game object that this behaviour is attached to.</param>
        /// <param name="fileName">The file defining this behaviour.</param>
        public FSMMilitant(GameObject parentGOH, String fileName)
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

            FSMMilitantDefinition def = GameObjectManager.pInstance.pContentManager.Load<FSMMilitantDefinition>(fileName);

            AddState(new FSMStateCower(), "Cower");
            AddState(new FSMStateFollowTarget(), "Follow");
            AddState(new FSMStateStay(), "Stay");
            AddState(new States.Civilian.FSMStateDead(), "Dead");
            AddState(new States.Civilian.FSMStateGoToStandingPosition(), "GoToStandingPosition");
            AddState(new FSMStateWaitAtStandingPosition(), "WaitAtStandingPosition");
            AddState(new States.Civilian.FSMStateGoToExtraction(), "GoToExtraction");
            AddState(new States.Civilian.FSMStateResearchStatBoost(), "ResearchStatBoost");
            AddState(new FSMStatePatrol(), "Patrol");
            AddState(new FSMStatePauseAtPatrolPoint(), "PatrolPause");

            mParentGOH.pDirection.mSpeed = 0.625f;

            mSafeHouseScore = def.mSafeHouseScore;
        }

        /// <summary>
        /// Called once per frame by the game object.
        /// </summary>
        /// <param name="gameTime">The amount of time that has passed this frame.</param>
        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
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
                if (curState is States.Civilian.FSMStateGoToStandingPosition ||
                    curState is FSMStateWaitAtStandingPosition ||
                    curState is FSMStatePatrol ||
                    curState is FSMStatePauseAtPatrolPoint)
                {
                    AdvanceToState("GoToExtraction");
                }
            }
            else if (msg is Civilian.GetExtractionPointMessage)
            {
                Civilian.GetExtractionPointMessage temp = (Civilian.GetExtractionPointMessage)msg;
                temp.mExtractionPoint_Out = mExtractionPoint;
            }
            else if (msg is Civilian.GetSafeHouseScoreMessage)
            {
                Civilian.GetSafeHouseScoreMessage temp = (Civilian.GetSafeHouseScoreMessage)msg;
                temp.mSafeHouseScore_Out = mSafeHouseScore;
            }
            else if (msg is StrandedPopup.GetIsScoutableMessage)
            {
                // Currently the only thing required for a Stranded to be Scoutable, is that
                // they are currently in the Cower state. This can later be expanded to include
                // things like distance from the sender.
                if (GetCurrentState() is FSMStateCower)
                {
                    StrandedPopup.GetIsScoutableMessage temp = (StrandedPopup.GetIsScoutableMessage)msg;

                    temp.mIsScoutable_Out = true;
                }
            }
        }
    }
}
