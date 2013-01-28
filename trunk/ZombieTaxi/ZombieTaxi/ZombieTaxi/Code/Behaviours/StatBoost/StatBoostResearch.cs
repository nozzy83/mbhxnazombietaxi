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
    class StatBoostResearch : MBHEngine.Behaviour.Behaviour
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
        /// How long does it take for this research to complete?
        /// </summary>
        private Int32 mFramesToComplete;

        /// <summary>
        /// Tracks how long the research has been taking place.
        /// </summary>
        private StopWatch mResearchTimer;

        /// <summary>
        /// The GameObject who will the stat bonus when the research is complete.
        /// </summary>
        private GameObject mTarget;

        /// <summary>
        /// Preallocated messages to avoid GC.
        /// </summary>
        protected BehaviourMessage mMessageOnComplete;
        private OnResearchComplete mOnResearchCompleteMsg;

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

            StatBoostResearchDefinition def = GameObjectManager.pInstance.pContentManager.Load<StatBoostResearchDefinition>(fileName);

            mFramesToComplete = def.mFramesToComplete;

            mOnResearchCompleteMsg = new OnResearchComplete();
        }

        /// <summary>
        /// Called at the end of the frame where mParentGOH was added to the GameObjectManager.
        /// </summary>
        public override void OnAdd()
        {
            mResearchTimer = StopWatchManager.pInstance.GetNewStopWatch();

            mResearchTimer.pLifeTime = mFramesToComplete;
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
        /// Called once per frame by the game object.
        /// </summary>
        /// <param name="gameTime">The amount of time that has passed this frame.</param>
        public override void Update(GameTime gameTime)
        {
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
                    mTarget.OnMessage(mMessageOnComplete);
                }

                // Turn ourselves off now that the research is complete.
                mParentGOH.SetBehaviourEnabled<StatBoostResearch>(false);

                // Announce that the research has been completed.
                mParentGOH.OnMessage(mOnResearchCompleteMsg);
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
        }
    }
}
