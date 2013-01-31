using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MBHEngine.Behaviour;
using MBHEngine.GameObject;
using ZombieTaxiContentDefs;
using ZombieTaxi.Behaviours.HUD;

namespace ZombieTaxi.Behaviours
{
    /// <summary>
    /// On creation, adds a Marker to the MiniMap representing this object.
    /// </summary>
    class MarkOnMiniMap : MBHEngine.Behaviour.Behaviour
    {
        /// <summary>
        /// Stored so that it can be used outside of LoadContent.
        /// </summary>
        private MarkOnMiniMapDefinition mDef;

        /// <summary>
        /// Preallocate messages.
        /// </summary>
        private MiniMap.RemoveObjectToMarkMessage mRemoveObjectToMarkMsg;
        private MiniMap.AddObjectToMarkMessage mAddMarkerMsg;

        /// <summary>
        /// Constructor which also handles the process of loading in the Behaviour
        /// Definition information.
        /// </summary>
        /// <param name="parentGOH">The game object that this behaviour is attached to.</param>
        /// <param name="fileName">The file defining this behaviour.</param>
        public MarkOnMiniMap(GameObject parentGOH, String fileName)
            : base(parentGOH, fileName)
        {
            mRemoveObjectToMarkMsg = new MiniMap.RemoveObjectToMarkMessage();
            mAddMarkerMsg = new MiniMap.AddObjectToMarkMessage();
        }

        /// <summary>
        /// Call this to initialize a Behaviour with data supplied in a file.
        /// </summary>
        /// <param name="fileName">The file to load from.</param>
        public override void LoadContent(String fileName)
        {
            base.LoadContent(fileName);

            mDef = GameObjectManager.pInstance.pContentManager.Load<MarkOnMiniMapDefinition>(fileName);
        }

        /// <summary>
        /// Called at the end of the frame where mParentGOH was added to the GameObjectManager.
        /// </summary>
        public override void OnAdd()
        {
            // This can't happen until PostInitialization because it might have been created in the
            // frame and BroadcastMessage won't work right away.
            //

            mAddMarkerMsg.mMarkerProfile_In = mDef.mMarkerProfile;
            mAddMarkerMsg.mObjectToMark_In = mParentGOH;
            GameObjectManager.pInstance.BroadcastMessage(mAddMarkerMsg);
        }

        /// <summary>
        /// Called at the end of the frame where mParentGOH was removed from the GameObjectManager.
        /// </summary>
        public override void OnRemove()
        {
            mRemoveObjectToMarkMsg.mObjectToRemove_In = mParentGOH;
            GameObjectManager.pInstance.BroadcastMessage(mRemoveObjectToMarkMsg);
        }
    }
}
