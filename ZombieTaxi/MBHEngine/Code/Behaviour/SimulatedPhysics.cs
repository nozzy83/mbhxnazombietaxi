using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MBHEngineContentDefs;
using MBHEngine.GameObject;
using Box2D.XNA;
using MBHEngine.Math;
using MBHEngine.Input;

namespace MBHEngine.Behaviour
{
    /// <summary>
    /// An object with this behaviour will have its physics behaviour simulated.
    /// </summary>
    public class SimulatedPhysics : MBHEngine.Behaviour.Behaviour
    {
        /// <summary>
        /// Every simulated object has a body.
        /// </summary>
        Body mBody;

        /// <summary>
        /// Retrieves the body representing this object in the physics world.
        /// </summary>
        public class GetBodyMessage : BehaviourMessage
        {
            /// <summary>
            /// A reference to the instance of the Physics Body that this class uses.
            /// </summary>
            public Body mBody_Out;

            /// <summary>
            /// Call this to put a message back to its default state.
            /// </summary>
            public override void Reset()
            {
                mBody_Out = null;
            }
        };


        /// <summary>
        /// Constructor which also handles the process of loading in the Behaviour
        /// Definition information.
        /// </summary>
        /// <param name="parentGOH">The game object that this behaviour is attached to.</param>
        /// <param name="fileName">The file defining this behaviour.</param>
        public SimulatedPhysics(GameObject.GameObject parentGOH, String fileName)
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

            // Load the definiton for this object.
            SimulatedPhysicsDefinition def = GameObjectManager.pInstance.pContentManager.Load<SimulatedPhysicsDefinition>(fileName);

            // Create the body representing this object in the physical world.
            BodyDef bd = new BodyDef();
            bd.type = BodyType.Dynamic;
            bd.position = PhysicsManager.pInstance.ScreenToPhysicalWorld(new Vector2(640.0f, 340.0f));
            bd.fixedRotation = true;
            bd.linearDamping = 1.0f;
            mBody = PhysicsManager.pInstance.pWorld.CreateBody(bd);

            /*
            // A bunch of code to create a capsule.  Not using it because it has all the same problems as
            // a box when it comes to catching edges and such.
            var shape = new PolygonShape();
            Vector2[] v = new Vector2[8];

            Double increment = System.Math.PI * 2.0 / (v.Length - 2);
            Double theta = 0.0;

            Vector2 center = PhysicsManager.pInstance.ScreenToPhysicalWorld(new Vector2(0.0f, 8.0f));
            Single radius = PhysicsManager.pInstance.ScreenToPhysicalWorld(8.0f);
            for (Int32 i = 0; i < v.Length; i++)
            {
                v[i] = center + radius * new Vector2((float)System.Math.Cos(theta), (float)System.Math.Sin(theta));

                if (i == (v.Length / 2) - 1)
                {
                    center = PhysicsManager.pInstance.ScreenToPhysicalWorld(new Vector2(0.0f, -8.0f));
                    i++;
                    v[i] = center + radius * new Vector2((float)System.Math.Cos(theta), (float)System.Math.Sin(theta));
                }
                theta += increment;
            }

            //v = v.Reverse().ToArray();

            shape.Set(v, v.Length);

            var fd = new FixtureDef();
            fd.shape = shape;
            fd.restitution = 0.0f;
            fd.friction = 0.5f;
            fd.density = 1.0f;
            mBody.CreateFixture(fd);
            */

            var shape = new PolygonShape();
            shape.SetAsBox(PhysicsManager.pInstance.ScreenToPhysicalWorld(8.0f), PhysicsManager.pInstance.ScreenToPhysicalWorld(8.0f));

            var fd = new FixtureDef();
            fd.shape = shape;
            fd.restitution = 0.0f;
            fd.friction = 0.5f;
            fd.density = 1.0f;
            //mBody.CreateFixture(fd);

            var circle = new CircleShape();
            circle._radius = PhysicsManager.pInstance.ScreenToPhysicalWorld(8);
            circle._p = PhysicsManager.pInstance.ScreenToPhysicalWorld(new Vector2(0.0f, 8.0f));

            fd = new FixtureDef();
            fd.shape = circle;
            fd.restitution = 0.0f;
            fd.friction = 0.5f;
            fd.density = 1.5f;
            mBody.CreateFixture(fd);

            circle = new CircleShape();
            circle._radius = PhysicsManager.pInstance.ScreenToPhysicalWorld(8);
            circle._p = PhysicsManager.pInstance.ScreenToPhysicalWorld(new Vector2(0.0f, -8.0f));

            fd = new FixtureDef();
            fd.shape = circle;
            fd.restitution = 0.0f;
            fd.friction = 0.5f;
            fd.density = 1.5f;
            mBody.CreateFixture(fd);
        }

        /// <summary>
        /// Called once per frame by the game object.
        /// </summary>
        /// <param name="gameTime">The amount of time that has passed this frame.</param>
        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            // Get the current orientation from the body.
            Vector2 pos = mBody.GetPosition();
            float rot = mBody.GetAngle();

            // Replace the current orientation information with the updated physics data.
            mParentGOH.pPosition = PhysicsManager.pInstance.PhysicalWorldToScreen(pos);
            mParentGOH.pRotation = rot;
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
            if (msg is SimulatedPhysics.GetBodyMessage)
            {
                SimulatedPhysics.GetBodyMessage temp = (SimulatedPhysics.GetBodyMessage)msg;
                temp.mBody_Out = mBody;
                msg = temp;
            }
        }
    }
}
