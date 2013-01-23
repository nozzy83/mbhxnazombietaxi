using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace MBHEngine.Behaviour
{
    /// <summary>
    /// When this GameObject posts a OnZeroHealth message, this behaviour calls into the 
    /// current level and tells it to remove any collision volumes at the position of this
    /// GameObject. This would be commom for things like destructable walls.
    /// </summary>
    public class RemoveTileOnDeath : Behaviour
    {
        /// <summary>
        /// Preallocated messages to avoid garbage collection.
        /// </summary>
        private Level.SetTileTypeAtPositionMessage mSetTileTypeAtPositionMsg;

        /// <summary>
        /// Constructor which also handles the process of loading in the Behaviour
        /// Definition information.
        /// </summary>
        /// <param name="parentGOH">The game object that this behaviour is attached to.</param>
        /// <param name="fileName">The file defining this behaviour.</param>
        public RemoveTileOnDeath(GameObject.GameObject parentGOH, String fileName)
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

            mSetTileTypeAtPositionMsg = new Level.SetTileTypeAtPositionMessage();
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
            if (msg is Health.OnZeroHealthMessage)
            {
                mSetTileTypeAtPositionMsg.mType_In = Level.Tile.TileTypes.Empty;
                mSetTileTypeAtPositionMsg.mPosition_In = mParentGOH.pPosition;
                MBHEngine.World.WorldManager.pInstance.pCurrentLevel.OnMessage(mSetTileTypeAtPositionMsg, mParentGOH);
            }
        }
    }
}
