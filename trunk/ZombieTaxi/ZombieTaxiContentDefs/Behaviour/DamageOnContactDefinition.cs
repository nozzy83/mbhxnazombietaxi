using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MBHEngineContentDefs;

namespace ZombieTaxiContentDefs
{
    public class DamageOnContactDefinition : BehaviourDefinition
    {
        /// <summary>
        /// The amount of damage that is caused by this explosion.
        /// </summary>
        public Single mDamageCaused;

        /// <summary>
        /// A list of the types of objects that this does damage to when exploding.
        /// </summary>
        public List<GameObjectDefinition.Classifications> mDamageAppliedTo;
    }
}
