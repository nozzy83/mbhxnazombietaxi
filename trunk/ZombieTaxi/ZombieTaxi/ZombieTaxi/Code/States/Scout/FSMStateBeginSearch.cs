using MBHEngine.StateMachine;
using MBHEngine.Behaviour;
using MBHEngine.World;
using MBHEngine.Input;
using ZombieTaxi.Behaviours;
using MBHEngine.GameObject;
using ZombieTaxi.StatBoost.Behaviours;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System;

namespace ZombieTaxi.States.Scout
{
    /// <summary>
    /// State where scout is asked to go out and find other Stranded.
    /// </summary>
    class FSMStateBeginSearch : FSMState
    {

        /// <summary>
        /// Preallocated messages to avoid triggering GC.
        /// </summary>
        private SpriteRender.GetAttachmentPointMessage mGetAttachmentPointMsg;
        private SpriteRender.SetActiveAnimationMessage mSetActiveAnimationMsg;
        private FiniteStateMachine.SetStateMessage mSetStateMsg;

        /// <summary>
        /// Constructor.
        /// </summary>
        public FSMStateBeginSearch()
        {
            mGetAttachmentPointMsg = new SpriteRender.GetAttachmentPointMessage();
            mSetActiveAnimationMsg = new SpriteRender.SetActiveAnimationMessage();
            mSetStateMsg = new FiniteStateMachine.SetStateMessage();
        }

        /// <summary>
        /// Called once when the state starts.
        /// </summary>
        public override void OnBegin()
        {
            // First do a little build up animation. When that finishes, we will find the
            // Stranded.
            mSetActiveAnimationMsg.mAnimationSetName_In = "RaiseArm";
            pParentGOH.OnMessage(mSetActiveAnimationMsg);
        }

        /// <summary>
        /// The main interface for communicating between behaviours.  Using polymorphism, we
        /// define a bunch of different messages deriving from BehaviourMessage.  Each behaviour
        /// can then check for particular upcasted messahe types, and either grab some data 
        /// from it (set message) or store some data in it (get message).
        /// </summary>
        /// <param name="msg">The message being communicated to the behaviour.</param>
        public override void OnMessage(ref BehaviourMessage msg)
        {
            if (msg is SpriteRender.OnAnimationCompleteMessage)
            {
                SpriteRender.OnAnimationCompleteMessage temp = (SpriteRender.OnAnimationCompleteMessage)msg;

                // Once the build up animation finishes, do the search for a target and move the Scout to
                // that position. Since the Scout appears on the minimap, he will show the player where to
                // go.
                if (temp.mAnimationSetName_In == "RaiseArm")
                {
                    // Spawn some smoke to be more ninja like.
                    GameObject go = GameObjectFactory.pInstance.GetTemplate("GameObjects\\Effects\\Dust\\Dust");

                    // Grab that attachment point and position the new object there.
                    mGetAttachmentPointMsg.mName_In = "Smoke";
                    pParentGOH.OnMessage(mGetAttachmentPointMsg);

                    // Put the smoke at the right position relative to the Scout.
                    go.pPosition = mGetAttachmentPointMsg.mPoisitionInWorld_Out;

                    // The Smoke gets pushed onto the GameObjectManager and will delete itself when
                    // it finishes the animation.
                    GameObjectManager.pInstance.Add(go);

                    // Find all the objects that are Allies.
                    /// <todo>
                    /// This needs to be refined further, as this could find Stranded that are already saved,
                    /// as well as other Scouts (or even the one doing the seach!).
                    /// </todo>
                    List<GameObject> objs = GameObjectManager.pInstance.GetGameObjectsOfClassification(MBHEngineContentDefs.GameObjectDefinition.Classifications.ALLY);

                    // There is a chance there are no Stranded left (well not really since it would find itself).
                    if (objs.Count > 0)
                    {
                        // Pick a random Stranded to "scout".
                        GameObject obj = objs[MBHEngine.Math.RandomManager.pInstance.RandomNumber() % objs.Count];

                        // Move the Scout to the position of the guy he found.
                        pParentGOH.pPosition = obj.pPosition;
                    }

                    // Wait for the Player to arrive. This is just to allow the player to make the connection between
                    // the marking on the minimap and the scout they sent out.
                    mSetStateMsg.mNextState_In = "WaitAtTarget";
                    pParentGOH.OnMessage(mSetStateMsg);
                }
            }
        }
    }
}
