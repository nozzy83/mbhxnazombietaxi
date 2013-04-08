using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MBHEngine.GameObject;
using MBHEngine.IO;
using MBHEngine.Math;
using MBHEngine.Input;
using MBHEngine.Debug;
using MBHEngine.Render;
using ZombieTaxi.Behaviours;
using MBHEngine.Behaviour;
using MBHEngine.World;
using MBHEngine.PathFind.GenericAStar;
using Microsoft.Xna.Framework.Input;

namespace ZombieTaxi
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        private GraphicsDeviceManager mGraphics;
        private SpriteBatch mSpriteBatch;

        /// <summary>
        /// Whether or not to draw debug information.
        /// </summary>
        private Boolean mDebugDrawEnabled;

        /// <summary>
        /// Debug controls for skipping updates calls to help debug in "slow motion".
        /// </summary>
        private Int32 mFrameSkip = 0;
        private Int32 mFameSkipCount = 0;

        /// <summary>
        /// Tracks the object that is spawned through some debug keys.
        /// </summary>
        private GameObject mSpawned;
        
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
            mDebugDrawEnabled = false;
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
            GameObjectFactory.pInstance.AddTemplate("GameObjects\\Characters\\Scout\\Scout", 32);
            GameObjectFactory.pInstance.AddTemplate("GameObjects\\Effects\\BulletSpark\\BulletSpark", 32);
            GameObjectFactory.pInstance.AddTemplate("GameObjects\\Effects\\Dust\\Dust", 10);
            GameObjectFactory.pInstance.AddTemplate("GameObjects\\Effects\\Explosion\\Explosion", 10);
            GameObjectFactory.pInstance.AddTemplate("GameObjects\\Environments\\WallWood\\WallWood", 600);
            GameObjectFactory.pInstance.AddTemplate("GameObjects\\Environments\\WallStone\\WallStone", 600);
            GameObjectFactory.pInstance.AddTemplate("GameObjects\\Environments\\WallSteel\\WallSteel", 600);
            GameObjectFactory.pInstance.AddTemplate("GameObjects\\Environments\\FloorSafeHouse\\FloorSafeHouse", 400);
            GameObjectFactory.pInstance.AddTemplate("GameObjects\\Items\\Bullet\\Bullet", 100);
            GameObjectFactory.pInstance.AddTemplate("GameObjects\\Items\\RifleBullet\\RifleBullet", 100);
            GameObjectFactory.pInstance.AddTemplate("GameObjects\\Items\\Grenade\\Grenade", 10);
            GameObjectFactory.pInstance.AddTemplate("GameObjects\\Items\\Flare\\Flare", 10);
            GameObjectFactory.pInstance.AddTemplate("GameObjects\\Items\\WoodPickup\\WoodPickup", 100);
            GameObjectFactory.pInstance.AddTemplate("GameObjects\\Items\\StonePickup\\StonePickup", 100);
            GameObjectFactory.pInstance.AddTemplate("GameObjects\\Items\\SteelPickup\\SteelPickup", 100);
            GameObjectFactory.pInstance.AddTemplate("GameObjects\\Items\\DetectorPickup\\DetectorPickup", 100);
            GameObjectFactory.pInstance.AddTemplate("GameObjects\\Items\\GunTurretPickup\\GunTurretPickup", 100);
            GameObjectFactory.pInstance.AddTemplate("GameObjects\\Items\\Detector\\Detector", 600);
            GameObjectFactory.pInstance.AddTemplate("GameObjects\\Items\\GunTurret\\GunTurret", 600);
            GameObjectFactory.pInstance.AddTemplate("GameObjects\\Interface\\ButtonHint\\ButtonHint", 4);
            GameObjectFactory.pInstance.AddTemplate("GameObjects\\Interface\\ResearchProgressBar\\ResearchProgressBar", 4);
            GameObjectFactory.pInstance.AddTemplate("GameObjects\\Interface\\StrandedPopup\\StrandedPopup", 1);
            GameObjectFactory.pInstance.AddTemplate("GameObjects\\Interface\\ScoutPopup\\ScoutPopup", 1);
            GameObjectFactory.pInstance.AddTemplate("GameObjects\\Interface\\MilitantPopup\\MilitantPopup", 1);

            // The tiled background image that travels will the player creating the illusion of
            // an infinite background image.
            GameObject bg = new GameObject();
            MBHEngine.Behaviour.Behaviour t = new InfiniteBG(bg, null);
            bg.AttachBehaviour(t);
            bg.pRenderPriority = 20;
            GameObjectManager.pInstance.Add(bg);

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

            // Cheat to start the player with some walls in their inventory.
            if (CommandLineManager.pInstance["CheatFillInventory"] != null)
            {
                Inventory.AddObjectMessage addObj = new Inventory.AddObjectMessage();
                for (Int32 i = 0; i < 25; i++)
                {
                    addObj.mObj_In = GameObjectFactory.pInstance.GetTemplate("GameObjects\\Environments\\WallWood\\WallWood");
                    player.OnMessage(addObj);

                    addObj.mObj_In = GameObjectFactory.pInstance.GetTemplate("GameObjects\\Environments\\WallStone\\WallStone");
                    player.OnMessage(addObj);

                    addObj.mObj_In = GameObjectFactory.pInstance.GetTemplate("GameObjects\\Environments\\WallSteel\\WallSteel");
                    player.OnMessage(addObj);
                }
            }

            // Store the player for easy access.
            GameObjectManager.pInstance.pPlayer = player;

            GameObject go = GameObjectFactory.pInstance.GetTemplate("GameObjects\\Items\\StonePickup\\StonePickup");
            go.pPosition = new Vector2(50, 50);
            GameObjectManager.pInstance.Add(go);

            for (Int32 i = 0; i < 16; i++)
            {
                GameObject chef = new GameObject("GameObjects\\Characters\\Civilian\\Civilian");
                chef.pPosX = 64;
                chef.pPosY = 64;
                GameObjectManager.pInstance.Add(chef);
            }
            
            //GameObject enemy = new GameObject("GameObjects\\Characters\\Kamikaze\\Kamikaze");
            //enemy.pPosition.X = 50;
            //enemy.pPosition.Y = 50;
            //GameObjectManager.pInstance.Add(enemy);
            
            // This GO doesn't need to exist beyond creation, so don't bother adding it to the GO Manager.
            new GameObject("GameObjects\\Utils\\RandEnemyGenerator\\RandEnemyGenerator");
            new GameObject("GameObjects\\Utils\\RandCivilianGenerator\\RandCivilianGenerator");
            
            // The vingette effect used to dim out the edges of the screen.
            GameObject ving = new GameObject("GameObjects\\Interface\\Vingette\\Vingette");
#if SMALL_WINDOW
            ving.pScale = new Vector2(0.5f, 0.5f);
#endif
            GameObjectManager.pInstance.Add(ving);

            // Add the HUD elements.
            //
            GameObjectManager.pInstance.Add(new GameObject("GameObjects\\Interface\\HUD\\PlayerHealthBar\\PlayerHealthBar"));
            GameObjectManager.pInstance.Add(new GameObject("GameObjects\\Interface\\HUD\\PlayerScore\\PlayerScore"));
            GameObjectManager.pInstance.Add(new GameObject("GameObjects\\Interface\\HUD\\PlayerInventory\\PlayerInventory"));
            GameObjectManager.pInstance.Add(new GameObject("GameObjects\\Interface\\HUD\\MiniMap\\MiniMap"));

            DebugMessageDisplay.pInstance.AddConstantMessage("Game Load Complete.");
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
            InputManager.pInstance.UpdateBegin();

            // Allows the game to exit
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                this.Exit();

            // Toggle the debug drawing with a click of the left stick.
            if (InputManager.pInstance.CheckAction(InputManager.InputActions.L3, true))
            {
                mDebugDrawEnabled ^= true;

                // When debug draw is enabled, turn on the hardware mouse so that things like the
                // GameObjectPicker work better.
                IsMouseVisible = mDebugDrawEnabled;
            }

            if (InputManager.pInstance.CheckAction(InputManager.InputActions.Y, true))
            {
                if (mSpawned != null)
                {
                    GameObjectManager.pInstance.Remove(mSpawned);
                    mSpawned = null;
                }

                //mSpawned = new GameObject("GameObjects\\Characters\\Civilian\\Civilian");
                mSpawned = new GameObject("GameObjects\\Characters\\Militant\\Militant");
                //mSpawned = new GameObject("GameObjects\\Characters\\Kamikaze\\Kamikaze");
                mSpawned.pPosition = GameObjectManager.pInstance.pPlayer.pPosition;
                mSpawned.pPosX += 64;
                GameObjectManager.pInstance.Add(mSpawned);
            }

#if DEBUG && false // Temporarily disable this feature while working on tile placement mode.
            if (InputManager.pInstance.CheckAction(InputManager.InputActions.DP_RIGHT, true))
            {
                mFrameSkip = Math.Max(mFrameSkip - 1, 0);
            }
            else if (InputManager.pInstance.CheckAction(InputManager.InputActions.DP_LEFT, true))
            {
                mFrameSkip++;
            }
#endif

            // If we are skipping frames, check if enough have passed before doing updates.
            if (mFameSkipCount >= mFrameSkip)
            {
                DebugMessageDisplay.pInstance.ClearDynamicMessages();
                DebugShapeDisplay.pInstance.Update();

                DebugMessageDisplay.pInstance.AddDynamicMessage("Game-Time Delta: " + gameTime.ElapsedGameTime.TotalSeconds);
                DebugMessageDisplay.pInstance.AddDynamicMessage("Path Find - Unused: " + MBHEngine.PathFind.GenericAStar.Planner.pNumUnusedNodes);
                DebugMessageDisplay.pInstance.AddDynamicMessage("Graph Neighbour - Unused: " + MBHEngine.PathFind.GenericAStar.GraphNode.pNumUnusedNeighbours);
                DebugMessageDisplay.pInstance.AddDynamicMessage("NavMesh - Unused: " + MBHEngine.PathFind.HPAStar.NavMesh.pUnusedGraphNodes);

                mFameSkipCount = 0;
                StopWatchManager.pInstance.Update();
                GameObjectManager.pInstance.Update(gameTime);
                PhysicsManager.pInstance.Update(gameTime);
            }
            else
            {
                mFameSkipCount++; 
            }

            if (mDebugDrawEnabled)
            {
                // This does some pretty expensive stuff, so only do it when it is really useful.
                GameObjectPicker.pInstance.Update(gameTime);
            }

            InputManager.pInstance.UpdateEnd();
            CameraManager.pInstance.Update(gameTime);

            DebugMessageDisplay.pInstance.AddDynamicMessage("Frame Skip: " + mFrameSkip);

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
