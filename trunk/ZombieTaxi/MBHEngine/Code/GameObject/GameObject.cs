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

namespace MBHEngine.GameObject
{
    /// <summary>
    /// The base interface for all objects managed by this engine.
    /// </summary>
    public class GameObject
    {
        /// <summary>
        /// Wrapper class for all the orientation data: position, rotation and scale.
        /// </summary>
        public class Orientation
        {
            /// <summary>
            /// The position of the object in space.
            /// </summary>
            public Vector2 mPosition = new Vector2(0, 0);

            /// <summary>
            /// The rotation of the object around the Z axis.
            /// </summary>
            public Single mRotation = 0.0f;

            /// <summary>
            /// The scale of the object in both the x and y axis independently.
            /// </summary>
            public Vector2 mScale = new Vector2(1, 1);
        };

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
        /// Every Game Object tracks its own transforms.
        /// </summary>
        protected Orientation mOrientation;

        /// <summary>
        /// Every Game Object tracks its own direction for movement.
        /// </summary>
        protected Direction mDirection;

        /// <summary>
        /// A collection of all the behaviors associated with this GameObject.
        /// </summary>
        protected List<Behaviour.Behaviour> mBehaviours = new List<MBHEngine.Behaviour.Behaviour>();

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
            mDirection = new Direction();
            mOrientation = new Orientation();

            if (null != fileName)
            {
                GameObjectDefinition def = GameObjectManager.pInstance.pContentManager.Load<GameObjectDefinition>(fileName);

                mRenderPriority = def.mRenderPriority;
                mDoUpdate = def.mDoUpdate;
                mDoRender = def.mDoRender;
                mOrientation.mPosition = def.mPosition;
                mOrientation.mRotation = def.mRotation;
                mOrientation.mScale = def.mScale;

                for (Int32 i = 0; i < def.mBehaviourFileNames.Count; i++)
                {
                    Behaviour.Behaviour temp = CreateBehaviourByName(def.mBehaviourClassNames[i], def.mBehaviourFileNames[i]);
                    mBehaviours.Add(temp);
                }
            }
            else
            {
                mRenderPriority = 50;
                mDoUpdate = true;
                mDoRender = true;
            }
        }

        /// <summary>
        /// Called once per frame by the game object manager.
        /// </summary>
        /// <param name="gameTime">The amount of time that has passed this frame.</param>
        public virtual void Update(GameTime gameTime)
        {
            for (int i = 0; i < mBehaviours.Count; i++)
            {
                mBehaviours[i].Update(gameTime);

                mOrientation.mPosition += mDirection.mForward * mDirection.mSpeed;
            }
        }

        /// <summary>
        /// Called once render cycle by the game object manager.
        /// </summary>
        /// <param name="batch">The sprite batch to render to.</param>
        public virtual void Render(SpriteBatch batch)
        {
            for (int i = 0; i < mBehaviours.Count; i++)
            {
                mBehaviours[i].Render(batch);
            }
        }

        /// <summary>
        /// Sends a message to all behaviours attached to this object.  As soon as a behaviour 
        /// handles the message, it will return.
        /// </summary>
        /// <param name="msg">The message to send.</param>
        /// <returns>The resulting message, or null if it was not handled at all.</returns>
        public virtual BehaviourMessage OnMessage(BehaviourMessage msg)
        {
            for (int i = 0; i < mBehaviours.Count; i++)
            {
                BehaviourMessage temp = mBehaviours[i].OnMessage(msg);
                if (temp != null)
                {
                    return temp;
                }
            }

            return null;
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
            switch (behaviourType)
            {
                case "MBHEngine.Code.Behaviour.Behaviour":
                    {
                        return new MBHEngine.Behaviour.Behaviour(this, fileName);
                    }
                case "MBHEngine.Code.Behaviour.SpriteRender":
                    {
                        return new MBHEngine.Behaviour.SpriteRender(this, fileName);
                    }
                case "MBHEngine.Code.Behaviour.SimulatedPhysics":
                    {
                        return new MBHEngine.Behaviour.SimulatedPhysics(this, fileName);
                    }
                case "MBHEngine.Code.Behaviour.TileMapRender":
                    {
                        return new MBHEngine.Behaviour.TileMapRender(this, fileName);
                    }
                case "MBHEngine.Code.Behaviour.FrameRateDisplay":
                    {
                        return new MBHEngine.Behaviour.FrameRateDisplay(this, fileName);
                    }
                default:
                    {
                        throw new Exception("Attempting to create unknown behaviour type, " + behaviourType + " linked to file " + fileName + "!");
                    }
            }
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

        public Orientation pOrientation
        {
            get { return mOrientation; }
            set { mOrientation = value; }
        }

        public Direction pDirection
        {
            get { return mDirection; }
            set { mDirection = value; }
        }
    }
}
