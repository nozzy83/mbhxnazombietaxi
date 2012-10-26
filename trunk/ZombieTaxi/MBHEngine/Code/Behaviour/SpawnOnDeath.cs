using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MBHEngine.GameObject;
using Microsoft.Xna.Framework;

namespace MBHEngine.Behaviour
{
    /// <summary>
    /// Waits for OnZeroHealth message at which point it spawns another GameObject
    /// </summary>
    public class SpawnOnDeath : Behaviour
    {

        /// <summary>
        /// Constructor which also handles the process of loading in the Behaviour
        /// Definition information.
        /// </summary>
        /// <param name="parentGOH">The game object that this behaviour is attached to.</param>
        /// <param name="fileName">The file defining this behaviour.</param>
        public SpawnOnDeath(GameObject.GameObject parentGOH, String fileName)
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
            if (msg is Health.OnZeroHealth)
            {
                GameObject.GameObject go = GameObjectFactory.pInstance.GetTemplate("GameObjects\\Items\\StonePickup\\StonePickup");
                go.pPosition = mParentGOH.pPosition;
                GameObjectManager.pInstance.Add(go);
            }
        }
    }
}
