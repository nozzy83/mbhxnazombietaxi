using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MBHEngine.Behaviour;
using Microsoft.Xna.Framework;
using MBHEngine.GameObject;
using ZombieTaxiContentDefs;

namespace ZombieTaxi.Behaviours
{
    /// <summary>
    /// Allows on GameObject to store a collection of items.
    /// </summary>
    class Inventory : MBHEngine.Behaviour.Behaviour
    {
        /// <summary>
        /// Message used for adding objects to this inventory.
        /// </summary>
        public class AddObjectMessage : MBHEngine.Behaviour.BehaviourMessage
        {
            /// <summary>
            /// The GameObject that should be added to this inventory.
            /// </summary>
            public GameObject mObj;

            /// <summary>
            /// Set to true if the object being added should become the selected object.
            /// </summary>
            public Boolean mDoSelectObj;

            /// <summary>
            /// Put the message back into its default state.
            /// </summary>
            public void Reset()
            {
                mObj = null;
                mDoSelectObj = false;
            }
        };

        /// <summary>
        /// Retrieves the Object at the top of the Inventory Queue.
        /// </summary>
        public class GetCurrentObjectMessage : MBHEngine.Behaviour.BehaviourMessage
        {
            /// <summary>
            /// The object at the front of the Inventory Queue.
            /// null if the Queue is empty.
            /// </summary>
            public GameObject mOutObj;

            /// <summary>
            /// Put the message back into its default state.
            /// </summary>
            public void Reset()
            {
                mOutObj = null;
            }
        }

        /// <summary>
        /// Retrieves the Object at the top of the Inventory Queue without actually removing it.
        /// </summary>
        public class PeekCurrentObjectMessage : MBHEngine.Behaviour.BehaviourMessage
        {
            /// <summary>
            /// The object at the front of the Inventory Queue.
            /// null if the Queue is empty.
            /// </summary>
            public GameObject mOutObj;

            /// <summary>
            /// The number of instances of this object type stored in the Inventory.
            /// </summary>
            public UInt32 mOutCount;

            /// <summary>
            /// Put the message back into its default state.
            /// </summary>
            public void Reset()
            {
                mOutObj = null;
                mOutCount = 0;
            }
        }

        /// <summary>
        /// Tells the inventory to go to the next group of items.
        /// </summary>
        public class SelectNextItemMessage : MBHEngine.Behaviour.BehaviourMessage
        {
        }

        /// <summary>
        /// The collection of objects this Inventory is storing. These are stored in a linear
        /// list but sorted so that all objects of a particular type are grouped in concurrent
        /// order. The type of object is defined by its value in pTemplateFileName.
        /// </summary>
        private List<GameObject> mObjects;

        /// <summary>
        /// Tracks the index of the currently selected object in the inventory.
        /// This should always be the first of a particular type of object in the 
        /// list. The type is based on pTemplateFileName.
        /// -1 indicated the index has not been set.
        /// </summary>
        private Int32 mCurrentObject;

        /// <summary>
        /// Constructor which also handles the process of loading in the Behaviour
        /// Definition information.
        /// </summary>
        /// <param name="parentGOH">The game object that this behaviour is attached to.</param>
        /// <param name="fileName">The file defining this behaviour.</param>
        public Inventory(GameObject parentGOH, String fileName)
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

            InventoryDefinition def = GameObjectManager.pInstance.pContentManager.Load<InventoryDefinition>(fileName);

            // Assume a starting size of the inventory.
            mObjects = new List<GameObject>(16);

            // To start the inventory is empty and so no items have been selected.
            mCurrentObject = -1;
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
            if (msg is AddObjectMessage)
            {
                AddObjectMessage temp = (AddObjectMessage)msg;

                // The template name is how we will categorize items.
                String newType = temp.mObj.pTemplateFileName;

                // Assume that the object was not added to the list.
                Boolean added = false;

                // Index will be needed outside of the loop to figure out where the object
                // was added.
                Int32 index = 0;

                // Loop through ever object in the inventory looking for another object of the
                // same type so that this new object can be grouped in with it.
                for (; index < mObjects.Count; index++)
                {
                    // Objects are grouped by the template that defined them.
                    if (mObjects[index].pTemplateFileName == newType)
                    {
                        // Put the new object at the front of the group. It doesn't need to
                        // be at the front, but it is the quickest/easiest. We may at some point
                        // want it to go at the end of the list, but that won't work in the case
                        // where temp.mDoSelectObj is true.
                        mObjects.Insert(index, temp.mObj);

                        // The object has been added so the special handling below can be skipped.
                        added = true;

                        break; // index < mObjects.Count
                    }
                }

                // If the object wasn't added above, it means that there are no other objects of this type
                // yet in the list (including when the list is completely empty). So it needs to be added
                // to the end of the list.
                if (!added)
                {
                    mObjects.Add(temp.mObj);
                }

                // If at this point there was no selected object, this new object becomes selected by default.
                if (-1 == mCurrentObject)
                {
                    mCurrentObject = 0;
                }
                else if (true == temp.mDoSelectObj)
                {
                    // The message can specifically request that the object added be selected. Since it was inserted
                    // at the front of its group, we just need to update mCurrentObject to be the current index.
                    mCurrentObject = index;
                }
            }
            else if (msg is GetCurrentObjectMessage)
            {
                // Trying to pop with an empty queue is an exception.
                if (0 != mObjects.Count)
                {
                    System.Diagnostics.Debug.Assert(mCurrentObject != -1, "mObjects isn't empty but mCurrentObject is undefined. This should never happen.");

                    // Should never happen.
                    if (-1 == mCurrentObject)
                    {
                        return;
                    }

                    GetCurrentObjectMessage temp = (GetCurrentObjectMessage)msg;

                    // Grab the currently selected object.
                    temp.mOutObj = mObjects[mCurrentObject];
                    mObjects.RemoveAt(mCurrentObject);

                    // No need to change mCurrentObject as we just removed the object at its index and
                    // so the object that followed it will now become the current object when it inherits
                    // that index in the list.

                    // This can happen when placing the last object in the list while there are still other 
                    // types of objects before it.
                    if (mObjects.Count <= mCurrentObject)
                    {
                        mCurrentObject = 0;
                    }

                    // If the list becomes empty set the mCurrentObject back to an undefined so that the 
                    // next object added becomes the current object by default.
                    if (0 == mObjects.Count)
                    {
                        mCurrentObject = -1;
                    }
                }
            }
            else if (msg is PeekCurrentObjectMessage)
            {
                if (0 != mObjects.Count)
                {
                    System.Diagnostics.Debug.Assert(mCurrentObject != -1, "mObjects isn't empty but mCurrentObject is undefined. This should never happen.");

                    PeekCurrentObjectMessage temp = (PeekCurrentObjectMessage)msg;

                    if (-1 != mCurrentObject)
                    {
                        temp.mOutObj = mObjects[mCurrentObject];

                        String type = mObjects[mCurrentObject].pTemplateFileName;

                        // We need to also return the number of objects of this type in the inventory.
                        // Since they are sorted in groups we just loop until we reach an item of
                        // a different type.
                        for (Int32 i = mCurrentObject; i < mObjects.Count; i++)
                        {
                            // If its the same type, increase the count.
                            if (mObjects[i].pTemplateFileName == type)
                            {
                                temp.mOutCount++;
                            }
                            else
                            {
                                // As soon as we hit another type, stop looking.
                                break;
                            }
                        }
                    }
                }
            }
            else if (msg is SelectNextItemMessage)
            {
                if (0 != mObjects.Count)
                {
                    System.Diagnostics.Debug.Assert(mCurrentObject != -1, "mObjects isn't empty but mCurrentObject is undefined. This should never happen.");

                    if (-1 != mCurrentObject)
                    {
                        String type = mObjects[mCurrentObject].pTemplateFileName;

                        Int32 count = mObjects.Count;

                        // The loop is slightly odd. We loop for the number of object in the list, but
                        // we don't use i to index into the array. Instead we are incrementing mCurrentObject
                        // as we go and using that to iterate. Once we hit an object of another type we stop 
                        // looping and mCurrentObject is left pointing at the next object of a different type.
                        for (Int32 i = 0; i < count; i++)
                        {
                            mCurrentObject++;

                            // Loop back to the start.
                            if (mCurrentObject >= count)
                            {
                                mCurrentObject = 0;
                            }

                            // If we hit another type, we are done.
                            if (mObjects[mCurrentObject].pTemplateFileName != type)
                            {
                                break;
                            }
                        }
                    }
                }
            }
        }

#if ALLOW_GARBAGE
        /// <summary>
        /// Returns a bunch of information about the behaviour which can be dumped to
        /// a debug display for debugging at runtime.
        /// </summary>
        /// <returns>A formatted string of debug information.</returns>
        public override String[] GetDebugInfo()
        {
            String [] info = new String[1];

            info[0] = "Num: " + mObjects.Count;

            return info;
        }
#endif // ALLOW_GARBAGE
    }
}
