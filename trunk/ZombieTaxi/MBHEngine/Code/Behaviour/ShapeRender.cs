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
    public class ShapeRender : MBHEngine.Behaviour.Behaviour
    {
        /// <summary>
        /// The minimap is just a proceedurally created texture which we directly draw 
        /// pixels to.
        /// </summary>
        private List<Texture2D> mShapeTextures;

        /// <summary>
        /// Constructor which also handles the process of loading in the Behaviour
        /// Definition information.
        /// </summary>
        /// <param name="parentGOH">The game object that this behaviour is attached to.</param>
        /// <param name="fileName">The file defining this behaviour.</param>
        public ShapeRender(GameObject.GameObject parentGOH, String fileName)
            : base(parentGOH, fileName)
        {
            throw new Exception("Attempting to use ShapeRender Behaviour. It is only partially implemented.");
        }

        /// <summary>
        /// Call this to initialize a Behaviour with data supplied in a file.
        /// </summary>
        /// <param name="fileName">The file to load from.</param>
        public override void LoadContent(String fileName)
        {
            base.LoadContent(fileName);

            ShapeRenderDefinition def = GameObjectManager.pInstance.pContentManager.Load<ShapeRenderDefinition>(fileName);

            mShapeTextures = new List<Texture2D>(def.mShapes.Count);

            for (Int32 i = 0; i < def.mShapes.Count; i++)
            {
                ShapeRenderDefinition.Shape shape = def.mShapes[i];

                // Create a color array for the pixels
                Color[] colors = new Color[shape.mDimensions.X * shape.mDimensions.Y];

                for (Int32 j = 0; j < colors.Length; j++)
                {
                    colors[j] = shape.mColor;
                }

                Texture2D newTexture = new Texture2D(GameObjectManager.pInstance.pGraphicsDevice, shape.mDimensions.X, shape.mDimensions.Y);
                newTexture.SetData(colors);
                mShapeTextures.Add(newTexture);
            }
        }

        /// <summary>
        /// Called at the end of the frame where mParentGOH was added to the GameObjectManager.
        /// </summary>
        public override void OnAdd()
        {
        }

        /// <summary>
        /// Called at the end of the frame on which this Behaviour's mParentGOH was removed from
        /// the GameObjectManager.
        /// </summary>
        public override void OnRemove()
        {
        }

        /// <summary>
        /// Called once per frame before the update function. Is called for ALL gameobjects, prior 
        /// to calling Update.
        /// </summary>
        /// <param name="gameTime">The amount of time that has passed this frame.</param>
        public override void PreUpdate(GameTime gameTime)
        {
        }

        /// <summary>
        /// Called once per frame by the game object.
        /// </summary>
        /// <param name="gameTime">The amount of time that has passed this frame.</param>
        public override void Update(GameTime gameTime)
        {
        }

        /// <summary>
        /// Called once per frame after the Update function. Is called after all objects have
        /// caled Update.
        /// </summary>
        /// <param name="gameTime">The amount of time that has passed this frame.</param>
        public override void PostUpdate(GameTime gameTime)
        {
        }

        /// <summary>
        /// Called once render cycle by the game object manager.
        /// </summary>
        /// <param name="batch">The sprite batch to render to.</param>
        public override void Render(SpriteBatch batch, Effect effect)
        {
            for (Int32 i = 0; i < mShapeTextures.Count; i++)
            {
                batch.Draw(
                    mShapeTextures[i],
                    new Vector2(mParentGOH.pPosition.X - (mShapeTextures[i].Width * 0.5f), mParentGOH.pPosition.Y - 10),
                    Color.White);
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

        /// <summary>
        /// Resets a behaviour to its initial state.
        /// </summary>
        public override void Reset()
        {
        }
    }
}
