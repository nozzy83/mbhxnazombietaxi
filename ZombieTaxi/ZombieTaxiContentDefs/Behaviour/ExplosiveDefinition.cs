﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MBHEngineContentDefs;
using Microsoft.Xna.Framework.Content;

namespace ZombieTaxiContentDefs
{
    public class ExplosiveDefinition : BehaviourDefinition
    {
        /// <summary>
        /// The name of the effect game object that will be spawned when this explosive explodes.
        /// </summary>
        public String mEffectFileName;

        /// <summary>
        /// A list of possible animations to play on mEffectFileName when an explosion occurs.  
        /// It will pick one at random.
        /// </summary>
        public List<String> mAnimationsToPlay;

        /// <summary>
        /// Is this explosive triggered manually?
        /// </summary>
        public Boolean mManualExplosion;

        /// <summary>
        /// The amount of damage that is caused by this explosion.
        /// </summary>
        public Single mDamageCaused;

        /// <summary>
        /// A list of the types of objects that this does damage to when exploding.
        /// </summary>
        public List<GameObjectDefinition.Classifications> mDamageAppliedTo;

        /// <summary>
        /// The number of frames that need to pass before the object detonates (if it is not
        /// a manual detonation).
        /// </summary>
        /// 
        [ContentSerializer(Optional = true)]
        public Single mDetonationTimerDuration;
    }
}
