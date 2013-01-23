using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MBHEngine.GameObject;
using Microsoft.Xna.Framework;
using ZombieTaxiContentDefs;
using MBHEngine.Behaviour;

namespace ZombieTaxi.Behaviours
{
    /// <summary>
    /// Once a Stranded is brought to the safe house they can be extracted at any point after that.
    /// To do that, the Player needs to place an ExtractionPoint where the Stranded will need to walk
    /// to in order to be rescued.
    /// </summary>
    class ExtractionPoint : MBHEngine.Behaviour.Behaviour
    {
        /// <summary>
        /// ExtractionPoint objects don't activate until being told to.
        /// </summary>
        /// <todo>
        /// This could probably use the SetBehaviourIsEnabled functionality instead.
        /// </todo>
        public class SetExtractionPointActivateMessage : BehaviourMessage
        {
            /// <summary>
            /// Call this to put a message back to its default state.
            /// </summary>
            public override void Reset() { }
        }

        /// <summary>
        /// A message that gets broadcase to tell interested parties that an ExtractionPoint has been
        /// activated.
        /// </summary>
        public class OnExtractionPointActivatedMessage : BehaviourMessage
        {
            /// <summary>
            /// Call this to put a message back to its default state.
            /// </summary>
            public override void Reset() { }
        }

        /// <summary>
        /// Preallocate messages to avoid GC.
        /// </summary>
        private OnExtractionPointActivatedMessage mOnExtractionPointActivatedMsg;

        /// <summary>
        /// Constructor which also handles the process of loading in the Behaviour
        /// Definition information.
        /// </summary>
        /// <param name="parentGOH">The game object that this behaviour is attached to.</param>
        /// <param name="fileName">The file defining this behaviour.</param>
        public ExtractionPoint(GameObject parentGOH, String fileName)
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

            ExtractionPointDefinition def = GameObjectManager.pInstance.pContentManager.Load<ExtractionPointDefinition>(fileName);

            mOnExtractionPointActivatedMsg = new OnExtractionPointActivatedMessage();
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
            if (msg is SetExtractionPointActivateMessage)
            {
                // Tell the world that we are activating.
                GameObjectManager.pInstance.BroadcastMessage(mOnExtractionPointActivatedMsg, mParentGOH);
            }
        }
    }
}
