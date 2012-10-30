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
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.Storage;
using MBHEngineContentDefs;
using MBHEngine;
using MBHEngine.Behaviour;
using System.IO;
using MBHEngine.Debug;

namespace MBHEngine.GameObject
{
    /// <summary>
    /// The base interface for all objects managed by this engine.
    /// </summary>
    public class GameObject
    {
        /// <summary>
        /// Wrapper class used for storing the objects direction, used to control movement.
        /// </summary>
        public class Direction
        {
            /// <summary>
            /// The forward direction of the object.
            /// </summary>
            public Vector2 mForward = new Vector2(0, 0);

            /// <summary>
            /// The speed at which the object is moving in this direction.
            /// </summary>
            public Single mSpeed = 0.0f;
        };


        /// <summary>
        /// A static list of behaviour creators which each instance will attempt to use to create behaviours
        /// before moving on to the engine behaviours.
        /// </summary>
        static protected List<Behaviour.BehaviourCreator> mBehaviourCreators = new List<BehaviourCreator>();

        /// <summary>
        /// To make debuging easier, we give each game object a unique id.  That id is just this static which
        /// gets incremented every time one is created.
        /// </summary>
        private static Int32 mUniqueIDCounter = 0;

        /// <summary>
        /// Determines the order at which GameObjects should be rendered.
        /// The higher the number the later it will be rendered.
        /// </summary>
        protected Int32 mRenderPriority;

        /// <summary>
        /// Whether on not this object should be updated.
        /// </summary>
        protected Boolean mDoUpdate;

        /// <summary>
        /// Whether on not this object should be rendered.
        /// </summary>
        protected Boolean mDoRender;

        /// <summary>
        /// The position of the object in space.
        /// </summary>
        protected Vector2 mPosition = new Vector2(0, 0);

        /// <summary>
        /// The position the object was, at the end of the previous frame.
        /// </summary>
        protected Vector2 mPrevPosition = new Vector2(0, 0);

        /// <summary>
        /// The rotation of the object around the Z axis.
        /// </summary>
        protected Single mRotation = 0.0f;

        /// <summary>
        /// The scale of the object in both the x and y axis independently.
        /// </summary>
        protected Vector2 mScale = new Vector2(1, 1);

        /// <summary>
        /// Every Game Object tracks its own direction for movement.
        /// </summary>
        protected Direction mDirection;

        /// <summary>
        /// True if this object will not move while being managed by the GameObjectManager.
        /// If it is a GameObjectFactory managed object, it is fine to move it in between
        /// GetTemplate and adding it to the GameObjectManager.
        /// </summary>
        protected Boolean mIsStatic;

        /// <summary>
        /// A rectangle surrounding this Game Object.  Used for all collision
		/// detection routines.
        /// </summary>
        private MBHEngine.Math.Rectangle mCollisionRectangle;

        /// <summary>
        /// The area on screen where this object is visible. Used for culling checks.
        /// </summary>
        private MBHEngine.Math.Rectangle mRenderRectangle;

        /// <summary>
        /// A list of classifications which this game objects fits into.  This can be used to filter
        /// larger lists of game objects into smaller ones.
        /// </summary>
        protected List<GameObjectDefinition.Classifications> mClassifications;

        /// <summary>
        /// The type of blending to be used when rendering this object.
        /// </summary>
        protected GameObjectDefinition.BlendMode mBlendMode;

        /// <summary>
        /// Information about how (and if) this object was spawned from a Factory.  This information is needed
        /// for recycling the object when it dies.
        /// </summary>
        protected GameObjectFactory.FactoryInfo mFactoryInfo;

        /// <summary>
        /// A collection of all the behaviors associated with this GameObject.
        /// </summary>
        protected List<Behaviour.Behaviour> mBehaviours = new List<MBHEngine.Behaviour.Behaviour>();

        /// <summary>
        /// Each Game Object is given a unique id to make debuging easier.
        /// </summary>
        protected Int32 mID;

        /// <summary>
        /// The offset from 0,0 that the collision volume should be centered on.  If not suppied, it will use
        /// mMotionRoot.
        /// </summary>
        private Vector2 mCollisionRoot;

        /// <summary>
        /// The offset from 0,0 that this sprite should be rendered at.
        /// </summary>
        private Vector2 mMotionRoot;

        /// <summary>
        /// The name of the template file defining this object.
        /// </summary>
        private String mTemplateFileName;

        /// <summary>
        /// Default Constructor.  Does nothing but needed to be overwritten so that
        /// we can create an OPTIONAL version which takes a parameter.
        /// </summary>
        public GameObject() { LoadContent(null); }

        /// <summary>
        /// Constructor which also handles the process of loading in the Game Object 
        /// Definition information.
        /// </summary>
        public GameObject(String fileName)
        {
            LoadContent(fileName);
        }

        /// <summary>
        /// Call this to initialize an GameObject with data supplied in a file.
        /// </summary>
        /// <param name="fileName">The file to load from.</param>
        public virtual void LoadContent(String fileName)
        {
            // Give this object a unique id and increment the counter so that the next
            // object gets a unique id as well.
            mID = mUniqueIDCounter++;

            mDirection = new Direction();
            mFactoryInfo = new GameObjectFactory.FactoryInfo();
            mClassifications = new List<GameObjectDefinition.Classifications>();
            mCollisionRectangle = new Math.Rectangle();
            mRenderRectangle = new Math.Rectangle();

            mTemplateFileName = fileName;

            if (null != fileName)
            {
                GameObjectDefinition def = GameObjectManager.pInstance.pContentManager.Load<GameObjectDefinition>(fileName);

                mRenderPriority = def.mRenderPriority;
                mDoUpdate = def.mDoUpdate;
                mDoRender = def.mDoRender;
                mPosition = def.mPosition;
                mRotation = def.mRotation;
                mScale = def.mScale;
                mIsStatic = def.mIsStatic;
                mCollisionRectangle = new Math.Rectangle(def.mCollisionBoxDimensions);
                mCollisionRectangle.pCenterPoint = mPosition;
                // Being lazy for now. Just assume that a scaler of collision box is big enough to always show character.
                mRenderRectangle = new Math.Rectangle(def.mCollisionBoxDimensions * 4f); 
                mRenderRectangle.pCenterPoint = mPosition;
                mMotionRoot = def.mMotionRoot;
                if (def.mCollisionRoot == null)
                {
                    mCollisionRoot = Vector2.Zero;
                }
                else
                {
                    mCollisionRoot = def.mCollisionRoot;
                }

                for (Int32 i = 0; def.mClassifications != null && i < def.mClassifications.Count; i++)
                {
                    mClassifications.Add(def.mClassifications[i]);
                }

                mBlendMode = def.mBlendMode;

                for (Int32 i = 0; i < def.mBehaviourFileNames.Count; i++)
                {
                    String goRootPath = System.IO.Path.GetDirectoryName(fileName);
                    Behaviour.Behaviour temp = CreateBehaviourByName(def.mBehaviourClassNames[i], goRootPath + "\\Behaviours\\" + def.mBehaviourFileNames[i]);
                    mBehaviours.Add(temp);
                }
            }
            else
            {
                mRenderPriority = 50;
                mDoUpdate = true;
                mDoRender = true;
                mBlendMode = GameObjectDefinition.BlendMode.STANDARD;
            }
        }

        /// <summary>
        /// Called once per frame before the update function. Is called for ALL gameobjects, prior 
        /// to calling Update.
        /// </summary>
        /// <param name="gameTime">The amount of time that has passed this frame.</param>
        public virtual void PreUpdate(GameTime gameTime)
        {
            BehaviourDefinition.Passes curPass = GameObjectManager.pInstance.pCurUpdatePass;

            for (int i = 0; i < mBehaviours.Count; i++)
            {
                if (0 == mBehaviours[i].pUpdatePasses.Count ||
                    mBehaviours[i].pUpdatePasses.Contains(curPass))
                {
                    mBehaviours[i].PreUpdate(gameTime);
                }
            }
        }

        /// <summary>
        /// Called once per frame by the game object manager.
        /// </summary>
        /// <param name="gameTime">The amount of time that has passed this frame.</param>
        public virtual void Update(GameTime gameTime)
        {
            BehaviourDefinition.Passes curPass = GameObjectManager.pInstance.pCurUpdatePass;

            for (int i = 0; i < mBehaviours.Count; i++)
            {
                if (0 == mBehaviours[i].pUpdatePasses.Count ||
                    mBehaviours[i].pUpdatePasses.Contains(curPass))
                {
                    mBehaviours[i].Update(gameTime);
                }
            }
        }

        /// <summary>
        /// Called once per frame after the Update function. Is called after all objects have
        /// caled Update.
        /// </summary>
        /// <param name="gameTime">The amount of time that has passed this frame.</param>
        public virtual void PostUpdate(GameTime gameTime)
        {
            BehaviourDefinition.Passes curPass = GameObjectManager.pInstance.pCurUpdatePass;

            for (int i = 0; i < mBehaviours.Count; i++)
            {
                if (0 == mBehaviours[i].pUpdatePasses.Count ||
                    mBehaviours[i].pUpdatePasses.Contains(curPass))
                {
                    mBehaviours[i].PostUpdate(gameTime);
                }
            }

            // With all behaviours done updating, it should be safe to
            // now update and draw the collision volume for this object.
            UpdateBounds();

            mPrevPosition = mPosition;
        }

        /// <summary>
        /// Called once render cycle by the game object manager.
        /// </summary>
        /// <param name="batch">The sprite batch to render to.</param>
        public virtual void Render(SpriteBatch batch)
        {
            BehaviourDefinition.Passes curPass = GameObjectManager.pInstance.pCurUpdatePass;

            for (int i = 0; i < mBehaviours.Count; i++)
            {
                if (null == mBehaviours[i].pRenderPassExclusions ||
                    !(mBehaviours[i].pRenderPassExclusions.Contains(curPass)))
                {
                    mBehaviours[i].Render(batch);
                }
            }
        }

        /// <summary>
        /// Sends a message to all behaviours attached to this object.  As soon as a behaviour 
        /// handles the message, it will return.
        /// </summary>
        /// <param name="msg">The message to send.</param>
        /// <returns>The message passed in, likely modified by handling behaviours.</returns>
        public virtual BehaviourMessage OnMessage(BehaviourMessage msg)
        {
            return OnMessage(msg, null);
        }

        /// <summary>
        /// Sends a message to all behaviours attached to this object.  As soon as a behaviour 
        /// handles the message, it will return.
        /// </summary>
        /// <param name="msg">The message to send.</param>
        /// <param name="sender">The GameObject sending the message.</param>
        /// <returns>The message passed in, likely modified by handling behaviours.</returns>
        public virtual BehaviourMessage OnMessage(BehaviourMessage msg, GameObject sender)
        {
            if (sender == null)
            {
                msg.pSender = this;
            }
            else
            {
                msg.pSender = sender;
            }

            for (int i = 0; i < mBehaviours.Count; i++)
            {
                mBehaviours[i].OnMessage(ref msg);
            }

            return msg;
        }

        /// <summary>
        /// Resets all behaviours on this GO to thier initial state.
        /// </summary>
        public virtual void ResetBehaviours()
        {
            for (int i = 0; i < mBehaviours.Count; i++)
            {
                mBehaviours[i].Reset();
            }
        }

        /// <summary>
        /// Attaches an already exisiting behaviour to this game object.  This is handy for manually
        /// creating GameObjects, instead of through the usually xml definitions.
        /// </summary>
        /// <param name="b">The behaviour to attach.</param>
        public virtual void AttachBehaviour(Behaviour.Behaviour b)
        {
            // Add the behaviour to the list of behaviours.
            mBehaviours.Add(b);
        }

        /// <summary>
        /// Helper function for creating behaviours based on strings of matching names.
        /// </summary>
        /// <param name="behaviourType">The name of the behaviour class we are creating.</param>
        /// <param name="fileName">The name of the file containing the behaviour definition.</param>
        /// <returns>The newly created behaviour.</returns>
        protected virtual Behaviour.Behaviour CreateBehaviourByName(String behaviourType, String fileName)
        {
            // Allow the client a chance to create non-engine behaviours.
            for (Int16 i = 0; i < mBehaviourCreators.Count; i++)
            {
                Behaviour.Behaviour b = mBehaviourCreators[i].CreateBehaviourByName(this, behaviourType, fileName);

                // Once the behaviour has been created there is no reason to continue.
                if (b != null)
                {
                    return b;
                }
            }

            switch (behaviourType)
            {
                case "MBHEngine.Behaviour.Behaviour":
                    {
                        return new MBHEngine.Behaviour.Behaviour(this, fileName);
                    }
                case "MBHEngine.Behaviour.SpriteRender":
                    {
                        return new MBHEngine.Behaviour.SpriteRender(this, fileName);
                    }
                case "MBHEngine.Behaviour.SimulatedPhysics":
                    {
                        return new MBHEngine.Behaviour.SimulatedPhysics(this, fileName);
                    }
                case "MBHEngine.Behaviour.TileMapRender":
                    {
                        return new MBHEngine.Behaviour.TileMapRender(this, fileName);
                    }
                case "MBHEngine.Behaviour.FrameRateDisplay":
                    {
                        return new MBHEngine.Behaviour.FrameRateDisplay(this, fileName);
                    }
                case "MBHEngine.Behaviour.Level":
                    {
                        return new MBHEngine.Behaviour.Level(this, fileName);
                    }
                case "MBHEngine.Behaviour.TileCollision":
                    {
                        return new MBHEngine.Behaviour.TileCollision(this, fileName);
                    }
                case "MBHEngine.Behaviour.Timer":
                    {
                        return new MBHEngine.Behaviour.Timer(this, fileName);
                    }
                case "MBHEngine.Behaviour.PathFind":
                    {
                        return new MBHEngine.Behaviour.PathFind(this, fileName);
                    }
                case "MBHEngine.Behaviour.RemoveTileOnDeath":
                    {
                        return new MBHEngine.Behaviour.RemoveTileOnDeath(this, fileName);
                    }
                case "MBHEngine.Behaviour.Health":
                    {
                        return new MBHEngine.Behaviour.Health(this, fileName);
                    }
                case "MBHEngine.Behaviour.SpawnOnDeath":
                    {
                        return new MBHEngine.Behaviour.SpawnOnDeath(this, fileName);
                    }
                case "MBHEngine.Behaviour.SimpleMomentum":
                    {
                        return new MBHEngine.Behaviour.SimpleMomentum(this, fileName);
                    }
                case "MBHEngine.Behaviour.Magnetic":
                    {
                        return new MBHEngine.Behaviour.Magnetic(this, fileName);
                    }
                default:
                    {
#if ALLOW_GARBAGE
                        System.Diagnostics.Debug.Assert(false, "Attempting to create unknown behaviour type, " + behaviourType + " linked to file " + fileName + "!");
#endif // ALLOW_GARBAGE
                        return null;
                    }
            }
        }

#if ALLOW_GARBAGE
        /// <summary>
        /// Returns a bunch of information about the GameObject which can be dumped to
        /// a debug display for debugging at runtime.
        /// </summary>
        /// <returns>A formatted string of debug information.</returns>
        public virtual String [] GetDebugInfo()
        {
            String [] info = new String[4];

            Int32 i = 0;

            info[i] = "Template: " + mTemplateFileName;
            i++;

            info[i] = "ID: " + mID;
            i++;

            info[i] = "Managed: " + pFactoryInfo.pIsManaged;
            i++;

            info[i] = "Static: " + mIsStatic;
            i++;

            return info;
        }
#endif // ALLOW_GARBAGE

        /// <summary>
        /// The game object factory will call this for each Game Object it creates so that it can
        /// later be return to the factory.
        /// </summary>
        /// <param name="templateName"></param>
        public void SetAsFactoryManaged(String templateName)
        {
            mFactoryInfo.pTemplateName = templateName;
        }

        /// <summary>
        /// Updates the different bounding volumes based on the current position.
        /// </summary>
        private void UpdateBounds()
        {
            mCollisionRectangle.pCenterPoint = pPosition + mCollisionRoot;
            mRenderRectangle.pCenterPoint = pPosition + mCollisionRoot;
        }

#if DEBUG
        private void StaticObjectMovementSafetyCheck()
        {
            if (mIsStatic)
            {
                List<GameObject> list = GameObjectManager.pInstance.GetObjectsInCell(mPosition);

                if (null != list)
                {
                    System.Diagnostics.Debug.Assert(!list.Contains(this), "Attempting to move a static object that is currently managed by the GameObjectManager. Remove it before attempting to move.");
                }
            }
        }
#endif // DEBUG

        /// <summary>
        /// The factory information for this game object.
        /// </summary>
        public GameObjectFactory.FactoryInfo pFactoryInfo
        {
            get
            {
                return mFactoryInfo;
            }
        }

        /// <summary>
        /// Use this function to register client implementation of behaviour creators.
        /// </summary>
        /// <param name="b">An implementation of the BehaviourCreator Interface</param>
        public static void AddBehaviourCreator(BehaviourCreator b)
        {
            mBehaviourCreators.Add(b);
        }

        /// <summary>
        /// Determines the order at which GameObjects should be rendered.
        /// The higher the number the later it will be rendered.
        /// </summary>
        public Int32 pRenderPriority
        {
            get { return mRenderPriority; }
            set { mRenderPriority = value; }
        }

        /// <summary>
        /// Whether on not this object should be updated.
        /// </summary>
        public Boolean pDoUpdate
        {
            get { return mDoUpdate; }
            set { mDoUpdate = value; }
        }

        /// <summary>
        /// Whether on not this object should be rendered.
        /// </summary>
        public Boolean pDoRender
        {
            get { return mDoRender; }
            set { mDoRender = value; }
        }

        /// <summary>
        /// The position of the object right now.
        /// </summary>
        public Vector2 pPosition
        {
            get { return mPosition; }
            set 
            { 
#if DEBUG
                StaticObjectMovementSafetyCheck();
#endif // DEBUG
                mPosition = value;
                UpdateBounds();
            }
        }

        /// <summary>
        /// The position of the object at the end of the previous frame.
        /// </summary>
        public Vector2 pPrevPos
        {
            get { return mPrevPosition; }
        }

        /// <summary>
        /// The position's X value.
        /// </summary>
        public Single pPosX
        {
            get { return mPosition.X; }
            set 
            {
#if DEBUG
                StaticObjectMovementSafetyCheck();
#endif // DEBUG

                mPosition.X = value;

                UpdateBounds();
            }
        }

        /// <summary>
        /// The position's Y value.
        /// </summary>
        public Single pPosY
        {
            get { return mPosition.Y; }
            set 
            {
#if DEBUG
                StaticObjectMovementSafetyCheck();
#endif // DEBUG

                mPosition.Y = value;

                UpdateBounds();
            }
        }

        /// <summary>
        /// The rotation of the object around the Z axis.
        /// </summary>
        public Single pRotation
        {
            get { return mRotation; }
            set { mRotation = value; }
        }

        /// <summary>
        /// The scale of the object.
        /// </summary>
        public Vector2 pScale
        {
            get { return mScale; }
            set { mScale = value; }
        }

        /// <summary>
        /// The forward vector that this object is pointing in, and the speed at which it is moving.
        /// </summary>
        public Direction pDirection
        {
            get { return mDirection; }
            set 
            { 
                mDirection.mForward = value.mForward;
                mDirection.mSpeed = value.mSpeed; 
            }
        }

        /// <summary>
        /// A rectangle representing the collision volume around this object.
        /// Can be a volume of 0 if there is none.
        /// </summary>
        public Math.Rectangle pCollisionRect
        {
            get
            {
                return mCollisionRectangle;
            }
        }

        /// <summary>
        /// A rectangle defining the area in the world where this object is visible.
        /// </summary>
        public Math.Rectangle pRenderRect
        {
            get
            {
                return mRenderRectangle;
            }
        }

        /// <summary>
        /// Access to the collision root offset.
        /// </summary>
        public Vector2 pCollisionRoot
        {
            get
            {
                return mCollisionRoot;
            }
        }

        /// <summary>
        /// Access to the motion root offset value.
        /// </summary>
        public Vector2 pMotionRoot
        {
            get
            {
                return mMotionRoot;
            }
        }

        /// <summary>
        /// A list of the classifications this game object has been declared to fall under.
        /// </summary>
        public List<GameObjectDefinition.Classifications> pClassifications
        {
            get
            {
                return mClassifications;
            }
        }

        /// <summary>
        /// The type of blending to be used when rendering this object.
        /// </summary>
        public GameObjectDefinition.BlendMode pBlendMode
        {
            get
            {
                return mBlendMode;
            }
        }

        /// <summary>
        /// A unique id assigned to this game object. Will possibly change each time 
        /// the game is run. Meant more for debugging than anything.
        /// </summary>
        public Int32 pID
        {
            get
            {
                return mID;
            }
        }

        /// <summary>
        /// A list of all behaviours attached to this GameObject.
        /// </summary>
        public List<Behaviour.Behaviour> pBehaviours
        {
            get
            {
                return mBehaviours;
            }
        }

        /// <summary>
        /// Will this object never move once it is added to the GameObjectManager.
        /// </summary>
        public Boolean pIsStatic
        {
            get
            {
                return mIsStatic;
            }
            set
            {
                mIsStatic = value;
            }
        }
    }
}
