using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MBHEngine.Behaviour;

namespace MBHEngine.StateMachine
{
    /// <summary>
    /// The 
    /// </summary>
    public abstract class FSMState
    {
        /// <summary>
        /// FSMState will likely need to access the GameObject of the FiniteStateMachine running it,
        /// so we just store it for easy access.  It is made private and exposed through a property
        /// to allow for error checking when it is not yet set.
        /// </summary>
        private GameObject.GameObject mParentGOH;

        /// <summary>
        /// Called once when the state starts.  This is a chance to do things that should only happen once
        /// during a particular state.
        /// </summary>
        public virtual void OnBegin() { }

        /// <summary>
        /// Called repeatedly until it returns a valid new state to transition to.
        /// </summary>
        /// <returns>Identifier of a state to transition to.  This is the same name passed into AddState 
        /// in the owning FiniteStateMachine.</returns>
        public virtual String OnUpdate() { return null; }

        /// <summary>
        /// Called once when leaving this state.  Called the frame after the Update which returned
        /// a valid state to transition to.  This is a chance to do any clean up needed.
        /// </summary>
        public virtual void OnEnd() { }

        /// <summary>
        /// The main interface for communicating between behaviours.  Using polymorphism, we
        /// define a bunch of different messages deriving from BehaviourMessage.  Each behaviour
        /// can then check for particular upcasted messahe types, and either grab some data 
        /// from it (set message) or store some data in it (get message).
        /// </summary>
        /// <param name="msg">The message being communicated to the behaviour.</param>
        public virtual void OnMessage(ref BehaviourMessage msg) { }
        
        /// <summary>
        /// The Parent GameObject of the FiniteStateMachine which is running this state.
        /// </summary>
        public GameObject.GameObject pParentGOH
        {
            get
            {
                System.Diagnostics.Debug.Assert((mParentGOH != null), "Attempting to Get parent GOH before it has been set.");

                return mParentGOH;
            }
            set
            {
                mParentGOH = value;
            }
        }
    }
}
