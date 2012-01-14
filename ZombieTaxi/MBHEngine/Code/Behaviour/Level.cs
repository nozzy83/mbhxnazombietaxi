using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OgmoXNA4;
using OgmoXNA4.Layers;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework;
using MBHEngine.GameObject;
using MBHEngineContentDefs;

namespace MBHEngine.Behaviour
{
    /// <summary>
    /// The root of most content for the game.  Contains all the data about a particular area of the game.
    /// </summary>
    public class Level : MBHEngine.Behaviour.Behaviour
    {
        /// <summary>
        /// Collision information for this level.
        /// </summary>
        Int32[,] mCollisionGrid;

        Texture2D debugTexture;

        /// <summary>
        /// Constructor which also handles the process of loading in the Behaviour
        /// Definition information.
        /// </summary>
        /// <param name="parentGOH">The game object that this behaviour is attached to.</param>
        /// <param name="fileName">The file defining this behaviour.</param>
        public Level(GameObject.GameObject parentGOH, String fileName)
            : base(parentGOH, fileName)
        {
        }

        /// <summary>
        /// Call this to initialize a Behaviour with data supplied in a file.
        /// </summary>
        /// <param name="fileName">The file to load from.</param>
        public override void LoadContent(String fileName)
        {
            LevelDefinition def = GameObjectManager.pInstance.pContentManager.Load<LevelDefinition>(fileName);

            mCollisionGrid = new Int32[80, 60];

            mCollisionGrid[40, 28] = 1;
            mCollisionGrid[40, 29] = 1;
            mCollisionGrid[40, 30] = 1;
            mCollisionGrid[41, 30] = 1;
            mCollisionGrid[42, 30] = 1;

            debugTexture = new Texture2D(GameObjectManager.pInstance.pGraphicsDevice, 1, 1);
            debugTexture.SetData(new Color[] { Color.White });

            base.LoadContent(fileName);
        }

        /// <summary>
        /// Called once render cycle by the game object manager.
        /// </summary>
        /// <param name="batch">The sprite batch to render to.</param>
        public override void Render(SpriteBatch batch)
        {
            for (Int32 y = 0; y < 60; y++)
            {
                for (Int32 x = 0; x < 80; x++)
                {
                    if (mCollisionGrid[x, y] != 0)
                    {
                        batch.Draw(debugTexture, new Rectangle(x * 8, y * 8, 8, 8), Color.Black);
                    }
                }
            }
        }
    }
}
