﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MBHEngine.Behaviour;
using Microsoft.Xna.Framework;
using MBHEngine.GameObject;
using MBHEngine.StateMachine;
using ZombieTaxiContentDefs;
using Microsoft.Xna.Framework.Graphics;
using MBHEngineContentDefs;
using MBHEngine.Debug;
using MBHEngine.World;
using MBHEngine.Math;
using ZombieTaxi.Behaviours.HUD;
using ZombieTaxi.States.Scout;

namespace ZombieTaxi.Behaviours
{
    /// <summary>
    /// The Scout type of stranded.
    /// </summary>
    class FSMScout : MBHEngine.Behaviour.FiniteStateMachine
    {
        /// <summary>
        /// Constructor which also handles the process of loading in the Behaviour
        /// Definition information.
        /// </summary>
        /// <param name="parentGOH">The game object that this behaviour is attached to.</param>
        /// <param name="fileName">The file defining this behaviour.</param>
        public FSMScout(GameObject parentGOH, String fileName)
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

            AddState(new States.Common.FSMStateWaitAtStandingPosition("GameObjects\\Interface\\ScoutPopup\\ScoutPopup"), "WaitAtStandingPosition");
            AddState(new FSMStateBeginSearch(), "BeginSearch");
            AddState(new FSMStateWaitAtTarget(), "WaitAtTarget");
            AddState(new States.Common.FSMStateDead(), "Dead");
        }

        /// <summary>
        /// Called once per frame by the game object.
        /// </summary>
        /// <param name="gameTime">The amount of time that has passed this frame.</param>
        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
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
            base.OnMessage(ref msg);

            if (msg is Health.OnZeroHealthMessage)
            {
                AdvanceToState("Dead");
            }
        }
    }
}
