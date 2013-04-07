using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace MBHEngine.GameObject
{
    /// <summary>
    /// A factory for recycling GameObject.  At startup the factory should be fed a number of game object
    /// defintions and it will preallocate them for later retrival.  Those GameObject get tags as being
    /// spawned from the GameObjectFactory, and so when they are removed from the GameObjectManager, they
    /// get returned to the Factory.
    /// </summary>
    public class GameObjectFactory
    {
        /// <summary>
        /// The data which is stored in a GameObject so that it can be returned to the factory
        /// when it is no longer in use.
        /// </summary>
        public class FactoryInfo
        {
            /// <summary>
            /// Constructor.
            /// </summary>
            public FactoryInfo()
            {
                mIsManaged = false;
            }

            /// <summary>
            /// The file defining this GameObject.  This is used as a look up in the factory, so it is
            /// needed to put it back into the proper list.
            /// </summary>
            private String mTemplateName;

            /// <summary>
            /// Since every GameObject has a FactoryInfo instance, it needs to store a Boolean to say
            /// if it is actually being used.
            /// </summary>
            private Boolean mIsManaged;

            /// <summary>
            /// The key used for accessing this GameObeject in the Factory.
            /// </summary>
            public String pTemplateName
            {
                get
                {
                    return mTemplateName;
                }
                set
                {
                    // By setting a template name, this object is assumed to be managed.
                    mTemplateName = value;
                    mIsManaged = true;
                }
            }

            /// <summary>
            /// True if this Object was spawned from the Factory.
            /// </summary>
            public Boolean pIsManaged
            {
                get
                {
                    return mIsManaged;
                }
            }
        };

        /// <summary>
        /// The static instance of this class, making this a singleton.
        /// </summary>
        static private GameObjectFactory mInstance = null;

        /// <summary>
        /// The Factory stores GameObjects in Stacks, organized by type, and mapped to the filename that defined them
        /// in a Dictionary.
        /// </summary>
        private Dictionary<String, Stack<GameObject>> mObjects;

        /// <summary>
        /// Must be called prior to any other use.
        /// </summary>
        public void Initialize()
        {
            // Allocate an empty Dcictionary.  The actual Stacks of GameObjects will be allocated later.
            mObjects = new Dictionary<String, Stack<GameObject>>();
        }

        /// <summary>
        /// This should be called during initialization to preallocate all the GameObjects this Factory will
        /// manage.
        /// </summary>
        /// <param name="fileName">The file defining a particular GameObject type.  This is the same file name
        /// used when creating a GameObject from scratch.</param>
        /// <param name="amount">How many need to be created.</param>
        public void AddTemplate(String fileName, Int32 amount)
        {
            // If this the first of its kind?
            if (!mObjects.ContainsKey(fileName))
            {
                // Allocate a new stack for this type of GameObject and add it to the Dictionary,
                // indexed by the filename itself.
                Stack<GameObject> temp = new Stack<GameObject>();
                mObjects.Add(fileName, temp);
            }

            // Create the request amount of GameObjects of the requested type.
            for (Int32 i = 0; i < amount; i++)
            {
                GameObject temp = new GameObject(fileName);

                // This object needs to be recycled on death and so it gets tagged with FactoryInfo.
                temp.SetAsFactoryManaged(fileName);

                // Push the new object onto the stack, indexed into the Dictionary by the filename used
                // to create it.
                mObjects[fileName].Push(temp);
            }
        }

        /// <summary>
        /// Retreives a GameObject of a particular type if any are free.
        /// </summary>
        /// <param name="fileName">The filename defining this type of GameObject</param>
        /// <returns>A GameObject of the requested type, or null if none are available.</returns>
        public GameObject GetTemplate(String fileName)
        {
            //CheckForDupes(fileName);

            Boolean isManaged = mObjects.ContainsKey(fileName);

            // The requested type of GameObject is not managed by the Factory.  For now throw an error,
            // but at some point we may want to change this to allocate some additional ones.
            System.Diagnostics.Debug.Assert(true == isManaged, "Attempted to get template which is not managed by Factory: " + fileName);

            // Make sure this type of Object was actually added to the Dictionary at some point.
            if (isManaged)
            {
                Boolean hasRemaining = (mObjects[fileName].Count > 0);

                // There are no GameObjects of the requested type available.  For now throw an error,
                // but at some point we may want to change this to allocate some additional ones.
                System.Diagnostics.Debug.Assert(hasRemaining, "Ran out of templates of type: " + fileName);

                // Ideally this will never happen in shipped code, but leaving it here as a fail safe since
                // a lot of clients depend on this function not returning null.
                if (!hasRemaining)
                {
                    AddTemplate(fileName, 100);
                }

                GameObject go = mObjects[fileName].Pop();

                // Reset all Behaviours so that the next client gets a "fresh" GameObject.
                // We do this right before returning using it again because there may be time
                // specific behaviours.
                go.ResetBehaviours();
                        
                // Pop one off the stack and return it to the client for use.
                return go;
            }

            return null;
        }

        /// <summary>
        /// Call this to return an object to the Factory when it is no longer used.  This is done automatically when
        /// a GameObject is removed from the GameObjectManager.
        /// </summary>
        /// <param name="go">The GameObject to return.</param>
        public void RecycleTemplate(GameObject go)
        {
            // Make sure the Object being recycled is actually managed by this Factory.
            if (!go.pFactoryInfo.pIsManaged)
            {
                System.Diagnostics.Debug.Assert(false, "Attempting to Recycle non-managed Game Object.");

                return;
            }

            // Sanity check to make sure this object isn't added more than once.
            CheckForDupes(go.pFactoryInfo.pTemplateName);

            // Push it back onto the appropriate list.
            mObjects[go.pFactoryInfo.pTemplateName].Push(go);
        }

        /// <summary>
        /// Accessor to the pInstance property.
        /// </summary>
        public static GameObjectFactory pInstance
        {
            get
            {
                // If this is the first time this is called, instantiate our
                // static instance of the class.
                if (mInstance == null)
                {
                    mInstance = new GameObjectFactory();
                }

                // Either way, at this point we should have an instantiated version
                // if the class.
                return mInstance;
            }
        }

        /// <summary>
        /// Sanity check to make sure the same object isn't Added more than once which
        /// will result in the Factory handing off the same GameObject to different clients.
        /// </summary>
        /// <param name="templateName">The template name.</param>
        [Conditional("DEBUG")]
        private void CheckForDupes(String templateName)
        {
            List<Int32> ids = new List<int>(1000);
            foreach (GameObject obj in mObjects[templateName])
            {
                System.Diagnostics.Debug.Assert(!ids.Contains(obj.pID), "Adding same object to factory multiple times.");

                ids.Add(obj.pID);
            }
        }
    }
}
