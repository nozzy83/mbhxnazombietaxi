using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using MBHEngine.GameObject;

namespace ZombieTaxi.Behaviours
{
    /// <summary>
    /// HUD element used for rendering the players current health as a meter.
    /// </summary>
    class PlayerHealthBar : MBHEngine.Behaviour.Behaviour
    {
        /// <summary>
        /// The diferent textures that make up this single HUD element.
        /// </summary>
        private Texture2D mTextureBG;
        private Texture2D mTextureBorder;
        private Texture2D mTextureFill;

        /// <summary>
        /// The fill texture gets resized to match the player's current health percentage.  This
        /// rectangle defines that area.
        /// </summary>
        private Rectangle mFillRect;

        /// <summary>
        /// Messages are preallocated to avoid garbage collection.
        /// </summary>
        private Health.GetHealthMessage mGetHealthMsg;

        /// <summary>
        /// Constructor which also handles the process of loading in the Behaviour
        /// Definition information.
        /// </summary>
        /// <param name="parentGOH">The game object that this behaviour is attached to.</param>
        /// <param name="fileName">The file defining this behaviour.</param>
        public PlayerHealthBar(GameObject parentGOH, String fileName)
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

            mTextureBG = GameObjectManager.pInstance.pContentManager.Load<Texture2D>("Sprites\\Interface\\HealthBarBG");
            mTextureBorder = GameObjectManager.pInstance.pContentManager.Load<Texture2D>("Sprites\\Interface\\HealthBarBorder");
            mTextureFill = GameObjectManager.pInstance.pContentManager.Load<Texture2D>("Sprites\\Interface\\HealthBarFill");

            mFillRect = new Rectangle(0, 0, mTextureFill.Width, mTextureFill.Height);

            mGetHealthMsg = new Health.GetHealthMessage();
        }

        /// <summary>
        /// Called once per frame by the game object.
        /// </summary>
        /// <param name="gameTime">The amount of time that has passed this frame.</param>
        public override void Update(GameTime gameTime)
        {
            GameObjectManager.pInstance.pPlayer.OnMessage(mGetHealthMsg);

            // Convert the player's health data into a percent full.
            Single percent = Math.Max(mGetHealthMsg.mCurrentHealth / mGetHealthMsg.mMaxHealth, 0.0f);

            // Use that percent to define a source rectangle for the fill texture.
            mFillRect.Width = (Int32)(mTextureFill.Width * percent);
        }

        /// <summary>
        /// Called once render cycle by the game object manager.
        /// </summary>
        /// <param name="batch">The sprite batch to render to.</param>
        public override void Render(SpriteBatch batch)
        {
            batch.Draw(mTextureBG, mParentGOH.pPosition, Color.White);
            batch.Draw(
                mTextureFill, 
                new Vector2(mParentGOH.pPosition.X + 1, mParentGOH.pPosition.Y + 1),
                mFillRect, 
                Color.White);
            batch.Draw(mTextureBorder, mParentGOH.pPosition, Color.White);
        }
    }
}
