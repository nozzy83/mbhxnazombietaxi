using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MBHEngineContentDefs;

namespace ZombieTaxiContentDefs
{
    public class PointAndShootDefinition : BehaviourDefinition
    {
        /// <summary>
        /// The number of frames between each shot.
        /// </summary>
        public Single mFiringDelay;

        /// <summary>
        /// The distance at which this Behaviour will start trying to target an object.
        /// </summary>
        public Single mFiringRange;

        /// <summary>
        /// The speed at which bullets will move when fired.
        /// </summary>
        public Single mBulletSpeed;

        /// <summary>
        /// The name of the GameObject script to use for the Gun.
        /// </summary>
        public String mGunScriptName;

        /// <summary>
        /// The name of the GameObject script to use for the Bullet fired by the gun.
        /// </summary>
        public String mBulletScriptName;

        /// <summary>
        /// The type of objects this behaviour should try to shoot.
        /// </summary>
        public List<MBHEngineContentDefs.GameObjectDefinition.Classifications> mTargetClassifications;
    }
}
