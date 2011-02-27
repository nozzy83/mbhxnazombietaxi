using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MBHEngineContentDefs;
using MBHEngine.GameObject;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MBHEngine.Input;

namespace MBHEngine.Behaviour
{
    /// <summary>
    /// Behaviour for rendering simple 2D sprites to the screen.
    /// </summary>
    public class SpriteRender : MBHEngine.Behaviour.Behaviour
    {
        /// <summary>
        /// Retrieves the current sprite effect being applied to this sprite.
        /// </summary>
        public class GetSpriteEffectsMessage : BehaviourMessage
        {
            public SpriteEffects mSpriteEffects;
        };

        /// <summary>
        /// Overrides the current sprite effect being applied to this sprite.
        /// </summary>
        public class SetSpriteEffectsMessage : BehaviourMessage
        {
            public SpriteEffects mSpriteEffects;
        };

        /// <summary>
        /// The texture used to render the sprite.
        /// </summary>
        private Texture2D mTexture;

        /// <summary>
        /// Describes some simple effects that can be applied to the sprite, such as flipping.
        /// </summary>
        private SpriteEffects mSpriteEffects;

        /// <summary>
        /// Constructor which also handles the process of loading in the Behaviour
        /// Definition information.
        /// </summary>
        /// <param name="parentGOH">The game object that this behaviour is attached to.</param>
        /// <param name="fileName">The file defining this behaviour.</param>
        public SpriteRender(GameObject.GameObject parentGOH, String fileName)
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

            SpriteRenderDefinition def = GameObjectManager.pInstance.pContentManager.Load<SpriteRenderDefinition>(fileName);

            mTexture = GameObjectManager.pInstance.pContentManager.Load<Texture2D>(def.mSpriteFileName);

            mSpriteEffects = SpriteEffects.None;
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

            batch.Draw(mTexture,
                       mParentGOH.pOrientation.mPosition,
                       null,
                       Color.White,
                       mParentGOH.pOrientation.mRotation,
                       new Vector2(mTexture.Width * 0.5f, mTexture.Height * 0.5f),
                       mParentGOH.pOrientation.mScale,
                       mSpriteEffects,
                       0);
        }

        /// <summary>
        /// The main interface for communicating between behaviours.  Using polymorphism, we
        /// define a bunch of different messages deriving from BehaviourMessage.  Each behaviour
        /// can then check for particular upcasted messahe types, and either grab some data 
        /// from it (set message) or store some data in it (get message).
        /// </summary>
        /// <param name="msg">The message being communicated to the behaviour.</param>
        /// <returns>The resulting message.  If not null, the message was handled.</returns>
        public override BehaviourMessage OnMessage(BehaviourMessage msg)
        {
            // Which type of message was sent to us?
            if (msg is SpriteRender.GetSpriteEffectsMessage)
            {
                SpriteRender.GetSpriteEffectsMessage temp = (SpriteRender.GetSpriteEffectsMessage)msg;
                temp.mSpriteEffects = mSpriteEffects;
                msg = temp;
            }
            else if (msg is SpriteRender.SetSpriteEffectsMessage)
            {
                SpriteRender.SetSpriteEffectsMessage temp = (SpriteRender.SetSpriteEffectsMessage)msg;
                mSpriteEffects = temp.mSpriteEffects;
            }
            else
            {
                // This is not a message we know how to handle.
                msg = null;
            }

            return msg;
        }
    }
}
