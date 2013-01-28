using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MBHEngineContentDefs;

namespace ZombieTaxiContentDefs.StatBoost
{
    public class StatBoostResearchDefinition : BehaviourDefinition
    {
        /// <summary>
        /// How long does it take for this research to complete?
        /// </summary>
        public Int32 mFramesToComplete;
    }
}
