using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using MBHEngine.GameObject;
using MBHEngine.Behaviour;

namespace ZombieTaxi.Behaviours.HUD
{
    /// <summary>
    /// HUD element used for rendering the players current health as a meter.
    /// </summary>
    class PlayerHealthBar : MBHEngine.Behaviour.Behaviour
    {
        /// <summary>
        /// For every Hit Point of Health, this many pixels should be rendered on the
        /// health bar. This allows us to dynamically resize the bar as we level up.
        /// </summary>
        private Single mPixPerHP;

        /// <summary>
        /// The diferent textures that make up this single HUD element.
        /// </summary>
        private Texture2D mTextureBG;
        private Texture2D mTextureBorder;
        private Texture2D mTextureBorderEnd;
        private Texture2D mTextureFill;

        /// <summary>
        /// The fill texture gets resized to match the player's current health.  This
        /// rectangle defines that area.
        /// </summary>
        private Rectangle mFillRect;

        /// <summary>
        /// The border and background textures get resized to match the current max health.
        /// This rectangle defines that area.
        /// </summary>
        private Rectangle mBGRect;

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
            mTextureBorderEnd = GameObjectManager.pInstance.pContentManager.Load<Texture2D>("Sprites\\Interface\\HealthBarBorderEnd");
            mTextureFill = GameObjectManager.pInstance.pContentManager.Load<Texture2D>("Sprites\\Interface\\HealthBarFill");

            mFillRect = new Rectangle(0, 0, mTextureFill.Width, mTextureFill.Height);
            mBGRect = new Rectangle(0, 0, mTextureBG.Width, mTextureBG.Height);

            mGetHealthMsg = new Health.GetHealthMessage();
        }

        /// <summary>
        /// Called at the end of the frame where mParentGOH was added to the GameObjectManager.
        /// </summary>
        public override void OnAdd()
        {
            // This can't be done during load, so we wait for it to be added to the manager first.
            GameObjectManager.pInstance.pPlayer.OnMessage(mGetHealthMsg);

            // The pixels per hit point are based on the actual width of the fill texture.
            // This means that the width of the health bar fill png will be the width of 
            // the health bar at level 1. As we level up it will grow, relative to that
            // starting size.
            mPixPerHP = mTextureFill.Width / mGetHealthMsg.mMaxHealth_Out;
        }

        /// <summary>
        /// Called once per frame by the game object.
        /// </summary>
        /// <param name="gameTime">The amount of time that has passed this frame.</param>
        public override void Update(GameTime gameTime)
        {
            GameObjectManager.pInstance.pPlayer.OnMessage(mGetHealthMsg);

            // Earlier we calculated the how many pixels per hitpoint should be drawn for the 
            // health bar. Now use that number scaled by the actually number of HP to determine
            // the side of the HP fill.
            mFillRect.Width = (Int32)Math.Round(mPixPerHP * mGetHealthMsg.mCurrentHealth_Out);

            // The backgound uses the same calculation except using the map HP since it doesn't
            // change as the HP lowers. Add 2 pixel since this overlaps with the border.
            mBGRect.Width = (Int32)Math.Round(mPixPerHP * mGetHealthMsg.mMaxHealth_Out) + 2;
        }

        /// <summary>
        /// Called once render cycle by the game object manager.
        /// </summary>
        /// <param name="batch">The sprite batch to render to.</param>
        /// <param name="effect">The currently set shader.</param>
        public override void Render(SpriteBatch batch, Effect effect)
        {
            batch.Draw(
                mTextureBG,
                new Vector2(mParentGOH.pPosition.X, mParentGOH.pPosition.Y),
                mBGRect,
                Color.White);

            batch.Draw(
                mTextureFill, 
                new Vector2(mParentGOH.pPosition.X + 1, mParentGOH.pPosition.Y + 1),
                mFillRect, 
                Color.White);

            batch.Draw(
                mTextureBorder,
                new Vector2(mParentGOH.pPosition.X, mParentGOH.pPosition.Y),
                mBGRect,
                Color.White);
            batch.Draw(
                mTextureBorderEnd,
                new Vector2(mParentGOH.pPosition.X + mBGRect.Width - 1, mParentGOH.pPosition.Y),
                Color.White);
        }
    }
}
