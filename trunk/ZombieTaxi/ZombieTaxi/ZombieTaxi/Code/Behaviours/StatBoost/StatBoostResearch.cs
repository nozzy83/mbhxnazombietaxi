using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MBHEngine.Behaviour;
using MBHEngine.GameObject;
using MBHEngine.Math;
using MBHEngine.Debug;
using ZombieTaxiContentDefs.StatBoost;

namespace ZombieTaxi.StatBoost.Behaviours
{
    /// <summary>
    /// Researchs an upgrade to a stat over time.
    /// </summary>
    abstract class StatBoostResearch : MBHEngine.Behaviour.Behaviour
    {
        /// <summary>
        /// Set the GameObject getting the stat boost.
        /// </summary>
        public class SetTargetMessage : BehaviourMessage
        {
            /// <summary>
            /// The GameObject who will recieve the stat boost.
            /// </summary>
            public GameObject mTarget_In;

            /// <summary>
            /// Call this to put a message back to its default state.
            /// </summary>
            public override void Reset()
            {
                mTarget_In = null;
            }
        }

        /// <summary>
        /// Sent when the stat research is finished.
        /// </summary>
        public class OnResearchComplete : BehaviourMessage
        {
            /// <summary>
            /// Call this to put a message back to its default state.
            /// </summary>
            public override void Reset()
            {
            }
        }

        /// <summary>
        /// Allows clients to check if there are any levels actually available, prior 
        /// showing the user a prompt.
        /// </summary>
        public class GetLevelsRemainingMessage : BehaviourMessage
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
        /// Store the definition since it stores the leveling information in a convienient package.
        /// </summary>
        StatBoostResearchDefinition mDef;

        /// <summary>
        /// Tracks how long the research has been taking place.
        /// </summary>
        private StopWatch mResearchTimer;

        /// <summary>
        /// The GameObject who will the stat bonus when the research is complete.
        /// </summary>
        private GameObject mTarget;

        /// <summary>
        /// Sprite used to show progress of research.
        /// </summary>
        private GameObject mProgressBar;

        /// <summary>
        /// The current level of this type of research. Static since there can be many instances of this
        /// Behaviour and they should all be at the same level.
        /// </summary>
        protected static Int32 mNextLevel = 0;

        /// <summary>
        /// Preallocated messages to avoid GC.
        /// </summary>
        protected BehaviourMessage mMessageOnComplete;
        private OnResearchComplete mOnResearchCompleteMsg;
        private SpriteRender.GetAttachmentPointMessage mGetAttachmentPointMsg;

        /// <summary>
        /// Constructor which also handles the process of loading in the Behaviour
        /// Definition information.
        /// </summary>
        /// <param name="parentGOH">The game object that this behaviour is attached to.</param>
        /// <param name="fileName">The file defining this behaviour.</param>
        public StatBoostResearch(GameObject parentGOH, String fileName)
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

            mDef = GameObjectManager.pInstance.pContentManager.Load<StatBoostResearchDefinition>(fileName);

            mOnResearchCompleteMsg = new OnResearchComplete();
            mGetAttachmentPointMsg = new SpriteRender.GetAttachmentPointMessage();
        }

        /// <summary>
        /// Called at the end of the frame where mParentGOH was added to the GameObjectManager.
        /// </summary>
        public override void OnAdd()
        {
            mResearchTimer = StopWatchManager.pInstance.GetNewStopWatch();
        }

        /// <summary>
        /// Called at the end of the frame on which this Behaviour's mParentGOH was removed from
        /// the GameObjectManager.
        /// </summary>
        public override void OnRemove()
        {
            StopWatchManager.pInstance.RecycleStopWatch(mResearchTimer);
        }

        /// <summary>
        /// Called when the Behaviour goes from being disabled to enabled.
        /// This will NOT be called if the behaviour initialially starts enabled.
        /// </summary>
        public override void OnEnable()
        {
            if (mProgressBar == null)
            {
                mGetAttachmentPointMsg.mName_In = "ProgressBar";
                mParentGOH.OnMessage(mGetAttachmentPointMsg);

                mProgressBar = GameObjectFactory.pInstance.GetTemplate("GameObjects\\Interface\\ResearchProgressBar\\ResearchProgressBar");
                mProgressBar.pPosition = mGetAttachmentPointMsg.mPoisitionInWorld_Out;
                GameObjectManager.pInstance.Add(mProgressBar);
            }

            mResearchTimer.pLifeTime = mDef.mLevels[mNextLevel].mFramesToComplete;
        }

        /// <summary>
        /// Called when the Behaviour goes from being enabled to disable.
        /// This will NOT be called if the behaviour initially starts disabled.
        /// </summary>
        public override void OnDisable()
        {
            GameObjectManager.pInstance.Remove(mProgressBar);

            mProgressBar = null;
        }

        /// <summary>
        /// Called once per frame by the game object.
        /// </summary>
        /// <param name="gameTime">The amount of time that has passed this frame.</param>
        public override void Update(GameTime gameTime)
        {
            // Scale the progress bar to show how much time is left. Subtract from 1 so that the bar 
            // drains down instead of filling up. Not sure if that is intuative to the player though.
            mProgressBar.pScaleX = 1.0f - mResearchTimer.pPercentRemaining;

            // Have we finished researching?
            if (mResearchTimer.IsExpired())
            {
#if DEBUG
                DebugMessageDisplay.pInstance.AddConstantMessage("Research complete.");

                System.Diagnostics.Debug.Assert(null != mTarget, "Attempting to send message without a target set.");
#endif // DEBUG
                // Safety check to make sure we don't try to access a null target. This
                // is possible since the target is set after the fact.
                if (null != mTarget)
                {
                    FillOnResearchCompleteMessage();

                    mTarget.OnMessage(mMessageOnComplete);
                }

                // Turn ourselves off now that the research is complete.
                mParentGOH.SetBehaviourEnabled<StatBoostResearch>(false);

                // Announce that the research has been completed.
                mParentGOH.OnMessage(mOnResearchCompleteMsg);

                mNextLevel++;
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
            if (msg is SetTargetMessage)
            {
                SetTargetMessage temp = (SetTargetMessage)msg;

                mTarget = temp.mTarget_In;

                mResearchTimer.Restart();
            }
            else if (msg is GetLevelsRemainingMessage)
            {
                GetLevelsRemainingMessage temp = (GetLevelsRemainingMessage)msg;

                temp.mLevelsRemaining = mDef.mLevels.Length - mNextLevel;
            }
        }

        /// <summary>
        /// Gets called right before the OnResearchCompleteMessage is sent. It is the responsibility of 
        /// the derived class to use this chance to populate the message with up to date data.
        /// </summary>
        protected abstract void FillOnResearchCompleteMessage();
    }
}
