using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MBHEngine.GameObject;
using Microsoft.Xna.Framework;
using MBHEngineContentDefs;

namespace MBHEngine.Behaviour
{
    /// <summary>
    /// Waits for OnZeroHealth message at which point it spawns another GameObject
    /// </summary>
    public class SpawnOnDeath : Behaviour
    {
        /// <summary>
        /// The name of Template which will be spawned.
        /// </summary>
        private String mTemplateFileName;

        /// <summary>
        /// An attachment point on this object at which to spawn the new object at.
        /// </summary>
        private String mAttachmentPoint;

        /// <summary>
        /// Preallocated messages to avoid triggering GC.
        /// </summary>
        private SpriteRender.GetAttachmentPointMessage mGetAttachmentPointMsg;

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

            SpawnOnDeathDefinition def = GameObjectManager.pInstance.pContentManager.Load<SpawnOnDeathDefinition>(fileName);

            mTemplateFileName = def.mTemplateFileName;
            mAttachmentPoint = def.mAttachmentPoint;

            mGetAttachmentPointMsg = new SpriteRender.GetAttachmentPointMessage();
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
                // By default just spawn the object where this object is.
                GameObject.GameObject go = GameObjectFactory.pInstance.GetTemplate(mTemplateFileName);
                Vector2 spawnPos = mParentGOH.pPosition;

                // Optionally, there could be an attachment point specified.
                if (null != mAttachmentPoint)
                {
                    // Grab that attachment point and position the new object there.
                    mGetAttachmentPointMsg.mName_In = mAttachmentPoint;
                    mParentGOH.OnMessage(mGetAttachmentPointMsg);
                    spawnPos = mGetAttachmentPointMsg.mPoisitionInWorld_Out;
                }

                go.pPosition = spawnPos;

                GameObjectManager.pInstance.Add(go);
            }
        }
    }
}
