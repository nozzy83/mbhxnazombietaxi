using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MBHEngineContentDefs
{
    public class HealNearbyDefinition : BehaviourDefinition
    {
        /// <summary>
        /// Which types of objects will be healed by this behaviour.
        /// </summary>
        public List<MBHEngineContentDefs.GameObjectDefinition.Classifications> mAppliedTo;

        /// <summary>
        /// The amount of health given per frame.
        /// </summary>
        public Single mHealRate;

        /// <summary>
        /// The distance a target must be from this object to get the healing effects.
        /// </summary>
        public Single mHealRange;
    }
}
