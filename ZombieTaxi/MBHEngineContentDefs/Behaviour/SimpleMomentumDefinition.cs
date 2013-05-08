using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Content;

namespace MBHEngineContentDefs
{
    public class SimpleMomentumDefinition : BehaviourDefinition
    {
        /// <summary>
        /// The rate at which the object will speed up.
        /// </summary>
        [ContentSerializer(Optional = true)]
        public Single mAcceleration = -1.0f;
    }
}
