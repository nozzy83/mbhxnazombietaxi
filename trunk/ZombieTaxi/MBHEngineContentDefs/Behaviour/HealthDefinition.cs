using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Content;

namespace MBHEngineContentDefs
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

        /// <summary>
        /// True if this object should be deleted when it reaches Zero Health.
        /// </summary>
        [ContentSerializer(Optional = true)]
        public Boolean mRemoveOnDeath;
    }
}
