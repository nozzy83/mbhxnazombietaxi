using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MBHEngine.Debug;

namespace MBHEngine.Math
{
    /// <summary>
    /// Singleton class used to manage Stop Watches.  It keeps the up to date and recycles them
    /// as needed.
    /// 
    /// A StopWatch is a simple little time that counts to zero, all clients to avoid having "curTime"
    /// "maxTime" copied all over the place.  Their timing is all based on Frames, not actual time.
    /// </summary>
    public class StopWatchManager
    {
        /// <summary>
        /// Static instance of the class; this is a singleton class.
        /// </summary>
        private static StopWatchManager mInstance = null;

        /// <summary>
        /// The list of all currently active StopWatches.
        /// </summary>
        private List<StopWatch> mActiveWatches;

        /// <summary>
        /// The preallocated list of watches.  When a client requests a new StopWatch it comes from this
        /// list and goes into the Active list.
        /// </summary>
        private Stack<StopWatch> mExpiredWatches;

        /// <summary>
        /// Constructor.
        /// </summary>
        private StopWatchManager()
        {
            // How many watches this manager creates.  If we go over this limit, the game will 
            // throw an exception.
            Int32 numWatches = 8192 * 2;

            // The active list starts empty and will become populated as clients request new StopWatch objects.
            mActiveWatches = new List<StopWatch>(numWatches);

            // All StopWatch objects we manage start in the expired Stack.
            mExpiredWatches = new Stack<StopWatch>(numWatches);

            for (int i = 0; i < numWatches; i++)
            {
                StopWatch temp = new StopWatch();
                mExpiredWatches.Push(temp);
            }
        }

        /// <summary>
        /// Clients call this before the first use of the Singleton.
        /// </summary>
        public void Initialize()
        {
        }

        /// <summary>
        /// Clients need to call this once a frame to make sure all managed StopWatch objects
        /// get their time updated.
        /// </summary>
        public void Update()
        {
            DebugMessageDisplay.pInstance.AddDynamicMessage("Stop Watches (Active): " + mActiveWatches.Count);

            // Loop through every active StopWatch and increment the frame count.
            for (Int32 i = 0; i < mActiveWatches.Count; i++)
            {
                mActiveWatches[i].AdvanceFrames();
            }
        }

        /// <summary>
        /// Clients wishing to create a StopWatch which is automatically updated should call this and use
        /// the returned StopWatch.
        /// </summary>
        /// <returns>A StopWatch which is managed by this class.</returns>
        public StopWatch GetNewStopWatch()
        {
            // Make sure we didn't go over the limit.
            if (mExpiredWatches.Count == 0)
            {
                System.Diagnostics.Debug.Assert(false, "Ran out of stop watches.  Increase amount.");

                // Hopefully all cases get caught in Debug, but incase one was missed, have a fallback
                // where we allocate additional StopWatch objects to avoid crashes.
                //
                Int32 numWatches = 128;
                for (int i = 0; i < numWatches; i++)
                {
                    StopWatch temp = new StopWatch();
                    mExpiredWatches.Push(temp);
                }
            }

            // Pop another StopWatch off the expired stack, and add it to the active list.
            StopWatch s = mExpiredWatches.Pop();
            mActiveWatches.Add(s);

            // Reset any values already set in this StopWatch from previous uses.
            s.ResetValues();

            // Now return it to the caller for them to use however they wish.
            return s;
        }

        public void RecycleStopWatch(StopWatch watch)
        {
            // If the watch is being managed by the Active list, remove it from there and put it back into
            // the Expired stack so that someone else can use it.
            //if (mActiveWatches.Contains(watch))
            {
                mActiveWatches.Remove(watch);
                mExpiredWatches.Push(watch);
            }
        }
        
        /// <summary>
        /// Access to the single instance of the class.
        /// </summary>
        public static StopWatchManager pInstance
        {
            get
            {
                if(mInstance == null)
                {
                    mInstance = new StopWatchManager();
                }

                return mInstance;
            }
        }
    }
}
