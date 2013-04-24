using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MBHEngine.GameObject;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MBHEngineContentDefs;

namespace MBHEngine.Behaviour
{
    public class HealNearby : MBHEngine.Behaviour.Behaviour
    {
        /// <summary>
        /// Used to get the list of all relavent nearby objects.
        /// </summary>
        private List<GameObject.GameObject> mObjectsInRange;

        /// <summary>
        /// Which types of objects does this guy heal?
        /// </summary>
        private List<GameObjectDefinition.Classifications> mAppliedTo;

        /// <summary>
        /// The distance a target must be from this object to get the healing effects.
        /// </summary>
        public Single mHealRange;

        /// <summary>
        /// Preallocated messages to avoid GC.
        /// </summary>
        private Health.IncrementHealthMessage mIncrementHealthMsg;

        /// <summary>
        /// Constructor which also handles the process of loading in the Behaviour
        /// Definition information.
        /// </summary>
        /// <param name="parentGOH">The game object that this behaviour is attached to.</param>
        /// <param name="fileName">The file defining this behaviour.</param>
        public HealNearby(GameObject.GameObject parentGOH, String fileName)
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

            HealNearbyDefinition def = GameObjectManager.pInstance.pContentManager.Load<HealNearbyDefinition>(fileName);

            mObjectsInRange = new List<GameObject.GameObject>(16);
            mAppliedTo = def.mAppliedTo;
            mHealRange = def.mHealRange;

            mIncrementHealthMsg = new Health.IncrementHealthMessage();
            mIncrementHealthMsg.mIncrementAmount_In = def.mHealRate;
        }

        /// <summary>
        /// Called once per frame by the game object.
        /// </summary>
        /// <param name="gameTime">The amount of time that has passed this frame.</param>
        public override void Update(GameTime gameTime)
        {
            mObjectsInRange.Clear();
            GameObjectManager.pInstance.GetGameObjectsInRange(mParentGOH.pPosition, mHealRange, ref mObjectsInRange, mAppliedTo);

            for (int i = 0; i < mObjectsInRange.Count; i++)
            {
                mObjectsInRange[i].OnMessage(mIncrementHealthMsg, mParentGOH);
            }
        }
    }
}
