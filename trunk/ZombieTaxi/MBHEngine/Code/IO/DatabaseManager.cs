using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework;

namespace MBHEngine.IO
{
    /// <summary>
    /// Singleton class used for storing the game variables.  This is a cheap way to
    /// communicate to different parts of the game.
    /// </summary>
    public class DatabaseManager
    {
        /// <summary>
        /// The static instance of this class, making this a singleton.
        /// </summary>
        static private DatabaseManager mInstance = null;

        /// <summary>
        /// Access to the static instance of the class.  This is the interface for our
        /// singleton.
        /// </summary>
        public static DatabaseManager pInstance
        {
            get
            {
                // If this is the first time this is called, instantiate our
                // static instance of the class.
                if (mInstance == null)
                {
                    mInstance = new DatabaseManager();
                }

                // Either way, at this point we should have an instantiated version
                // if the class.
                return mInstance;
            }
        }
    }
}
