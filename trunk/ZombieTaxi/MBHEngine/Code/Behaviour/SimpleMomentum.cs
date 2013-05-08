using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using MBHEngineContentDefs;
using MBHEngine.GameObject;

namespace MBHEngine.Behaviour
{
    /// <summary>
    /// Moves the object based on its forward direction and speed.
    /// </summary>
    public class SimpleMomentum : Behaviour
    {
        /// <summary>
        /// The actual speed the object is moving right now.
        /// </summary>
        private Single mSpeedActual;

        /// <summary>
        /// The rate at which the object will speed up.
        /// </summary>
        private Single mAcceleration;

        /// <summary>
        /// Constructor which also handles the process of loading in the Behaviour
        /// Definition information.
        /// </summary>
        /// <param name="parentGOH">The game object that this behaviour is attached to.</param>
        /// <param name="fileName">The file defining this behaviour.</param>
        public SimpleMomentum(GameObject.GameObject parentGOH, String fileName)
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

            SimpleMomentumDefinition def = GameObjectManager.pInstance.pContentManager.Load<SimpleMomentumDefinition>(fileName);

            mSpeedActual = 0.0f;

            mAcceleration = def.mAcceleration;
        }

        /// <summary>
        /// Called once per frame by the game object.
        /// </summary>
        /// <param name="gameTime">The amount of time that has passed this frame.</param>
        public override void PostUpdate(GameTime gameTime)
        {
            Single speedWanted = mParentGOH.pDirection.mSpeed;

            if (mAcceleration > 0.0f)
            {
                if (speedWanted < mSpeedActual || mParentGOH.pDirection.mForward == Vector2.Zero)
                {
                    mSpeedActual -= mAcceleration;
                }
                else if (speedWanted > mSpeedActual)
                {
                    mSpeedActual += mAcceleration;
                }
            }
            else
            {
                mSpeedActual = speedWanted;
            }

            mSpeedActual = MathHelper.Clamp(mSpeedActual, 0.0f, speedWanted);

            mParentGOH.pPosition += mParentGOH.pDirection.mForward * mSpeedActual;
        }
    }
}
