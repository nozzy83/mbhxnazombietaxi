using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;

namespace MBHEngineContentDefs
{
    public class TimerDefinition : BehaviourDefinition
    {
        [ContentSerializer(Optional = true)]
        public Int32 mDays = 0;

        [ContentSerializer(Optional = true)]
        public Int32 mHours = 0;

        [ContentSerializer(Optional = true)]
        public Int32 mMinutes = 0;

        [ContentSerializer(Optional = true)]
        public Int32 mSeconds = 0;

        [ContentSerializer(Optional = true)]
        public Int32 mMilliseconds = 0;
    }
}
