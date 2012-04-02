using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MBHEngineContentDefs;
using Microsoft.Xna.Framework;

namespace ZombieTaxiContentDefs
{
    public class RandomEnemyGeneratorDefinition : BehaviourDefinition
    {
        /// <summary>
        /// The number of enemies to be spawned.
        /// </summary>
        public Single mNumEnemies;

        /// <summary>
        /// A rectangle defining the area that the enemies can be spawned in.
        /// </summary>
        public Rectangle mConstraints;
    }
}
