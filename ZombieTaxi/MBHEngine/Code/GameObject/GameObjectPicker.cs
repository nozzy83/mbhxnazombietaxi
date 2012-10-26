using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MBHEngine.Render;
using MBHEngine.Debug;
using MBHEngineContentDefs;

namespace MBHEngine.GameObject
{
    public class GameObjectPicker
    {
        /// <summary>
        /// Static instance of itself, making this a singleton.
        /// </summary>
        static private GameObjectPicker mInstance;

        /// <summary>
        /// The game object last selected with a mouse click.
        /// </summary>
        private GameObject mSelectedGameObject = null;

        /// <summary>
        /// A rectangle roughly defining the area the mouse takes up. This should probably 
        /// be scaled by the camera's current scale.
        /// </summary>
        private Math.Rectangle mMouseRect = new Math.Rectangle(4.0f, 8.0f);

        /// <summary>
        /// Preallocated list used for getting a list of all the objects we are pointing
        /// at in a givent frame.
        /// </summary>
        private List<GameObject> mCollidedObjects = new List<GameObject>();

        /// <summary>
        /// We need to track the previous mouse state so that we don't get repeating
        /// press events.
        /// </summary>
        /// <remarks>This should be moved into InputManager.</remarks>
        private MouseState mPreviousMouseState = Mouse.GetState();

        /// <summary>
        /// Must be called once every update to check which objects are being picked.
        /// </summary>
        /// <param name="gameTime"></param>
        public void Update(GameTime gameTime)
        {
            // Get the current state of the mouse.
            MouseState ms = Mouse.GetState();

            // Project that mouse position into 2D world space so that it can be tested for collision.
            Vector2 proj = CameraManager.pInstance.ProjectMouseToWorldSpace(new Vector2(ms.X, ms.Y));

            // Reposition the collision rect of the mouse pointer to the position of the actual 
            // mouse in world space.
            mMouseRect.pCenterPoint = proj;

            // Clear any objects that might still be stored from the previous frame.
            mCollidedObjects.Clear();

            // Check if any objects are colliding with the mouse.
            GameObjectManager.pInstance.GetGameObjectsInRange(mMouseRect, ref mCollidedObjects);

            // Temp storing which object the mouse is currently over top of (if any).
            GameObject mousedObject = null;

            // Only count mouse clicks that happen after the button was previously released.
            Boolean clickChanged = (mPreviousMouseState.LeftButton != ms.LeftButton);

            const String dbgLayer = "GameObjectPicker";

            // Did the mouse actually collide with any objects?
            if (mCollidedObjects.Count > 0)
            {
                // We just use index 0 for now. Eventually we might need to determine some sort
                // of sorting order, perhaps based on rect size, or render order.
                mousedObject = mCollidedObjects[0];

                // If while hovering over an object, the user presses the mouse button, that object
                // not becomes the new "selected" object.
                if (ms.LeftButton == ButtonState.Pressed && clickChanged)
                {
                    if (mousedObject != mSelectedGameObject)
                    {
                        DebugMessageDisplay.pInstance.pCurrentTag = dbgLayer;

                        mSelectedGameObject = mousedObject;
                    }
                    else
                    {
                        DebugMessageDisplay.pInstance.pCurrentTag = null;

                        // If they click the same object which is already selected, consider that an
                        // attempt to unselect the GameObject.
                        mSelectedGameObject = null;
                    }
                }
            }
            else
            {
                // If the user clicks while not over any GameObject, unselect the currently selected.
                if (ms.LeftButton == ButtonState.Pressed && clickChanged)
                {
                    DebugMessageDisplay.pInstance.pCurrentTag = null;

                    mSelectedGameObject = null;
                }
            }

#if ALLOW_GARBAGE
            // Display some information about the object we are hovering over.
            //
            if (null != mousedObject)
            {
                DebugMessageDisplay.pInstance.AddDynamicMessage("Picked GO (over): " + mousedObject.pID);
                DebugShapeDisplay.pInstance.AddAABB(mousedObject.pCollisionRect, Color.Orange);
            }
            else
            {
                DebugMessageDisplay.pInstance.AddDynamicMessage("Picked GO (over): --");
            }

            // The Behaviour and GameObject classes expose a bunch of debug information through the GetDebugInfo
            // functions. If there is an object currently selected, we want to get that info about the selected
            // object and print it on screen for real-time debugging.
            if (null != mSelectedGameObject)
            {
                // So the user knows what is going on, highlight the object.
                DebugShapeDisplay.pInstance.AddAABB(mSelectedGameObject.pCollisionRect, Color.Red);

                // The the GameObject debug info. Every GameObject has this.
                String [] goInfo = mSelectedGameObject.GetDebugInfo();

                // Print the class name and the info.
                DebugMessageDisplay.pInstance.AddDynamicMessage(mSelectedGameObject.GetType().ToString(), dbgLayer);

                for (Int32 i = 0; i < goInfo.Length; i++)
                {
                    DebugMessageDisplay.pInstance.AddDynamicMessage(" - " + goInfo[i], dbgLayer);
                }

                // Loop through every Behaviour attached to this GameObject and call the corisponding 
                // GetDebugInfo functions.
                for (Int32 i = 0; i < mSelectedGameObject.pBehaviours.Count; i++)
                {
                    Behaviour.Behaviour b = mSelectedGameObject.pBehaviours[i];

                    // Show the behaviour even if the GetDebugInfo isn't implmented for it since we 
                    // may just want to know which behaviours it has.5
                    DebugMessageDisplay.pInstance.AddDynamicMessage(b.GetType().ToString(), dbgLayer);

                    String [] dbgInfo = b.GetDebugInfo();

                    // Not every Behaviour overrides the GetDebugInfo function. In those cases the 
                    // default implementation will return null.
                    if (null != dbgInfo)
                    {
                        for (Int32 j = 0; j < dbgInfo.Length; j++)
                        {
                            DebugMessageDisplay.pInstance.AddDynamicMessage(" - " + dbgInfo[j], dbgLayer);
                        }
                    }
                }
            }
#endif // ALLOW_GARBAGE

            // Update the previous state with the current state.
            mPreviousMouseState = Mouse.GetState();
        }

        /// <summary>
        /// Accessor to the pInstance property.
        /// </summary>
        public static GameObjectPicker pInstance
        {
            get
            {
                // If this is the first time this is called, instantiate our
                // static instance of the class.
                if (mInstance == null)
                {
                    mInstance = new GameObjectPicker();
                }

                // Either way, at this point we should have an instantiated version
                // if the class.
                return mInstance;
            }
        }
    }
}
