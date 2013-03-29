using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MBHEngineContentDefs;

namespace ZombieTaxiContentDefs
{
    public class ObjectPlacementDefinition : BehaviourDefinition
    {
        /// <summary>
        /// The limits of how far the cursor can be offset from the player. These are absolute values, and should
        /// never be negative.
        /// </summary>
        public Microsoft.Xna.Framework.Point mAbsOffsetRange;
    }
}
