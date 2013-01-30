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
        /// A single level of upgrades for a stat.
        /// </summary>
        public class LevelStat
        {
            /// <summary>
            /// How many frames need to pass before this upgrade is researched.
            /// </summary>
            public Int32 mFramesToComplete;

            /// <summary>
            /// Meaning changes based on derived class.
            /// </summary>
            public Int32 mIntValue;
        }

        /// <summary>
        /// All the LevelStats for this stat boost type.
        /// </summary>
        public LevelStat[] mLevels;
    }
}
