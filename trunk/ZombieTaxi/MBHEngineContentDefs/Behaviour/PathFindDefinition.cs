using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Content;

namespace MBHEngineContentDefs
{
    public class PathFindDefinition : BehaviourDefinition
    {
        /// <summary>
        /// How many frames is this path finder allowed to run before it is considered a failure?
        /// </summary>
        /// 
        [ContentSerializer(Optional = true)]
        public Int32 mSearchPassLimit = 5;
    }
}
