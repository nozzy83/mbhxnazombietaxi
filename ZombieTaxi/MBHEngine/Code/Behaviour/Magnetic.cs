using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MBHEngine.GameObject;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MBHEngineContentDefs;

namespace MBHEngine.Behaviour
{
    /// <summary>
    /// When mParentGOH is within a specified distance to the target, it begins slowly sliding 
    /// targets the target, speeding up as it gets closer.
    /// </summary>
    public class Magnetic : MBHEngine.Behaviour.Behaviour
    {
        /// <summary>
        /// The furthest distance from the target (squared) at which the Magnectic movement still occurs.
        /// Used as an optimization.
        /// </summary>
        private Single mMaxDistSq;

        /// <summary>
        /// The slowest speed at which the object will magnetically move towards the target.
        /// </summary>
        private Single mMinSpeed;

        /// <summary>
        /// The fastest speed at which the object will magnetically move towards the target.
        /// </summary>
        private Single mMaxSpeed;

        /// <summary>
        /// Constructor which also handles the process of loading in the Behaviour
        /// Definition information.
        /// </summary>
        /// <param name="parentGOH">The game object that this behaviour is attached to.</param>
        /// <param name="fileName">The file defining this behaviour.</param>
        public Magnetic(GameObject.GameObject parentGOH, String fileName)
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

            MagneticDefinition def = GameObjectManager.pInstance.pContentManager.Load<MagneticDefinition>(fileName);

            mMaxDistSq = def.mMaxDist * def.mMaxDist;
            mMinSpeed = def.mMinSpeed;
            mMaxSpeed = def.mMaxSpeed;
        }

        /// <summary>
        /// Called once per frame by the game object.
        /// </summary>
        /// <param name="gameTime">The amount of time that has passed this frame.</param>
        public override void Update(GameTime gameTime)
        {
            // For now we assume the target is always the player, but we could update this to be based on 
            // Classification, or set by a BehaviourMessage.
            GameObject.GameObject player = GameObjectManager.pInstance.pPlayer;

            if (null != player)
            {
                // Get the vector from our current position to the player.
                Vector2 delta = player.pPosition - mParentGOH.pPosition;

                // Calculate how far it is from our current position to the target.
                Single distanceSq = delta.LengthSquared();

                // It must be within a minimum distance before we start to travel towards the target.
                if (distanceSq < mMaxDistSq)
                {
                    // Normalize the vector so that it can be scaled by a speed factor.
                    delta.Normalize();

                    // Speed up as the target gets closer.
                    Single speed = (MathHelper.Lerp(mMaxSpeed, mMinSpeed, (distanceSq / mMaxDistSq)));

                    // Move!
                    mParentGOH.pPosition += delta * speed;
                }
            }
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
        }
    }
}
