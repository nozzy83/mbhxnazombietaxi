using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace ZombieTaxiContentDefs
{
    public class DamageWobbleDefinition : MBHEngineContentDefs.BehaviourDefinition
    {
        /// <summary>
        /// Once the time since this Game Object last took damage reaches this value, 
        /// the color is set back to white, and the behaviour waits for the next
        /// damage message to come in.
        /// </summary>
        public Single mFramesToReset;
    }
}
