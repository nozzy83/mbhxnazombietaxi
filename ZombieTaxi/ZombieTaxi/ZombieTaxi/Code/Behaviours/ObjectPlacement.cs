using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using MBHEngine.GameObject;
using Microsoft.Xna.Framework.Graphics;
using MBHEngine.Behaviour;
using MBHEngine.Input;
using MBHEngineContentDefs;
using MBHEngine.World;

namespace ZombieTaxi.Behaviours
{
    /// <summary>
    /// Logic for controls which allows the player to place objects in the world.
    /// </summary>
    class ObjectPlacement : MBHEngine.Behaviour.Behaviour
    {
        /// <summary>
        /// Sent out when the user attempts to place an object in the world.
        /// </summary>
        public class OnPlaceObjectMessage : BehaviourMessage
        {
            /// <summary>
            /// The position they wish to place it at.
            /// </summary>
            public Vector2 mPosition_In;

            /// <summary>
            /// Set to true of the object was successfully placed. If set to false, the 
            /// object will be returned to the Player's Inventory.
            /// </summary>
            public Boolean mObjectPlaced_Out;

            /// <summary>
            /// Return the Message to a default state.
            /// </summary>
            public override void Reset()
            {
                mPosition_In = Vector2.Zero;
                mObjectPlaced_Out = false;
            }
        }

        /// <summary>
        /// Cursor used to show the user which tile they are manipulating.
        /// </summary>
        private GameObject mCursor;

        /// <summary>
        /// The user can move the cursor around the world, one tile at a time. This
        /// vector is the number of tiles in the x and y that the cursor has moved.
        /// </summary>
        private Vector2 mCursorOffset;

        /// <summary>
        /// Preallocated list used for getting a list Wall objects at a given location.
        /// </summary>
        private List<GameObject> mCollidedObjects = new List<GameObject>();

        /// <summary>
        /// List of the classification of objects which will be removed if they collide with the
        /// cursor when the user presses the "Remove" button.
        /// </summary>
        private List<MBHEngineContentDefs.GameObjectDefinition.Classifications> mRemoveClassifications;

        /// <summary>
        /// The texture used to render the item under the cursor.
        /// </summary>
        private Texture2D mTextureItem;

        /// <summary>
        /// Items need to have slightly different positions than the background to account for 
        /// padding and border.
        /// </summary>
        private Vector2 mItemOffset;

        /// <summary>
        /// The texture used for the inventory item might be animation, so we only want to render the
        /// first frame.
        /// </summary>
        private Rectangle mItemSourceRect;

        /// <summary>
        /// The color used to render the item sprite.
        /// </summary>
        private Color mItemColor;

        /// <summary>
        /// Preallocated messages to avoid garbage collection.
        /// </summary>
        private Level.GetTileAtPositionMessage mGetTileAtPositionMsg;
        private Level.GetMapInfoMessage mGetMapInfoMsg;
        private Level.SetTileTypeAtPositionMessage mSetTileTypeAtPositionMsg;
        private Inventory.GetCurrentObjectMessage mGetCurrentObjectMsg;
        private ObjectPlacement.OnPlaceObjectMessage mOnPlaceObjectMsg;
        private Inventory.AddObjectMessage mAddObjectMsg;
        private Inventory.SelectNextItemMessage mSelectNextItemMsg;
        private SpriteRender.GetTexture2DMessage mGetTexture2DMsg;
        private Inventory.PeekCurrentObjectMessage mPeekCurrentObjectMsg;
        private Level.OnNavMeshInvalidatedMessage mOnNavMeshInvalidatedMsg;

        /// <summary>
        /// Constructor which also handles the process of loading in the Behaviour
        /// Definition information.
        /// </summary>
        /// <param name="parentGOH">The game object that this behaviour is attached to.</param>
        /// <param name="fileName">The file defining this behaviour.</param>
        public ObjectPlacement(GameObject parentGOH, String fileName)
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

            //InventoryDefinition def = GameObjectManager.pInstance.pContentManager.Load<InventoryDefinition>(fileName);

            mCursor = new GameObject("GameObjects\\Interface\\PlacementCursor\\PlacementCursor");
            mCursor.pPosition = mParentGOH.pPosition;
            GameObjectManager.pInstance.Add(mCursor);

            mCursorOffset = Vector2.Zero;

            mItemOffset = new Vector2(-4, -4);
            mItemSourceRect = new Rectangle(0, 0, 8, 8);
            mItemColor = new Color(255, 255, 255, 200);

            mRemoveClassifications = new List<MBHEngineContentDefs.GameObjectDefinition.Classifications>(1);
            mRemoveClassifications.Add(MBHEngineContentDefs.GameObjectDefinition.Classifications.WALL);

            mGetTileAtPositionMsg = new Level.GetTileAtPositionMessage();
            mGetMapInfoMsg = new Level.GetMapInfoMessage();
            mSetTileTypeAtPositionMsg = new Level.SetTileTypeAtPositionMessage();
            mGetCurrentObjectMsg = new Inventory.GetCurrentObjectMessage();
            mOnPlaceObjectMsg = new OnPlaceObjectMessage();
            mAddObjectMsg = new Inventory.AddObjectMessage();
            mSelectNextItemMsg = new Inventory.SelectNextItemMessage();
            mGetTexture2DMsg = new SpriteRender.GetTexture2DMessage();
            mPeekCurrentObjectMsg = new Inventory.PeekCurrentObjectMessage();
            mOnNavMeshInvalidatedMsg = new Level.OnNavMeshInvalidatedMessage();
        }

        /// <summary>
        /// Called once per frame by the game object.
        /// </summary>
        /// <param name="gameTime">The amount of time that has passed this frame.</param>
        public override void Update(GameTime gameTime)
        {
            // Toggle between PLACEMENT mode on regular gameplay.
            //
            if (InputManager.pInstance.CheckAction(InputManager.InputActions.R2))
            {
                GameObjectManager.pInstance.pCurUpdatePass = BehaviourDefinition.Passes.PLACEMENT;
            }
            else
            {
                GameObjectManager.pInstance.pCurUpdatePass = BehaviourDefinition.Passes.DEFAULT;
            }

            if (GameObjectManager.pInstance.pCurUpdatePass != BehaviourDefinition.Passes.PLACEMENT)
            {
                // No need to continue on if we are not in placement mode.
                return;
            }

            // Switch to the next item in the inventory.
            //
            if (InputManager.pInstance.CheckAction(InputManager.InputActions.L1, true))
            {
                mParentGOH.OnMessage(mSelectNextItemMsg);
            }

            // Update the texture which appears on the cursor.
            UpdateItemTexture();

            // Move the cursor based on user input.
            MoveCursor();

            // Cache the current level object for quick reference.
            GameObject lvl = MBHEngine.World.WorldManager.pInstance.pCurrentLevel;

            // The the tile which the player is currently standing on. This will be our
            // starting point.
            mGetTileAtPositionMsg.mPosition_In = mParentGOH.pPosition;
            lvl.OnMessage(mGetTileAtPositionMsg);

            // We jump by tiles in terms of offsets (rather than pixels or something), so
            // figure out how many pixels each offset should be.
            lvl.OnMessage(mGetMapInfoMsg);

            // The final offset of the cursor is the cursor offset we are tracking times the 
            // size of a single tile, so that we are always jumping one tile at a time.
            Vector2 offsetPx = new Vector2();
            offsetPx.X = mGetMapInfoMsg.mInfo_Out.mTileWidth * mCursorOffset.X;
            offsetPx.Y = mGetMapInfoMsg.mInfo_Out.mTileHeight * mCursorOffset.Y;

            // Reposition the cursor.
            mCursor.pPosition = mGetTileAtPositionMsg.mTile_Out.mCollisionRect.pCenterPoint + offsetPx;

            // Place and remove tiles.
            if (InputManager.pInstance.CheckAction(InputManager.InputActions.A, true))
            {
                // Remove the Object at the top of the Inventory.
                mGetCurrentObjectMsg.mObj_Out = null;
                mParentGOH.OnMessage(mGetCurrentObjectMsg);

                // We only want to attempt to place an object if we have something in the inventory to place.
                if (null != mGetCurrentObjectMsg.mObj_Out)
                {
                    mOnPlaceObjectMsg.Reset();

                    // Tell the object to place itself.
                    mOnPlaceObjectMsg.mPosition_In = mCursor.pPosition;
                    mGetCurrentObjectMsg.mObj_Out.OnMessage(mOnPlaceObjectMsg);

                    // It is possible that someone caught this event as decide this object should not
                    // be placed.
                    if (mOnPlaceObjectMsg.mObjectPlaced_Out)
                    {
                        // The object has been placed in the world, so the GameObjectManager needs to
                        // start managing it.
                        GameObjectManager.pInstance.Add(mGetCurrentObjectMsg.mObj_Out);

                        lvl.OnMessage(mOnNavMeshInvalidatedMsg);
                    }
                    else
                    {
                        mAddObjectMsg.Reset();

                        // The object was removed from the inventory with the GerCurrentObjectMessage, so
                        // since it wasn't placed, it needs to be added back.
                        mAddObjectMsg.mObj_In = mGetCurrentObjectMsg.mObj_Out;
                        mAddObjectMsg.mDoSelectObj_In = true;
                        mParentGOH.OnMessage(mAddObjectMsg);
                    }
                }
            }
            if (InputManager.pInstance.CheckAction(InputManager.InputActions.B, true))
            {
                mSetTileTypeAtPositionMsg.mType_In = Level.Tile.TileTypes.Empty;
                mSetTileTypeAtPositionMsg.mPosition_In = mCursor.pPosition;
                MBHEngine.World.WorldManager.pInstance.pCurrentLevel.OnMessage(mSetTileTypeAtPositionMsg, mParentGOH);

                // Clear any objects that might still be stored from the previous frame.
                mCollidedObjects.Clear();

                // Check if any objects are colliding with the mouse.
                GameObjectManager.pInstance.GetGameObjectsInRange(mCursor, ref mCollidedObjects, mRemoveClassifications);

                for (Int32 i = 0; i < mCollidedObjects.Count; i++)
                {
                    GameObjectManager.pInstance.Remove(mCollidedObjects[i]);
                }
            }
        }

        /// <summary>
        /// Updates the texture that gets rendering on the cursor.
        /// </summary>
        private void UpdateItemTexture()
        {
            mGetTexture2DMsg.Reset();
            mPeekCurrentObjectMsg.Reset();

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
            if (null != mTextureItem && GameObjectManager.pInstance.pCurUpdatePass == BehaviourDefinition.Passes.PLACEMENT)
            {
                batch.Draw(mTextureItem,
                mCursor.pPosition + mItemOffset,
                mItemSourceRect,
                mItemColor);
            }
        }

        /// <summary>
        /// Moves the cursor offset based on user input.
        /// </summary>
        private void MoveCursor()
        {
            // TODO: Remember to reenable the FrameSkip debug stuff in Game1.cs once this is all
            //       up and running!
            //

            if (InputManager.pInstance.CheckAction(InputManager.InputActions.DP_LEFT, true) ||
                InputManager.pInstance.CheckAction(InputManager.InputActions.LA_LEFT, true))
            {
                mCursorOffset.X -= 1;
            }
            else if (InputManager.pInstance.CheckAction(InputManager.InputActions.DP_RIGHT, true) ||
                InputManager.pInstance.CheckAction(InputManager.InputActions.LA_RIGHT, true))
            {
                mCursorOffset.X += 1;
            }
            else if (InputManager.pInstance.CheckAction(InputManager.InputActions.DP_UP, true) ||
                InputManager.pInstance.CheckAction(InputManager.InputActions.LA_UP, true))
            {
                mCursorOffset.Y -= 1;
            }
            else if (InputManager.pInstance.CheckAction(InputManager.InputActions.DP_DOWN, true) ||
                InputManager.pInstance.CheckAction(InputManager.InputActions.LA_DOWN, true))
            {
                mCursorOffset.Y += 1;
            }
        }
    }
}
