using System;
using MBHEngine.StateMachine;
using MBHEngine.Behaviour;
using MBHEngine.GameObject;
using Microsoft.Xna.Framework;

namespace ZombieTaxi.States.Militant
{
    /// <summary>
    /// State where the Game Object stands in place waiting for the target to get far enough away
    /// to trigger a transition back to the follow state.
    /// </summary>
    class FSMStatePatrol : FSMState
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public FSMStatePatrol()
        {
        }

        /// <summary>
        /// Called once when the state starts.
        /// </summary>
        public override void OnBegin()
        {
        }

        /// <summary>
        /// Call repeatedly until it returns a valid new state to transition to.
        /// </summary>
        /// <returns>Identifier of a state to transition to.</returns>
        public override String OnUpdate()
        {
            return null;
        }
    }
}
