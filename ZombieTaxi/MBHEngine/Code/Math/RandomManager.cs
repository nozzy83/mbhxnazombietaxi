using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MBHEngine.Math
{
    /// <summary>
    /// The random functionality found in System is a little overly complex for most cases,
    /// and requires that an instance of Random be declared and initialized to simply get
    /// a random number.  Depending on the code that executes that, it can often result in 
    /// getting the exact same number every time.
    /// 
    /// To avoid this, and to simplify the interface, the RandomManager class wraps everything
    /// up, and seeds the generator early on creating more random results.
    /// </summary>
    public class RandomManager
    {
        /// <summary>
        /// Static instance of the class; this is a singleton class.
        /// </summary>
        private static RandomManager mInstance = null;

        /// <summary>
        /// The main purpose of this class is to wrap this object.
        /// </summary>
        private Random rand;

        /// <summary>
        /// Constructor.
        /// </summary>
        private RandomManager()
        {
            rand = new Random();
        }

        /// <summary>
        /// Retrives a random number between 0 and MAX_INT.
        /// </summary>
        /// <remarks>If a floating point number is needed, see RandomPercent.</remarks>
        /// <returns>A random number between 0 and MAX_INT, inclusive.</returns>
        public Int32 RandomNumber()
        {
            return rand.Next();
        }

        /// <summary>
        /// Retrives a random floating point number between 0 and 1.  This is the best way to
        /// get a random floating point number.  Eg r = RandomPercent( ) * maxValue
        /// </summary>
        /// <returns>A random floating point number between 0 and 1, inclusive.</returns>
        public Double RandomPercent()
        {
            return rand.NextDouble();
        }

        /// <summary>
        /// Access to the single instance of the class.
        /// </summary>
        public static RandomManager pInstance
        {
            get
            {
                if(mInstance == null)
                {
                    mInstance = new RandomManager();
                }

                return mInstance;
            }
        }

        /// <summary>
        /// Allow clients direct access to the System.Random object so that we aren't required
        /// to wrap every piece of functionality.
        /// </summary>
        public Random pRand
        {
            get
            {
                return rand;
            }
        }
    }
}
