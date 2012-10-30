using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MBHEngineContentDefs
{
    public class MagneticDefinition : BehaviourDefinition
    {
        /// <summary>
        /// The furthest distance from the target at which the Magnectic movement still occurs.
        /// </summary>
        public Single mMaxDist;

        /// <summary>
        /// The slowest speed at which the object will magnetically move towards the target.
        /// </summary>
        public Single mMinSpeed;

        /// <summary>
        /// The fastest speed at which the object will magnetically move towards the target.
        /// </summary>
        public Single mMaxSpeed;
    }
}
