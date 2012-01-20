using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MBHEngine.GameObject;

namespace MBHEngine.World
{
    public class WorldManager
    {
        /// <summary>
        /// Static instance of the class; this is a singleton class.
        /// </summary>
        private static WorldManager mInstance = null;

        /// <summary>
        /// Stores the currently level so that it can be easily accessed by other classes.
        /// </summary>
        private GameObject.GameObject mCurrentLevel;

        /// <summary>
        /// Must be called before the singleton is used.
        /// </summary>
        public void Initialize()
        {
            mCurrentLevel = new GameObject.GameObject("GameObjects\\Levels\\EnvironmentTest\\EnvironmentTest");
            GameObjectManager.pInstance.Add(mCurrentLevel);
        }

        /// <summary>
        /// Access to the single instance of the class.
        /// </summary>
        public static WorldManager pInstance
        {
            get
            {
                if (mInstance == null)
                {
                    mInstance = new WorldManager();
                }

                return mInstance;
            }
        }

        /// <summary>
        /// The current level object.
        /// </summary>
        public GameObject.GameObject pCurrentLevel
        {
            get
            {
                return mCurrentLevel;
            }
        }
    }
}
