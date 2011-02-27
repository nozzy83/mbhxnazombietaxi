using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using MBHEngine.IO;
using MBHEngine.Math;
using MBHEngine.GameObject;
using MBHEngine.Input;
using MBHEngine.Debug;
using ZombieTaxi.Behaviour;

namespace ZombieTaxi
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager mGraphics;
        SpriteBatch mSpriteBatch;

        /// <summary>
        /// Reder state for rendering clear, crisp sprites.
        /// </summary>
        RasterizerState mSpriteRasterState; // Prevent the edge of the sprite showing garabage.
        SamplerState mSpriteSamplerState; // Keep the sprites looking Crisp.

        /// <summary>
        /// Constuctor
        /// </summary>
        /// <param name="args">Command-line arguments passed to the executable.</param>
        public Game1(string[] args)
        {
            CommandLineManager.pInstance.pArgs = args;

            mGraphics = new GraphicsDeviceManager(this);
            mGraphics.PreferredBackBufferWidth = 1280;
            mGraphics.PreferredBackBufferHeight = 720;
            Content.RootDirectory = "Content";

            // Avoid the "jitter".
            // http://forums.create.msdn.com/forums/p/9934/53561.aspx#53561
            IsFixedTimeStep = false;

            //mGraphics.GraphicsDevice.PresentationParameters.MultiSampleType = MultiSampleType.TwoSamples;
            //mGraphics.GraphicsDevice.RenderState.MultiSampleAntiAlias = true;
            mGraphics.PreferMultiSampling = true;
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            mSpriteBatch = new SpriteBatch(GraphicsDevice);

            GameObjectManager.pInstance.Initialize(Content, mGraphics);
            PhysicsManager.pInstance.Initialize(mGraphics, mSpriteBatch);

            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            mSpriteBatch = new SpriteBatch(GraphicsDevice);

            mSpriteRasterState = new RasterizerState();
            mSpriteSamplerState = new SamplerState();

            // Prevent the edge of the sprite showing garabage.
            mSpriteRasterState.MultiSampleAntiAlias = false;

            // Keep the sprites looking Crisp.
            mSpriteSamplerState.Filter = TextureFilter.Point;

            GameObject debugStatsDisplay = new GameObject();
            MBHEngine.Behaviour.Behaviour fps = new MBHEngine.Behaviour.FrameRateDisplay(debugStatsDisplay, null);
            debugStatsDisplay.AttachBehaviour(fps);
            GameObjectManager.pInstance.Add(debugStatsDisplay);

            GameObject player = new GameObject("Player\\Player");
            MBHEngine.Behaviour.Behaviour t = new TwinStick(player, null);
            player.AttachBehaviour(t);
            GameObjectManager.pInstance.Add(player);

#if ALLOW_GARBAGE
            DebugMessageDisplay.pInstance.AddConstantMessage("Game Load Complete.");
#endif
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            // Allows the game to exit
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                this.Exit();

            DebugMessageDisplay.pInstance.ClearDynamicMessages();

#if ALLOW_GARBAGE
            DebugMessageDisplay.pInstance.AddDynamicMessage("Game-Time Delta: " + gameTime.ElapsedGameTime.TotalSeconds);
#endif
            GameObjectManager.pInstance.Update(gameTime);
            PhysicsManager.pInstance.Update(gameTime);
            InputManager.pInstance.UpdateEnd();

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            mSpriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);

            // Keep the sprites looking crisp.
            mSpriteBatch.GraphicsDevice.SamplerStates[0] = mSpriteSamplerState;
            mSpriteBatch.GraphicsDevice.RasterizerState = mSpriteRasterState;
            GameObjectManager.pInstance.Render(mSpriteBatch);
            DebugMessageDisplay.pInstance.Render(mSpriteBatch);
            mSpriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
