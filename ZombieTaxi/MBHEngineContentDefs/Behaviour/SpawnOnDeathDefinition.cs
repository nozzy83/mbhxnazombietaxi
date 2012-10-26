using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Content;

namespace MBHEngineContentDefs
{
    public class SpawnOnDeathDefinition : BehaviourDefinition
    {        
        /// <summary>
        /// The name of Template which will be spawned.
        /// </summary>
        public String mTemplateFileName;

        /// <summary>
        /// Position the spawned object at this attachment point.
        /// </summary>
        [ContentSerializer(Optional = true)]
        public String mAttachmentPoint;
    }
}
