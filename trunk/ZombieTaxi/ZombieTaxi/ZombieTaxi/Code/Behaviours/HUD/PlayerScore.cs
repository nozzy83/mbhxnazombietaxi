using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MBHEngine.GameObject;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MBHEngine.Behaviour;

namespace ZombieTaxi.Behaviours.HUD
{
    class PlayerScore : MBHEngine.Behaviour.Behaviour
    {
        // Message used for incrementing the current score.
        //
        public class IncrementScoreMessage : BehaviourMessage
        {
            // The amount to increment the score by.
            //
            public Int32 mAmount;
        }

        /// <summary>
        /// The font object we use for rendering.
        /// </summary>
        private SpriteFont mFont;

        /// <summary>
        /// The current score.
        /// </summary>
        private Int32 mScore;

        /// <summary>
        /// The score needs to be displayed as a string.
        /// </summary>
        private String mScoreDisplay;

        /// <summary>
        /// Constructor which also handles the process of loading in the Behaviour
        /// Definition information.
        /// </summary>
        /// <param name="parentGOH">The game object that this behaviour is attached to.</param>
        /// <param name="fileName">The file defining this behaviour.</param>
        public PlayerScore(GameObject parentGOH, String fileName)
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

            // Create the font
            mFont = GameObjectManager.pInstance.pContentManager.Load<SpriteFont>("Fonts\\Retro");

            mScore = 0;
            mScoreDisplay = mScore.ToString(); ;
        }

        /// <summary>
        /// Called once per frame by the game object.
        /// </summary>
        /// <param name="gameTime">The amount of time that has passed this frame.</param>
        public override void Update(GameTime gameTime)
        {
        }

        /// <summary>
        /// Called once render cycle by the game object manager.
        /// </summary>
        /// <param name="batch">The sprite batch to render to.</param>
        public override void Render(SpriteBatch batch)
        {
            // The size of a single character, used to calculate offset needed for centering.
            Single characterWidth = 8.0f;

            // This GameObject is positioned at the center of the screen, but needs to be offset based on the
            // number of characters in the current score.
            // First find the number of characters needed to offset, which is half of the string.
            // Then multiply that by the size of a single character.
            // Finally negate the number since we want to move left, not right.
            Single centerOffset = mScoreDisplay.Length * 0.5f * characterWidth * - 1.0f;

            Single xPos = centerOffset + mParentGOH.pOrientation.mPosition.X;
            batch.DrawString(mFont, mScoreDisplay, new Vector2(xPos + 1, mParentGOH.pOrientation.mPosition.Y + 1), Color.Black);
            batch.DrawString(mFont, mScoreDisplay, new Vector2(xPos, mParentGOH.pOrientation.mPosition.Y), Color.White);
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
            if (msg is IncrementScoreMessage)
            {
                IncrementScoreMessage temp = (IncrementScoreMessage)msg;
                pScore += temp.mAmount;
            }
        }

        /// <summary>
        /// Property accessing the current score.  Ensures that display string is kept up to date
        /// as well.
        /// </summary>
        private Int32 pScore
        {
            get
            {
                return mScore;
            }
            set
            {
                mScore = value;

                // It would be a little safe to do this at render time, but that will incur a heap
                // allocation and we want to do this as little as possible to avoid triggering the
                // garbage collector.
                mScoreDisplay = mScore.ToString();
            }
        }
    }
}
