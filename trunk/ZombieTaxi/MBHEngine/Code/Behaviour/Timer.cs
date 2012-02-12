using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using MBHEngine.Debug;
using MBHEngineContentDefs;
using MBHEngine.GameObject;

namespace MBHEngine.Behaviour
{
    /// <summary>
    /// A simple timer that counts and sends messages when complete.
    /// </summary>
    public class Timer : Behaviour
    {
        /// <summary>
        /// Sent when this timer completes.
        /// </summary>
        public class OnTimerCompleteMessage : BehaviourMessage
        {
        }

        /// <summary>
        /// Turns the timer on and off, and optionally resets it to its original time.
        /// </summary>
        public class ToggleTimerMessage : BehaviourMessage
        {
            /// <summary>
            /// Should the timer be turned on or off?
            /// </summary>
            public Boolean mActivate;

            /// <summary>
            /// Should the timer be reset to its original starting time.
            /// </summary>
            public Boolean mReset;
        }

        /// <summary>
        /// Counts down towards zero based on the original starting time and the amount of time that has passed
        /// while active.
        /// </summary>
        private Double mTimePassedSeconds;

        /// <summary>
        /// The amount of time that must pass for this timer to expire.
        /// </summary>
        private Double mLifeTimeSeconds;

        /// <summary>
        /// Is the timer on right now.
        /// </summary>
        private Boolean mActive;

        /// <summary>
        /// Preallocated messages to avoid allocations mid game.
        /// </summary>
        private OnTimerCompleteMessage mOnTimerCompleteMsg;

        /// <summary>
        /// Constructor which also handles the process of loading in the Behaviour
        /// Definition information.
        /// </summary>
        /// <param name="parentGOH">The game object that this behaviour is attached to.</param>
        /// <param name="fileName">The file defining this behaviour.</param>
        public Timer(GameObject.GameObject parentGOH, String fileName)
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

            TimerDefinition def = GameObjectManager.pInstance.pContentManager.Load<TimerDefinition>(fileName);

            mTimePassedSeconds = def.mSeconds;
            mLifeTimeSeconds = def.mSeconds;
            mActive = false;

            mOnTimerCompleteMsg = new OnTimerCompleteMessage();
        }

        /// <summary>
        /// Called once per frame by the game object.
        /// </summary>
        /// <param name="gameTime">The amount of time that has passed this frame.</param>
        public override void Update(GameTime gameTime)
        {
            // Only update the timer if we are active.
            if (mActive)
            {
                // Subtract the amount of time that has passed since the last update.
                mTimePassedSeconds -= gameTime.ElapsedGameTime.TotalSeconds;

#if ALLOW_GARBAGE
                //DebugMessageDisplay.pInstance.AddDynamicMessage("Timer: " + mTimePassedSeconds);
#endif
                // Once we reach 0 the timer is done.
                if (mTimePassedSeconds <= 0)
                {
                    mParentGOH.OnMessage(mOnTimerCompleteMsg);
                    mActive = false;
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
            if (msg is ToggleTimerMessage)
            {
                ToggleTimerMessage tmp = (ToggleTimerMessage)msg;
                mActive = tmp.mActivate;
                if (tmp.mReset)
                {
                    // Reset the timer back to the original.
                    mTimePassedSeconds = mLifeTimeSeconds;
                };
            }
        }
    }
}
