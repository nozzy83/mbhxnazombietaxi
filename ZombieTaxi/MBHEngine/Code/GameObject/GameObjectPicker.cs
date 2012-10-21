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
        private Math.Rectangle rect = new Math.Rectangle(4.0f, 8.0f);

        /// <summary>
        /// Preallocated list used for getting a list of all the objects we are pointing
        /// at in a givent frame.
        /// </summary>
        List<GameObject> refObjects = new List<GameObject>();

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
            rect.pCenterPoint = proj;

            // Clear any objects that might still be stored from the previous frame.
            refObjects.Clear();

            // Check if any objects are colliding with the mouse.
            GameObjectManager.pInstance.GetGameObjectsInRange(rect, ref refObjects);

            // Temp storing which object the mouse is currently over top of (if any).
            GameObject mousedObject = null;

            // Did the mouse actually collide with any objects?
            if (refObjects.Count > 0)
            {
                // We just use index 0 for now. Eventually we might need to determine some sort
                // of sorting order, perhaps based on rect size, or render order.
                mousedObject = refObjects[0];

                // If while hovering over an object, the user presses the mouse button, that object
                // not becomes the new "selected" object.
                if (ms.LeftButton == ButtonState.Pressed)
                {
                    mSelectedGameObject = mousedObject;
                }            
            }

#if ALLOW_GARBAGE
            // Display some information about the selected object and the object we are hovering over.
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
            if (null != mSelectedGameObject)
            {
                DebugMessageDisplay.pInstance.AddDynamicMessage("Picked GO (selected): " + mSelectedGameObject.pID);
                DebugShapeDisplay.pInstance.AddAABB(mSelectedGameObject.pCollisionRect, Color.Red);
            }
            else
            {
                DebugMessageDisplay.pInstance.AddDynamicMessage("Picked GO (selected): --");
            }
#endif // ALLOW_GARBAGE    
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
