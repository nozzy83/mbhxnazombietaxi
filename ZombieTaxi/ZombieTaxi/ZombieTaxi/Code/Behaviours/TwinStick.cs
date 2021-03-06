﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MBHEngine.GameObject;
using Microsoft.Xna.Framework.Graphics;
using MBHEngine.Input;
using Microsoft.Xna.Framework.Input;
using MBHEngine.Debug;
using MBHEngine.Math;
using MBHEngine.Render;
using MBHEngine.Behaviour;
using MBHEngine.World;
using Microsoft.Xna.Framework;
using ZombieTaxiContentDefs;
using MBHEngineContentDefs;

namespace ZombieTaxi.Behaviours
{
    /// <summary>
    /// A class to handle the controls of a twin stick shooter.
    /// </summary>
    class TwinStick : MBHEngine.Behaviour.Behaviour
    {
        /// <summary>
        /// Increase the level of weapon used by the player.
        /// </summary>
        public class IncrementGunLevelMessage : BehaviourMessage
        {
            /// <summary>
            /// How many levels to increment.
            /// </summary>
            public Int32 mIncrementAmount_In;

            /// <summary>
            /// Call this to put a message back to its default state.
            /// </summary>
            public override void Reset()
            {
                mIncrementAmount_In = 0;
            }
        }

        /// <summary>
        /// Retrive how many levels are still possible to upgrade the weapon.
        /// </summary>
        public class GetGunLevelsRemainingMessage : BehaviourMessage
        {
            /// <summary>
            /// How many more levels of upgrades are available.
            /// </summary>
            public Int32 mLevelRemaining_In;

            /// <summary>
            /// Call this to put a message back to its default state.
            /// </summary>
            public override void Reset()
            {
                mLevelRemaining_In = 0;
            }
        }

        /// <summary>
        /// Upgrades are defined in script as just levels. What each level is gets defined by these
        /// structures.
        /// </summary>
        public struct GunLevelInfo
        {
            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="damageMod">How much the base damage will be multiplied by.</param>
            public GunLevelInfo(Single damageMod, Single scaleMod, Single speedMod)
            {
                mDamageMod = damageMod;
                mScaleMod = scaleMod;
                mSpeedMod = speedMod;
            }

            /// <summary>
            /// How much the base damage will be multiplied by.
            /// </summary>
            public Single mDamageMod;

            /// <summary>
            /// How much to scale up the bullet based on level.
            /// </summary>
            public Single mScaleMod;

            /// <summary>
            /// How much faster should the bullet move based on level.
            /// </summary>
            public Single mSpeedMod;
        }

        /// <summary>
        /// The speed at which the GOH is moved when the user presses the dpad.
        /// </summary>
        private Single mMoveSpeed;

        /// <summary>
        /// The gun used for firing.
        /// </summary>
        private GameObject mGun;

        /// <summary>
        /// The amount of time that needs to pass between shots of the gun.
        /// </summary>
        private StopWatch mGunCooldown;

        /// <summary>
        /// The amont of time that needs to pass between grenades being thrown.
        /// </summary>
        private StopWatch mGrenadeCooldown;

        /// <summary>
        /// We only want to place a safe house once.
        /// </summary>
        private Boolean mSafeHousePlaced;

        /// <summary>
        /// Used to fill a structure with SafeHouseFloor objects.
        /// </summary>
        private GameObjectFloodFill mGOFloodFill;

        /// <summary>
        /// The current upgrade level of the gun.
        /// </summary>
        private Int32 mGunLevel;

        /// <summary>
        /// A list of all the possible gun levels.
        /// </summary>
        private GunLevelInfo[] mGunLevelInfo;

        /// <summary>
        /// Preallocated messages to avoid GC.
        /// </summary>
        private SpriteRender.SetSpriteEffectsMessage mSpriteFxMsg;
        private SpriteRender.GetSpriteEffectsMessage mGetSpriteFxMsg;
        private SpriteRender.SetActiveAnimationMessage mSpriteActiveAnimMsg;
        private ExtractionPoint.SetExtractionPointActivateMessage mSetExtractionPointActivateMsg;
        private SpriteRender.GetAttachmentPointMessage mGetAttachmentPointMsg;
        private Level.GetTileAtObjectMessage mGetTileAtObjectMsg;
        private Explosive.SetDamageMultiplierMessage mSetDamageModifierMessage;

        /// <summary>
        /// Constructor which also handles the process of loading in the Behaviour
        /// Definition information.
        /// </summary>
        /// <param name="parentGOH">The game object that this behaviour is attached to.</param>
        /// <param name="fileName">The file defining this behaviour.</param>
        public TwinStick(GameObject parentGOH, String fileName)
            : base(parentGOH, fileName)
        {
        }

        /// <summary>
        /// Destructor.
        /// </summary>
        ~TwinStick()
        {
            StopWatchManager.pInstance.RecycleStopWatch(mGunCooldown);
            StopWatchManager.pInstance.RecycleStopWatch(mGrenadeCooldown);
        }

        /// <summary>
        /// Call this to initialize a Behaviour with data supplied in a file.
        /// </summary>
        /// <param name="fileName">The file to load from.</param>
        public override void LoadContent(String fileName)
        {
            base.LoadContent(fileName);

            TwinStickDefinition def = GameObjectManager.pInstance.pContentManager.Load<TwinStickDefinition>(fileName);

            mMoveSpeed = def.mMoveSpeed;

            mGun = new GameObject("GameObjects\\Items\\Gun\\Gun");
            GameObjectManager.pInstance.Add(mGun);
            mGun.pPosition = mParentGOH.pPosition;
            mGun.pPosX = mGun.pPosX + 1.0f;
            //mGun.pPosition.Y = mGun.pPosition.Y + 4.5f;

            mGunCooldown = StopWatchManager.pInstance.GetNewStopWatch();
            mGunCooldown.pLifeTime = 5; // 1 bullet fired every 5 frames.

            mGrenadeCooldown = StopWatchManager.pInstance.GetNewStopWatch();
            mGrenadeCooldown.pLifeTime = 60;

            mSafeHousePlaced = false;

            mGunLevel = 0;

            mGOFloodFill = new GameObjectFloodFill();

            mGunLevelInfo = new GunLevelInfo[] 
            {
                new GunLevelInfo(1.0f, 1.0f, 1.0f),
                new GunLevelInfo(2.0f, 1.5f, 1.1f),
                new GunLevelInfo(4.0f, 1.5f, 1.2f),
                new GunLevelInfo(7.0f, 1.5f, 1.3f),
                new GunLevelInfo(11.0f, 1.5f, 1.4f),
                new GunLevelInfo(16.0f, 2.0f, 1.5f),
            };

            mSpriteFxMsg = new SpriteRender.SetSpriteEffectsMessage();
            mGetSpriteFxMsg = new SpriteRender.GetSpriteEffectsMessage();
            mSpriteActiveAnimMsg = new SpriteRender.SetActiveAnimationMessage();
            mSetExtractionPointActivateMsg = new ExtractionPoint.SetExtractionPointActivateMessage();
            mGetAttachmentPointMsg = new SpriteRender.GetAttachmentPointMessage();
            mGetTileAtObjectMsg = new Level.GetTileAtObjectMessage();
            mSetDamageModifierMessage = new Explosive.SetDamageMultiplierMessage();
        }

        /// <summary>
        /// Called once per frame by the game object.
        /// </summary>
        /// <param name="gameTime">The amount of time that has passed this frame.</param>
        public override void Update(GameTime gameTime)
        {
            // Grab the current state of the gamepad.
            GamePadThumbSticks padState = InputManager.pInstance.GetDirectionalInfo();

            // Store the original position prior to polling any input for use with collision reactions.
            Vector2 origPos = mParentGOH.pPosition;

            // The character will move at this rate in the direction of the Left Analog Stick.
            Vector2 dir1 = new Vector2(mMoveSpeed, -mMoveSpeed);
            dir1 *= padState.Left;
            mParentGOH.pPosition += dir1;

            // The position the Gun gets attached to is configured via an AttachPoint in the 
            // Player template.
            mGetAttachmentPointMsg.mName_In = "Gun";
            mParentGOH.OnMessage(mGetAttachmentPointMsg);
            Vector2 mGunAttachPos = mGetAttachmentPointMsg.mPoisitionInWorld_Out;

            // Position the gun at the attachment point.
            mGun.pPosition = mGunAttachPos;

            // Flip the sprite to face the direction that we are moving.
            if (padState.Left.X > 0)
            {
                mSpriteFxMsg.mSpriteEffects_In = SpriteEffects.None;
                mParentGOH.OnMessage(mSpriteFxMsg);

                // Initially the gun is positioned assuming the R-Stick is not pressed.  Just point straight
                // in the direction the player is walking.
                mSpriteFxMsg.mSpriteEffects_In = SpriteEffects.None;
                mGun.OnMessage(mSpriteFxMsg);
                mGun.pRotation = 0.0f;
            }
            else if (padState.Left.X < 0)
            {
                mSpriteFxMsg.mSpriteEffects_In = SpriteEffects.FlipHorizontally;
                mParentGOH.OnMessage(mSpriteFxMsg);

                mSpriteFxMsg.mSpriteEffects_In = SpriteEffects.FlipVertically;
                mGun.OnMessage(mSpriteFxMsg);
                mGun.pRotation = MathHelper.ToRadians(180.0f);
            }
            
            // If the player is moving in any direction play the walking animation.
            if (padState.Left != Vector2.Zero)
            {
                mSpriteActiveAnimMsg.mAnimationSetName_In = "Run";
                mParentGOH.OnMessage(mSpriteActiveAnimMsg);
            }
            else
            {
                mSpriteActiveAnimMsg.mAnimationSetName_In = "Idle";
                mParentGOH.OnMessage(mSpriteActiveAnimMsg);
            }

            // Convert the direction of the right analog stick into an angle so that it can be used to set the rotation of
            // the sprite.
            Double angle = Math.Atan2(-padState.Right.Y, padState.Right.X);
            if (angle < 0)
            {
                angle += 2 * Math.PI;
            }

            //DebugMessageDisplay.pInstance.AddDynamicMessage("Angle: " + MathHelper.ToDegrees((Single)angle);
            //DebugMessageDisplay.pInstance.AddDynamicMessage("X: " + g.ThumbSticks.Right.X);
            //DebugMessageDisplay.pInstance.AddDynamicMessage("Y: " + g.ThumbSticks.Right.Y);

            // Determine the direction that right analog stick is pointing (if any).
            Vector2 dir = Vector2.Normalize(padState.Right);

            // If the user is pressing the right analog stick, then they need to fire a bullet.
            if (!Single.IsNaN(dir.X) && !Single.IsNaN(dir.Y))
            {
                //mGun.pPosition += dir;
                mGun.pRotation = (Single)angle;

                // Use dir, not finalDir, so that the direction does not include the spread randomization.
                if (dir.X > 0)
                {
                    mSpriteFxMsg.mSpriteEffects_In = SpriteEffects.None;
                    mParentGOH.OnMessage(mSpriteFxMsg);

                    // To start the gun would be set to point in the direction we are walking.  We have turned to face the direction
                    // the player is shooting, so the gun needs to be updated as well.
                    mSpriteFxMsg.mSpriteEffects_In = SpriteEffects.None;
                    mGun.OnMessage(mSpriteFxMsg);
                }
                else if (dir.X < 0)
                {
                    mSpriteFxMsg.mSpriteEffects_In = SpriteEffects.FlipHorizontally;
                    mParentGOH.OnMessage(mSpriteFxMsg);

                    mSpriteFxMsg.mSpriteEffects_In = SpriteEffects.FlipVertically;
                    mGun.OnMessage(mSpriteFxMsg);
                }

                // We want some slight randomness to the bullets fired.  This is the randomness in radians.
                Single spread = 0.1f;

                // If they are holding R2, then the spread is even larger.
                if (InputManager.pInstance.CheckAction(InputManager.InputActions.R2, false))
                {
                    spread = 0.5f;
                }

                // Offset by a random amount within the spread range.
                Single offset = ((Single)RandomManager.pInstance.RandomPercent() * spread) - (spread * 0.5f);
                angle += offset;

                // Convert the angle back into a vector so that it can be used to move the bullet.
                Vector2 finalDir = new Vector2((Single)Math.Cos(angle), (Single)Math.Sin(angle));
                finalDir.Y *= -1;

                Vector2 finalUp = new Vector2(-finalDir.Y, -finalDir.X);
                if (finalDir.X < 0) finalUp *= -1;

                if (mGunCooldown.IsExpired())
                {
                    mGunCooldown.Restart();

                    GameObject bullet = GameObjectFactory.pInstance.GetTemplate("GameObjects\\Items\\Bullet\\Bullet");

                    if (bullet != null)
                    {
                        // Store the direction locally so as to not alter it and screw things
                        // up for the grenade afterwards.
                        Vector2 bulletDir = finalDir;

                        bullet.pDirection.mSpeed = 1.75f * mGunLevelInfo[mGunLevel].mSpeedMod;

                        // Update the game object with all the new data.
                        bullet.pPosition = mGun.pPosition;
                        bullet.pRotation = (Single)angle;
                        bullet.pDirection.mForward = bulletDir;

                        bulletDir.Y *= -1;
                        bullet.pPosition += finalUp * 1.0f;
                        bullet.pPosition += bulletDir * 3.5f;

                        // The screen's y direction is opposite the controller.
                        bullet.pDirection.mForward.Y *= -1;

                        // Use our current gun level to upgrade the bullet.
                        mSetDamageModifierMessage.mDamageMod_In = mGunLevelInfo[mGunLevel].mDamageMod;
                        bullet.OnMessage(mSetDamageModifierMessage);

                        bullet.pScaleX = mGunLevelInfo[mGunLevel].mScaleMod;

                        GameObjectManager.pInstance.Add(bullet);
                    }
                }

                if (InputManager.pInstance.CheckAction(InputManager.InputActions.R1, true))
                {
                    if (mGrenadeCooldown.IsExpired())
                    {
                        mGrenadeCooldown.Restart();

                        GameObject go = GameObjectFactory.pInstance.GetTemplate("GameObjects\\Items\\Grenade\\Grenade");
                        Vector2 grenadeDir = finalDir;
                        grenadeDir.Y *= -1;
                        go.pPosition = mGun.pPosition;
                        go.pPosition += finalUp * 1.0f;
                        go.pPosition += grenadeDir * 3.5f;
                        go.pPosition = go.pPosition;
                        go.pRotation = (Single)angle;
                        go.pDirection.mForward = grenadeDir;
                        go.pDirection.mSpeed = 0.25f;
                        GameObjectManager.pInstance.Add(go);
                    }
                }
            }

            if (InputManager.pInstance.CheckAction(InputManager.InputActions.L1, true))
            {
                // The first time they press L1, we place the safe house. After that, we drop extraction
                // flares.
                if (!mSafeHousePlaced)
                {
                    // Get the tile where mParentGOH is standing, so that we can start a Flood Fill
                    // at that position.
                    mGetTileAtObjectMsg.mObject_In = mParentGOH;
                    mGetTileAtObjectMsg.mTile_Out = null;

                    WorldManager.pInstance.pCurrentLevel.OnMessage(mGetTileAtObjectMsg);

                    // Attempting to fill the world will SafeHouseFloors.
                    mSafeHousePlaced = mGOFloodFill.FloodFill(
                                                        mGetTileAtObjectMsg.mTile_Out, 
                                                        10 * 10, 
                                                        "GameObjects\\Environments\\FloorSafeHouse\\FloorSafeHouse");
                }
                else
                {
                    GameObject go = GameObjectFactory.pInstance.GetTemplate("GameObjects\\Items\\Flare\\Flare");
                    go.pPosition = mGun.pPosition;
                    GameObjectManager.pInstance.Add(go);

                    go.OnMessage(mSetExtractionPointActivateMsg);
                }
            }

            mGOFloodFill.ProcessFill();

            DebugMessageDisplay.pInstance.AddDynamicMessage("Player Pos: " + mParentGOH.pPosition);
            
            CameraManager.pInstance.pTargetPosition = mParentGOH.pPosition;
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
            // Which type of message was sent to us?
            if (msg is Health.OnZeroHealthMessage)
            {
                DebugMessageDisplay.pInstance.AddConstantMessage("GAME OVER - Player Died");
            }
            else if (msg is IncrementGunLevelMessage)
            {
                IncrementGunLevelMessage temp = (IncrementGunLevelMessage)msg;

                mGunLevel = System.Math.Min(mGunLevelInfo.Length - 1, mGunLevel + temp.mIncrementAmount_In);
            }
            else if (msg is GetGunLevelsRemainingMessage)
            {
                GetGunLevelsRemainingMessage temp = (GetGunLevelsRemainingMessage)msg;
                temp.mLevelRemaining_In = (mGunLevelInfo.Length - 1) - mGunLevel;
            }
        }
    }
}
