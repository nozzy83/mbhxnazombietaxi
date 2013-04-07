using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MBHEngine.Behaviour;
using MBHEngine.GameObject;
using MBHEngine.World;

namespace ZombieTaxi.Behaviours
{
    /// <summary>
    /// Works in conjucture with the ObjectPlacement behaviour. The class implements specific functionality
    /// needed when placing an object that is part of the tile map. It allows it to do things like
    /// add collision flags to the Level.
    /// </summary>
    class TilePlacement : MBHEngine.Behaviour.Behaviour
    {

        /// <summary>
        /// Preallocate messages to avoid GC.
        /// </summary>
        private Level.SetTileTypeAtPositionMessage mSetTileTypeAtPositionMsg;

        /// <summary>
        /// Constructor which also handles the process of loading in the Behaviour
        /// Definition information.
        /// </summary>
        /// <param name="parentGOH">The game object that this behaviour is attached to.</param>
        /// <param name="fileName">The file defining this behaviour.</param>
        public TilePlacement(GameObject parentGOH, String fileName)
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

            //TilePlacementDefinition def = GameObjectManager.pInstance.pContentManager.Load<TilePlacementDefinition>(fileName);

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
            if (msg is ObjectPlacement.OnPlaceObjectMessage)
            {
                ObjectPlacement.OnPlaceObjectMessage temp = (ObjectPlacement.OnPlaceObjectMessage)msg;

                // By default assume the object could not be placed.
                temp.mObjectPlaced_Out = false;

                // The level needs to have this tile set to be Solid.
                mSetTileTypeAtPositionMsg.mType_In = Level.Tile.TileTypes.Solid;
                mSetTileTypeAtPositionMsg.mPosition_In = temp.mPosition_In;
                WorldManager.pInstance.pCurrentLevel.OnMessage(mSetTileTypeAtPositionMsg, mParentGOH);

                // Only spawn a tile if we actually changed the tile type.
                if (mSetTileTypeAtPositionMsg.mType_In != mSetTileTypeAtPositionMsg.mPreviousType_Out)
                {
                    mParentGOH.pPosition = temp.mPosition_In;

                    temp.mObjectPlaced_Out = true;

                    // Note: We don't need to add this object to the GameObjectManager; the default
                    //       ObjectPlacement will handle that when it sees that mOutObjectPlaced is
                    //       true.
                }
            }
        }
    }
}
