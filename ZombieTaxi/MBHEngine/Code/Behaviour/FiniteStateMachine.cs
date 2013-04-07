using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MBHEngine.StateMachine;
using Microsoft.Xna.Framework;

namespace MBHEngine.Behaviour
{
    /// <summary>
    /// Classic Finite State Machine with Enter and End passes book ending a main Update pass.
    /// This should be used in conjucture with objects derived from MBHEngine.StateMachine.FSMState.
    /// This class could be used as is for cases where all logic is within the statestates themselves,
    /// except that we do not have a way of defining states for it to contain for XML yet.  For now it is
    /// used by creating a derived class to share some logic and data across the lifetime of the FSM.
    /// </summary>
    public class FiniteStateMachine : Behaviour
    {
        /// <summary>
        /// Triggers the state machine to advance to a specified state.
        /// This will still go through the standard OnEnd/Begin flow.
        /// </summary>
        public class SetStateMessage : BehaviourMessage
        {
            /// <summary>
            /// The name of the state to transition to.
            /// </summary>
            public String mNextState_In;
            
            /// <summary>
            /// Call this to put a message back to its default state.
            /// </summary>
            public override void Reset()
            {
            }
        }

        /// <summary>
        /// Every state machine goes through 3 distict phases during its lifetime.
        /// </summary>
        private enum FlowStates
        {
            /// <summary>
            /// A state which only happens for 1 frame when a state is activated.
            /// It automatically triggers a transition into the next flow state, "UPDATE".
            /// </summary>
            BEGIN = 0,

            /// <summary>
            /// A state which happens repeatedly until the current state returns a valid new 
            /// state to transition to.  At which point this flow state automatically transitions
            /// to the END flow state.
            /// </summary>
            UPDATE,

            /// <summary>
            /// Another state which only happens for 1 frame, but in this case it happens after the
            /// UPDATE flow state has finished, and we are transitions to a new FSMState.
            /// </summary>
            END,

            /// <summary>
            /// The state which we want to start with.
            /// </summary>
            INITIAL_STATE = BEGIN,
        }

        /// <summary>
        /// A list of all the states this machine contains, indexed by a client specified String.  That
        /// String will be used as an identifier when transitioning between FSMStates.
        /// </summary>
        private Dictionary<String, FSMState> mStates;

        /// <summary>
        /// The state currently being run by this FiniteStateMachine.
        /// </summary>
        private FSMState mCurrentState;

        /// <summary>
        /// A state which this FinitStateMachine has been requested to transition to.  We can't just set
        /// the mCurrentState right away, as we need to going through all FlowStates first.
        /// </summary>
        private FSMState mNextState;

        /// <summary>
        /// Save the initial state so that in the event of a Reset() we can go back to the original state.
        /// Perhaps one day this will be set by client.
        /// </summary>
        private FSMState mInitialState;

        /// <summary>
        /// The current FlowStates of the the mCurrentState.
        /// </summary>
        private FlowStates mCurrentFlowState;

        /// <summary>
        /// Constructor which also handles the process of loading in the Behaviour
        /// Definition information.
        /// </summary>
        /// <param name="parentGOH">The game object that this behaviour is attached to.</param>
        /// <param name="fileName">The file defining this behaviour.</param>
        public FiniteStateMachine(GameObject.GameObject parentGOH, String fileName)
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

            mStates = new Dictionary<String, FSMState>();
        }

        /// <summary>
        /// Called at the end of the frame where this object is remove from the GameObjectManager.
        /// </summary>
        public override void OnRemove()
        {
            mCurrentState.OnEnd();
        }

        /// <summary>
        /// Called once per frame by the game object.
        /// </summary>
        /// <param name="gameTime">The amount of time that has passed this frame.</param>
        public override void Update(GameTime gameTime)
        {
            // The basic flow for every state machine:
            //  1) Run OnBegin (1 frame).
            //  2) Run OnUpdate (Indefinitly; until it returns a new state to transition to).
            //  3) Run OnEnd (1 frame).
            //
            switch (mCurrentFlowState)
            {
            case FlowStates.BEGIN:
                {
                    // Transition directly to the UPDATE pass. Do it before calling OnBegin incase
                    // OnBegin actually sets the current state through a message, in which case we
                    // don't want to go to UPDATE.
                    mCurrentFlowState = FlowStates.UPDATE;

                    mCurrentState.OnBegin();

                    break;
                }
            case FlowStates.UPDATE:
                {
                    // Update the state which will potentially return a new state in which to transition to.
                    String nextState = mCurrentState.OnUpdate();

                    AdvanceToState(nextState);

                    break;
                }
            case FlowStates.END:
                {
                    mCurrentState.OnEnd();

                    // Once the FlowStates.END has finished it is time to move onto the next FSMState.
                    mCurrentState = mNextState;

                    // On the next update pass we will be running a new FSMState, so we want to be sitting
                    // in the BEGIN flow state.
                    mCurrentFlowState = FlowStates.BEGIN;

                    break;
                }
            default:
                {
                    System.Diagnostics.Debug.Assert(false, "Unhandled FSM Flow State.");

                    break;
                }
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
            if (msg is SetStateMessage)
            {
                SetStateMessage temp = (SetStateMessage)msg;

                AdvanceToState(temp.mNextState_In);
            }

            // Pass the message down to the currently running state.
            mCurrentState.OnMessage(ref msg);
        }

        /// <summary>
        /// Called by GameObjectFactory when object is recycled.
        /// </summary>
        public override void Reset()
        {
            mCurrentState = mNextState = mInitialState;
            mCurrentFlowState = FlowStates.INITIAL_STATE;
        }

        /// <summary>
        /// Interface for adding new FSMState to this FiniteStateMachine.
        /// </summary>
        /// <param name="state">Implementation of the FSMState.</param>
        /// <param name="id">An indentifier used to manage and transition between states.</param>
        protected void AddState(FSMState state, String id)
        {
            // The state will be expecting to access the Parent GameObject a lot.
            state.pParentGOH = mParentGOH;

            // Check for duplicate entries.
            if (mStates.ContainsKey(id))
            {
                System.Diagnostics.Debug.Assert(false, "Attempting to add two FSMState objects with the same ID to a FiniteStateMachine: " + id);

                return;
            }
            else
            {
                // Add it to our list of states.
                mStates.Add(id, state);
            }

            // If this is the first state to be added, use it as the initial FSMState to be run.
            if (mCurrentState == null)
            {
                mInitialState = mCurrentState = mNextState = state;
                mCurrentFlowState = FlowStates.INITIAL_STATE;
            }
        }

        /// <summary>
        /// Attempts to advance to a new state, checking for unknown and null states.
        /// </summary>
        /// <param name="nextState">The name of the state to go to (as sent to AddState).</param>
        protected void AdvanceToState(String nextState)
        {
            // If they passed a value state name...
            if (!String.IsNullOrEmpty(nextState))
            {
                // ...and it is one that we are managing.
                if (mStates.ContainsKey(nextState))
                {
                    // Request a new state and move to the END pass of this flow.
                    mNextState = mStates[nextState];
                    mCurrentFlowState = FlowStates.END;
                }
                else
                {
                    System.Diagnostics.Debug.Assert(false, "FSMState returned a new state which is not managed by this state machine: " + nextState);
                }
            }
        }

        /// <summary>
        /// Access to the currently running state.
        /// </summary>
        /// <returns>The currently running state.  Used "is" keyword to determine what state it is.</returns>
        protected FSMState GetCurrentState()
        {
            return mCurrentState;
        }
    }
}
