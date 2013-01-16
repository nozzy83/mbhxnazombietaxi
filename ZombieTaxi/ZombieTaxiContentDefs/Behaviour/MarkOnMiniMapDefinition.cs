using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MBHEngineContentDefs;

namespace ZombieTaxiContentDefs
{
    public class MarkOnMiniMapDefinition : BehaviourDefinition
    {
        /// <summary>
        /// The name of a MarkerProfile XML script defining what Marker should be used to 
        /// represent this GameObject this behaviour is attached to.
        /// </summary>
        public String mMarkerProfile;
    }
}
