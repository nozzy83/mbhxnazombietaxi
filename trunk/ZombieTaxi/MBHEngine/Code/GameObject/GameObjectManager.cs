using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.Storage;
using MBHEngine.Debug;
using MBHEngineContentDefs;
//using dreambuildplay2010.Code.Utilities;
//using dreambuildplay2010.Code.Game.GameStates;

namespace MBHEngine.GameObject
{
    /// <summary>
    /// Singleton class which manages all the GameObject instances of the engine.
    /// By calling update and Render on this class, all the GameObjects it contains
    /// will be updated and rendered.
    /// </summary>
    public class GameObjectManager
    {
        /// <summary>
        /// The static instance of this class, making this a singleton.
        /// </summary>
        static private GameObjectManager mInstance = null;

        /// <summary>
        /// A list of all the GameObjects in the game.  The main purpose
        /// of this class is to manage this list.
        /// </summary>
        private List<GameObject> mGameObjects;

        /// <summary>
        /// A list of all the GameObjects that need to be added at the next possible
        /// time.  We can't just add the objects right away because we could be 
        /// interating through the list at the time.
        /// </summary>
        private List<GameObject> mGameObjectsToAdd;

        /// <summary>
        /// A list of all the GameObjects that need to be removed at the next possible
        /// time.  We can't just remove the objects right away because we could be 
        /// interating through the list at the time.
        /// </summary>
        private List<GameObject> mGameObjectsToRemove;

        /// <summary>
        /// Holds on to an instance of the ContentManager so that we can load new objects
        /// on the fly.
        ///
        /// This probably shouldn't be here.  For one, we probably want to preload most of
        /// our content.  In the cases where we don't we should probably have some sort of
        /// ContentManager...Manager.  That feels a little weird right now, so I am just 
        /// putting this here for now, and we can re-examine this concent once things are a
        /// little more flushed out.
        /// </summary>
        private ContentManager mContent;

        /// <summary>
        /// Similar to the ContentManager.  Store this here for conventience.
        /// </summary>
        private GraphicsDeviceManager mGraphics;

        /// <summary>
        /// Special case handling for the player object.  This is just to make things easier and quicker.
        /// </summary>
        private GameObject mPlayer;

        /// <summary>
        /// We make the constructor private so that no one accidentally creates
        /// an instance of the class.
        /// </summary>
        private GameObjectManager()
        {
            mGameObjects            = new List<GameObject>();
            mGameObjectsToAdd       = new List<GameObject>();
            mGameObjectsToRemove    = new List<GameObject>();
        }

        /// <summary>
        /// Needs to be called before any of the usage methods are called.
        /// </summary>
        /// <param name="content">GameObjects will load into this conent manager.</param>
        /// <param name="graphics">Used for rendering.</param>
        public void Initialize(ContentManager content, GraphicsDeviceManager graphics)
        {
            mContent = content;
            mGraphics = graphics;
        }

        /// <summary>
        /// Used to sort the game objects based on render priority.  We want the higher
        /// priority (which is a smaller number) to appear later on the list.
        /// This implementation also sorts top to bottom in screen space.  This is for 
        /// top down view points.
        /// </summary>
        /// <param name="x">Left side.</param>
        /// <param name="y">Right side.</param>
        /// <returns>0 The objects have equal render priority.</returns>
        /// <returns>-1 x has a higher render priority than y.</returns>
        /// <returns>1 y has a higher render priority than x.</returns>
        private static int CompareByRenderPriority(GameObject x, GameObject y)
        {
            // Is x allocated?
            if (x == null)
            {
                if (y == null)
                {
                    // Y also is not allocated, so they are considered equal
                    return 0;
                }
                else
                {
                    // Y is not null but x is, so that means Y is considered greater.
                    return 1;
                }
            }
            else if (y == null)
            {
                // X is not null by y is, so x is greater.
                return -1;
            }
            else
            {
                if (x.pRenderPriority == y.pRenderPriority)
                {
                    // The have the same render priority, so now we should check who is
                    // closer to the bottom of the screen and render them on top of the
                    // other.

                    if (x.pOrientation.mPosition.Y < y.pOrientation.mPosition.Y)
                    {
                        // Y is closer to the bottom.
                        return -1;
                    }
                    else if (x.pOrientation.mPosition.Y > y.pOrientation.mPosition.Y)
                    {
                        // X is closer to the bottom.
                        return 1;
                    }
                    else
                    {
                        // The are at the exact same position and have the same render priority.
                        return 0;
                    }
                }
                else if (x.pRenderPriority > y.pRenderPriority)
                {
                    // Y is greater
                    return 1;
                }
                else
                {
                    // X is greater
                    return -1;
                }
            }
        }

        /// <summary>
        /// Should be called once per frame.
        /// </summary>
        /// <param name="gameTime">The amount of time passed since last update.</param>
        public void Update(GameTime gameTime)
        {
            // Keep track of how many objects were updated this frame.
            int count = 0;

            // Some behaviours require some logic to be done prior to the standard update.  This is their chance.
            //
            for (int i = 0; i < mGameObjects.Count; i++)
            {
                if (mGameObjects[i].pDoUpdate)
                {
                    mGameObjects[i].PreUpdate(gameTime);
                }
            }

            // Update every object we are managing.
            //
            for (int i = 0; i < mGameObjects.Count; i++)
            {
                if (mGameObjects[i].pDoUpdate)
                {
                    mGameObjects[i].Update(gameTime);
                }

                count++;
            }

            // A final chance to update behaviours after all the updates have been completed.
            //
            for (int i = 0; i < mGameObjects.Count; i++)
            {
                if (mGameObjects[i].pDoUpdate)
                {
                    mGameObjects[i].PostUpdate(gameTime);
                }
            }

#if ALLOW_GARBAGE
            DebugMessageDisplay.pInstance.AddDynamicMessage("Updated " + count + " GameObjects");
#endif

            // Now loop through our list of objects which need to be removed and
            // remove them.
            //
            for (int i = 0; i < mGameObjectsToRemove.Count; i++)
            {
                // If this is a GameObject that was spawned throught a Factory then it needs to be
                // returned to that Factory.
                if (mGameObjectsToRemove[i].pFactoryInfo.pIsManaged)
                {
                    GameObjectFactory.pInstance.RecycleTemplate(mGameObjectsToRemove[i]);
                }

                // What happens if someone adds and removes an element within the same
                // update?  It would mean we are about to remove an item that hasn't
                // actually been added yet!  To get around this flaw, we will attempt to
                // remove the item from the main list and if that fails, try to remove it 
                // from the list of items about to be added.
                if (mGameObjectsToRemove[i] != null && mGameObjects.Remove(mGameObjectsToRemove[i]) == false)
                {
                    if (mGameObjectsToAdd.Remove(mGameObjectsToRemove[i]) == false)
                    {
                        //throw new Exception("Attempting to remove a game object which isn't in any of the managed lists.");
                    }
                }
            }
            mGameObjectsToRemove.Clear();

            // Loop through all the game objects that exist.  We want to insert the new game objects
            // in the order that they were added, based on render priority.  If the new object shares
            // a render priority with another object, it is inserted in front of the first same-priority
            // object it hits.
            //
            // This bit of code assumes that the mGameObjectsToAdd and mGameObjects are both sorted 
            // based on render priority.
            //
            int curIndex = 0;
            for (int j = 0; j < mGameObjectsToAdd.Count; j++)
            {
                bool alreadyAdded = false;

                // Loop through all the currently exisiting game objects.  We continue moving
                // forward even after inserting a new object.  This can be done because we assume
                // the mGameObjectsToAdd is also sorted by render priority, which means the next
                // element must be placed somewhere after the current one.
                for (; curIndex < mGameObjects.Count; curIndex++)
                {
                    if (mGameObjectsToAdd[j].pRenderPriority < mGameObjects[curIndex].pRenderPriority)
                    {
                        // We have found the proper place for this element.
                        mGameObjects.Insert(curIndex, mGameObjectsToAdd[j]);

                        // We don't want to test against the elemt we just added.  Since it was
                        // inserted at i, the object we just compared against is actually at i + 1
                        // now.  Let's start the next comparison there.
                        curIndex++;

                        alreadyAdded = true;

                        break;
                    }
                }

                if (!alreadyAdded)
                {
                    // If we make it to this point all the remaining elements have a greater or equal
                    // render priority to the highest priority item already existing.
                    // This will also take care of the cases where this is the first item being added.
                    mGameObjects.Add(mGameObjectsToAdd[j]);

                    // We don't want to test against the element we just added.  Since it was
                    // inserted at i, the object we just compared against is actually at i + 1
                    // now.  Let's start the next comparison there.
                    curIndex++;
                }
            }
            if (mGameObjectsToAdd.Count != 0)
            {
                mGameObjectsToAdd.Clear();
            }
        }

        /// <summary>
        /// Should be called once per render update.
        /// </summary>
        /// <param name="batch">Where sprites will be rendered to.</param>
        public void Render(SpriteBatch batch)
        {
            mGameObjects.Sort(CompareByRenderPriority);

            for (int i = 0; i < mGameObjects.Count; i++)
            {
                if (mGameObjects[i].pDoRender == true)
                {
                    mGameObjects[i].Render(batch);

                    DebugShapeDisplay.pInstance.AddTransform(mGameObjects[i].pOrientation.mPosition);
                }
            }
        }

        /// <summary>
        /// Interface for adding a game object to the manger.
        /// </summary>
        /// <param name="go">The object to add.</param>
        public void Add(GameObject go)
        {
            // We need this list to be sorted based on render priority.
            // See the Update method in this class for more details why.
            //

            for (int i = 0; i < mGameObjectsToAdd.Count; i++)
            {
                if (go.pRenderPriority < mGameObjectsToAdd[i].pRenderPriority)
                {
                    mGameObjectsToAdd.Insert(i, go);

                    return;
                }
            }

            // If we make it to this point than the object has a higher priority than any
            // of the existing elements (or the same as the highest).
            // This will also take care of the case where the list is empty.
            mGameObjectsToAdd.Add(go);
        }

        /// <summary>
        /// Interface for implicitly removing a game object from the manager.
        /// </summary>
        /// <param name="go">The object to remove.</param>
        public void Remove(GameObject go)
        {
            // This is to terminate the Countdown timer object
            if (go != null)
            {
                //go.Shutdown();
                mGameObjectsToRemove.Add(go);
            }
        }

        /// <summary>
        /// Populates a list of all the objects within a certain range of a position.
        /// </summary>
        /// <param name="centerPoint">The position to test from.</param>
        /// <param name="radius">The radius from that position that the other objects must be within.</param>
        /// <param name="refObjects">A preallocated list of objects.  This is to avoid GC.</param>
        /// <param name="classifications">The types of objects to check for.</param>
        public void GetGameObjectsInRange(Vector2 centerPoint, Single radius, ref List<GameObject> refObjects, List<GameObject.Classification> classifications)
        {
            Single radSqr = radius * radius;
            for (int i = 0; i < mGameObjects.Count; i++)
            {
                for (Int32 j = 0; j < classifications.Count; j++)
                {
                    if (mGameObjects[i].pClassifications.Contains(classifications[j]))
                    {
                        if (Vector2.DistanceSquared(centerPoint, mGameObjects[i].pOrientation.mPosition) < radSqr)
                        {
                            refObjects.Add(mGameObjects[i]);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Accessor to the pContent property.
        /// </summary>
        public ContentManager pContentManager
        {
            get
            {
                return mContent;
            }
        }

        /// <summary>
        /// Accessor to the pGraphicsDevicemanager property.
        /// </summary>
        public GraphicsDevice pGraphicsDevice
        {
            get
            {
                return mGraphics.GraphicsDevice;
            }
        }

        /// <summary>
        /// The graphics object itself.
        /// </summary>
        public GraphicsDeviceManager pGraphics
        {
            get
            {
                return mGraphics;
            }
        }

        /// <summary>
        /// The player object.  Not guarenteed to be defined during start up.
        /// </summary>
        public GameObject pPlayer
        {
            get
            {
                return mPlayer;
            }

            set
            {
                if (mPlayer != null)
                {
                    throw new Exception("Setting Player more than once.  If this is intentional this exception should be removed.");
                }

                mPlayer = value;
            }
        }

        /// <summary>
        /// Accessor to the pInstance property.
        /// </summary>
        public static GameObjectManager pInstance
        {
            get
            {
                // If this is the first time this is called, instantiate our
                // static instance of the class.
                if (mInstance == null)
                {
                    mInstance = new GameObjectManager();
                }

                // Either way, at this point we should have an instantiated version
                // if the class.
                return mInstance;
            }
        }
    }
}
