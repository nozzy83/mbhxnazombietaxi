using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MBHEngine.Behaviour;
using Microsoft.Xna.Framework;
using MBHEngine.GameObject;
using MBHEngine.Debug;
using ZombieTaxiContentDefs;

namespace ZombieTaxi.Behaviours
{
    /// <summary>
    /// Allows a GameObject to be picked up and added to an inventory.
    /// </summary>
    class Pickup : MBHEngine.Behaviour.Behaviour
    {
        /// <summary>
        /// The name of the Template which should be added to the Player's inventory when 
        /// this Pickup is picked up.
        /// </summary>
        private String mInventoryTemplateFileName;

        /// <summary>
        /// Preallocated messages to avoid GC.
        /// </summary>
        private Inventory.AddObjectMessage mAddObjectMsg;

        /// <summary>
        /// Constructor which also handles the process of loading in the Behaviour
        /// Definition information.
        /// </summary>
        /// <param name="parentGOH">The game object that this behaviour is attached to.</param>
        /// <param name="fileName">The file defining this behaviour.</param>
        public Pickup(GameObject parentGOH, String fileName)
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

            PickupDefinition def = GameObjectManager.pInstance.pContentManager.Load<PickupDefinition>(fileName);

            mInventoryTemplateFileName = def.mInventoryTemplateFileName;

            mAddObjectMsg = new Inventory.AddObjectMessage();
        }

        /// <summary>
        /// Called once per frame by the game object.
        /// </summary>
        /// <param name="gameTime">The amount of time that has passed this frame.</param>
        public override void Update(GameTime gameTime)
        {
            GameObject player = GameObjectManager.pInstance.pPlayer;

            // For now just check if we are colliding with the player.
            // TODO: Object should be able to specify what types of objects can pick it up.
            if (mParentGOH.pCollisionRect.Intersects(player.pCollisionRect))
            {
                DebugMessageDisplay.pInstance.AddConstantMessage("Pickup grabbed");

                // Grab an instance of the object that needs to be added to the Player's invantory
                // when this object is picked up.
                GameObject obj = GameObjectFactory.pInstance.GetTemplate(mInventoryTemplateFileName);

                // Add the object to the player's inventory.
                mAddObjectMsg.mObj_In = obj;
                player.OnMessage(mAddObjectMsg);

                // Stop updating and rendering the object. Also prevent things like collision checks finding it.
                GameObjectManager.pInstance.Remove(mParentGOH);
            }
        }
    }
}
