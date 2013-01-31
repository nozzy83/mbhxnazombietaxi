using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MBHEngine.Behaviour;
using MBHEngine.GameObject;
using ZombieTaxi.Behaviours.HUD;
using ZombieTaxi.StatBoost.Behaviours;

namespace ZombieTaxi.Behaviours
{
    /// <summary>
    /// This class will be used to allow us to create client side Behaviours.
    /// </summary>
    public class ClientBehaviourCreator : BehaviourCreator
    {
        /// <summary>
        /// Helper function for creating behaviours based on strings of matching names.
        /// </summary>
        /// <param name="go">The game object that this behaviour is being attached to.</param>
        /// <param name="behaviourType">The name of the behaviour class we are creating.</param>
        /// <param name="fileName">The name of the file containing the behaviour definition.</param>
        /// <returns>The newly created behaviour.</returns>
        Behaviour BehaviourCreator.CreateBehaviourByName(GameObject go, String behaviourType, String fileName)
        {
            switch (behaviourType)
            {
                case "ZombieTaxi.Behaviours.TwinStick":
                    {
                        return new TwinStick(go, fileName);
                    }
                case "ZombieTaxi.Behaviours.Kamikaze":
                    {
                        return new Kamikaze(go, fileName);
                    }
                case "ZombieTaxi.Behaviours.Explosive":
                    {
                        return new Explosive(go, fileName);
                    }
                case "ZombieTaxi.Behaviours.Projectile":
                    {
                        return new Projectile(go, fileName);
                    }
                case "ZombieTaxi.Behaviours.RandomEnemyGenerator":
                    {
                        return new RandomEnemyGenerator(go, fileName);
                    }
                case "ZombieTaxi.Behaviours.Civilian":
                    {
                        return new Civilian(go, fileName);
                    }
                case "ZombieTaxi.Behaviours.FSMScout":
                    {
                        return new FSMScout(go, fileName);
                    }
                case "ZombieTaxi.Behaviours.DamageFlash":
                    {
                        return new DamageFlash(go, fileName);
                    }
                case "ZombieTaxi.Behaviours.ExtractionPoint":
                    {
                        return new ExtractionPoint(go, fileName);
                    }
                case "ZombieTaxi.Behaviours.HUD.PlayerHealthBar":
                    {
                        return new PlayerHealthBar(go, fileName);
                    }
                case "ZombieTaxi.Behaviours.HUD.PlayerScore":
                    {
                        return new PlayerScore(go, fileName);
                    }
                case "ZombieTaxi.Behaviours.HUD.PlayerInventory":
                    {
                        return new PlayerInventory(go, fileName);
                    }
                case "ZombieTaxi.Behaviours.HUD.MiniMap":
                    {
                        return new MiniMap(go, fileName);
                    }
                case "ZombieTaxi.Behaviours.DamageOnContact":
                    {
                        return new DamageOnContact(go, fileName);
                    }
                case "ZombieTaxi.Behaviours.Inventory":
                    {
                        return new Inventory(go, fileName);
                    }
                case "ZombieTaxi.Behaviours.Pickup":
                    {
                        return new Pickup(go, fileName);
                    }
                case "ZombieTaxi.Behaviours.ObjectPlacement":
                    {
                        return new ObjectPlacement(go, fileName);
                    }
                case "ZombieTaxi.Behaviours.TilePlacement":
                    {
                        return new TilePlacement(go, fileName);
                    }
                case "ZombieTaxi.Behaviours.MarkOnMiniMap":
                    {
                        return new MarkOnMiniMap(go, fileName);
                    }
                case "ZombieTaxi.Behaviours.StatBoost.HealthStatBoostResearch":
                    {
                        return new HealthStatBoostResearch(go, fileName);
                    }
                default:
                    {
                        return null;
                    }
            }
        }
    }
}
