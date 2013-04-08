using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MBHEngineContentDefs;

namespace ZombieTaxiContentDefs
{
    public class StrandedPopupDefinition : BehaviourDefinition
    {
        /// <summary>
        /// Enums for all the possible Button selections.
        /// </summary>
        public enum ButtonTypes
        {
            None = 0,       // No Button was selected.
            HpUp,           // Health Level Up.
            GunUp,          // Gun Level Up.
            MakeScout,      // Morph into Scout.
            ScoutSearch,    // Send the Scout out to search for other Stranded.
            MilitantPatrol, // Send the Militant out to patrol the surrounding area.
            MilitantFollow, // Tell the Militant to follow the player.
        }

        /// <summary>
        /// Defines a single Button used by the pop up.
        /// </summary>
        public class ButtonDefinition
        {
            /// <summary>
            /// Script file used to define the icon's GameObject.
            /// </summary>
            public String mIconFileName;

            /// <summary>
            /// The text that will appear when this button is highlighted.
            /// </summary>
            public String mHintText;

            /// <summary>
            /// The type of button this is.
            /// </summary>
            public ButtonTypes mButtonType;
        }

        /// <summary>
        /// A list of all the buttons on the popup. The order they are defined is the order they
        /// will appear on the popup.
        /// </summary>
        public List<ButtonDefinition> mButtons;
    }
}
