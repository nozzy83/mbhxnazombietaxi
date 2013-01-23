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
        /// Offset from the parent object to place the button hints at.
        /// </summary>
        private Vector2 mButtonOffset;

        /// <summary>
        /// Offset from the parent object to place the inventory count at.
        /// </summary>
        private Vector2 mCountOffset;

        /// <summary>
        /// The shadown below the inventory count.
        /// </summary>
        private Vector2 mCountOffsetShadow;

        /// <summary>
        /// Texture for the R2 button hint.
        /// </summary>
        private Texture2D mTextureR2;

        /// <summary>
        /// Texture for the L1 button hint.
        /// </summary>
        private Texture2D mTextureL1;

        /// <summary>
        /// The texture used for the inventory item might be animation, so we only want to render the
        /// first frame.
        /// </summary>
        private Rectangle mItemSourceRect;

        /// <summary>
        /// The font object we use for rendering.
        /// </summary>
        private SpriteFont mFont;

        /// <summary>
        /// The color of the text drop shadow,
        /// </summary>
        private Color mDropColor;

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
            mButtonOffset = new Vector2(0, -13);

            mCountOffset = new Vector2(15, 3);
            mCountOffsetShadow = new Vector2(mCountOffset.X, mCountOffset.Y + 1);

            mTextureR2 = GameObjectManager.pInstance.pContentManager.Load<Texture2D>("Sprites\\Interface\\R2");
            mTextureL1 = GameObjectManager.pInstance.pContentManager.Load<Texture2D>("Sprites\\Interface\\L1");

            // Create the font
            mFont = GameObjectManager.pInstance.pContentManager.Load<SpriteFont>("Fonts\\Retro");

            mDropColor = new Color(162, 162, 162);

            mItemSourceRect = new Rectangle(0, 0, 8, 8);

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
            if (null != mPeekCurrentObjectMsg.mObj_Out)
            {
                // Now check what texture is used to render that item.
                mPeekCurrentObjectMsg.mObj_Out.OnMessage(mGetTexture2DMsg);

                // Store it for Render to use.
                mTextureItem = mGetTexture2DMsg.mTexture_Out;
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
        /// <param name="effect">The currently set shader.</param>
        public override void Render(SpriteBatch batch, Effect effect)
        {
            batch.Draw(mTextureBG, mParentGOH.pPosition, Color.White);

            if (GameObjectManager.pInstance.pCurUpdatePass == MBHEngineContentDefs.BehaviourDefinition.Passes.DEFAULT)
            {
                batch.Draw(mTextureR2, mParentGOH.pPosition + mButtonOffset, Color.White);
            }
            else if (GameObjectManager.pInstance.pCurUpdatePass == MBHEngineContentDefs.BehaviourDefinition.Passes.PLACEMENT)
            {
                batch.Draw(mTextureL1, mParentGOH.pPosition + mButtonOffset, Color.White);
            }

            batch.DrawString(
                mFont,
                mPeekCurrentObjectMsg.mCount_Out.ToString(),
                mParentGOH.pPosition + mCountOffsetShadow,
                mDropColor);

            batch.DrawString(
                mFont, 
                mPeekCurrentObjectMsg.mCount_Out.ToString(),
                mParentGOH.pPosition + mCountOffset, 
                Color.White);

            if (null != mTextureItem)
            {
                batch.Draw(mTextureItem,
                           mParentGOH.pPosition + mItemOffset,
                           mItemSourceRect,
                           Color.White);
            }
        }
    }
}
