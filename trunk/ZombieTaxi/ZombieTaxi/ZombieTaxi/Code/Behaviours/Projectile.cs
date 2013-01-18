using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MBHEngine.GameObject;
using MBHEngineContentDefs;
using ZombieTaxiContentDefs;
using MBHEngine.Behaviour;

namespace ZombieTaxi.Behaviours
{
    /// <summary>
    /// An object like a bullet which does damage when colliding with other objects.
    /// </summary>
    public class Projectile : MBHEngine.Behaviour.Behaviour
    {
        /// <summary>
        /// Preallocated list of GameObjects which will be used when detecting GameObjects near by.
        /// </summary>
        private List<GameObject> mObjectsInRange;

        /// <summary>
        /// Preallocated messages to avoid GC.
        /// </summary>
        private Health.OnApplyDamage mOnApplyDamageMsg;

        /// <summary>
        /// A list of the types of objects that this does damage to when exploding.
        /// </summary>
        private List<GameObjectDefinition.Classifications> mDamageAppliedTo;

        /// <summary>
        /// Constructor which also handles the process of loading in the Behaviour
        /// Definition information.
        /// </summary>
        /// <param name="parentGOH">The game object that this behaviour is attached to.</param>
        /// <param name="fileName">The file defining this behaviour.</param>
        public Projectile(GameObject parentGOH, String fileName)
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

            ProjectileDefinition def = GameObjectManager.pInstance.pContentManager.Load<ProjectileDefinition>(fileName);

            mDamageAppliedTo = new List<GameObjectDefinition.Classifications>();

            for (Int32 i = 0; i < def.mDamageAppliedTo.Count; i++)
            {
                mDamageAppliedTo.Add(def.mDamageAppliedTo[i]);
            }

            mObjectsInRange = new List<GameObject>(16);
            mOnApplyDamageMsg = new Health.OnApplyDamage(def.mDamageCaused);

        }

        /// <summary>
        /// Called once per frame by the game object.
        /// </summary>
        /// <param name="gameTime">The amount of time that has passed this frame.</param>
        public override void Update(GameTime gameTime)
        {
            // Find all the objects near by and apply some damage to them.
            mObjectsInRange.Clear();
            GameObjectManager.pInstance.GetGameObjectsInRange(mParentGOH, ref mObjectsInRange, mDamageAppliedTo);

            // Are we touching anything?
            if (mObjectsInRange.Count > 0)
            {
                for (Int32 i = 0; i < mObjectsInRange.Count; i++)
                {
                    mObjectsInRange[i].OnMessage(mOnApplyDamageMsg);
                }

                // After it hits something it should disappear.
                GameObjectManager.pInstance.Remove(mParentGOH);
            }
        }
    }
}
