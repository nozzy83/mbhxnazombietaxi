using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MBHEngineContentDefs;

namespace ZombieTaxiContentDefs
{
    public class PickupDefinition : BehaviourDefinition
    {
        /// <summary>
        /// The name of the Template which should be added to the Player's inventory when 
        /// this Pickup is picked up.
        /// </summary>
        public String mInventoryTemplateFileName;
    }
}
