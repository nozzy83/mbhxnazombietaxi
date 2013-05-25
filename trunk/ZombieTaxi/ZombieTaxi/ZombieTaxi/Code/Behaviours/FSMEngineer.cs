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

namespace ZombieTaxi.Behaviours
{
    /// <summary>
    /// The civilian type of stranded.
    /// </summary>
    class FSMEngineer : MBHEngine.Behaviour.FiniteStateMachine
    {
        /// <summary>
        /// Used for passing which Tile is being repaired between states.
        /// </summary>
        public class SetTileToRepairMessage : BehaviourMessage
        {
            /// <summary>
            /// The Tile being repaired.
            /// </summary>
            public GameObject mTile_In;

            /// <summary>
            /// Put this message back to its initial state.
            /// </summary>
            public override void Reset()
            {
                mTile_In = null;
            }
        }

        /// <summary>
        /// Used for passing which Tile is being repaired between states.
        /// </summary>
        public class GetTileToRepairMessage : BehaviourMessage
        {
            /// <summary>
            /// The Tile being repaired.
            /// </summary>
            public GameObject mTile_Out;

            /// <summary>
            /// Put this message back to its initial state.
            /// </summary>
            public override void Reset()
            {
                mTile_Out = null;
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
        /// The Tile that this guy is currently trying to repair.
        /// </summary>
        private GameObject mTileToRepair;

        /// <summary>
        /// Preallocate messages to avoid GC.
        /// </summary>
        private PathFind.ClearDestinationMessage mClearDestinationMsg;

        /// <summary>
        /// Constructor which also handles the process of loading in the Behaviour
        /// Definition information.
        /// </summary>
        /// <param name="parentGOH">The game object that this behaviour is attached to.</param>
        /// <param name="fileName">The file defining this behaviour.</param>
        public FSMEngineer(GameObject parentGOH, String fileName)
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

            FSMMedicDefinition def = GameObjectManager.pInstance.pContentManager.Load<FSMMedicDefinition>(fileName);

            AddState(new States.Common.FSMStateCower("Hide"), "Cower");
            AddState(new States.Common.FSMStateFollowTarget("Run"), "Follow");
            AddState(new States.Common.FSMStateStay(), "Stay");
            AddState(new States.Common.FSMStateGoToStandingPosition(), "GoToStandingPosition");
            AddState(new States.Common.FSMStateWaitAtStandingPosition("GameObjects\\Interface\\EngineerPopup\\EngineerPopup"), "WaitAtStandingPosition");
            AddState(new States.Common.FSMStateDead(), "Dead");
            AddState(new States.Common.FSMStateGoToExtraction(), "GoToExtraction");
            AddState(new States.Common.FSMStateResearchStatBoost(), "ResearchStatBoost");
            AddState(new States.Engineer.FSMStateRepair(), "Repair");
            AddState(new States.Engineer.FSMStateDoRepair(), "DoRepair");
            AddState(new States.Engineer.FSMStateWaitForRepairChance(), "WaitForRepairChance");

            mParentGOH.pDirection.mSpeed = 0.5f;

            mSafeHouseScore = def.mSafeHouseScore;

            mClearDestinationMsg = new PathFind.ClearDestinationMessage();
        }

        /// <summary>
        /// Called at the end of the frame on which this Behaviour's mParentGOH was removed from
        /// the GameObjectManager.
        /// </summary>
        public override void OnRemove()
        {
            mParentGOH.OnMessage(mClearDestinationMsg);

            base.OnRemove();
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
                if (curState is States.Common.FSMStateGoToStandingPosition ||
                    curState is States.Common.FSMStateWaitAtStandingPosition)
                {
                    AdvanceToState("GoToExtraction");
                }
            }
            else if (msg is FSMCivilian.GetExtractionPointMessage)
            {
                FSMCivilian.GetExtractionPointMessage temp = (FSMCivilian.GetExtractionPointMessage)msg;
                temp.mExtractionPoint_Out = mExtractionPoint;
            }
            else if (msg is FSMCivilian.GetSafeHouseScoreMessage)
            {
                FSMCivilian.GetSafeHouseScoreMessage temp = (FSMCivilian.GetSafeHouseScoreMessage)msg;
                temp.mSafeHouseScore_Out = mSafeHouseScore;
            }
            else if (msg is StrandedPopup.GetIsScoutableMessage)
            {
                // Currently the only thing required for a Civilian to be Scoutable, is that
                // they are currently in the Cower state. This can later be expanded to include
                // things like distance from the sender.
                if (GetCurrentState() is States.Common.FSMStateCower)
                {
                    StrandedPopup.GetIsScoutableMessage temp = (StrandedPopup.GetIsScoutableMessage)msg;

                    temp.mIsScoutable_Out = true;
                }
            }
            else if (msg is SetTileToRepairMessage)
            {
                SetTileToRepairMessage temp = (SetTileToRepairMessage)msg;

                mTileToRepair = temp.mTile_In;
            }
            else if (msg is GetTileToRepairMessage)
            {
                System.Diagnostics.Debug.Assert(mTileToRepair != null, "Getting mTileToRepair when it has not yet been set.");

                GetTileToRepairMessage temp = (GetTileToRepairMessage)msg;

                temp.mTile_Out = mTileToRepair;
            }
        }
    }
}
