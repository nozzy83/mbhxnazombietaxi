using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace MBHEngine.Behaviour
{
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

            //RemoveTileOnDeathDefinition def = GameObjectManager.pInstance.pContentManager.Load<RemoveTileOnDeathDefinition>(fileName);

            mSetTileTypeAtPositionMsg = new Level.SetTileTypeAtPositionMessage();
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
            if (msg is Health.OnZeroHealth)
            {
                mSetTileTypeAtPositionMsg.mType = Level.Tile.TileTypes.Empty;
                mSetTileTypeAtPositionMsg.mPosition = mParentGOH.pPosition;
                MBHEngine.World.WorldManager.pInstance.pCurrentLevel.OnMessage(mSetTileTypeAtPositionMsg, mParentGOH);
            }
        }
    }
}
