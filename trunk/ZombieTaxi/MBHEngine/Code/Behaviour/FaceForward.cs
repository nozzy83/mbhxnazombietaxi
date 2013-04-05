using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MBHEngine.GameObject;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MBHEngine.Behaviour
{
    /// <summary>
    /// Simple behvaiour to force a sprite to face the direction it is moving.
    /// Also can optionally be forced to face a specific target when needed.
    /// </summary>
    public class FaceForward : MBHEngine.Behaviour.Behaviour
    {
        /// <summary>
        /// Forces a sprite to face the direction it is currently moving.
        /// </summary>
        public class SetLookTargetMessage : BehaviourMessage
        {
            /// <summary>
            /// The object to look at regardless of move direction.
            /// Set to null to return to standard behaviour.
            /// </summary>
            public MBHEngine.GameObject.GameObject mTarget_In;

            /// <summary>
            /// Put message back into a default state.
            /// </summary>
            public override void Reset()
            {
                mTarget_In = null;
            }
        }

        /// <summary>
        /// An optional GameObject to face regardless of movement direction.
        /// </summary>
        private MBHEngine.GameObject.GameObject mTarget;

        /// <summary>
        /// Preallocate messages to avoid GC.
        /// </summary>
        private SpriteRender.SetSpriteEffectsMessage mSetSpriteFxMsg;

        /// <summary>
        /// Constructor which also handles the process of loading in the Behaviour
        /// Definition information.
        /// </summary>
        /// <param name="parentGOH">The game object that this behaviour is attached to.</param>
        /// <param name="fileName">The file defining this behaviour.</param>
        public FaceForward(GameObject.GameObject parentGOH, String fileName)
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

            //ExampleDefinition def = GameObjectManager.pInstance.pContentManager.Load<ExampleDefinition>(fileName);

            mSetSpriteFxMsg = new SpriteRender.SetSpriteEffectsMessage();
        }

        /// <summary>
        /// Called once per frame after the Update function. Is called after all objects have
        /// caled Update.
        /// </summary>
        /// <param name="gameTime">The amount of time that has passed this frame.</param>
        public override void PostUpdate(GameTime gameTime)
        {
            base.PostUpdate(gameTime);

            Single dir = mParentGOH.pDirection.mForward.X;

            if (null != mTarget)
            {
                dir = mTarget.pPosition.X - mParentGOH.pPosition.X;
            }

            if (dir < 0)
            {
                mSetSpriteFxMsg.mSpriteEffects_In = SpriteEffects.FlipHorizontally;
                mParentGOH.OnMessage(mSetSpriteFxMsg);
            }
            else if (dir > 0)
            {
                mSetSpriteFxMsg.mSpriteEffects_In = SpriteEffects.None;
                mParentGOH.OnMessage(mSetSpriteFxMsg);
            }
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
            if (msg is SetLookTargetMessage)
            {
                SetLookTargetMessage temp = (SetLookTargetMessage)msg;

                mTarget = temp.mTarget_In;
            }
            else if (msg is Health.OnZeroHealthMessage)
            {
                mParentGOH.SetBehaviourEnabled<FaceForward>(false);
            }
        }
    }
}
