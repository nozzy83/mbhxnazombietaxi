using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MBHEngine.Debug;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace MBHEngine.Behaviour
{
    /// <summary>
    /// Displays the framerate.
    /// </summary>
    public class FrameRateDisplay : MBHEngine.Behaviour.Behaviour
    {
        /// <summary>
        /// The user facing frame rate value.
        /// </summary>
        private Int32 mFrameRate = 0;

        /// <summary>
        /// Keeps tracks of how many frames have passed.
        /// </summary>
        private Int32 mFrameCounter = 0;

        /// <summary>
        /// How much time has passed since the last update to the framerate value.  Everytime this
        /// passes one second we recompute the current frame rate.
        /// </summary>
        private TimeSpan mElapsedTime = TimeSpan.Zero;

        /// <summary>
        /// Constructor which also handles the process of loading in the Behaviour
        /// Definition information.
        /// </summary>
        /// <param name="parentGOH">The game object that this behaviour is attached to.</param>
        /// <param name="fileName">The file defining this behaviour.</param>
        public FrameRateDisplay(GameObject.GameObject parentGOH, String fileName)
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
        /// Called once per frame by the game object.
        /// </summary>
        /// <param name="gameTime">The amount of time that has passed this frame.</param>
        public override void Update(GameTime gameTime)
        {
            mElapsedTime += gameTime.ElapsedGameTime;

            if (mElapsedTime > TimeSpan.FromSeconds(1))
            {
                mElapsedTime -= TimeSpan.FromSeconds(1);
                mFrameRate = mFrameCounter;
                mFrameCounter = 0;
            }

            DebugMessageDisplay.pInstance.AddDynamicMessage(string.Format("FPS: {0}", mFrameRate));
        }

        /// <summary>
        /// Called once render cycle by the game object manager.
        /// </summary>
        /// <param name="batch">The sprite batch to render to.</param>
        /// <param name="effect">The currently set shader.</param>
        public override void Render(SpriteBatch batch, Effect effect)
        {
            mFrameCounter++;
        }
    }
}
