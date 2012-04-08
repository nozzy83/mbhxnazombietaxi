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
using MBHEngine.World;

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
        /// Whether or not to draw debug information.
        /// </summary>
        Boolean mDebugDrawEnabled;
        
        /// <summary>
        /// Constuctor
        /// </summary>
        /// <param name="args">Command-line arguments passed to the executable.</param>
        public Game1(string[] args)
        {
            CommandLineManager.pInstance.pArgs = args;

            mGraphics = new GraphicsDeviceManager(this);
#if SMALL_WINDOW
            mGraphics.PreferredBackBufferWidth = 640;
            mGraphics.PreferredBackBufferHeight = 360;
#else
            mGraphics.PreferredBackBufferWidth = 1280; // 1366; // 1280;
            mGraphics.PreferredBackBufferHeight = 720; // 768; // 720;
#endif
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
            GameObjectFactory.pInstance.Initialize();
            CameraManager.pInstance.Initialize(mGraphics.GraphicsDevice);
            StopWatchManager.pInstance.Initialize();

#if DEBUG
            // By default, in DEBUG the debug drawing is enabled.
            mDebugDrawEnabled = true;
#else
            // In release it is not.
            mDebugDrawEnabled = false;
#endif
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

            // Add any objects desired to the Game Object Factory.  These will be allocated now and can
            // be retrived later without any heap allocations.
            //
            GameObjectFactory.pInstance.AddTemplate("GameObjects\\Items\\Bullet\\Bullet", 100);
            GameObjectFactory.pInstance.AddTemplate("GameObjects\\Items\\Grenade\\Grenade", 10);
            GameObjectFactory.pInstance.AddTemplate("GameObjects\\Effects\\BulletSpark\\BulletSpark", 32);
            GameObjectFactory.pInstance.AddTemplate("GameObjects\\Effects\\Explosion\\Explosion", 10);
            

            // The tiled background image that travels will the player creating the illusion of
            // an infinite background image.
            GameObject bg = new GameObject();
            MBHEngine.Behaviour.Behaviour t = new InfiniteBG(bg, null);
            bg.AttachBehaviour(t);
            bg.pRenderPriority = 25;
            GameObjectManager.pInstance.Add(bg);

            // The place where the player must bring back recused characters to.
            GameObject safeHouse = new GameObject("GameObjects\\Environments\\SafeHouse\\SafeHouse");
            GameObjectManager.pInstance.Add(safeHouse);

            // Create the level.
            WorldManager.pInstance.Initialize();

            // Debug display for different states in the game.  This by creating new behaviours, additional
            // stats can be displayed.
            GameObject debugStatsDisplay = new GameObject();
            MBHEngine.Behaviour.Behaviour fps = new MBHEngine.Behaviour.FrameRateDisplay(debugStatsDisplay, null);
            debugStatsDisplay.AttachBehaviour(fps);
            GameObjectManager.pInstance.Add(debugStatsDisplay);

            // The player himself.
            GameObject player = new GameObject("GameObjects\\Characters\\Player\\Player");
            GameObjectManager.pInstance.Add(player);

            // Store the player for easy access.
            GameObjectManager.pInstance.pPlayer = player;

            GameObject chef = new GameObject("GameObjects\\Characters\\Civilian\\Civilian");
            GameObjectManager.pInstance.Add(chef);
            
            //GameObject enemy = new GameObject("GameObjects\\Characters\\Kamikaze\\Kamikaze");
            //enemy.pOrientation.mPosition.X = 50;
            //enemy.pOrientation.mPosition.Y = 50;
            //GameObjectManager.pInstance.Add(enemy);
            

            // This GO doesn't need to exist beyond creation, so don't bother adding it to the GO Manager.
            new GameObject("GameObjects\\Utils\\RandEnemyGenerator\\RandEnemyGenerator");
            new GameObject("GameObjects\\Utils\\RandCivilianGenerator\\RandCivilianGenerator");
            
            // The vingette effect used to dim out the edges of the screen.
            GameObject ving = new GameObject("GameObjects\\Interface\\Vingette\\Vingette");
#if SMALL_WINDOW
            ving.pOrientation.mScale = new Vector2(0.5f, 0.5f);
#endif
            GameObjectManager.pInstance.Add(ving);

            // The HUD element representing the player's health.
            GameObject health = new GameObject("GameObjects\\Interface\\PlayerHealthBar\\PlayerHealthBar");
            GameObjectManager.pInstance.Add(health);            

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

            // Toggle the debug drawing with a click of the left stick.
            if (InputManager.pInstance.CheckAction(InputManager.InputActions.L3, true))
            {
                mDebugDrawEnabled ^= true;
            }

            DebugMessageDisplay.pInstance.ClearDynamicMessages();
            DebugShapeDisplay.pInstance.Update();

#if ALLOW_GARBAGE
            DebugMessageDisplay.pInstance.AddDynamicMessage("Game-Time Delta: " + gameTime.ElapsedGameTime.TotalSeconds);
            DebugMessageDisplay.pInstance.AddDynamicMessage("Path Find - Unused: " + PathFind.pNumUnusedNodes);
#endif
            StopWatchManager.pInstance.Update();
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
            GameObjectManager.pInstance.Render(mSpriteBatch);

            if (mDebugDrawEnabled)
            {
                DebugShapeDisplay.pInstance.Render();

                // We need to go back to standard alpha blend before drawing the debug layer.
                mSpriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
                DebugMessageDisplay.pInstance.Render(mSpriteBatch);
                mSpriteBatch.End();
            }

            base.Draw(gameTime);
        }
    }
}
