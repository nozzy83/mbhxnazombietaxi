using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MBHEngine.Behaviour;
using MBHEngine.GameObject;
using MBHEngine.Math;
using MBHEngine.Debug;
using ZombieTaxiContentDefs.StatBoost;

namespace ZombieTaxi.StatBoost.Behaviours
{
    /// <summary>
    /// Researchs an upgrade to a stat over time.
    /// </summary>
    class HealthStatBoostResearch : ZombieTaxi.StatBoost.Behaviours.StatBoostResearch
    {
        /// <summary>
        /// Constructor which also handles the process of loading in the Behaviour
        /// Definition information.
        /// </summary>
        /// <param name="parentGOH">The game object that this behaviour is attached to.</param>
        /// <param name="fileName">The file defining this behaviour.</param>
        public HealthStatBoostResearch(GameObject parentGOH, String fileName)
            : base(parentGOH, fileName)
        {
        }

        /// <summary>
        /// Call this to initialize a Behaviour with data supplied in a file.
        /// </summary>
        /// <param name="fileName">The file to load from.</param>
        public override void LoadContent(String fileName)
        {
            base.LoadContent(fileName);

            HealthStatBoostResearchDefinition def = GameObjectManager.pInstance.pContentManager.Load<HealthStatBoostResearchDefinition>(fileName);

            Health.SetMaxHealthMessage temp = new Health.SetMaxHealthMessage();
            temp.mMaxHealth_In = def.mAmount;

            mMessageOnComplete = temp;
        }
    }
}
