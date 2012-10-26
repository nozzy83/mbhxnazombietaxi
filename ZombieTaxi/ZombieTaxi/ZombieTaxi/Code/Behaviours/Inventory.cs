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
        };

        /// <summary>
        /// The collection of objects this Inventory is storing.
        /// </summary>
        private List<GameObject> mObjects;

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

            mObjects = new List<GameObject>(16);
        }

        /// <summary>
        /// Called once per frame by the game object.
        /// </summary>
        /// <param name="gameTime">The amount of time that has passed this frame.</param>
        public override void Update(GameTime gameTime)
        {
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

                mObjects.Add(temp.mObj);
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
