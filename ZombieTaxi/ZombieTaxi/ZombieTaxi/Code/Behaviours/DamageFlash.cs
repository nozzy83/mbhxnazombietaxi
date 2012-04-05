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
    ///  Simple class which reacts to damage events and as a result flashes the sprite a specified
    ///  colour.
    /// </summary>
    class DamageFlash : MBHEngine.Behaviour.Behaviour
    {
        /// <summary>
        /// Tracks how long it has been since this Game Object last took damage.
        /// </summary>
        private Single mFramesSinceLastDamage;

        /// <summary>
        /// Once the time since this Game Object last took damage reaches this value, 
        /// the color is set back to white, and the behaviour waits for the next
        /// damage message to come in.
        /// </summary>
        private Single mFramesToExpire;

        /// <summary>
        /// Color is cycled between multiple colours and than happens at a set interval of frames.
        /// This keeps track of how many frames have passed since they last time the color was 
        /// cycled.
        /// </summary>
        private Single mFramesSinceColorChange;

        /// <summary>
        /// How many frames need to pass before cycling to the next color.
        /// </summary>
        private Single mFramesBetweenColorChange;

        /// <summary>
        /// A list of colors to cycle between as the Game Object takes damage.
        /// </summary>
        private Color[] mColors;

        /// <summary>
        /// Keeps track of which color is being shown as an index into the mColors array.
        /// </summary>
        private Int32 mCurrentColor;

        /// <summary>
        /// Preallocated messages to avoid GC.
        /// </summary>
        private SpriteRender.SetColorMessage mSetColorMsg;

        /// <summary>
        /// Constructor which also handles the process of loading in the Behaviour
        /// Definition information.
        /// </summary>
        /// <param name="parentGOH">The game object that this behaviour is attached to.</param>
        /// <param name="fileName">The file defining this behaviour.</param>
        public DamageFlash(GameObject parentGOH, String fileName)
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

            DamageFlashDefinition def = GameObjectManager.pInstance.pContentManager.Load<DamageFlashDefinition>(fileName);

            mFramesSinceLastDamage = -1;
            mFramesToExpire = def.mFramesToReset;

            mFramesSinceColorChange = 0;
            mFramesBetweenColorChange = def.mFramesBetweenColorChange;

            if (mFramesToExpire < mFramesBetweenColorChange)
            {
                throw new Exception("mFramesBetweenColorChange must be less than mFramesToReset.");
            }

            mColors = def.mColors;
            mCurrentColor = 0;

            mSetColorMsg = new SpriteRender.SetColorMessage();
        }

        /// <summary>
        /// Called once per frame by the game object.
        /// </summary>
        /// <param name="gameTime">The amount of time that has passed this frame.</param>
        public override void Update(GameTime gameTime)
        {
            // If the mFramesSinceLastDamage it means it has already expired.  Nothing to do but wait
            // for the next damage event.
            if (mFramesSinceLastDamage != -1)
            {
                // Increment the frame counters.
                mFramesSinceLastDamage++;
                mFramesSinceColorChange++;

                // Has the effect experied?
                if (mFramesSinceLastDamage > mFramesToExpire)
                {
                    // TODO: This should not assume the original colour was white.
                    mSetColorMsg.mColor = Color.White;
                    mParentGOH.OnMessage(mSetColorMsg);

                    // Next time through the Update function, this will tell it that the
                    // effect has already expired.
                    mFramesSinceLastDamage = -1;

                    // Next time damage is registered we want the color to change right away.
                    mFramesSinceColorChange = mFramesBetweenColorChange;
                }
                else
                {
                    // Has enough time passed to switch to the next color?
                    if (mFramesSinceColorChange >= mFramesBetweenColorChange)
                    {
                        // If it has, reset the timer.
                        mFramesSinceColorChange = 0;

                        // Move on to the next color.
                        mCurrentColor++;

                        // Avoid going out of bounds of the array.
                        if (mCurrentColor >= mColors.Length)
                        {
                            mCurrentColor = 0;
                        }

                        // Update sprite to use the new color.
                        mSetColorMsg.mColor = mColors[mCurrentColor];
                        mParentGOH.OnMessage(mSetColorMsg);
                    }
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
            // Which type of message was sent to us?
            if (msg is Health.OnApplyDamage)
            {
                // The only thing we need to do is set this to zero.
                // If it was previous expired, this will trigger it to start again.
                // If it was already running, than this will give us more time before we
                // hit the mFramesToExpire.
                mFramesSinceLastDamage = 0;
            }
        }
    }
}
