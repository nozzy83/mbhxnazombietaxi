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
            public Vector2 mPosition;

            /// <summary>
            /// Set to true of the object was successfully placed. If set to false, the 
            /// object will be returned to the Player's Inventory.
            /// </summary>
            public Boolean mOutObjectPlaced;

            /// <summary>
            /// Return the Message to a default state.
            /// </summary>
            public void Reset()
            {
                mPosition = Vector2.Zero;

                mOutObjectPlaced = false;
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
        /// Preallocated messages to avoid garbage collection.
        /// </summary>
        private Level.GetTileAtPositionMessage mGetTileAtPositionMsg;
        private Level.GetMapInfoMessage mGetMapInfoMsg;
        private Level.SetTileTypeAtPositionMessage mSetTileTypeAtPositionMsg;
        private Inventory.GetCurrentObjectMessage mGetCurrentObjectMsg;
        private ObjectPlacement.OnPlaceObjectMessage mOnPlaceObjectMsg;
        private Inventory.AddObjectMessage mAddObjectMsg;

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

            mRemoveClassifications = new List<MBHEngineContentDefs.GameObjectDefinition.Classifications>(1);
            mRemoveClassifications.Add(MBHEngineContentDefs.GameObjectDefinition.Classifications.WALL);

            mGetTileAtPositionMsg = new Level.GetTileAtPositionMessage();
            mGetMapInfoMsg = new Level.GetMapInfoMessage();
            mSetTileTypeAtPositionMsg = new Level.SetTileTypeAtPositionMessage();
            mGetCurrentObjectMsg = new Inventory.GetCurrentObjectMessage();
            mOnPlaceObjectMsg = new OnPlaceObjectMessage();
            mAddObjectMsg = new Inventory.AddObjectMessage();
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

            // Move the cursor based on user input.
            MoveCursor();

            // Cache the current level object for quick reference.
            GameObject lvl = MBHEngine.World.WorldManager.pInstance.pCurrentLevel;

            // The the tile which the player is currently standing on. This will be our
            // starting point.
            mGetTileAtPositionMsg.mPosition = mParentGOH.pPosition;
            lvl.OnMessage(mGetTileAtPositionMsg);

            // We jump by tiles in terms of offsets (rather than pixels or something), so
            // figure out how many pixels each offset should be.
            lvl.OnMessage(mGetMapInfoMsg);

            // The final offset of the cursor is the cursor offset we are tracking times the 
            // size of a single tile, so that we are always jumping one tile at a time.
            Vector2 offsetPx = new Vector2();
            offsetPx.X = mGetMapInfoMsg.mInfo.mTileWidth * mCursorOffset.X;
            offsetPx.Y = mGetMapInfoMsg.mInfo.mTileHeight * mCursorOffset.Y;

            // Reposition the cursor.
            mCursor.pPosition = mGetTileAtPositionMsg.mTile.mCollisionRect.pCenterPoint + offsetPx;

            // Place and remove tiles.
            if (InputManager.pInstance.CheckAction(InputManager.InputActions.A, true))
            {
                // Remove the Object at the top of the Inventory.
                mGetCurrentObjectMsg.mOutObj = null;
                mParentGOH.OnMessage(mGetCurrentObjectMsg);

                // We only want to attempt to place an object if we have something in the inventory to place.
                if (null != mGetCurrentObjectMsg.mOutObj)
                {
                    mOnPlaceObjectMsg.Reset();

                    // Tell the object to place itself.
                    mOnPlaceObjectMsg.mPosition = mCursor.pPosition;
                    mGetCurrentObjectMsg.mOutObj.OnMessage(mOnPlaceObjectMsg);

                    // It is possible that someone caught this event as decide this object should not
                    // be placed.
                    if (mOnPlaceObjectMsg.mOutObjectPlaced)
                    {
                        // The object has been placed in the world, so the GameObjectManager needs to
                        // start managing it.
                        GameObjectManager.pInstance.Add(mGetCurrentObjectMsg.mOutObj);
                    }
                    else
                    {
                        mAddObjectMsg.Reset();

                        // The object was removed from the inventory with the GerCurrentObjectMessage, so
                        // since it wasn't placed, it needs to be added back.
                        mAddObjectMsg.mObj = mGetCurrentObjectMsg.mOutObj;
                        mAddObjectMsg.mDoSelectObj = true;
                        mParentGOH.OnMessage(mAddObjectMsg);
                    }
                }
            }
            if (InputManager.pInstance.CheckAction(InputManager.InputActions.B, true))
            {
                mSetTileTypeAtPositionMsg.mType = Level.Tile.TileTypes.Empty;
                mSetTileTypeAtPositionMsg.mPosition = mCursor.pPosition;
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
