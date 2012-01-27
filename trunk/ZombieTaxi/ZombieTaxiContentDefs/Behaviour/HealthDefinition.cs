using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MBHEngineContentDefs;

namespace ZombieTaxiContentDefs
{
    public class HealthDefinition : BehaviourDefinition
    {
        /// <summary>
        /// The current amount of health.
        /// </summary>
        public Single mCurrentHealth;

        /// <summary>
        /// The maxium amount of health this game object can store.
        /// </summary>
        public Single mMaxHealth;
    }
}
