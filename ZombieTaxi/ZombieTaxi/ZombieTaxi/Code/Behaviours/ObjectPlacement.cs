using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using MBHEngine.GameObject;
using Microsoft.Xna.Framework.Graphics;
using MBHEngine.Behaviour;
using MBHEngine.Input;

namespace ZombieTaxi.Behaviours
{
    /// <summary>
    /// Logic for controls which allows the player to place objects in the world.
    /// </summary>
    class ObjectPlacement : MBHEngine.Behaviour.Behaviour
    {
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
        /// Preallocated messages to avoid garbage collection.
        /// </summary>
        private Level.GetTileAtPositionMessage mGetTileAtPositionMsg;
        private Level.GetMapInfoMessage mGetMapInfoMsg;
        private Level.SetTileTypeAtPositionMessage mSetTileTypeAtPositionMsg;

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

            mGetTileAtPositionMsg = new Level.GetTileAtPositionMessage();
            mGetMapInfoMsg = new Level.GetMapInfoMessage();
            mSetTileTypeAtPositionMsg = new Level.SetTileTypeAtPositionMessage();
        }

        /// <summary>
        /// Called once per frame by the game object.
        /// </summary>
        /// <param name="gameTime">The amount of time that has passed this frame.</param>
        public override void Update(GameTime gameTime)
        {
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
                mSetTileTypeAtPositionMsg.mType = Level.Tile.TileTypes.Solid;
                mSetTileTypeAtPositionMsg.mPosition = mCursor.pPosition;
                MBHEngine.World.WorldManager.pInstance.pCurrentLevel.OnMessage(mSetTileTypeAtPositionMsg, mParentGOH);
            }
            if (InputManager.pInstance.CheckAction(InputManager.InputActions.B, true))
            {
                mSetTileTypeAtPositionMsg.mType = Level.Tile.TileTypes.Empty;
                mSetTileTypeAtPositionMsg.mPosition = mCursor.pPosition;
                MBHEngine.World.WorldManager.pInstance.pCurrentLevel.OnMessage(mSetTileTypeAtPositionMsg, mParentGOH);
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

            if (InputManager.pInstance.CheckAction(InputManager.InputActions.DP_LEFT, true))
            {
                mCursorOffset.X -= 1;
            }
            else if (InputManager.pInstance.CheckAction(InputManager.InputActions.DP_RIGHT, true))
            {
                mCursorOffset.X += 1;
            }
            else if (InputManager.pInstance.CheckAction(InputManager.InputActions.DP_UP, true))
            {
                mCursorOffset.Y -= 1;
            }
            else if (InputManager.pInstance.CheckAction(InputManager.InputActions.DP_DOWN, true))
            {
                mCursorOffset.Y += 1;
            }
        }
    }
}
