﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MBHEngine.GameObject;
using MBHEngine.Math;
using MBHEngine.Behaviour;
using ZombieTaxiContentDefs;

namespace ZombieTaxi.Behaviours
{
    class RandomEnemyGenerator : MBHEngine.Behaviour.Behaviour
    {
        /// <summary>
        /// Constructor which also handles the process of loading in the Behaviour
        /// Definition information.
        /// </summary>
        /// <param name="parentGOH">The game object that this behaviour is attached to.</param>
        /// <param name="fileName">The file defining this behaviour.</param>
        public RandomEnemyGenerator(GameObject parentGOH, String fileName)
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

            RandomEnemyGeneratorDefinition def = GameObjectManager.pInstance.pContentManager.Load<RandomEnemyGeneratorDefinition>(fileName);

            for (Int32 i = 0; i < def.mNumEnemies; i++)
            {
                Single rangeX = def.mConstraints.Right - def.mConstraints.Left;
                Single rangeY = def.mConstraints.Bottom - def.mConstraints.Top;

                GameObject enemy = new GameObject("GameObjects\\Characters\\Kamikaze\\Kamikaze");
                Behaviour t = new Kamikaze(enemy, null);
                enemy.AttachBehaviour(t);
                enemy.pOrientation.mPosition.X = (RandomManager.pInstance.RandomNumber() % (rangeX)) + def.mConstraints.Left;
                enemy.pOrientation.mPosition.Y = (RandomManager.pInstance.RandomNumber() % (rangeY)) + def.mConstraints.Top;
                GameObjectManager.pInstance.Add(enemy);
            }
        }
    }
}
