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
using MBHEngine.GameObject;
using MBHEngine.IO;
using MBHEngine.Math;
using MBHEngine.Input;
using MBHEngine.Debug;
using MBHEngine.Render;
using ZombieTaxi.Behaviours;
using OgmoXNA4;
using MBHEngine.Behaviour;

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
        /// This will defined as a multiply blend state.
        /// </summary>
        BlendState mMultiply;

        /// <summary>
        /// Graphic for the vingette.
        /// </summary>
        GameObject mVingetting;

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
            //mGraphics.IsFullScreen = true;
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
            GameObject.AddBehaviourCreator(new ClientBehaviourCreator());

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

            mMultiply = new BlendState();
            mMultiply.ColorSourceBlend = Blend.Zero;
            mMultiply.ColorDestinationBlend = Blend.SourceColor;

            // Prevent the edge of the sprite showing garabage.
            mSpriteRasterState.MultiSampleAntiAlias = false;

            // Keep the sprites looking Crisp.
            mSpriteSamplerState.Filter = TextureFilter.Point;

            GameObject bg = new GameObject();
            MBHEngine.Behaviour.Behaviour t = new InfiniteBG(bg, null);
            bg.AttachBehaviour(t);
            GameObjectManager.pInstance.Add(bg);

            GameObject debugStatsDisplay = new GameObject();
            MBHEngine.Behaviour.Behaviour fps = new MBHEngine.Behaviour.FrameRateDisplay(debugStatsDisplay, null);
            debugStatsDisplay.AttachBehaviour(fps);
            GameObjectManager.pInstance.Add(debugStatsDisplay);

            GameObject level = new GameObject("GameObjects\\Levels\\Demo\\Demo");
            //t = new Level(level, null);
            //level.AttachBehaviour(t);
            GameObjectManager.pInstance.Add(level);

            GameObject player = new GameObject("GameObjects\\Characters\\Player\\Player");
            t = new TwinStick(player, null);
            player.AttachBehaviour(t);
            GameObjectManager.pInstance.Add(player);
            // Store the player for easy access.
            GameObjectManager.pInstance.pPlayer = player;

            GameObject enemy = new GameObject("GameObjects\\Characters\\Kamikaze\\Kamikaze");
            t = new Kamikaze(enemy, null);
            enemy.AttachBehaviour( t );
            //GameObjectManager.pInstance.Add(enemy);

            mVingetting = new GameObject("GameObjects\\Interface\\Vingette\\Vingette");
            //GameObjectManager.pInstance.Add(ving);

            //OgmoLevel ogmoLevel = this.Content.Load<OgmoLevel>("Levels\\Sample\\SampleLevel");    

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
            CameraManager.pInstance.Update(gameTime);

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            // First draw all the objects managed by the game object manager.
            mSpriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, null, null, null, null, CameraManager.pInstance.pFinalTransform);
            // Keep the sprites looking crisp.
            mSpriteBatch.GraphicsDevice.SamplerStates[0] = mSpriteSamplerState;
            mSpriteBatch.GraphicsDevice.RasterizerState = mSpriteRasterState;
            GameObjectManager.pInstance.Render(mSpriteBatch);
            mSpriteBatch.End();

            // Cheap hack for now to add Vingetting around the edge of the screen.  Ultimatly we will need a more
            // formal way to sort by render stlyes while still respecting render priority.
            mSpriteBatch.Begin(SpriteSortMode.Immediate, mMultiply);
            mVingetting.Render(mSpriteBatch);
            mSpriteBatch.End();

            // We need to go back to standard alpha blend before drawing the debug layer.
            mSpriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
            mSpriteBatch.GraphicsDevice.SamplerStates[0] = mSpriteSamplerState;
            mSpriteBatch.GraphicsDevice.RasterizerState = mSpriteRasterState;
            DebugMessageDisplay.pInstance.Render(mSpriteBatch);
            mSpriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
