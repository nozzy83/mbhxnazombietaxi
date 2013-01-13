using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MBHEngineContentDefs;
using MBHEngine.GameObject;

namespace MBHEngine.Math
{
    /// <summary>
    /// Simple Stop Watch timer.  Can be used directly on managed through the StopWatchManager.
    /// </summary>
    public class StopWatch
    {
        /// <summary>
        /// The number of frames the StopWatch counts down from.
        /// </summary>
        private Single mLifeTime;

        /// <summary>
        /// Tracks the number of frames that have passed since this StopWatch was started.
        /// </summary>
        private Single mNumFramesPassed;

        /// <summary>
        /// Clients can stop a StopWatch from counting down by pausing it.
        /// </summary>
        private Boolean mIsPaused;

        /// <summary>
        /// If populated, the object will only be updated during these passes.
        /// </summary>
        protected List<BehaviourDefinition.Passes> mUpdatePasses;

        /// <summary>
        /// Constructor.
        /// </summary>
        public StopWatch()
        {
            mUpdatePasses = new List<BehaviourDefinition.Passes>(4);

            ResetValues();
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="lifeTime">The number of frames the StopWatch counts down from.</param>
        public StopWatch(Single lifeTime)
        {
            mUpdatePasses = new List<BehaviourDefinition.Passes>(4);

            ResetValues();

            mLifeTime = lifeTime;
        }

        /// <summary>
        /// Resets all member variables so that an instance of this object can be reused.
        /// Not to be confused with Restart which restarts the timer itself.  This is for 
        /// clearing out any data that was set in this instance.
        /// </summary>
        public void ResetValues()
        {
            mLifeTime = 0;
            mNumFramesPassed = 0;
            mIsPaused = false;
            mUpdatePasses.Clear();
        }

        /// <summary>
        /// Increment the number of frames passed.  This should be called once a frame for 
        /// most cases.  If this StopWatch was created through the manager then this will be
        /// called automatically.
        /// </summary>
        /// <param name="numFrames"></param>
        public void AdvanceFrames(Single numFrames = 1)
        {
            if (!pIsPaused)
            {
                BehaviourDefinition.Passes curPass = GameObjectManager.pInstance.pCurUpdatePass;

                if (0 == mUpdatePasses.Count || mUpdatePasses.Contains(curPass))
                {
                    mNumFramesPassed += numFrames;
                }
            }
        }

        /// <summary>
        /// Checks if the StopWatch has counted down to Zero.
        /// </summary>
        /// <returns></returns>
        public Boolean IsExpired()
        {
            return (mNumFramesPassed >= mLifeTime);
        }

        /// <summary>
        /// Restart the timer.
        /// </summary>
        public void Restart()
        {
            mNumFramesPassed = 0;
        }

        /// <summary>
        /// Force the timer to just to an expired state.
        /// </summary>
        public void ForceExpire()
        {
            mNumFramesPassed = mLifeTime;
        }

        /// <summary>
        /// Sets which update passes are required to be active for this StopWatch to be updated.
        /// If not set it will be updated on every pass.
        /// </summary>
        /// <param name="pass">The pass on which this StopWatch should be updated.</param>
        public void SetUpdatePass(BehaviourDefinition.Passes pass)
        {
            mUpdatePasses.Add(pass);
        }

        /// <summary>
        /// Property defining he number of frames the StopWatch counts down from.
        /// </summary>
        public Single pLifeTime
        {
            get
            {
                return mLifeTime;
            }
            set
            {
                mLifeTime = value;
            }
        }

        /// <summary>
        /// Pausing the timer stops it from counting down.
        /// </summary>
        public Boolean pIsPaused
        {
            get
            {
                return mIsPaused;
            }
            set
            {
                mIsPaused = value;
            }
        }
    }
}
