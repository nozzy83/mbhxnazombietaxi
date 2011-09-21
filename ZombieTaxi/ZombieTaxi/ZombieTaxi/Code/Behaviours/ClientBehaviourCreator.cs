﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MBHEngine.Behaviour;
using MBHEngine.GameObject;

namespace ZombieTaxi.Behaviours
{
    /// <summary>
    /// This class will be used to allow us to create client side Behaviours.
    /// </summary>
    public class ClientBehaviourCreator : BehaviourCreator
    {
        /// <summary>
        /// Helper function for creating behaviours based on strings of matching names.
        /// </summary>
        /// <param name="go">The game object that this behaviour is being attached to.</param>
        /// <param name="behaviourType">The name of the behaviour class we are creating.</param>
        /// <param name="fileName">The name of the file containing the behaviour definition.</param>
        /// <returns>The newly created behaviour.</returns>
        Behaviour BehaviourCreator.CreateBehaviourByName(GameObject go, String behaviourType, String fileName)
        {
            switch (behaviourType)
            {
                case "ZombieTaxi.Code.Behaviour.TwinStick":
                    {
                        return new TwinStick(go, fileName);
                    }
                case "ZombieTaxi.Code.Behaviour.Kamikaze":
                    {
                        return new Kamikaze(go, fileName);
                    }
                default:
                    {
                        return null;
                    }
            }
        }
    }
}