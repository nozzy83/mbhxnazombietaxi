using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Box2D.XNA;
using Microsoft.Xna.Framework;
using MBHEngine.Render;
using MBHEngine.GameObject;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace MBHEngine.Math
{
    /// <summary>
    /// Interface for the physic simulation used by the engine.
    /// </summary>
    public class PhysicsManager
    {
        /// <summary>
        /// Static instance of our class, allowing use to make this a singleton.
        /// </summary>
        public static PhysicsManager mInstance = null;

        /// <summary>
        /// This world is where all the simulation is done.
        /// </summary>
        private Box2D.XNA.World mPhysicsWorld;

        /// <summary>
        /// Used for the debug rendering of the Box2D simulation.
        /// </summary>
        private DebugDrawBox2D mBox2DDebugDraw;

        /// <summary>
        /// The amount to multiply a position in Physical World Space in order to transform it
        /// into Screen Space.
        /// </summary>
        private Single mPhysicalWorldScale;

        /// <summary>
        /// Constructor.
        /// </summary>
        public PhysicsManager()
        {
            mPhysicsWorld = new Box2D.XNA.World(new Vector2(0, 10.0f), true);
        }

        /// <summary>
        /// Initializes the singleton class.  This must be called before the class is used.
        /// </summary>
        /// <param name="graphics">Used for rendering the debug drawing.</param>
        /// <param name="sprite">Used for rendering the debug drawing.</param>
        public void Initialize(GraphicsDeviceManager graphics, SpriteBatch sprite)
        {
            mBox2DDebugDraw = new DebugDrawBox2D(graphics.GraphicsDevice, sprite);

            uint flags = 0;
            flags += (uint)DebugDrawFlags.Shape;
            flags += (uint)DebugDrawFlags.Joint;
            flags += (uint)DebugDrawFlags.AABB;
            flags += (uint)DebugDrawFlags.Pair;
            flags += (uint)DebugDrawFlags.CenterOfMass;
            mBox2DDebugDraw.Flags = (DebugDrawFlags)flags;

            mPhysicsWorld.DebugDraw = mBox2DDebugDraw;

            mPhysicalWorldScale = 64.0f;
        }

        /// <summary>
        /// Updates the physics simulation for a single step.
        /// </summary>
        /// <param name="gameTime">The amount of time that has passed since the last update.</param>
        public void Update(GameTime gameTime)
        {
            mPhysicsWorld.Step((float)gameTime.ElapsedGameTime.TotalSeconds, 10, 3);
        }

        /// <summary>
        /// Special handling needed for our implimentation of the box 2d renderer.  This needs to be 
        /// called after the regular call to the box2d debug draw.
        /// </summary>
        public void FinishDebugDraw()
        {
            mBox2DDebugDraw.FinishDrawShapes();
        }

        /// <summary>
        /// Transforms a screen position into a position that the physics system can work with.
        /// </summary>
        /// <param name="v">A position in Screen Space.</param>
        /// <returns>The position in Physical Space.</returns>
        public Vector3 ScreenToPhysicalWorld(Vector3 v)
        {
            return v / mPhysicalWorldScale;
        }

        /// <summary>
        /// Transforms a screen position into a position that the physics system can work with.
        /// </summary>
        /// <param name="v">A position in Screen Space.</param>
        /// <returns>The position in Physical Space.</returns>
        public Vector2 ScreenToPhysicalWorld(Vector2 v)
        {
            return v / mPhysicalWorldScale;
        }

        /// <summary>
        /// Transforms a screen position into a position that the physics system can work with.
        /// </summary>
        /// <param name="p1">A position in Screen Space.</param>
        /// <param name="p2">A position in Screen Space.</param>
        /// <returns>The position in Physical Space.</returns>
        public Vector2 ScreenToPhysicalWorld(Single p1, Single p2)
        {
            return new Vector2((p1 / mPhysicalWorldScale),(p2 / mPhysicalWorldScale));
        }

        /// <summary>
        /// Transforms a screen position into a position that the physics system can work with.
        /// </summary>
        /// <param name="p">A position in Screen Space.</param>
        /// <returns>The position in Physical Space.</returns>
        public Single ScreenToPhysicalWorld(Single p)
        {
            return p / mPhysicalWorldScale;
        }

        /// <summary>
        /// Transforms a position in the physics systems world space, into the relative position
        /// on screeen.
        /// </summary>
        /// <param name="v">A position in Physical Space.</param>
        /// <returns>The position in Screen Space.</returns>
        public Vector3 PhysicalWorldToScreen(Vector3 v)
        {
            return v * mPhysicalWorldScale;
        }

        /// <summary>
        /// Transforms a position in the physics systems world space, into the relative position
        /// on screeen.
        /// </summary>
        /// <param name="v">A position in Physical Space.</param>
        /// <returns>The position in Screen Space.</returns>
        public Vector2 PhysicalWorldToScreen(Vector2 v)
        {
            return v * mPhysicalWorldScale;
        }

        /// <summary>
        /// Transforms a position in the physics systems world space, into the relative position
        /// on screeen.
        /// </summary>
        /// <param name="p">A position in Physical Space.</param>
        /// <returns>The position in Screen Space.</returns>
        public Single PhysicalWorldToScreen(Single p)
        {
            return p * mPhysicalWorldScale;
        }

        /// <summary>
        /// Access to the single instance of the class.
        /// </summary>
        public static PhysicsManager pInstance
        {
            get
            {
                if (mInstance == null)
                {
                    mInstance = new PhysicsManager();
                }

                return mInstance;
            }
        }

        /// <summary>
        /// Allow others access to the box 2d world object to reduce the number of wrappers needed.
        /// </summary>
        public Box2D.XNA.World pWorld
        {
            get
            {
                return mPhysicsWorld;
            }
        }
    }
}
