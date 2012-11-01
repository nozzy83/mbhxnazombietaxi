using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MBHEngine.GameObject;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using MBHEngine.Behaviour;

namespace ZombieTaxi.Behaviours.HUD
{
    class PlayerInventory : MBHEngine.Behaviour.Behaviour
    {
        /// <summary>
        /// The diferent textures that make up this single HUD element.
        /// </summary>
        private Texture2D mTextureBG;

        /// <summary>
        /// Holds a reference to the texture used by the SpriteRender beaviour of the current 
        /// Inventory Item.
        /// </summary>
        private Texture2D mTextureItem;

        /// <summary>
        /// Items need to have slightly different positions than the background to account for 
        /// padding and border.
        /// </summary>
        private Vector2 mItemOffset;

        /// <summary>
        /// Preallocated messages to avoid GC.
        /// </summary>
        private Inventory.PeekCurrentObjectMessage mPeekCurrentObjectMsg;
        private SpriteRender.GetTexture2DMessage mGetTexture2DMsg;

        /// <summary>
        /// Constructor which also handles the process of loading in the Behaviour
        /// Definition information.
        /// </summary>
        /// <param name="parentGOH">The game object that this behaviour is attached to.</param>
        /// <param name="fileName">The file defining this behaviour.</param>
        public PlayerInventory(GameObject parentGOH, String fileName)
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

            mTextureBG = GameObjectManager.pInstance.pContentManager.Load<Texture2D>("Sprites\\Interface\\InventoryView");

            mItemOffset = new Vector2(4, 4);

            mPeekCurrentObjectMsg = new Inventory.PeekCurrentObjectMessage();
            mGetTexture2DMsg = new SpriteRender.GetTexture2DMessage();
        }

        /// <summary>
        /// Called once per frame by the game object.
        /// </summary>
        /// <param name="gameTime">The amount of time that has passed this frame.</param>
        public override void Update(GameTime gameTime)
        {
            mPeekCurrentObjectMsg.Reset();
            mGetTexture2DMsg.Reset();

            GameObjectManager.pInstance.pPlayer.OnMessage(mPeekCurrentObjectMsg);

            // Check if their is any object currently active.
            if (null != mPeekCurrentObjectMsg.mOutObj)
            {
                // Now check what texture is used to render that item.
                mPeekCurrentObjectMsg.mOutObj.OnMessage(mGetTexture2DMsg);

                // Store it for Render to use.
                mTextureItem = mGetTexture2DMsg.mOutTexture;
            }
            else
            {
                // If there is no current item in the Inventory than show it as empty.
                mTextureItem = null;
            }
        }

        /// <summary>
        /// Called once render cycle by the game object manager.
        /// </summary>
        /// <param name="batch">The sprite batch to render to.</param>
        public override void Render(SpriteBatch batch)
        {
            batch.Draw(mTextureBG, mParentGOH.pPosition, Color.White);

            if (null != mTextureItem)
            {
                Rectangle rect = new Rectangle(0, 0, 8, 8);
                batch.Draw(mTextureItem,
                           mParentGOH.pPosition + mItemOffset,
                           rect,
                           Color.White);
            }
        }
    }
}
