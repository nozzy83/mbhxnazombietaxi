using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MBHEngine.Behaviour;
using MBHEngine.GameObject;
using MBHEngine.Math;
using MBHEngine.Debug;
using ZombieTaxiContentDefs.StatBoost;
using ZombieTaxi.Behaviours;

namespace ZombieTaxi.StatBoost.Behaviours
{
    /// <summary>
    /// Researchs an upgrade to a stat over time.
    /// </summary>
    class GunStatBoostResearch : ZombieTaxi.StatBoost.Behaviours.StatBoostResearch
    {

        /// <summary>
        /// Allows clients to check if there are any levels actually available, prior 
        /// showing the user a prompt.
        /// </summary>
        /// <remarks>
        /// This needs to be done for each stat boost type because the message is sent to 
        /// every object in the game, so it cannot be handled properly by the base class
        /// level.
        /// </remarks>
        public class GetGunLevelsRemainingMessage : BehaviourMessage
        {
            /// <summary>
            /// The number of levels still available for upgrading to.
            /// </summary>
            public Int32 mLevelsRemaining;

            /// <summary>
            /// Call this to put a message back to its default state.
            /// </summary>
            public override void Reset()
            {
                mLevelsRemaining = 0;
            }
        }

        /// <summary>
        /// Store the definition for later use since it stores the leveling information in a
        /// nice clean way.
        /// </summary>
        private GunStatBoostResearchDefinition mDef;

        /// <summary>
        /// The current level of this type of research. Static since there can be many instances of this
        /// Behaviour and they should all be at the same level.
        /// It must be defined at this higher level (not in StatBoostResearch) because that would mean that
        /// all types of StatBoostResearch would share the same level.
        /// </summary>
        private static Int32 mNextLevel = 0;

        /// <summary>
        /// Constructor which also handles the process of loading in the Behaviour
        /// Definition information.
        /// </summary>
        /// <param name="parentGOH">The game object that this behaviour is attached to.</param>
        /// <param name="fileName">The file defining this behaviour.</param>
        public GunStatBoostResearch(GameObject parentGOH, String fileName)
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

            mDef = GameObjectManager.pInstance.pContentManager.Load<GunStatBoostResearchDefinition>(fileName);

            mMessageOnComplete = new TwinStick.IncrementGunLevelMessage();
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
            if (msg is GetGunLevelsRemainingMessage)
            {
                GetGunLevelsRemainingMessage temp = (GetGunLevelsRemainingMessage)msg;

                temp.mLevelsRemaining = mDef.mLevels.Length - pNextLevel;
            }
            else
            {
                base.OnMessage(ref msg);
            }
        }

        /// <summary>
        /// Gets called right before the OnResearchCompleteMessage is sent. It is the responsibility of 
        /// the derived class to use this chance to populate the message with up to date data.
        /// </summary>
        protected override void FillOnResearchCompleteMessage()
        {
            TwinStick.IncrementGunLevelMessage temp = (TwinStick.IncrementGunLevelMessage)mMessageOnComplete;

            temp.mIncrementAmount_In = mDef.mLevels[mNextLevel].mIntValue;
        }

        /// <summary>
        /// Required by the base class to track the current level across all instances of the class.
        /// </summary>
        protected override Int32 pNextLevel
        {
            get
            {
                return mNextLevel;
            }
            set
            {
                mNextLevel = value;
            }
        }
    }
}
